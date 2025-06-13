using MultiTenantBilling.Core.DTOs;
using MultiTenantBilling.Core.Entities;
using MultiTenantBilling.Core.Exceptions;
using MultiTenantBilling.Core.Interfaces;

namespace MultiTenantBilling.Infrastructure.Services;

public class UserService : IUserService
{
    private readonly IUserRepository _userRepository;
    private readonly ITenantRepository _tenantRepository;
    private readonly IAuthService _authService;
    private readonly IAuditLogService _auditLogService;

    public UserService(
        IUserRepository userRepository,
        ITenantRepository tenantRepository,
        IAuthService authService,
        IAuditLogService auditLogService)
    {
        _userRepository = userRepository;
        _tenantRepository = tenantRepository;
        _authService = authService;
        _auditLogService = auditLogService;
    }

    public async Task<UserDto> CreateUserAsync(string tenantId, CreateUserRequest request, string createdBy)
    {
        // Verify tenant exists
        var tenant = await _tenantRepository.GetByTenantIdAsync(tenantId);
        if (tenant == null)
        {
            throw new NotFoundException("Tenant", tenantId);
        }

        // Check if email already exists for this tenant
        if (await _userRepository.EmailExistsAsync(request.Email, tenantId))
        {
            throw new DuplicateException("User", "Email", request.Email);
        }

        var hashedPassword = await _authService.HashPasswordAsync(request.Password);

        var user = new User
        {
            FirstName = request.FirstName,
            LastName = request.LastName,
            Email = request.Email.ToLowerInvariant(),
            PasswordHash = hashedPassword,
            TenantId = tenantId,
            Role = request.Role,
            PhoneNumber = request.PhoneNumber,
            Preferences = request.Preferences,
            CreatedBy = createdBy,
            UpdatedBy = createdBy,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        await _userRepository.CreateAsync(user);

        return MapToDto(user);
    }

    public async Task<UserDto> UpdateUserAsync(string userId, UpdateUserRequest request, string updatedBy)
    {
        var user = await _userRepository.GetByIdAsync(userId);
        if (user == null)
        {
            throw new NotFoundException("User", userId);
        }

        // Check if email change conflicts with existing user
        if (user.Email != request.Email.ToLowerInvariant() && 
            await _userRepository.EmailExistsAsync(request.Email, user.TenantId))
        {
            throw new DuplicateException("User", "Email", request.Email);
        }

        user.FirstName = request.FirstName;
        user.LastName = request.LastName;
        user.Email = request.Email.ToLowerInvariant();
        user.Role = request.Role;
        user.IsActive = request.IsActive;
        user.PhoneNumber = request.PhoneNumber;
        user.Preferences = request.Preferences;
        user.UpdatedBy = updatedBy;
        user.UpdatedAt = DateTime.UtcNow;

        await _userRepository.UpdateAsync(user);

        return MapToDto(user);
    }

    public async Task<UserDto?> GetUserAsync(string userId)
    {
        var user = await _userRepository.GetByIdAsync(userId);
        return user != null ? MapToDto(user) : null;
    }

    public async Task<IEnumerable<UserDto>> GetUsersByTenantAsync(string tenantId)
    {
        var users = await _userRepository.GetByTenantIdAsync(tenantId);
        return users.Select(MapToDto);
    }

    public async Task<bool> DeleteUserAsync(string userId)
    {
        return await _userRepository.DeleteAsync(userId);
    }

    public async Task<bool> ChangePasswordAsync(string userId, ChangePasswordRequest request)
    {
        var user = await _userRepository.GetByIdAsync(userId);
        if (user == null)
        {
            throw new NotFoundException("User", userId);
        }

        if (!await _authService.ValidatePasswordAsync(request.CurrentPassword, user.PasswordHash))
        {
            throw new UnauthorizedException("Current password is incorrect");
        }

        user.PasswordHash = await _authService.HashPasswordAsync(request.NewPassword);
        user.UpdatedAt = DateTime.UtcNow;

        await _userRepository.UpdateAsync(user);

        return true;
    }

    public async Task<UserDto?> GetUserByEmailAsync(string email, string tenantId)
    {
        var user = await _userRepository.GetByEmailAndTenantAsync(email, tenantId);
        return user != null ? MapToDto(user) : null;
    }

    private static UserDto MapToDto(User user)
    {
        return new UserDto
        {
            Id = user.Id,
            FirstName = user.FirstName,
            LastName = user.LastName,
            Email = user.Email,
            TenantId = user.TenantId,
            Role = user.Role.ToString(),
            IsActive = user.IsActive,
            CreatedAt = user.CreatedAt,
            LastLoginAt = user.LastLoginAt,
            PhoneNumber = user.PhoneNumber,
            FullName = user.FullName
        };
    }
}
