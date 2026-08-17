# 财务管理系统 - 深度开发指导文件

> 本文档是面向 AI 辅助开发的完整开发指南，整合了所有设计文档、提示词模板和业务规则。
> 开发任何功能前，请将本文档作为核心上下文提供给 AI。

**文档版本**: v1.0
**最后更新**: 2026-03-13
**适用阶段**: 从项目初始化到生产部署的全流程

---

## 目录

1. [项目全景](#1-项目全景)
2. [开发环境搭建](#2-开发环境搭建)
3. [第一阶段：基础架构与核心模块](#3-第一阶段基础架构与核心模块)
4. [第二阶段：业务数据与导入](#4-第二阶段业务数据与导入)
5. [第三阶段：应收应付与分摊](#5-第三阶段应收应付与分摊)
6. [第四阶段：报表与系统管理](#6-第四阶段报表与系统管理)
7. [编码规范与约定](#7-编码规范与约定)
8. [关键业务逻辑实现指南](#8-关键业务逻辑实现指南)
9. [数据库操作指南](#9-数据库操作指南)
10. [前端开发指南](#10-前端开发指南)
11. [测试策略](#11-测试策略)
12. [部署指南](#12-部署指南)
13. [常见问题与陷阱](#13-常见问题与陷阱)

---

## 1. 项目全景

### 1.1 系统定位

这是一套**财务管理系统**，核心用途是让公司负责人掌握真实经营状况。

**关键区分**：
- ❌ 不是外账/税务系统，不需要遵循会计准则
- ❌ 不是 ERP，不涉及进销存
- ✅ 是管理者视角的经营分析工具，重点在"看清楚钱的流向"

### 1.2 核心价值

1. **数据采集自动化**：从 Excel 导入银行流水（仅支持 .xlsx 格式），自动生成交易记录
2. **智能分类**：规则引擎自动匹配交易分类、项目、客户等维度
3. **项目维度分析**：每个项目的收入、成本、利润一目了然
4. **费用分摊**：支持一笔支出按比例分配到多个项目或人员
5. **应收应付跟踪**：实时掌握资金往来，逾期自动提醒
6. **多维度报表**：月度利润、现金流、项目利润、人员成本、供应商支出等

### 1.3 技术栈（已确定，不可更改）

| 层级 | 技术 | 版本 | 说明 |
|------|------|------|------|
| 后端框架 | .NET | 8.0 | LTS 版本，LINQ 适合财务聚合查询 |
| Web 框架 | ASP.NET Core Web API | 8.0 | RESTful API |
| ORM | EF Core + Npgsql | 8.0 | Code First，PostgreSQL 驱动 |
| 前端框架 | Vue | 3.x | Composition API + script setup |
| 构建工具 | Vite | 5.x | 快速开发服务器 |
| UI 组件库 | Element Plus | 2.x | 表格、表单组件丰富 |
| 状态管理 | Pinia | 2.x | Vue 3 官方推荐 |
| 图表库 | ECharts | 5.x | 报表可视化 |
| 数据库 | PostgreSQL | 14+ | JSONB 支持审计日志 |
| 反向代理 | Nginx | 1.24+ | 静态资源托管 + API 代理 |
| 容器化 | Docker + Docker Compose | 24+ | 服务编排 |
| 认证 | JWT Bearer Token | - | 24 小时过期 |

### 1.4 后端分层架构

```
FinanceApp/
├── FinanceApp.Api/              # Web API 层
│   ├── Controllers/                  # 控制器（路由、参数绑定）
│   ├── Filters/                      # 过滤器（验证）
│   ├── Middleware/                   # 中间件（异常、日志、审计）
│   ├── Extensions/                   # DI 注册扩展
│   └── Program.cs                    # 启动配置
├── FinanceApp.Application/      # 应用服务层
│   ├── DTOs/                         # 数据传输对象
│   ├── Services/                     # 业务逻辑实现
│   ├── Interfaces/                   # 服务接口
│   ├── Mappings/                     # AutoMapper 配置
│   └── Validators/                   # FluentValidation 校验器
├── FinanceApp.Domain/           # 领域层
│   ├── Entities/                     # 实体定义
│   ├── Enums/                        # 枚举定义
│   └── Interfaces/                   # 仓储接口
└── FinanceApp.Infrastructure/   # 基础设施层
    ├── Data/                         # DbContext、拦截器
    ├── Repositories/                 # 仓储实现
    ├── Configurations/               # EF Core 实体配置
    └── Migrations/                   # 数据库迁移
```

### 1.5 前端项目结构

```
finance-web/
├── src/
│   ├── api/                          # API 请求封装（按模块拆分）
│   ├── assets/                       # 静态资源
│   ├── components/                   # 公共组件
│   ├── composables/                  # 组合式函数（useXxx）
│   ├── layouts/                      # 布局组件
│   ├── router/                       # 路由配置 + 守卫
│   ├── stores/                       # Pinia 状态管理
│   ├── types/                        # TypeScript 类型定义
│   ├── utils/                        # 工具函数
│   ├── views/                        # 页面组件（按模块分目录）
│   ├── App.vue                       # 根组件
│   └── main.ts                       # 入口文件
├── index.html
├── vite.config.ts
├── tsconfig.json
└── package.json
```

### 1.6 数据库表清单（18 张表 + 3 视图）

**核心业务表**：
- `users` - 用户
- `accounts` - 资金账户
- `categories` - 收支分类（树形）
- `customers` - 客户
- `suppliers` - 供应商
- `persons` - 人员（员工、合伙人、股东、外包）
- `projects` - 项目

**交易相关表**：
- `import_batches` - 导入批次
- `bank_transactions` - 银行流水
- `transactions` - 交易记录（核心表）
- `transaction_allocations` - 费用分摊

**应收应付表**：
- `receivables` - 应收账款
- `receivable_details` - 应收明细
- `payables` - 应付账款
- `payable_details` - 应付明细

**系统管理表**：
- `classification_rules` - 分类规则
- `audit_logs` - 审计日志
- `system_configs` - 系统配置

**视图**：
- `v_project_profit` - 项目利润视图
- `v_account_balance` - 账户余额视图
- `v_table_statistics` - 表统计视图

### 1.7 开发路线图（4 周）

| 阶段 | 周期 | 模块 | 预估工作量 |
|------|------|------|------------|
| 第一阶段 | 第 1 周 | 项目骨架 + 认证 + 账户 + 分类 + 交易（基础 CRUD） | 40 小时 |
| 第二阶段 | 第 2 周 | 客户/供应商/人员 + 项目 + Excel 导入 + 规则引擎 | 40 小时 |
| 第三阶段 | 第 3 周 | 应收应付 + 费用分摊 + 批量操作 | 40 小时 |
| 第四阶段 | 第 4 周 | 报表系统 + 仪表盘 + 审计日志 + 系统配置 + 部署 | 40 小时 |

**预估规模**：
- 后端代码：约 15,000 行
- 前端代码：约 12,000 行
- 数据库表：18 张 + 3 视图
- API 接口：约 80 个

---

## 2. 开发环境搭建

### 2.1 必需软件清单

| 软件 | 版本要求 | 用途 | 下载地址 |
|------|----------|------|----------|
| .NET SDK | 8.0+ | 后端开发 | https://dotnet.microsoft.com/download |
| Node.js | 20.x LTS | 前端开发 | https://nodejs.org/ |
| PostgreSQL | 14+ | 数据库 | https://www.postgresql.org/download/ |
| Docker Desktop | 24+ | 容器化部署 | https://www.docker.com/products/docker-desktop |
| Git | 2.40+ | 版本控制 | https://git-scm.com/ |
| VS Code | 最新版 | 代码编辑器 | https://code.visualstudio.com/ |

**推荐 VS Code 扩展**：
- C# Dev Kit（后端开发）
- Vue - Official（前端开发）
- PostgreSQL（数据库管理）
- Docker（容器管理）
- REST Client（API 测试）

### 2.2 后端环境搭建

#### 2.2.1 验证 .NET 安装

```bash
dotnet --version
# 应输出 8.0.x
```

#### 2.2.2 创建解决方案结构

```bash
# 创建解决方案目录
mkdir FinanceApp
cd FinanceApp

# 创建解决方案文件
dotnet new sln -n FinanceApp

# 创建各层项目
dotnet new webapi -n FinanceApp.Api
dotnet new classlib -n FinanceApp.Application
dotnet new classlib -n FinanceApp.Domain
dotnet new classlib -n FinanceApp.Infrastructure

# 添加项目到解决方案
dotnet sln add FinanceApp.Api/FinanceApp.Api.csproj
dotnet sln add FinanceApp.Application/FinanceApp.Application.csproj
dotnet sln add FinanceApp.Domain/FinanceApp.Domain.csproj
dotnet sln add FinanceApp.Infrastructure/FinanceApp.Infrastructure.csproj

# 添加项目引用
cd FinanceApp.Api
dotnet add reference ../FinanceApp.Application/FinanceApp.Application.csproj
cd ../FinanceApp.Application
dotnet add reference ../FinanceApp.Domain/FinanceApp.Domain.csproj
dotnet add reference ../FinanceApp.Infrastructure/FinanceApp.Infrastructure.csproj
cd ../FinanceApp.Infrastructure
dotnet add reference ../FinanceApp.Domain/FinanceApp.Domain.csproj
cd ..
```

#### 2.2.3 安装 NuGet 包

**FinanceApp.Api**：
```bash
cd FinanceApp.Api
dotnet add package Microsoft.AspNetCore.Authentication.JwtBearer
dotnet add package Swashbuckle.AspNetCore
dotnet add package Serilog.AspNetCore
dotnet add package Serilog.Sinks.File
dotnet add package Serilog.Sinks.Console
```

**FinanceApp.Application**：
```bash
cd ../FinanceApp.Application
dotnet add package AutoMapper.Extensions.Microsoft.DependencyInjection
dotnet add package FluentValidation.AspNetCore
dotnet add package EPPlus
```

**FinanceApp.Infrastructure**：
```bash
cd ../FinanceApp.Infrastructure
dotnet add package Npgsql.EntityFrameworkCore.PostgreSQL
dotnet add package Microsoft.EntityFrameworkCore.Design
dotnet add package BCrypt.Net-Next
```

### 2.3 前端环境搭建

#### 2.3.1 验证 Node.js 安装

```bash
node --version
# 应输出 v20.x.x

npm --version
# 应输出 10.x.x
```

#### 2.3.2 创建 Vue 3 项目

```bash
# 使用 Vite 创建项目
npm create vite@latest finance-web -- --template vue-ts
cd finance-web

# 安装依赖
npm install

# 安装项目依赖
npm install vue-router@4 pinia axios element-plus
npm install echarts @vueuse/core
npm install -D @types/node
```

#### 2.3.3 配置 Vite

创建 `vite.config.ts`：
```typescript
import { defineConfig } from 'vite'
import vue from '@vitejs/plugin-vue'
import { resolve } from 'path'

export default defineConfig({
  plugins: [vue()],
  resolve: {
    alias: {
      '@': resolve(__dirname, 'src')
    }
  },
  server: {
    port: 3000,
    proxy: {
      '/api': {
        target: 'http://localhost:5000',
        changeOrigin: true
      }
    }
  }
})
```

### 2.4 数据库环境搭建

#### 2.4.1 安装 PostgreSQL

**macOS**：
```bash
brew install postgresql@14
brew services start postgresql@14
```

**Windows**：
下载安装包：https://www.postgresql.org/download/windows/

**Linux (Ubuntu)**：
```bash
sudo apt update
sudo apt install postgresql-14
sudo systemctl start postgresql
```

#### 2.4.2 创建数据库

```bash
# 连接到 PostgreSQL
psql -U postgres

# 创建数据库和用户
CREATE DATABASE finance;
CREATE USER finance_user WITH PASSWORD 'your_secure_password';
GRANT ALL PRIVILEGES ON DATABASE finance TO finance_user;

# 退出
\q
```

#### 2.4.3 执行 DDL 脚本

```bash
# 执行数据库初始化脚本
psql -U finance_user -d finance -f docs/02_Database/01_database_schema.sql
```

### 2.5 配置文件设置

#### 2.5.1 后端 appsettings.json

创建 `FinanceApp.Api/appsettings.Development.json`：
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Database=finance;Username=finance_user;Password=your_secure_password"
  },
  "Jwt": {
    "Secret": "your-32-character-or-longer-secret-key-here",
    "Issuer": "FinanceApp",
    "Audience": "FinanceAppClient",
    "ExpiresInHours": 24
  },
  "Serilog": {
    "MinimumLevel": {
      "Default": "Information",
      "Override": {
        "Microsoft.AspNetCore": "Warning",
        "Microsoft.EntityFrameworkCore": "Warning"
      }
    },
    "WriteTo": [
      {
        "Name": "Console"
      },
      {
        "Name": "File",
        "Args": {
          "path": "logs/app-.log",
          "rollingInterval": "Day",
          "retainedFileCountLimit": 30,
          "outputTemplate": "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj}{NewLine}{Exception}"
        }
      }
    ]
  }
}
```

#### 2.5.2 前端环境变量

创建 `finance-web/.env.development`：
```env
VITE_API_BASE_URL=/api/v1
VITE_APP_TITLE=财务管理系统
```

创建 `finance-web/.env.production`：
```env
VITE_API_BASE_URL=/api/v1
VITE_APP_TITLE=财务管理系统
```

### 2.6 验证环境

#### 2.6.1 验证后端

```bash
cd FinanceApp.Api
dotnet run
# 应启动成功，访问 https://localhost:5001/swagger
```

#### 2.6.2 验证前端

```bash
cd finance-web
npm run dev
# 应启动成功，访问 http://localhost:3000
```

#### 2.6.3 验证数据库连接

```bash
# 测试数据库连接
psql -U finance_user -d finance -c "SELECT version();"
```

---

## 3. 第一阶段：基础架构与核心模块

**目标**：搭建项目骨架，实现认证、账户、分类、交易的基础 CRUD 功能。

**周期**：第 1 周（40 小时）

**交付物**：
- ✅ 后端项目骨架（4 层架构）
- ✅ 前端项目骨架（路由、状态管理、API 封装）
- ✅ 认证模块（登录、JWT）
- ✅ 账户管理模块（CRUD + 余额汇总）
- ✅ 分类管理模块（树形结构）
- ✅ 交易管理模块（基础 CRUD，不含分摊）

### 3.1 后端项目骨架搭建

#### 3.1.1 创建 BaseEntity

**文件**：`FinanceApp.Domain/Entities/BaseEntity.cs`

```csharp
namespace FinanceApp.Domain.Entities;

public abstract class BaseEntity
{
    public long Id { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public bool IsDeleted { get; set; } = false;
    public DateTime? DeletedAt { get; set; }
}
```

#### 3.1.2 创建统一响应格式

**文件**：`FinanceApp.Application/DTOs/Common/ApiResponse.cs`

```csharp
namespace FinanceApp.Application.DTOs.Common;

public class ApiResponse<T>
{
    public int Code { get; set; }
    public string Message { get; set; } = string.Empty;
    public T? Data { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    public static ApiResponse<T> Success(T data, string message = "操作成功")
    {
        return new ApiResponse<T>
        {
            Code = 200,
            Message = message,
            Data = data
        };
    }

    public static ApiResponse<T> Error(string message, int code = 500)
    {
        return new ApiResponse<T>
        {
            Code = code,
            Message = message
        };
    }
}
```

#### 3.1.3 创建分页基类

**文件**：`FinanceApp.Application/DTOs/Common/PageRequest.cs`

```csharp
namespace FinanceApp.Application.DTOs.Common;

public class PageRequest
{
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
    public string? SortBy { get; set; }
    public string? SortOrder { get; set; } = "asc";
}
```

**文件**：`FinanceApp.Application/DTOs/Common/PageResponse.cs`

```csharp
namespace FinanceApp.Application.DTOs.Common;

public class PageResponse<T>
{
    public List<T> Items { get; set; } = new();
    public int Total { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalPages => (int)Math.Ceiling((double)Total / PageSize);
}
```

#### 3.1.4 创建全局异常处理中间件

**文件**：`FinanceApp.Api/Middleware/ExceptionMiddleware.cs`

```csharp
using FinanceApp.Application.DTOs.Common;
using System.Net;
using System.Text.Json;

namespace FinanceApp.Api.Middleware;

public class ExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionMiddleware> _logger;

    public ExceptionMiddleware(RequestDelegate next, ILogger<ExceptionMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled exception occurred");
            await HandleExceptionAsync(context, ex);
        }
    }

    private static Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        context.Response.ContentType = "application/json";
        context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;

        var response = ApiResponse<object>.Error(
            exception.Message,
            context.Response.StatusCode
        );

        var options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        return context.Response.WriteAsync(JsonSerializer.Serialize(response, options));
    }
}
```

#### 3.1.5 创建泛型仓储

**文件**：`FinanceApp.Domain/Interfaces/IRepository.cs`

```csharp
using FinanceApp.Domain.Entities;
using System.Linq.Expressions;

namespace FinanceApp.Domain.Interfaces;

public interface IRepository<T> where T : BaseEntity
{
    Task<T?> GetByIdAsync(long id);
    Task<IEnumerable<T>> GetAllAsync();
    Task<IEnumerable<T>> FindAsync(Expression<Func<T, bool>> predicate);
    Task<T> AddAsync(T entity);
    Task UpdateAsync(T entity);
    Task DeleteAsync(long id);
    Task<int> CountAsync(Expression<Func<T, bool>>? predicate = null);
}
```

**文件**：`FinanceApp.Infrastructure/Repositories/Repository.cs`

```csharp
using FinanceApp.Domain.Entities;
using FinanceApp.Domain.Interfaces;
using FinanceApp.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace FinanceApp.Infrastructure.Repositories;

public class Repository<T> : IRepository<T> where T : BaseEntity
{
    protected readonly AppDbContext _context;
    protected readonly DbSet<T> _dbSet;

    public Repository(AppDbContext context)
    {
        _context = context;
        _dbSet = context.Set<T>();
    }

    public async Task<T?> GetByIdAsync(long id)
    {
        return await _dbSet.FindAsync(id);
    }

    public async Task<IEnumerable<T>> GetAllAsync()
    {
        return await _dbSet.ToListAsync();
    }

    public async Task<IEnumerable<T>> FindAsync(Expression<Func<T, bool>> predicate)
    {
        return await _dbSet.Where(predicate).ToListAsync();
    }

    public async Task<T> AddAsync(T entity)
    {
        await _dbSet.AddAsync(entity);
        await _context.SaveChangesAsync();
        return entity;
    }

    public async Task UpdateAsync(T entity)
    {
        entity.UpdatedAt = DateTime.UtcNow;
        _dbSet.Update(entity);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteAsync(long id)
    {
        var entity = await GetByIdAsync(id);
        if (entity != null)
        {
            entity.IsDeleted = true;
            entity.DeletedAt = DateTime.UtcNow;
            await UpdateAsync(entity);
        }
    }

    public async Task<int> CountAsync(Expression<Func<T, bool>>? predicate = null)
    {
        return predicate == null
            ? await _dbSet.CountAsync()
            : await _dbSet.CountAsync(predicate);
    }
}
```

#### 3.1.6 创建 DbContext

**文件**：`FinanceApp.Infrastructure/Data/AppDbContext.cs`

```csharp
using FinanceApp.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace FinanceApp.Infrastructure.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    // DbSets will be added here as we create entities
    // public DbSet<User> Users { get; set; }
    // public DbSet<Account> Accounts { get; set; }
    // ...

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // 全局查询过滤器：自动过滤软删除的记录
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            if (typeof(BaseEntity).IsAssignableFrom(entityType.ClrType))
            {
                var parameter = Expression.Parameter(entityType.ClrType, "e");
                var body = Expression.Equal(
                    Expression.Property(parameter, nameof(BaseEntity.IsDeleted)),
                    Expression.Constant(false)
                );
                modelBuilder.Entity(entityType.ClrType).HasQueryFilter(Expression.Lambda(body, parameter));
            }
        }

        // 应用所有配置
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
    }
}
```

#### 3.1.7 配置 Program.cs

**文件**：`FinanceApp.Api/Program.cs`

```csharp
using FinanceApp.Api.Middleware;
using FinanceApp.Infrastructure.Data;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Serilog;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// 配置 Serilog
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .CreateLogger();

builder.Host.UseSerilog();

// 添加服务
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

// 配置 Swagger
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "Finance API", Version = "v1" });
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });
    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

// 配置数据库
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// 配置 JWT 认证
var jwtSettings = builder.Configuration.GetSection("Jwt");
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtSettings["Issuer"],
            ValidAudience = jwtSettings["Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(jwtSettings["Secret"]!))
        };
    });

// 配置 CORS
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.WithOrigins("http://localhost:3000")
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

// 注册 AutoMapper
builder.Services.AddAutoMapper(AppDomain.CurrentDomain.GetAssemblies());

// 注册仓储和服务（将在后续添加）
// builder.Services.AddScoped(typeof(IRepository<>), typeof(Repository<>));

var app = builder.Build();

// 配置 HTTP 请求管道
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseMiddleware<ExceptionMiddleware>();

app.UseSerilogRequestLogging();

app.UseHttpsRedirection();

app.UseCors();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
```

### 3.2 前端项目骨架搭建

#### 3.2.1 配置 TypeScript

**文件**：`finance-web/tsconfig.json`

```json
{
  "compilerOptions": {
    "target": "ES2020",
    "useDefineForClassFields": true,
    "module": "ESNext",
    "lib": ["ES2020", "DOM", "DOM.Iterable"],
    "skipLibCheck": true,
    "moduleResolution": "bundler",
    "allowImportingTsExtensions": true,
    "resolveJsonModule": true,
    "isolatedModules": true,
    "noEmit": true,
    "jsx": "preserve",
    "strict": true,
    "noUnusedLocals": true,
    "noUnusedParameters": true,
    "noFallthroughCasesInSwitch": true,
    "baseUrl": ".",
    "paths": {
      "@/*": ["src/*"]
    }
  },
  "include": ["src/**/*.ts", "src/**/*.d.ts", "src/**/*.tsx", "src/**/*.vue"],
  "references": [{ "path": "./tsconfig.node.json" }]
}
```

#### 3.2.2 创建 API 类型定义

**文件**：`finance-web/src/types/api.ts`

```typescript
export interface ApiResponse<T = any> {
  code: number
  message: string
  data: T
  timestamp: string
}

export interface PageRequest {
  page: number
  pageSize: number
  sortBy?: string
  sortOrder?: 'asc' | 'desc'
}

export interface PageResponse<T> {
  items: T[]
  total: number
  page: number
  pageSize: number
  totalPages: number
}
```

#### 3.2.3 封装 Axios

**文件**：`finance-web/src/utils/request.ts`

```typescript
import axios, { AxiosError, AxiosInstance, AxiosRequestConfig, AxiosResponse } from 'axios'
import { ElMessage } from 'element-plus'
import { useUserStore } from '@/stores/user'
import router from '@/router'
import type { ApiResponse } from '@/types/api'

const instance: AxiosInstance = axios.create({
  baseURL: import.meta.env.VITE_API_BASE_URL || '/api/v1',
  timeout: 30000,
  headers: {
    'Content-Type': 'application/json'
  }
})

// 请求拦截器
instance.interceptors.request.use(
  (config) => {
    const userStore = useUserStore()
    if (userStore.token) {
      config.headers.Authorization = `Bearer ${userStore.token}`
    }
    return config
  },
  (error: AxiosError) => {
    return Promise.reject(error)
  }
)

// 响应拦截器
instance.interceptors.response.use(
  (response: AxiosResponse<ApiResponse>) => {
    const { code, message, data } = response.data

    if (code === 200) {
      return data
    } else {
      ElMessage.error(message || '请求失败')
      return Promise.reject(new Error(message || '请求失败'))
    }
  },
  (error: AxiosError<ApiResponse>) => {
    if (error.response) {
      const { status, data } = error.response

      switch (status) {
        case 401:
          ElMessage.error('未授权，请重新登录')
          useUserStore().logout()
          router.push('/login')
          break
        case 403:
          ElMessage.error('拒绝访问')
          break
        case 404:
          ElMessage.error('请求的资源不存在')
          break
        case 500:
          ElMessage.error(data?.message || '服务器错误')
          break
        default:
          ElMessage.error(data?.message || '请求失败')
      }
    } else if (error.request) {
      ElMessage.error('网络错误，请检查网络连接')
    } else {
      ElMessage.error('请求配置错误')
    }

    return Promise.reject(error)
  }
)

export default instance
```

#### 3.2.4 创建用户状态管理

**文件**：`finance-web/src/stores/user.ts`

```typescript
import { defineStore } from 'pinia'
import { ref } from 'vue'
import router from '@/router'

export interface UserInfo {
  id: number
  username: string
  role: string
}

export const useUserStore = defineStore('user', () => {
  const token = ref<string>(localStorage.getItem('token') || '')
  const userInfo = ref<UserInfo | null>(null)

  const setToken = (newToken: string) => {
    token.value = newToken
    localStorage.setItem('token', newToken)
  }

  const setUserInfo = (info: UserInfo) => {
    userInfo.value = info
  }

  const logout = () => {
    token.value = ''
    userInfo.value = null
    localStorage.removeItem('token')
    router.push('/login')
  }

  return {
    token,
    userInfo,
    setToken,
    setUserInfo,
    logout
  }
})
```

#### 3.2.5 配置路由

**文件**：`finance-web/src/router/index.ts`

```typescript
import { createRouter, createWebHistory, RouteRecordRaw } from 'vue-router'
import { useUserStore } from '@/stores/user'

const routes: RouteRecordRaw[] = [
  {
    path: '/login',
    name: 'Login',
    component: () => import('@/views/login/LoginView.vue'),
    meta: { requiresAuth: false }
  },
  {
    path: '/',
    component: () => import('@/layouts/MainLayout.vue'),
    meta: { requiresAuth: true },
    children: [
      {
        path: '',
        name: 'Dashboard',
        component: () => import('@/views/dashboard/DashboardView.vue'),
        meta: { title: '仪表盘' }
      },
      {
        path: 'accounts',
        name: 'Accounts',
        component: () => import('@/views/account/AccountList.vue'),
        meta: { title: '账户管理' }
      },
      {
        path: 'categories',
        name: 'Categories',
        component: () => import('@/views/category/CategoryList.vue'),
        meta: { title: '分类管理' }
      },
      {
        path: 'transactions',
        name: 'Transactions',
        component: () => import('@/views/transaction/TransactionList.vue'),
        meta: { title: '交易管理' }
      }
      // 更多路由将在后续添加
    ]
  }
]

const router = createRouter({
  history: createWebHistory(),
  routes
})

// 路由守卫
router.beforeEach((to, from, next) => {
  const userStore = useUserStore()
  const requiresAuth = to.matched.some(record => record.meta.requiresAuth !== false)

  if (requiresAuth && !userStore.token) {
    next('/login')
  } else if (to.path === '/login' && userStore.token) {
    next('/')
  } else {
    next()
  }
})

export default router
```

---

## 7. 编码规范与约定

### 7.1 后端编码规范

#### 7.1.1 命名约定

| 类型 | 规范 | 示例 |
|------|------|------|
| 类名 | PascalCase | `AccountService`, `TransactionController` |
| 接口名 | I + PascalCase | `IAccountService`, `IRepository<T>` |
| 方法名 | PascalCase | `GetByIdAsync`, `CreateAccount` |
| 参数/变量 | camelCase | `accountId`, `userName` |
| 常量 | PascalCase | `MaxPageSize`, `DefaultCurrency` |
| 私有字段 | _camelCase | `_context`, `_logger` |
| 异步方法 | 以 Async 结尾 | `GetAccountsAsync`, `SaveChangesAsync` |

#### 7.1.2 DTO 命名规范

- **Create**: `CreateAccountDto` - 创建时的输入
- **Update**: `UpdateAccountDto` - 更新时的输入
- **Query**: `AccountQueryDto` - 查询条件
- **Response**: `AccountDto` 或 `AccountResponseDto` - 返回给前端的数据

#### 7.1.3 代码组织

**Controller 规范**：
```csharp
[ApiController]
[Route("api/v1/[controller]")]
[Authorize]
public class AccountsController : ControllerBase
{
    private readonly IAccountService _accountService;
    private readonly ILogger<AccountsController> _logger;

    public AccountsController(IAccountService accountService, ILogger<AccountsController> logger)
    {
        _accountService = accountService;
        _logger = logger;
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<PageResponse<AccountDto>>>> GetAccounts(
        [FromQuery] PageRequest request)
    {
        var result = await _accountService.GetAccountsAsync(request);
        return Ok(ApiResponse<PageResponse<AccountDto>>.Success(result));
    }
}
```

**Service 规范**：
```csharp
public class AccountService : IAccountService
{
    private readonly IRepository<Account> _accountRepository;
    private readonly IMapper _mapper;
    private readonly ILogger<AccountService> _logger;

    public AccountService(
        IRepository<Account> accountRepository,
        IMapper mapper,
        ILogger<AccountService> logger)
    {
        _accountRepository = accountRepository;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<PageResponse<AccountDto>> GetAccountsAsync(PageRequest request)
    {
        // 实现逻辑
    }
}
```

#### 7.1.4 异常处理

- 使用全局异常中间件统一处理
- Service 层抛出业务异常，不捕获
- 使用自定义异常类型（如 `BusinessException`, `NotFoundException`）
- 记录异常日志

#### 7.1.5 日志规范

```csharp
// Information - 关键业务操作
_logger.LogInformation("Account created: {AccountId}, Name: {AccountName}", account.Id, account.Name);

// Warning - 业务校验失败
_logger.LogWarning("Invalid account balance: {AccountId}, Balance: {Balance}", accountId, balance);

// Error - 异常情况
_logger.LogError(ex, "Failed to create account: {AccountName}", dto.Name);
```

### 7.2 前端编码规范

#### 7.2.1 命名约定

| 类型 | 规范 | 示例 |
|------|------|------|
| 组件文件 | PascalCase.vue | `AccountList.vue`, `TransactionForm.vue` |
| 组合式函数 | use + PascalCase | `useTable.ts`, `useCrud.ts` |
| 变量/函数 | camelCase | `accountList`, `fetchAccounts` |
| 常量 | UPPER_SNAKE_CASE | `API_BASE_URL`, `MAX_FILE_SIZE` |
| 类型/接口 | PascalCase | `Account`, `ApiResponse<T>` |

#### 7.2.2 组件结构

```vue
<script setup lang="ts">
// 1. 导入
import { ref, onMounted } from 'vue'
import { ElMessage } from 'element-plus'
import { getAccounts } from '@/api/account'
import type { Account } from '@/types/account'

// 2. Props 和 Emits
interface Props {
  accountId?: number
}
const props = defineProps<Props>()
const emit = defineEmits<{
  (e: 'update', account: Account): void
}>()

// 3. 响应式数据
const accounts = ref<Account[]>([])
const loading = ref(false)

// 4. 计算属性
const totalBalance = computed(() => {
  return accounts.value.reduce((sum, acc) => sum + acc.currentBalance, 0)
})

// 5. 方法
const fetchAccounts = async () => {
  loading.value = true
  try {
    accounts.value = await getAccounts()
  } catch (error) {
    ElMessage.error('获取账户列表失败')
  } finally {
    loading.value = false
  }
}

// 6. 生命周期
onMounted(() => {
  fetchAccounts()
})
</script>

<template>
  <!-- 模板内容 -->
</template>

<style scoped>
/* 样式 */
</style>
```

#### 7.2.3 API 封装规范

```typescript
// src/api/account.ts
import request from '@/utils/request'
import type { Account, CreateAccountDto, UpdateAccountDto } from '@/types/account'
import type { PageRequest, PageResponse } from '@/types/api'

export const getAccounts = (params?: PageRequest) => {
  return request.get<PageResponse<Account>>('/accounts', { params })
}

export const getAccountById = (id: number) => {
  return request.get<Account>(`/accounts/${id}`)
}

export const createAccount = (data: CreateAccountDto) => {
  return request.post<Account>('/accounts', data)
}

export const updateAccount = (id: number, data: UpdateAccountDto) => {
  return request.put<Account>(`/accounts/${id}`, data)
}

export const deleteAccount = (id: number) => {
  return request.delete(`/accounts/${id}`)
}
```

### 7.3 数据库约定

#### 7.3.1 命名规范

- 表名：snake_case 复数形式（`accounts`, `bank_transactions`）
- 字段名：snake_case（`account_type`, `created_at`）
- 主键：统一使用 `id`
- 外键：`{表名单数}_id`（`account_id`, `project_id`）
- 布尔字段：`is_` 或 `has_` 前缀（`is_deleted`, `is_active`）
- 时间字段：`_at` 后缀（`created_at`, `updated_at`）

#### 7.3.2 必需字段

所有业务表必须包含：
```sql
id BIGSERIAL PRIMARY KEY,
created_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
updated_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
is_deleted BOOLEAN NOT NULL DEFAULT false,
deleted_at TIMESTAMP
```

#### 7.3.3 索引规范

- 主键自动创建索引
- 外键字段创建索引
- 常用查询条件创建索引
- 使用条件索引过滤软删除：`WHERE is_deleted = false`

---

## 8. 关键业务逻辑实现指南

### 8.1 账户余额更新机制

**核心原则**：账户余额必须与交易记录保持一致，所有余额更新必须在事务中完成。

**实现方式**：
```csharp
public async Task<Transaction> CreateTransactionAsync(CreateTransactionDto dto)
{
    using var transaction = await _context.Database.BeginTransactionAsync();
    try
    {
        // 1. 创建交易记录
        var trans = _mapper.Map<Transaction>(dto);
        await _transactionRepository.AddAsync(trans);

        // 2. 更新账户余额
        var account = await _accountRepository.GetByIdAsync(dto.AccountId);
        if (account == null) throw new NotFoundException("账户不存在");

        if (dto.TransactionType == TransactionType.Income)
        {
            account.CurrentBalance += dto.Amount;
        }
        else
        {
            account.CurrentBalance -= dto.Amount;
        }
        await _accountRepository.UpdateAsync(account);

        // 3. 记录日志
        _logger.LogInformation(
            "Transaction created: {TransactionId}, Amount: {Amount}, Account: {AccountId}, NewBalance: {Balance}",
            trans.Id, dto.Amount, account.Id, account.CurrentBalance);

        await transaction.CommitAsync();
        return trans;
    }
    catch
    {
        await transaction.RollbackAsync();
        throw;
    }
}
```

### 8.2 费用分摊实现

**核心规则**：
1. `SUM(allocations.amount) == transaction.amount`（精确相等）
2. 每个 `allocation.amount > 0`
3. `projectId` 或 `personId` 不能重复
4. 分摊后设置 `transaction.is_allocated = true`
5. 所有操作必须在同一事务中完成

**实现示例**：
```csharp
public async Task AllocateTransactionAsync(long transactionId, List<AllocationDto> allocations)
{
    using var transaction = await _context.Database.BeginTransactionAsync();
    try
    {
        // 1. 获取交易记录
        var trans = await _transactionRepository.GetByIdAsync(transactionId);
        if (trans == null) throw new NotFoundException("交易不存在");

        // 2. 校验分摊金额
        var totalAmount = allocations.Sum(a => a.Amount);
        if (totalAmount != trans.Amount)
        {
            throw new BusinessException($"分摊金额之和({totalAmount})必须等于交易金额({trans.Amount})");
        }

        // 3. 校验每项金额 > 0
        if (allocations.Any(a => a.Amount <= 0))
        {
            throw new BusinessException("分摊金额必须大于 0");
        }

        // 4. 校验不重复
        var projectIds = allocations.Where(a => a.ProjectId.HasValue).Select(a => a.ProjectId.Value);
        if (projectIds.Count() != projectIds.Distinct().Count())
        {
            throw new BusinessException("不能重复分摊到同一个项目");
        }

        var personIds = allocations.Where(a => a.PersonId.HasValue).Select(a => a.PersonId.Value);
        if (personIds.Count() != personIds.Distinct().Count())
        {
            throw new BusinessException("不能重复分摊到同一个人员");
        }

        // 5. 删除旧分摊记录（如果存在）
        var existingAllocations = await _context.TransactionAllocations
            .Where(a => a.TransactionId == transactionId)
            .ToListAsync();
        _context.TransactionAllocations.RemoveRange(existingAllocations);

        // 6. 创建新分摊记录
        foreach (var dto in allocations)
        {
            var allocation = new TransactionAllocation
            {
                TransactionId = transactionId,
                ProjectId = dto.ProjectId,
                PersonId = dto.PersonId,
                Amount = dto.Amount,
                AllocationRate = dto.AllocationRate
            };
            await _context.TransactionAllocations.AddAsync(allocation);
        }

        // 7. 更新 is_allocated 标记
        trans.IsAllocated = true;
        await _transactionRepository.UpdateAsync(trans);

        // 8. 记录日志
        _logger.LogInformation(
            "Transaction allocated: {TransactionId}, Allocations: {Count}, Total: {Amount}",
            transactionId, allocations.Count, totalAmount);

        await transaction.CommitAsync();
    }
    catch
    {
        await transaction.RollbackAsync();
        throw;
    }
}
```

### 8.3 应收应付状态管理

**核心规则**：
1. 逾期状态通过查询时动态计算，不存储为数据库状态
2. 状态只有三种：`pending`, `partial`, `settled`
3. 登记收/付款时自动更新状态

**实现示例**：
```csharp
public async Task<ReceivableDto> ReceivePaymentAsync(long receivableId, ReceivePaymentDto dto)
{
    using var transaction = await _context.Database.BeginTransactionAsync();
    try
    {
        // 1. 获取应收记录
        var receivable = await _receivableRepository.GetByIdAsync(receivableId);
        if (receivable == null) throw new NotFoundException("应收记录不存在");

        // 2. 校验金额
        if (dto.Amount <= 0 || dto.Amount > receivable.RemainingAmount)
        {
            throw new BusinessException("收款金额无效");
        }

        // 3. 创建收款明细
        var detail = new ReceivableDetail
        {
            ReceivableId = receivableId,
            TransactionId = dto.TransactionId,
            PaymentDate = dto.PaymentDate,
            Amount = dto.Amount,
            PaymentMethod = dto.PaymentMethod,
            Note = dto.Note
        };
        await _context.ReceivableDetails.AddAsync(detail);

        // 4. 更新应收金额和状态
        receivable.ReceivedAmount += dto.Amount;
        receivable.RemainingAmount = receivable.TotalAmount - receivable.ReceivedAmount;

        if (receivable.RemainingAmount == 0)
        {
            receivable.Status = PaymentStatus.Settled;
            receivable.SettledAt = DateTime.UtcNow;
        }
        else if (receivable.ReceivedAmount > 0)
        {
            receivable.Status = PaymentStatus.Partial;
        }

        await _receivableRepository.UpdateAsync(receivable);

        // 5. 记录日志
        _logger.LogInformation(
            "Payment received: {ReceivableId}, Amount: {Amount}, NewStatus: {Status}",
            receivableId, dto.Amount, receivable.Status);

        await transaction.CommitAsync();

        // 6. 返回 DTO（包含逾期标记）
        var result = _mapper.Map<ReceivableDto>(receivable);
        result.IsOverdue = receivable.DueDate < DateTime.UtcNow &&
                          receivable.Status != PaymentStatus.Settled;
        return result;
    }
    catch
    {
        await transaction.RollbackAsync();
        throw;
    }
}
```

### 8.4 Excel 导入与重复检测

**支持格式**：
- 仅支持 .xlsx 格式（Excel 2007 及以上版本，基于 Office Open XML）
- 不支持 .xls 格式（Excel 97-2003 BIFF 格式）
- 用户需要将 .xls 文件手动转换为 .xlsx 格式后再导入

**核心流程**：
1. 解析 Excel（.xlsx 格式） → 生成 `unique_hash`（MD5）
2. 查询数据库检测重复
3. 返回预览数据（含重复标记）
4. 用户确认后写入数据库

**unique_hash 计算**：
```csharp
public string GenerateUniqueHash(DateTime date, decimal amount, string counterparty, string memo)
{
    var input = $"{date:yyyy-MM-dd}{amount}{counterparty}{memo}";
    using var md5 = MD5.Create();
    var bytes = md5.ComputeHash(Encoding.UTF8.GetBytes(input));
    return BitConverter.ToString(bytes).Replace("-", "").ToLower();
}
```

**重复检测**：
```csharp
public async Task<ImportPreviewDto> UploadAndPreviewAsync(IFormFile file, long accountId)
{
    // 1. 解析 Excel
    var rows = await _excelParserService.ParseAsync(file);

    // 2. 生成 unique_hash 并检测重复
    var hashes = rows.Select(r => GenerateUniqueHash(
        r.TransactionDate, r.Amount, r.Counterparty, r.Memo)).ToList();

    var existingHashes = await _context.BankTransactions
        .Where(bt => hashes.Contains(bt.UniqueHash))
        .Select(bt => bt.UniqueHash)
        .ToListAsync();

    // 3. 标记重复行
    foreach (var row in rows)
    {
        row.UniqueHash = GenerateUniqueHash(
            row.TransactionDate, row.Amount, row.Counterparty, row.Memo);
        row.IsDuplicate = existingHashes.Contains(row.UniqueHash);
    }

    // 4. 调用规则引擎自动分类
    foreach (var row in rows.Where(r => !r.IsDuplicate))
    {
        var matchResult = await _ruleEngineService.MatchAsync(
            row.Counterparty, row.Memo, row.Amount);
        if (matchResult != null)
        {
            row.CategoryId = matchResult.CategoryId;
            row.ProjectId = matchResult.ProjectId;
            // ... 其他字段
        }
    }

    // 5. 创建导入批次记录
    var batch = new ImportBatch
    {
        AccountId = accountId,
        FileName = file.FileName,
        Status = ImportStatus.Pending,
        TotalCount = rows.Count,
        DuplicateCount = rows.Count(r => r.IsDuplicate)
    };
    await _context.ImportBatches.AddAsync(batch);
    await _context.SaveChangesAsync();

    return new ImportPreviewDto
    {
        BatchId = batch.Id,
        Rows = rows,
        TotalCount = rows.Count,
        DuplicateCount = rows.Count(r => r.IsDuplicate),
        NewCount = rows.Count - rows.Count(r => r.IsDuplicate)
    };
}
```

### 8.5 规则引擎匹配逻辑

**匹配流程**：
1. 按 `priority` 降序、`id` 升序排序
2. 逐条匹配，首个命中即返回
3. 支持 6 种匹配操作符

**实现示例**：
```csharp
public async Task<RuleMatchResult?> MatchAsync(string counterparty, string memo, decimal amount)
{
    var rules = await _context.ClassificationRules
        .Where(r => !r.IsDeleted)
        .OrderByDescending(r => r.Priority)
        .ThenBy(r => r.Id)
        .ToListAsync();

    foreach (var rule in rules)
    {
        bool isMatch = rule.MatchField switch
        {
            MatchField.Counterparty => MatchValue(counterparty, rule.MatchOperator, rule.MatchValue),
            MatchField.Memo => MatchValue(memo, rule.MatchOperator, rule.MatchValue),
            MatchField.Amount => MatchAmount(amount, rule.MatchOperator, rule.MatchValue),
            _ => false
        };

        if (isMatch)
        {
            _logger.LogDebug("Rule matched: {RuleId}, {RuleName}", rule.Id, rule.RuleName);
            return new RuleMatchResult
            {
                RuleId = rule.Id,
                CategoryId = rule.CategoryId,
                ProjectId = rule.ProjectId,
                CustomerId = rule.CustomerId,
                SupplierId = rule.SupplierId,
                PersonId = rule.PersonId
            };
        }
    }

    return null;
}

private bool MatchValue(string value, MatchOperator op, string matchValue)
{
    value = value?.Trim() ?? "";
    matchValue = matchValue?.Trim() ?? "";

    return op switch
    {
        MatchOperator.Equals => value.Equals(matchValue, StringComparison.OrdinalIgnoreCase),
        MatchOperator.Contains => value.Contains(matchValue, StringComparison.OrdinalIgnoreCase),
        MatchOperator.StartsWith => value.StartsWith(matchValue, StringComparison.OrdinalIgnoreCase),
        MatchOperator.EndsWith => value.EndsWith(matchValue, StringComparison.OrdinalIgnoreCase),
        MatchOperator.Regex => Regex.IsMatch(value, matchValue, RegexOptions.IgnoreCase, TimeSpan.FromSeconds(1)),
        _ => false
    };
}

private bool MatchAmount(decimal amount, MatchOperator op, string matchValue)
{
    if (op != MatchOperator.Range) return false;

    var parts = matchValue.Split('-');
    if (parts.Length != 2) return false;

    var hasMin = decimal.TryParse(parts[0], out var min);
    var hasMax = decimal.TryParse(parts[1], out var max);

    if (hasMin && hasMax)
        return amount >= min && amount <= max;
    else if (hasMin)
        return amount >= min;
    else if (hasMax)
        return amount <= max;
    else
        return false;
}
```

---

## 13. 常见问题与陷阱

### 13.1 后端常见问题

#### 问题 1：EF Core 查询包含软删除的记录

**原因**：忘记配置全局查询过滤器。

**解决方案**：
```csharp
// 在 AppDbContext.OnModelCreating 中配置
foreach (var entityType in modelBuilder.Model.GetEntityTypes())
{
    if (typeof(BaseEntity).IsAssignableFrom(entityType.ClrType))
    {
        var parameter = Expression.Parameter(entityType.ClrType, "e");
        var body = Expression.Equal(
            Expression.Property(parameter, nameof(BaseEntity.IsDeleted)),
            Expression.Constant(false)
        );
        modelBuilder.Entity(entityType.ClrType).HasQueryFilter(Expression.Lambda(body, parameter));
    }
}
```

#### 问题 2：费用分摊后成本重复计算

**原因**：`is_allocated` 标记与 `transaction_allocations` 记录不一致。

**解决方案**：
- 所有分摊操作必须在同一事务中完成
- 创建分摊记录时同时设置 `is_allocated = true`
- 删除所有分摊记录时恢复 `is_allocated = false`

#### 问题 3：账户余额与交易记录不一致

**原因**：余额更新未在事务中完成，或删除交易时未回滚余额。

**解决方案**：
- 使用数据库事务确保原子性
- 删除交易时必须回滚余额
- 定期使用 `v_account_balance` 视图校验余额准确性

#### 问题 4：逾期状态存储到数据库

**错误做法**：
```csharp
// ❌ 错误：将 overdue 存储为状态
receivable.Status = PaymentStatus.Overdue;
```

**正确做法**：
```csharp
// ✅ 正确：查询时动态计算
var dto = _mapper.Map<ReceivableDto>(receivable);
dto.IsOverdue = receivable.DueDate < DateTime.UtcNow &&
                receivable.Status != PaymentStatus.Settled;
```

### 13.2 前端常见问题

#### 问题 1：Token 过期后未跳转登录页

**原因**：Axios 响应拦截器未正确处理 401 状态。

**解决方案**：
```typescript
instance.interceptors.response.use(
  (response) => response.data,
  (error) => {
    if (error.response?.status === 401) {
      useUserStore().logout()
      router.push('/login')
    }
    return Promise.reject(error)
  }
)
```

#### 问题 2：金额显示精度丢失

**原因**：JavaScript 浮点数精度问题。

**解决方案**：
```typescript
// 使用 toFixed 格式化
const formatAmount = (amount: number) => {
  return amount.toFixed(2)
}

// 或使用第三方库如 decimal.js
import Decimal from 'decimal.js'
const result = new Decimal(amount1).plus(amount2).toNumber()
```

#### 问题 3：表格数据未响应式更新

**原因**：直接修改数组元素未触发响应式。

**解决方案**：
```typescript
// ❌ 错误
tableData[index].status = 'settled'

// ✅ 正确
tableData.value[index] = { ...tableData.value[index], status: 'settled' }
// 或
tableData.value = tableData.value.map((item, i) =>
  i === index ? { ...item, status: 'settled' } : item
)
```

### 13.3 数据库常见问题

#### 问题 1：unique_hash 冲突

**原因**：MD5 哈希碰撞（极低概率）或输入数据完全相同。

**解决方案**：
- 检查是否真的是重复数据
- 如果确实不同，可在 hash 中加入更多字段（如银行账号）

#### 问题 2：迁移冲突

**原因**：多人开发时 EF Core 迁移文件冲突。

**解决方案**：
```bash
# 删除冲突的迁移
dotnet ef migrations remove

# 重新生成迁移
dotnet ef migrations add YourMigrationName

# 应用迁移
dotnet ef database update
```

#### 问题 3：PostgreSQL 连接池耗尽

**原因**：未正确释放数据库连接。

**解决方案**：
- 使用 `using` 语句确保 DbContext 被释放
- 配置连接池大小：`Maximum Pool Size=100` in connection string
- 避免长时间持有 DbContext

### 13.4 业务逻辑陷阱

#### 陷阱 1：应付管理是强制的

**错误理解**：所有支出都必须先记录应付。

**正确理解**：应付管理是可选的，小额即时支付可以只记录交易，不记录应付。

#### 陷阱 2：费用分摊只能按项目

**错误理解**：`transaction_allocations` 只支持 `project_id`。

**正确理解**：支持按项目、按人员、或混合分摊。`project_id` 和 `person_id` 至少有一个不为空。

#### 陷阱 3：超额支付必须拒绝

**错误理解**：实际支付超过应付金额时必须报错。

**正确理解**：MVP 阶段严格校验，增强版可显示警告但允许超额（可配置）。

---

## 总结

本开发指导文件涵盖了从环境搭建到生产部署的完整流程，包括：

1. **项目全景**：系统定位、技术栈、架构设计
2. **环境搭建**：后端、前端、数据库的详细配置步骤
3. **分阶段实施**：4 周开发计划，每个阶段的详细任务
4. **编码规范**：后端、前端、数据库的命名和组织规范
5. **业务逻辑**：账户余额、费用分摊、应收应付、Excel 导入、规则引擎的实现细节
6. **常见问题**：开发过程中容易遇到的陷阱和解决方案

**使用建议**：
- 开发每个模块前，先阅读对应章节
- 遇到问题时，查阅"常见问题与陷阱"章节
- 严格遵循编码规范，确保代码一致性
- 关键业务逻辑必须使用数据库事务
- 定期运行测试，确保功能正确性

**文档维护**：
- 发现新问题时，及时更新"常见问题"章节
- 业务规则变更时，同步更新相关章节
- 保持文档与代码的一致性

---

**文档结束**

