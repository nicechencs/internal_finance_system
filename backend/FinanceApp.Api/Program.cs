using System.Data;
using System.Security.Claims;
using System.Threading.RateLimiting;
using FinanceApp.Api;
using FinanceApp.Api.Middleware;
using FinanceApp.Application.Common;
using FinanceApp.Application.Extensions;
using FinanceApp.Domain.Configuration;
using FinanceApp.Infrastructure.Data;
using FinanceApp.Infrastructure.Extensions;
using FinanceApp.Infrastructure.Services;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);

var logDir = Environment.GetEnvironmentVariable("LOG_DIR")
    ?? Path.Combine(builder.Environment.ContentRootPath, "..", "..", "logs");
Directory.CreateDirectory(logDir);

Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .Enrich.WithCorrelationId()
    .Filter.ByExcluding(ShouldSuppressExceptionLogEvent)
    .WriteTo.Console(outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {CorrelationId} {Message:lj}{NewLine}{Exception}")
    .WriteTo.File(
        Path.Combine(logDir, "app-.log"),
        outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {CorrelationId} [{SourceContext}] {Message:lj}{NewLine}{Exception}",
        rollingInterval: RollingInterval.Day,
        retainedFileCountLimit: 30,
        fileSizeLimitBytes: 50_000_000)
    .CreateLogger();

builder.Host.UseSerilog();

var authOptions = builder.Configuration.GetSection(AuthOptions.SectionName).Get<AuthOptions>() ?? new AuthOptions();
var cookieSecurePolicy = ResolveCookieSecurePolicy(authOptions.CookieSecurePolicy);

builder.Services.AddMemoryCache();

builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
        options.JsonSerializerOptions.DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull;
        options.JsonSerializerOptions.AllowTrailingCommas = true;
        options.JsonSerializerOptions.PropertyNameCaseInsensitive = true;
    });
builder.Services.AddHttpContextAccessor();

builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddApplicationServices();
// 反向代理转发头处理：生产链路为 Traefik → web(nginx) → api。
// nginx 使用 $proxy_add_x_forwarded_for 在链尾追加真实客户端 IP，
// 因此到达 api 的 X-Forwarded-For 形如 "<真实客户端IP>, <Traefik IP>"
//（web 与 api 位于同一 Docker bridge 私有网络内互联）。
//
// 安全要点：登录限流按 RemoteIpAddress 分区（见下方 auth-login 策略）。
// 旧实现 KnownNetworks/KnownProxies 全清空 + ForwardLimit=2，等于无条件信任任意来源的
// XFF——攻击者可预置伪造 XFF 条目，每换一个伪造 IP 就获得一个全新限流窗口，绕过登录限流。
//
// 修复方案：把可信代理配置为私有网段，并交由 KnownNetworks 终止解析。
// UseForwardedHeaders 从 XFF 最右向左逐跳回溯，遇到第一个「不在可信网段内」的地址即停止，
// 并将其作为 RemoteIpAddress。真实中间代理（nginx、Traefik）都在私有网段内会被逐一吃掉，
// 最终停在由 nginx 追加、来自公网的真实客户端 IP；攻击者伪造的条目位于更左侧，永远轮不到
// 被信任，因而无法借伪造条目刷出新的限流分区。
// ForwardLimit = null：不设跳数硬上限，改由 KnownNetworks 决定何时停止（越过可信网段即停）。
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    options.ForwardLimit = null;

    // 可信代理网段：支持通过配置节 ForwardedHeaders:KnownNetworks（CIDR 字符串数组）覆盖；
    // 未配置时默认信任 loopback + 常见私有网段（含 Docker 默认 bridge 所在的 172.16.0.0/12）。
    var configuredNetworks = builder.Configuration
        .GetSection("ForwardedHeaders:KnownNetworks")
        .Get<string[]>();
    var knownNetworks = configuredNetworks is { Length: > 0 }
        ? configuredNetworks
        : new[]
        {
            "127.0.0.0/8",   // IPv4 loopback
            "::1/128",       // IPv6 loopback
            "10.0.0.0/8",    // 私有网段 A
            "172.16.0.0/12", // 私有网段 B（Docker 默认 bridge）
            "192.168.0.0/16" // 私有网段 C
        };

    options.KnownNetworks.Clear();
    options.KnownProxies.Clear();
    foreach (var cidr in knownNetworks)
    {
        var parts = cidr.Split('/', 2);
        var prefix = System.Net.IPAddress.Parse(parts[0]);
        var prefixLength = parts.Length == 2
            ? int.Parse(parts[1])
            : (prefix.AddressFamily == System.Net.Sockets.AddressFamily.InterNetworkV6 ? 128 : 32);
        options.KnownNetworks.Add(new Microsoft.AspNetCore.HttpOverrides.IPNetwork(prefix, prefixLength));
    }
});

builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.Cookie.Name = authOptions.CookieName;
        options.Cookie.HttpOnly = true;
        options.Cookie.SameSite = SameSiteMode.Lax;
        options.Cookie.SecurePolicy = cookieSecurePolicy;
        options.ExpireTimeSpan = TimeSpan.FromHours(authOptions.CookieExpirationHours);
        options.SlidingExpiration = true;
        options.EventsType = typeof(CookieSessionValidationEvents);
    });

builder.Services.AddAuthorization();

builder.Services.AddCors(options =>
{
    options.AddPolicy("AppCors", policy =>
    {
        var configuredOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>();
        var origins = configuredOrigins?.Length > 0
            ? configuredOrigins
            : new[]
            {
                "http://localhost:3000",
                "http://127.0.0.1:3000",
                "http://localhost:5173",
                "http://127.0.0.1:5173",
                "http://localhost:4173",
                "http://127.0.0.1:4173",
                "http://localhost:8080",
                "http://127.0.0.1:8080"
            };

        policy.WithOrigins(origins)
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();
    });
});

builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.OnRejected = async (context, cancellationToken) =>
    {
        context.HttpContext.Response.ContentType = "application/json";
        await context.HttpContext.Response.WriteAsJsonAsync(
            ApiResponse<object>.ErrorResponse(429, "登录请求过于频繁，请稍后再试"),
            cancellationToken);
    };

    options.AddPolicy("auth-login", httpContext =>
    {
        var ipAddress = httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        var permitLimit = builder.Environment.IsEnvironment("Testing")
            ? int.MaxValue
            : authOptions.LoginRateLimitPermitLimit;
        var windowSeconds = builder.Environment.IsEnvironment("Testing")
            ? 1
            : authOptions.LoginRateLimitWindowSeconds;

        return RateLimitPartition.GetFixedWindowLimiter(
            ipAddress,
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = permitLimit,
                Window = TimeSpan.FromSeconds(windowSeconds),
                QueueLimit = 0,
                AutoReplenishment = true
            });
    });
});

builder.Services.AddHealthChecks()
    .AddNpgSql(
        builder.Configuration.GetConnectionString("DefaultConnection")!,
        name: "postgresql",
        tags: ["db", "ready"]);
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Finance API",
        Version = "v1"
    });

    c.CustomSchemaIds(type =>
    {
        var ns = type.Namespace ?? string.Empty;
        var nsParts = ns.Split('.');
        var prefix = nsParts.Length > 0 ? nsParts[^1] : string.Empty;
        var typeName = type.Name;

        if (type.IsGenericType)
        {
            typeName = type.Name.Split('`')[0];
            var genericArgs = type.GetGenericArguments();
            var argNames = genericArgs.Select(t =>
            {
                var argNs = t.Namespace?.Split('.') ?? Array.Empty<string>();
                var argPrefix = argNs.Length > 0 ? argNs[^1] : string.Empty;
                var argName = t.Name;
                if (t.IsGenericType)
                {
                    argName = t.Name.Split('`')[0];
                    var innerArgs = string.Join("And", t.GetGenericArguments().Select(it => it.Name));
                    argName = $"{argName}Of{innerArgs}";
                }

                return $"{argPrefix}{argName}";
            });

            return $"{prefix}{typeName}Of{string.Join("And", argNames)}";
        }

        return $"{prefix}{typeName}";
    });
});

var app = builder.Build();

await InitializeDatabaseAsync(app.Services);

