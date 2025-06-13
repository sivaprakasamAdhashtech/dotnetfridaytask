using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using MultiTenantBilling.Core.DTOs;
using MultiTenantBilling.Core.Exceptions;
using MultiTenantBilling.Core.Interfaces;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace MultiTenantBilling.Infrastructure.Services;

public class AuthService : IAuthService
{
    private readonly IUserRepository _userRepository;
    private readonly IAuditLogService _auditLogService;
    private readonly IConfiguration _configuration;

    public AuthService(
        IUserRepository userRepository,
        IAuditLogService auditLogService,
        IConfiguration configuration)
    {
        _userRepository = userRepository;
        _auditLogService = auditLogService;
        _configuration = configuration;
    }

    public async Task<LoginResponse> LoginAsync(LoginRequest request)
    {
        var user = await _userRepository.GetByEmailAsync(request.Email);
        if (user == null || !user.IsActive)
        {
            await _auditLogService.LogAsync(
                tenantId: user?.TenantId ?? "UNKNOWN",
                userId: user?.Id ?? "UNKNOWN",
                action: "LOGIN_FAILED",
                entityType: "User",
                description: $"Failed login attempt for email: {request.Email}",
                severity: "Warning"
            );
            throw new UnauthorizedException("Invalid email or password.");
        }

        if (!await ValidatePasswordAsync(request.Password, user.PasswordHash))
        {
            await _auditLogService.LogAsync(
                tenantId: user.TenantId,
                userId: user.Id,
                action: "LOGIN_FAILED",
                entityType: "User",
                description: "Invalid password provided",
                severity: "Warning"
            );
            throw new UnauthorizedException("Invalid email or password.");
        }

        // Update last login time
        await _userRepository.UpdateLastLoginAsync(user.Id, DateTime.UtcNow);

        var userDto = new UserDto
        {
            Id = user.Id,
            FirstName = user.FirstName,
            LastName = user.LastName,
            Email = user.Email,
            TenantId = user.TenantId,
            Role = user.Role.ToString(),
            IsActive = user.IsActive,
            CreatedAt = user.CreatedAt,
            LastLoginAt = DateTime.UtcNow,
            PhoneNumber = user.PhoneNumber,
            FullName = user.FullName
        };

        var token = await GenerateJwtTokenAsync(userDto);
        var expiresAt = DateTime.UtcNow.AddHours(24); // Token expires in 24 hours

        await _auditLogService.LogAsync(
            tenantId: user.TenantId,
            userId: user.Id,
            action: "LOGIN_SUCCESS",
            entityType: "User",
            description: "User logged in successfully"
        );

        return new LoginResponse
        {
            Token = token,
            ExpiresAt = expiresAt,
            User = userDto
        };
    }

    public async Task<string> GenerateJwtTokenAsync(UserDto user)
    {
        var jwtSettings = _configuration.GetSection("JwtSettings");
        var secretKey = jwtSettings["SecretKey"] ?? throw new InvalidOperationException("JWT SecretKey not configured");
        var issuer = jwtSettings["Issuer"] ?? "MultiTenantBilling";
        var audience = jwtSettings["Audience"] ?? "MultiTenantBilling";

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id),
            new Claim(ClaimTypes.Email, user.Email),
            new Claim(ClaimTypes.Name, user.FullName),
            new Claim("TenantId", user.TenantId),
            new Claim(ClaimTypes.Role, user.Role),
            new Claim("FirstName", user.FirstName),
            new Claim("LastName", user.LastName)
        };

        var token = new JwtSecurityToken(
            issuer: issuer,
            audience: audience,
            claims: claims,
            expires: DateTime.UtcNow.AddHours(24),
            signingCredentials: credentials
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public async Task<bool> ValidatePasswordAsync(string password, string hashedPassword)
    {
        return await Task.FromResult(BCrypt.Net.BCrypt.Verify(password, hashedPassword));
    }

    public async Task<string> HashPasswordAsync(string password)
    {
        return await Task.FromResult(BCrypt.Net.BCrypt.HashPassword(password));
    }

    public async Task LogoutAsync(string userId)
    {
        var user = await _userRepository.GetByIdAsync(userId);
        if (user != null)
        {
            await _auditLogService.LogAsync(
                tenantId: user.TenantId,
                userId: user.Id,
                action: "LOGOUT",
                entityType: "User",
                description: "User logged out"
            );
        }
    }
}
