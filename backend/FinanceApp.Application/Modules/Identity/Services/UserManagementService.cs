using MapsterMapper;
using FinanceApp.Application.Common;
using FinanceApp.Application.Modules.Identity.DTOs;
using FinanceApp.Application.Modules.Identity.Interfaces;
using FinanceApp.Domain.Configuration;
using FinanceApp.Domain.Entities;
using FinanceApp.Domain.Enums;
using FinanceApp.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using System.Text.Json;

namespace FinanceApp.Application.Modules.Identity.Services;

public class UserManagementService : IUserManagementService
{
    private readonly IRepository<User> _userRepository;
    private readonly IPasswordService _passwordService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly AuthOptions _authOptions;
    private readonly IAuditLogService _auditLogService;

    public UserManagementService(
        IRepository<User> userRepository,
        IPasswordService passwordService,
        IUnitOfWork unitOfWork,
        IMapper mapper,
        IOptions<AuthOptions> authOptions,
        IAuditLogService auditLogService)
    {
        _userRepository = userRepository;
        _passwordService = passwordService;
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _authOptions = authOptions.Value;
        _auditLogService = auditLogService;
    }

    public async Task<IReadOnlyList<UserAdminDto>> GetUsersAsync()
    {
        var now = DateTime.UtcNow;
        var users = await _userRepository.GetQueryable()
            .OrderBy(u => u.Role)
            .ThenBy(u => u.Username)
            .ToListAsync();

        return users.Select(user =>
        {
            EnsureUserAuthDefaults(user);
            var dto = _mapper.Map<UserAdminDto>(user);
            dto.IsLocked = user.LockoutEndAt.HasValue && user.LockoutEndAt.Value > now;
            return dto;
        }).ToList();
    }

    public async Task<UserAdminDto> CreateUserAsync(CreateUserRequest request)
    {
        AuthValidationHelper.ValidateUsername(request.Username);
        AuthValidationHelper.ValidateFullName(request.FullName);
        AuthValidationHelper.ValidatePassword(request.Password, _authOptions);

        var normalizedUsername = AuthValidationHelper.NormalizeUsername(request.Username);
        var exists = await _userRepository.GetQueryable()
            .AnyAsync(u =>
                u.NormalizedUsername == normalizedUsername
                || u.Username.ToUpper() == normalizedUsername);

        if (exists)
        {
            throw new ValidationException("用户名已存在");
        }

        var role = AuthValidationHelper.ParseRole(request.Role);
        var now = DateTime.UtcNow;

        var user = new User
        {
            Username = request.Username.Trim(),
            NormalizedUsername = normalizedUsername,
            PasswordHash = _passwordService.HashPassword(request.Password),
            SecurityStamp = AuthValidationHelper.CreateSecurityStamp(),
            FullName = request.FullName.Trim(),
            Email = string.IsNullOrWhiteSpace(request.Email) ? null : request.Email.Trim(),
            Role = role,
            IsActive = request.IsActive,
            MustChangePassword = request.MustChangePassword,
            PasswordChangedAt = now,
            CreatedAt = now,
            UpdatedAt = now
        };

        await _userRepository.AddAsync(user);
        await _unitOfWork.SaveChangesAsync();

        var dto = _mapper.Map<UserAdminDto>(user);
        await _auditLogService.LogAsync("Create", "User", user.Id, null, SerializeForAudit(dto));

        return dto;
    }

    public async Task SetUserPasswordAsync(long userId, SetUserPasswordRequest request)
    {
        AuthValidationHelper.ValidatePassword(request.NewPassword, _authOptions, "新密码");

        var user = await FindUserAsync(userId);
        user.PasswordHash = _passwordService.HashPassword(request.NewPassword);
        user.PasswordChangedAt = DateTime.UtcNow;
        user.MustChangePassword = request.MustChangePassword;
        user.SecurityStamp = AuthValidationHelper.CreateSecurityStamp();
        user.AccessFailedCount = 0;
        user.LockoutEndAt = null;

        _userRepository.Update(user);
        await _unitOfWork.SaveChangesAsync();

        await _auditLogService.LogAsync("SetPassword", "User", user.Id, null,
            SerializeForAudit(new { UserId = userId, MustChangePassword = request.MustChangePassword }));
    }

