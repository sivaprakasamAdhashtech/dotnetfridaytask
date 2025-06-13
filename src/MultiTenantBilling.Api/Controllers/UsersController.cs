using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MultiTenantBilling.Core.DTOs;
using MultiTenantBilling.Core.Interfaces;

namespace MultiTenantBilling.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class UsersController : ControllerBase
{
    private readonly IUserService _userService;
    private readonly IAuditLogService _auditLogService;
    private readonly ILogger<UsersController> _logger;

    public UsersController(
        IUserService userService,
        IAuditLogService auditLogService,
        ILogger<UsersController> logger)
    {
        _userService = userService;
        _auditLogService = auditLogService;
        _logger = logger;
    }

    /// <summary>
    /// Create a new user in a tenant (TenantAdmin only)
    /// </summary>
    /// <param name="request">User creation request</param>
    /// <returns>Created user information</returns>
    [HttpPost]
    [Authorize(Roles = "TenantAdmin")]
    public async Task<ActionResult<ApiResponse<UserDto>>> CreateUser([FromBody] CreateUserRequest request)
    {
        try
        {
            var tenantId = HttpContext.Items["TenantId"]?.ToString();
            var userId = HttpContext.Items["UserId"]?.ToString() ?? "system";

            if (string.IsNullOrEmpty(tenantId))
            {
                return BadRequest(new ApiResponse<UserDto>
                {
                    Success = false,
                    Message = "Tenant context is required"
                });
            }

            var user = await _userService.CreateUserAsync(tenantId, request, userId);

            await _auditLogService.LogAsync(
                tenantId: tenantId,
                userId: userId,
                action: "CREATE_USER",
                entityType: "User",
                entityId: user.Id,
                description: $"Created user: {user.Email}",
                ipAddress: HttpContext.Connection.RemoteIpAddress?.ToString(),
                userAgent: HttpContext.Request.Headers["User-Agent"].FirstOrDefault()
            );

            return CreatedAtAction(nameof(GetUser), new { id = user.Id }, 
                new ApiResponse<UserDto>
                {
                    Success = true,
                    Message = "User created successfully",
                    Data = user
                });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create user");
            return BadRequest(new ApiResponse<UserDto>
            {
                Success = false,
                Message = "Failed to create user",
                Errors = new List<string> { ex.Message }
            });
        }
    }

    /// <summary>
    /// Get user by ID
    /// </summary>
    /// <param name="id">User ID</param>
    /// <returns>User information</returns>
    [HttpGet("{id}")]
    public async Task<ActionResult<ApiResponse<UserDto>>> GetUser(string id)
    {
        try
        {
            var user = await _userService.GetUserAsync(id);
            if (user == null)
            {
                return NotFound(new ApiResponse<UserDto>
                {
                    Success = false,
                    Message = "User not found"
                });
            }

            return Ok(new ApiResponse<UserDto>
            {
                Success = true,
                Message = "User retrieved successfully",
                Data = user
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve user {UserId}", id);
            return BadRequest(new ApiResponse<UserDto>
            {
                Success = false,
                Message = "Failed to retrieve user",
                Errors = new List<string> { ex.Message }
            });
        }
    }

    /// <summary>
    /// Get all users in the current tenant
    /// </summary>
    /// <returns>List of users</returns>
    [HttpGet]
    [Authorize(Roles = "TenantAdmin,BillingUser")]
    public async Task<ActionResult<ApiResponse<IEnumerable<UserDto>>>> GetUsers()
    {
        try
        {
            var tenantId = HttpContext.Items["TenantId"]?.ToString();
            if (string.IsNullOrEmpty(tenantId))
            {
                return BadRequest(new ApiResponse<IEnumerable<UserDto>>
                {
                    Success = false,
                    Message = "Tenant context is required"
                });
            }

            var users = await _userService.GetUsersByTenantAsync(tenantId);
            
            return Ok(new ApiResponse<IEnumerable<UserDto>>
            {
                Success = true,
                Message = "Users retrieved successfully",
                Data = users
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve users");
            return BadRequest(new ApiResponse<IEnumerable<UserDto>>
            {
                Success = false,
                Message = "Failed to retrieve users",
                Errors = new List<string> { ex.Message }
            });
        }
    }

    /// <summary>
    /// Update user information
    /// </summary>
    /// <param name="id">User ID</param>
    /// <param name="request">Update request</param>
    /// <returns>Updated user information</returns>
    [HttpPut("{id}")]
    [Authorize(Roles = "TenantAdmin")]
    public async Task<ActionResult<ApiResponse<UserDto>>> UpdateUser(string id, [FromBody] UpdateUserRequest request)
    {
        try
        {
            var currentUserId = HttpContext.Items["UserId"]?.ToString() ?? "system";
            var user = await _userService.UpdateUserAsync(id, request, currentUserId);

            await _auditLogService.LogAsync(
                tenantId: user.TenantId,
                userId: currentUserId,
                action: "UPDATE_USER",
                entityType: "User",
                entityId: user.Id,
                description: $"Updated user: {user.Email}",
                ipAddress: HttpContext.Connection.RemoteIpAddress?.ToString(),
                userAgent: HttpContext.Request.Headers["User-Agent"].FirstOrDefault()
            );

            return Ok(new ApiResponse<UserDto>
            {
                Success = true,
                Message = "User updated successfully",
                Data = user
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update user {UserId}", id);
            return BadRequest(new ApiResponse<UserDto>
            {
                Success = false,
                Message = "Failed to update user",
                Errors = new List<string> { ex.Message }
            });
        }
    }

    /// <summary>
    /// Change user password
    /// </summary>
    /// <param name="id">User ID</param>
    /// <param name="request">Password change request</param>
    /// <returns>Success status</returns>
    [HttpPost("{id}/change-password")]
    public async Task<ActionResult<ApiResponse<object>>> ChangePassword(string id, [FromBody] ChangePasswordRequest request)
    {
        try
        {
            var currentUserId = HttpContext.Items["UserId"]?.ToString();
            
            // Users can only change their own password unless they're TenantAdmin
            var userRole = HttpContext.Items["UserRole"]?.ToString();
            if (currentUserId != id && userRole != "TenantAdmin")
            {
                return Forbid();
            }

            await _userService.ChangePasswordAsync(id, request);

            await _auditLogService.LogAsync(
                tenantId: HttpContext.Items["TenantId"]?.ToString() ?? "unknown",
                userId: currentUserId ?? "unknown",
                action: "CHANGE_PASSWORD",
                entityType: "User",
                entityId: id,
                description: "Password changed",
                ipAddress: HttpContext.Connection.RemoteIpAddress?.ToString(),
                userAgent: HttpContext.Request.Headers["User-Agent"].FirstOrDefault()
            );

            return Ok(new ApiResponse<object>
            {
                Success = true,
                Message = "Password changed successfully"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to change password for user {UserId}", id);
            return BadRequest(new ApiResponse<object>
            {
                Success = false,
                Message = "Failed to change password",
                Errors = new List<string> { ex.Message }
            });
        }
    }
}
