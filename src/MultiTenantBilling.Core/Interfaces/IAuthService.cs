using MultiTenantBilling.Core.DTOs;

namespace MultiTenantBilling.Core.Interfaces;

public interface IAuthService
{
    Task<LoginResponse> LoginAsync(LoginRequest request);
    Task<string> GenerateJwtTokenAsync(UserDto user);
    Task<bool> ValidatePasswordAsync(string password, string hashedPassword);
    Task<string> HashPasswordAsync(string password);
    Task LogoutAsync(string userId);
}
