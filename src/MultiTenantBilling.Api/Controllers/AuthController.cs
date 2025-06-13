using Microsoft.AspNetCore.Mvc;
using MultiTenantBilling.Core.DTOs;
using MultiTenantBilling.Core.Interfaces;

namespace MultiTenantBilling.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;
    private readonly IAuditLogService _auditLogService;
    private readonly ILogger<AuthController> _logger;

    public AuthController(
        IAuthService authService,
        IAuditLogService auditLogService,
        ILogger<AuthController> logger)
    {
        _authService = authService;
        _auditLogService = auditLogService;
        _logger = logger;
    }

    /// <summary>
    /// Authenticate user and return JWT token
    /// </summary>
    /// <param name="request">Login credentials</param>
    /// <returns>JWT token and user information</returns>
    [HttpPost("login")]
    public async Task<ActionResult<ApiResponse<LoginResponse>>> Login([FromBody] LoginRequest request)
    {
        try
        {
            var response = await _authService.LoginAsync(request);
            
            return Ok(new ApiResponse<LoginResponse>
            {
                Success = true,
                Message = "Login successful",
                Data = response
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Login failed for email: {Email}", request.Email);
            
            return Unauthorized(new ApiResponse<LoginResponse>
            {
                Success = false,
                Message = "Login failed",
                Errors = new List<string> { ex.Message }
            });
        }
    }

    /// <summary>
    /// Logout user (for audit logging purposes)
    /// </summary>
    [HttpPost("logout")]
    public async Task<ActionResult<ApiResponse<object>>> Logout()
    {
        try
        {
            var userId = HttpContext.Items["UserId"]?.ToString();
            if (!string.IsNullOrEmpty(userId))
            {
                await _authService.LogoutAsync(userId);
            }

            return Ok(new ApiResponse<object>
            {
                Success = true,
                Message = "Logout successful"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Logout failed");
            
            return BadRequest(new ApiResponse<object>
            {
                Success = false,
                Message = "Logout failed",
                Errors = new List<string> { ex.Message }
            });
        }
    }
}
