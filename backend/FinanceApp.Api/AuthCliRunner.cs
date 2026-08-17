using FinanceApp.Application.Modules.Identity.DTOs;
using FinanceApp.Application.Modules.Identity.Interfaces;
using FinanceApp.Domain.Entities;
using FinanceApp.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace FinanceApp.Api;

public static class AuthCliRunner
{
    public static async Task RunAsync(string[] args, IServiceProvider serviceProvider)
    {
        if (args.Length == 0)
        {
            PrintUsage();
            return;
        }

        try
        {
            var command = args[0].ToLowerInvariant();
            var options = ParseOptions(args.Skip(1).ToArray());

            using var scope = serviceProvider.CreateScope();
            var userRepository = scope.ServiceProvider.GetRequiredService<IRepository<User>>();
            var userManagementService = scope.ServiceProvider.GetRequiredService<IUserManagementService>();

            switch (command)
            {
                case "create-user":
                    await CreateUserAsync(options, userManagementService);
                    return;
                case "set-password":
                    await SetPasswordAsync(options, userRepository, userManagementService);
                    return;
                case "unlock-user":
                    await UnlockUserAsync(options, userRepository, userManagementService);
                    return;
                case "set-active":
                    await SetActiveAsync(options, userRepository, userManagementService);
                    return;
                default:
                    Console.WriteLine($"未知命令: {command}");
                    PrintUsage();
                    Environment.ExitCode = 1;
                    return;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"命令执行失败: {ex.Message}");
            Environment.ExitCode = 1;
        }
    }

    private static async Task CreateUserAsync(
        IReadOnlyDictionary<string, string> options,
        IUserManagementService userManagementService)
    {
        var request = new CreateUserRequest
        {
            Username = GetRequiredOption(options, "username"),
            Password = GetRequiredOption(options, "password"),
            FullName = GetRequiredOption(options, "full-name"),
            Email = GetOptionalOption(options, "email"),
            Role = GetOptionalOption(options, "role") ?? "Viewer",
            IsActive = !options.ContainsKey("inactive"),
            MustChangePassword = options.ContainsKey("must-change-password")
        };

        var user = await userManagementService.CreateUserAsync(request);
        Console.WriteLine($"用户创建成功: {user.Username} ({user.Role})");
    }

    private static async Task SetPasswordAsync(
        IReadOnlyDictionary<string, string> options,
        IRepository<User> userRepository,
        IUserManagementService userManagementService)
    {
        var userId = await ResolveUserIdAsync(options, userRepository);
        await userManagementService.SetUserPasswordAsync(userId, new SetUserPasswordRequest
        {
            NewPassword = GetRequiredOption(options, "password"),
            MustChangePassword = options.ContainsKey("must-change-password")
        });

        Console.WriteLine("密码设置成功");
    }

    private static async Task UnlockUserAsync(
        IReadOnlyDictionary<string, string> options,
        IRepository<User> userRepository,
        IUserManagementService userManagementService)
    {
        var userId = await ResolveUserIdAsync(options, userRepository);
        await userManagementService.UnlockUserAsync(userId);
        Console.WriteLine("用户已解锁");
    }

    private static async Task SetActiveAsync(
        IReadOnlyDictionary<string, string> options,
        IRepository<User> userRepository,
        IUserManagementService userManagementService)
    {
        var userId = await ResolveUserIdAsync(options, userRepository);
        var activeValue = GetRequiredOption(options, "active");
        if (!bool.TryParse(activeValue, out var isActive))
        {
            throw new InvalidOperationException("--active 仅支持 true 或 false");
        }

        await userManagementService.SetUserStatusAsync(userId, new SetUserStatusRequest
        {
            IsActive = isActive
        }, currentUserId: -1);

        Console.WriteLine($"用户已{(isActive ? "启用" : "禁用")}");
    }

    private static async Task<long> ResolveUserIdAsync(
        IReadOnlyDictionary<string, string> options,
        IRepository<User> userRepository)
    {
        if (options.TryGetValue("user-id", out var userIdValue))
        {
            if (long.TryParse(userIdValue, out var userId))
            {
                return userId;
            }

            throw new InvalidOperationException("--user-id 必须是数字");
        }

        var username = GetRequiredOption(options, "username").Trim().ToUpperInvariant();
        var user = await userRepository.GetQueryable()
            .FirstOrDefaultAsync(u => u.NormalizedUsername == username || u.Username.ToUpper() == username);

        if (user == null)
        {
            throw new InvalidOperationException("用户不存在");
        }

        return user.Id;
    }

    private static Dictionary<string, string> ParseOptions(string[] args)
    {
        var options = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        for (var index = 0; index < args.Length; index++)
        {
            var current = args[index];
            if (!current.StartsWith("--", StringComparison.Ordinal))
            {
                continue;
            }

            var key = current[2..];
            if (index + 1 < args.Length && !args[index + 1].StartsWith("--", StringComparison.Ordinal))
            {
                options[key] = args[index + 1];
                index += 1;
            }
            else
            {
                options[key] = "true";
            }
        }

        return options;
    }

    private static string GetRequiredOption(IReadOnlyDictionary<string, string> options, string key)
    {
        if (options.TryGetValue(key, out var value) && !string.IsNullOrWhiteSpace(value))
        {
            return value;
        }

        throw new InvalidOperationException($"缺少参数 --{key}");
    }

    private static string? GetOptionalOption(IReadOnlyDictionary<string, string> options, string key)
    {
        return options.TryGetValue(key, out var value) ? value : null;
    }

    private static void PrintUsage()
    {
        Console.WriteLine("auth-cli 用法:");
        Console.WriteLine("  create-user --username <用户名> --password <密码> --full-name <姓名> [--role Admin|Accountant|Viewer] [--email <邮箱>] [--inactive] [--must-change-password]");
        Console.WriteLine("  set-password --username <用户名> | --user-id <ID> --password <新密码> [--must-change-password]");
        Console.WriteLine("  unlock-user --username <用户名> | --user-id <ID>");
        Console.WriteLine("  set-active --username <用户名> | --user-id <ID> --active true|false");
    }
}
