using FinanceApp.Application.Modules.Identity.DTOs;

namespace FinanceApp.Application.Modules.Identity.Interfaces;

public interface IUserManagementService
{
    Task<IReadOnlyList<UserAdminDto>> GetUsersAsync();
    Task<UserAdminDto> CreateUserAsync(CreateUserRequest request);
    Task SetUserPasswordAsync(long userId, SetUserPasswordRequest request);
    Task SetUserStatusAsync(long userId, SetUserStatusRequest request, long currentUserId);
    Task UnlockUserAsync(long userId);
    Task<UserAdminDto> UpdateUserAsync(long userId, UpdateUserRequest request, long currentUserId);
}
