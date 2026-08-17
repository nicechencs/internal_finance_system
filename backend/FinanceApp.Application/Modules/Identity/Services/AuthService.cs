using MapsterMapper;
using FinanceApp.Application.Common;
using FinanceApp.Application.Modules.Identity.DTOs;
using FinanceApp.Application.Modules.Identity.Interfaces;
using FinanceApp.Domain.Configuration;
using FinanceApp.Domain.Entities;
using FinanceApp.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Text.Json;

namespace FinanceApp.Application.Modules.Identity.Services;

public class AuthService : IAuthService
{
    private const string GenericLoginErrorMessage = "用户名或密码错误";

    private readonly IRepository<User> _userRepository;
    private readonly IPasswordService _passwordService;
    private readonly IMapper _mapper;
    private readonly ILogger<AuthService> _logger;
    private readonly IUnitOfWork _unitOfWork;
    private readonly AuthOptions _authOptions;
    private readonly IAuditLogService _auditLogService;

    public AuthService(
        IRepository<User> userRepository,
        IPasswordService passwordService,
        IMapper mapper,
        ILogger<AuthService> logger,
        IUnitOfWork unitOfWork,
        IOptions<AuthOptions> authOptions,
        IAuditLogService auditLogService)
    {
        _userRepository = userRepository;
        _passwordService = passwordService;
        _mapper = mapper;
        _logger = logger;
        _unitOfWork = unitOfWork;
        _authOptions = authOptions.Value;
        _auditLogService = auditLogService;
    }

    public async Task<AuthenticatedUserDto> LoginAsync(LoginRequest request)
    {
        AuthValidationHelper.ValidateLoginRequest(request);

        var normalizedUsername = AuthValidationHelper.NormalizeUsername(request.Username);
        var user = await _userRepository.GetQueryable()
            .FirstOrDefaultAsync(u =>
                u.NormalizedUsername == normalizedUsername
                || u.Username.ToUpper() == normalizedUsername);

        if (user == null)
        {
            _passwordService.VerifyAgainstDummyHash(request.Password);
            _logger.LogWarning("Login failed: user not found, Username={Username}", request.Username);
            await _auditLogService.LogAsync("LoginFailed", "User", 0, null,
                SerializeForAudit(new { Username = request.Username, Reason = "UserNotFound" }));
            throw new UnauthorizedAccessException(GenericLoginErrorMessage);
        }

        var now = DateTime.UtcNow;
        EnsureUserAuthDefaults(user);

        if (user.LockoutEndAt.HasValue && user.LockoutEndAt.Value > now)
        {
            _logger.LogWarning("Login failed: user is locked out, UserId={UserId}, Username={Username}", user.Id, user.Username);
            await _auditLogService.LogAsync("LoginFailed", "User", user.Id, null,
                SerializeForAudit(new { Username = user.Username, Reason = "LockedOut", LockoutEndAt = user.LockoutEndAt }));
            throw new UnauthorizedAccessException(GenericLoginErrorMessage);
        }

        if (user.LockoutEndAt.HasValue && user.LockoutEndAt.Value <= now)
        {
            user.AccessFailedCount = 0;
            user.LockoutEndAt = null;
        }

        if (!_passwordService.VerifyPassword(request.Password, user.PasswordHash))
        {
            user.AccessFailedCount += 1;

            if (user.AccessFailedCount >= _authOptions.MaxFailedAccessAttempts)
            {
                user.LockoutEndAt = now.AddMinutes(_authOptions.LockoutMinutes);
                _logger.LogWarning(
                    "Login failed: lockout threshold reached, UserId={UserId}, Username={Username}, LockoutEndAt={LockoutEndAt}",
                    user.Id,
                    user.Username,
                    user.LockoutEndAt);
                await _auditLogService.LogAsync("LoginFailed", "User", user.Id, null,
                    SerializeForAudit(new { Username = user.Username, Reason = "LockedDueToFailedAttempts", LockoutEndAt = user.LockoutEndAt }));
            }
            else
            {
                _logger.LogWarning(
                    "Login failed: invalid password, UserId={UserId}, Username={Username}, FailedCount={FailedCount}",
                    user.Id,
                    user.Username,
                    user.AccessFailedCount);
                await _auditLogService.LogAsync("LoginFailed", "User", user.Id, null,
                    SerializeForAudit(new { Username = user.Username, Reason = "InvalidPassword", FailedCount = user.AccessFailedCount }));
            }

            _userRepository.Update(user);
            await _unitOfWork.SaveChangesAsync();

            throw new UnauthorizedAccessException(GenericLoginErrorMessage);
        }

        if (!user.IsActive)
        {
            _logger.LogWarning("Login failed: user is disabled, UserId={UserId}, Username={Username}", user.Id, user.Username);
            await _auditLogService.LogAsync("LoginFailed", "User", user.Id, null,
                SerializeForAudit(new { Username = user.Username, Reason = "UserDisabled" }));
            throw new UnauthorizedAccessException(GenericLoginErrorMessage);
        }

        user.AccessFailedCount = 0;
        user.LockoutEndAt = null;
        user.LastLoginAt = now;

        _userRepository.Update(user);
        await _unitOfWork.SaveChangesAsync();

        _logger.LogInformation("Login succeeded, UserId={UserId}, Username={Username}, Role={Role}", user.Id, user.Username, user.Role);
        await _auditLogService.LogAsync("Login", "User", user.Id, null,
            SerializeForAudit(new { Username = user.Username, Role = user.Role.ToString() }));

        return ToAuthenticatedUserDto(user);
    }