    public async Task SetUserStatusAsync(long userId, SetUserStatusRequest request, long currentUserId)
    {
        var user = await FindUserAsync(userId);

        if (!request.IsActive && user.Id == currentUserId)
        {
            throw new ValidationException("不能禁用当前登录用户");
        }

        if (!request.IsActive && user.Role == UserRole.Admin)
        {
            var activeAdminCount = await _userRepository.GetQueryable()
                .CountAsync(u => u.Role == UserRole.Admin && u.IsActive && u.Id != user.Id);

            if (activeAdminCount == 0)
            {
                throw new ValidationException("至少保留一个启用的管理员账号");
            }
        }

        var oldStatus = user.IsActive;
        user.IsActive = request.IsActive;
        user.SecurityStamp = AuthValidationHelper.CreateSecurityStamp();

        _userRepository.Update(user);
        await _unitOfWork.SaveChangesAsync();

        await _auditLogService.LogAsync(request.IsActive ? "Enable" : "Disable", "User", user.Id,
            SerializeForAudit(new { IsActive = oldStatus }),
            SerializeForAudit(new { IsActive = request.IsActive }));
    }

    public async Task UnlockUserAsync(long userId)
    {
        var user = await FindUserAsync(userId);
        user.AccessFailedCount = 0;
        user.LockoutEndAt = null;

        _userRepository.Update(user);
        await _unitOfWork.SaveChangesAsync();

        await _auditLogService.LogAsync("Unlock", "User", user.Id, null,
            SerializeForAudit(new { UserId = userId, Action = "Unlocked" }));
    }

    public async Task<UserAdminDto> UpdateUserAsync(long userId, UpdateUserRequest request, long currentUserId)
    {
        AuthValidationHelper.ValidateFullName(request.FullName);

        var user = await FindUserAsync(userId);
        var oldDto = _mapper.Map<UserAdminDto>(user);

        var newRole = AuthValidationHelper.ParseRole(request.Role);
        var roleChanged = user.Role != newRole;

        if (roleChanged && user.Role == UserRole.Admin && newRole != UserRole.Admin)
        {
            var activeAdminCount = await _userRepository.GetQueryable()
                .CountAsync(u => u.Role == UserRole.Admin && u.IsActive && u.Id != user.Id);

            if (activeAdminCount == 0)
            {
                throw new ValidationException("至少保留一个启用的管理员账号");
            }
        }

        user.FullName = request.FullName.Trim();
        user.Email = string.IsNullOrWhiteSpace(request.Email) ? null : request.Email.Trim();
        user.Role = newRole;

        if (roleChanged)
        {
            user.SecurityStamp = AuthValidationHelper.CreateSecurityStamp();
        }

        _userRepository.Update(user);
        await _unitOfWork.SaveChangesAsync();

        var dto = _mapper.Map<UserAdminDto>(user);
        var now = DateTime.UtcNow;
        dto.IsLocked = user.LockoutEndAt.HasValue && user.LockoutEndAt.Value > now;

        await _auditLogService.LogAsync("Update", "User", user.Id, SerializeForAudit(oldDto), SerializeForAudit(dto));

        return dto;
    }

    private async Task<User> FindUserAsync(long userId)
    {
        var user = await _userRepository.GetByIdAsync(userId);
        if (user == null)
        {
            throw new NotFoundException("用户不存在");
        }

        EnsureUserAuthDefaults(user);
        return user;
    }

    private static void EnsureUserAuthDefaults(User user)
    {
        if (string.IsNullOrWhiteSpace(user.NormalizedUsername))
        {
            user.NormalizedUsername = AuthValidationHelper.NormalizeUsername(user.Username);
        }

        if (string.IsNullOrWhiteSpace(user.SecurityStamp))
        {
            user.SecurityStamp = AuthValidationHelper.CreateSecurityStamp();
        }

        if (user.PasswordChangedAt == default)
        {
            user.PasswordChangedAt = user.CreatedAt == default ? DateTime.UtcNow : user.CreatedAt;
        }
    }

    private static string SerializeForAudit(object obj)
    {
        return JsonSerializer.Serialize(obj, new JsonSerializerOptions
        {
            WriteIndented = false,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });
    }
}
