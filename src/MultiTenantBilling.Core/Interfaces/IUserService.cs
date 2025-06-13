using MultiTenantBilling.Core.DTOs;

namespace MultiTenantBilling.Core.Interfaces;

public interface IUserService
{
    Task<UserDto> CreateUserAsync(string tenantId, CreateUserRequest request, string createdBy);
    Task<UserDto> UpdateUserAsync(string userId, UpdateUserRequest request, string updatedBy);
    Task<UserDto?> GetUserAsync(string userId);
    Task<IEnumerable<UserDto>> GetUsersByTenantAsync(string tenantId);
    Task<bool> DeleteUserAsync(string userId);
    Task<bool> ChangePasswordAsync(string userId, ChangePasswordRequest request);
    Task<UserDto?> GetUserByEmailAsync(string email, string tenantId);
}