    public async Task<UserDto> GetCurrentUserAsync(long userId)
    {
        var user = await FindUserByIdAsync(userId);
        return _mapper.Map<UserDto>(user);
    }

    public async Task<AuthenticatedUserDto> ChangePasswordAsync(long userId, ChangePasswordRequest request)
    {
        var user = await FindUserByIdAsync(userId);

        if (!_passwordService.VerifyPassword(request.CurrentPassword, user.PasswordHash))
        {
            throw new ValidationException("当前密码不正确");
        }

        AuthValidationHelper.ValidatePassword(request.NewPassword, _authOptions, "新密码");

        if (_passwordService.VerifyPassword(request.NewPassword, user.PasswordHash))
        {
            throw new ValidationException("新密码不能与当前密码相同");
        }

        user.PasswordHash = _passwordService.HashPassword(request.NewPassword);
        user.PasswordChangedAt = DateTime.UtcNow;
        user.MustChangePassword = false;
        user.SecurityStamp = AuthValidationHelper.CreateSecurityStamp();
        user.AccessFailedCount = 0;
        user.LockoutEndAt = null;

        _userRepository.Update(user);
        await _unitOfWork.SaveChangesAsync();

        _logger.LogInformation("Password changed successfully, UserId={UserId}, Username={Username}", user.Id, user.Username);
        await _auditLogService.LogAsync("ChangePassword", "User", user.Id, null,
            SerializeForAudit(new { UserId = userId, Username = user.Username }));

        return ToAuthenticatedUserDto(user);
    }

    public async Task<UserDto> UpdateProfileAsync(long userId, UpdateProfileRequest request)
    {
        AuthValidationHelper.ValidateFullName(request.FullName);

        var user = await FindUserByIdAsync(userId);

        var oldProfile = new { FullName = user.FullName, Email = user.Email };

        user.FullName = request.FullName.Trim();
        user.Email = string.IsNullOrWhiteSpace(request.Email) ? null : request.Email.Trim();

        _userRepository.Update(user);
        await _unitOfWork.SaveChangesAsync();

        _logger.LogInformation("Profile updated, UserId={UserId}, Username={Username}", user.Id, user.Username);
        await _auditLogService.LogAsync("UpdateProfile", "User", user.Id,
            SerializeForAudit(oldProfile),
            SerializeForAudit(new { FullName = user.FullName, Email = user.Email }));

        return _mapper.Map<UserDto>(user);
    }

    public async Task<AuthenticatedUserDto> GetAuthenticatedUserAsync(long userId)
    {
        var user = await FindUserByIdAsync(userId);
        EnsureUserAuthDefaults(user);
        return ToAuthenticatedUserDto(user);
    }

    private async Task<User> FindUserByIdAsync(long userId)
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

    private AuthenticatedUserDto ToAuthenticatedUserDto(User user)
    {
        return new AuthenticatedUserDto
        {
            UserId = user.Id,
            Username = user.Username,
            FullName = user.FullName,
            Email = user.Email ?? string.Empty,
            Role = user.Role.ToString(),
            SecurityStamp = user.SecurityStamp,
            MustChangePassword = user.MustChangePassword,
            User = _mapper.Map<UserDto>(user)
        };
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