if (args.Length > 0 && string.Equals(args[0], "auth-cli", StringComparison.OrdinalIgnoreCase))
{
    using var cliScope = app.Services.CreateScope();
    await AuthCliRunner.RunAsync(args.Skip(1).ToArray(), cliScope.ServiceProvider);
    return;
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Honor reverse proxy headers before redirect, auth, and request logging.
app.UseForwardedHeaders();
app.UseMiddleware<CorrelationIdMiddleware>();
app.UseSerilogRequestLogging(options =>
{
    options.MessageTemplate =
        "HTTP {RequestMethod} {RequestPath} 返回 {StatusCode}，耗时 {Elapsed:0.0000} ms (UserId={UserId}, Username={Username}, ClientIp={ClientIp})";

    options.EnrichDiagnosticContext = (diagnosticContext, httpContext) =>
    {
        diagnosticContext.Set("CorrelationId", httpContext.TraceIdentifier);
        diagnosticContext.Set("UserId", GetRequestUserId(httpContext));
        diagnosticContext.Set("Username", GetRequestUsername(httpContext));
        diagnosticContext.Set("ClientIp", httpContext.Connection.RemoteIpAddress?.ToString() ?? "未知");
    };

    options.GetLevel = (httpContext, elapsed, ex) =>
    {
        if (httpContext.Request.Method == "OPTIONS")
        {
            return Serilog.Events.LogEventLevel.Debug;
        }

        if (ex != null || httpContext.Response.StatusCode >= 500)
        {
            return Serilog.Events.LogEventLevel.Error;
        }

        if (elapsed > 3000)
        {
            return Serilog.Events.LogEventLevel.Warning;
        }

        return Serilog.Events.LogEventLevel.Information;
    };
});
app.UseMiddleware<PerformanceLoggingMiddleware>();
app.UseMiddleware<GlobalExceptionHandlerMiddleware>();

// HTTPS 终止由反向代理（Traefik）处理，内部容器间通信使用 HTTP
// 仅在显式设置 ENABLE_HTTPS_REDIRECT=true 时启用（用于非反向代理的直接暴露场景）
if (string.Equals(Environment.GetEnvironmentVariable("ENABLE_HTTPS_REDIRECT"), "true", StringComparison.OrdinalIgnoreCase))
{
    app.UseHttpsRedirection();
}

app.UseCors("AppCors");
app.UseRateLimiter();
app.UseMiddleware<SecurityHeadersMiddleware>();
app.UseAuthentication();
app.UseAuthorization();

app.MapHealthChecks("/health", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
{
    ResponseWriter = async (context, report) =>
    {
        context.Response.ContentType = "application/json";
        var result = System.Text.Json.JsonSerializer.Serialize(new
        {
            status = report.Status.ToString(),
            checks = report.Entries.Select(e => new
            {
                name = e.Key,
                status = e.Value.Status.ToString(),
                duration = e.Value.Duration.TotalMilliseconds + "ms"
            })
        });
        await context.Response.WriteAsync(result);
    }
});
app.MapControllers();

await app.RunAsync();

static async Task InitializeDatabaseAsync(IServiceProvider services)
{
    using var scope = services.CreateScope();
    var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("DbInitializer");

    try
    {
        if (!dbContext.Database.IsRelational())
        {
            logger.LogInformation("Detected a non-relational test database. Skipping migrations.");
            await dbContext.Database.EnsureCreatedAsync();
        }
        else
        {
            logger.LogInformation("开始检查数据库架构状态");
            var availableMigrations = dbContext.Database.GetMigrations().ToArray();
            var hasTables = await DatabaseHasAnyTablesAsync(dbContext);

            if (availableMigrations.Length == 0)
            {
                if (!hasTables)
                {
                    logger.LogInformation("未找到迁移文件且数据库为空，按当前模型创建数据库结构");
                    await dbContext.Database.EnsureCreatedAsync();
                }
                else
                {
                    logger.LogInformation("未找到迁移文件，开始应用旧版数据库兼容升级");
                    await LegacySchemaUpgrader.ApplyAsync(dbContext, logger);
                }
            }
            else if (!hasTables)
            {
                logger.LogInformation("数据库为空，开始执行 {Count} 个迁移", availableMigrations.Length);
                await dbContext.Database.MigrateAsync();
            }
            else
            {
                var hasMigrationHistory = await LegacySchemaUpgrader.MigrationHistoryTableExistsAsync(dbContext);
                var appliedMigrations = hasMigrationHistory
                    ? (await dbContext.Database.GetAppliedMigrationsAsync()).ToArray()
                    : Array.Empty<string>();

                if (appliedMigrations.Length == 0)
                {
                    logger.LogWarning(
                        "检测到已有数据库但缺少 EF 迁移历史，先执行旧版兼容升级并登记基线迁移 {MigrationId}",
                        availableMigrations[0]);

                    await LegacySchemaUpgrader.ApplyAsync(dbContext, logger);
                    await LegacySchemaUpgrader.BaselineInitialMigrationAsync(dbContext, logger, availableMigrations[0]);
                }

                var pendingMigrations = (await dbContext.Database.GetPendingMigrationsAsync()).ToArray();
                if (pendingMigrations.Length > 0)
                {
                    logger.LogInformation("执行数据库迁移: {Count} 个待执行迁移", pendingMigrations.Length);
                    await dbContext.Database.MigrateAsync();
                }
                else
                {
                    logger.LogInformation("数据库架构已是最新状态");
                }
            }
        }
    }
    catch (Exception ex)
    {
        logger.LogCritical(ex, "数据库初始化失败，应用无法启动");
        throw;
    }

    await DbInitializer.SeedAsync(dbContext, logger, scope.ServiceProvider);
    logger.LogInformation("数据库初始化完成");
}

static async Task<bool> DatabaseHasAnyTablesAsync(AppDbContext dbContext, CancellationToken cancellationToken = default)
{
    var connection = dbContext.Database.GetDbConnection();
    var shouldClose = connection.State != ConnectionState.Open;

    if (shouldClose)
    {
        await connection.OpenAsync(cancellationToken);
    }

    try
    {
        await using var command = connection.CreateCommand();
        command.CommandText = dbContext.Database.ProviderName?.Contains("Sqlite", StringComparison.OrdinalIgnoreCase) == true
            ? "SELECT EXISTS(SELECT 1 FROM sqlite_master WHERE type = 'table' AND name NOT LIKE 'sqlite_%')"
            : """
              SELECT EXISTS (
                  SELECT 1
                  FROM information_schema.tables
                  WHERE table_schema = 'public')
              """;

        var result = await command.ExecuteScalarAsync(cancellationToken);
        return result switch
        {
            bool boolValue => boolValue,
            long longValue => longValue != 0,
            int intValue => intValue != 0,
            string stringValue when bool.TryParse(stringValue, out var parsedBool) => parsedBool,
            string stringValue when long.TryParse(stringValue, out var parsedLong) => parsedLong != 0,
            _ => false
        };
    }
    finally
    {
        if (shouldClose)
        {
            await connection.CloseAsync();
        }
    }
}

static bool ShouldSuppressExceptionLogEvent(Serilog.Events.LogEvent logEvent)
{
    if (logEvent.Exception == null || logEvent.Level < Serilog.Events.LogEventLevel.Error)
    {
        return false;
    }

    var sourceContext = GetSourceContext(logEvent);
    if (string.IsNullOrWhiteSpace(sourceContext))
    {
        return false;
    }

    if (sourceContext.StartsWith("FinanceApp.Api.Controllers", StringComparison.Ordinal))
    {
        return true;
    }

    return IsExpectedAppException(logEvent.Exception)
        && sourceContext.StartsWith("FinanceApp.Application.Services", StringComparison.Ordinal);
}

static CookieSecurePolicy ResolveCookieSecurePolicy(string? configuredPolicy)
{
    return Enum.TryParse<CookieSecurePolicy>(configuredPolicy, ignoreCase: true, out var parsedPolicy)
        ? parsedPolicy
        : CookieSecurePolicy.SameAsRequest;
}

static string? GetSourceContext(Serilog.Events.LogEvent logEvent)
{
    if (!logEvent.Properties.TryGetValue("SourceContext", out var propertyValue))
    {
        return null;
    }

    return propertyValue is Serilog.Events.ScalarValue scalarValue
        ? scalarValue.Value as string
        : propertyValue.ToString().Trim('"');
}

static bool IsExpectedAppException(Exception exception)
{
    return exception is UnauthorizedAccessException
        or ForbiddenException
        or NotFoundException
        or ValidationException
        or BusinessException;
}

static string GetRequestUserId(HttpContext context)
{
    return context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "匿名";
}

static string GetRequestUsername(HttpContext context)
{
    return context.User.Identity?.Name ?? "匿名";
}

public partial class Program { }
