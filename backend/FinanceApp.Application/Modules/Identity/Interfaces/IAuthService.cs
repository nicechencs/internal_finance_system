using FinanceApp.Application.Modules.Identity.DTOs;

namespace FinanceApp.Application.Modules.Identity.Interfaces;

public interface IAuthService
{
    Task<AuthenticatedUserDto> LoginAsync(LoginRequest request);
    Task<UserDto> GetCurrentUserAsync(long userId);
    Task<AuthenticatedUserDto> ChangePasswordAsync(long userId, ChangePasswordRequest request);
    Task<AuthenticatedUserDto> GetAuthenticatedUserAsync(long userId);
    Task<UserDto> UpdateProfileAsync(long userId, UpdateProfileRequest request);
}
