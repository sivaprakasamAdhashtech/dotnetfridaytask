using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MultiTenantBilling.Core.DTOs;
using MultiTenantBilling.Core.Interfaces;

namespace MultiTenantBilling.Api.Controllers;

[ApiController]
[Route("api/audit-logs")]
[Authorize]
public class AuditLogsController : ControllerBase
{
    private readonly IAuditLogService _auditLogService;
    private readonly ILogger<AuditLogsController> _logger;

    public AuditLogsController(
        IAuditLogService auditLogService,
        ILogger<AuditLogsController> logger)
    {
        _auditLogService = auditLogService;
        _logger = logger;
    }

    /// <summary>
    /// Get audit logs with filters (SuperAdmin can see all, others see only their tenant)
    /// </summary>
    /// <param name="query">Query parameters for filtering</param>
    /// <returns>Filtered audit logs</returns>
    [HttpGet]
    public async Task<ActionResult<ApiResponse<IEnumerable<AuditLogDto>>>> GetAuditLogs([FromQuery] AuditLogQueryRequest query)
    {
        try
        {
            var userRole = HttpContext.Items["UserRole"]?.ToString();
            var userTenantId = HttpContext.Items["TenantId"]?.ToString();

            // Non-SuperAdmin users can only see their own tenant's logs
            if (userRole != "SuperAdmin")
            {
                if (string.IsNullOrEmpty(userTenantId))
                {
                    return BadRequest(new ApiResponse<IEnumerable<AuditLogDto>>
                    {
                        Success = false,
                        Message = "Tenant context is required"
                    });
                }
                query.TenantId = userTenantId;
            }

            var auditLogs = await _auditLogService.GetAuditLogsAsync(query);
            
            return Ok(new ApiResponse<IEnumerable<AuditLogDto>>
            {
                Success = true,
                Message = "Audit logs retrieved successfully",
                Data = auditLogs
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve audit logs");
            return BadRequest(new ApiResponse<IEnumerable<AuditLogDto>>
            {
                Success = false,
                Message = "Failed to retrieve audit logs",
                Errors = new List<string> { ex.Message }
            });
        }
    }

    /// <summary>
    /// Get audit log count for the current tenant
    /// </summary>
    /// <returns>Audit log count</returns>
    [HttpGet("count")]
    [Authorize(Roles = "TenantAdmin,BillingUser")]
    public async Task<ActionResult<ApiResponse<long>>> GetAuditLogCount()
    {
        try
        {
            var tenantId = HttpContext.Items["TenantId"]?.ToString();
            if (string.IsNullOrEmpty(tenantId))
            {
                return BadRequest(new ApiResponse<long>
                {
                    Success = false,
                    Message = "Tenant context is required"
                });
            }

            var count = await _auditLogService.GetAuditLogCountAsync(tenantId);
            
            return Ok(new ApiResponse<long>
            {
                Success = true,
                Message = "Audit log count retrieved successfully",
                Data = count
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve audit log count");
            return BadRequest(new ApiResponse<long>
            {
                Success = false,
                Message = "Failed to retrieve audit log count",
                Errors = new List<string> { ex.Message }
            });
        }
    }

    /// <summary>
    /// Cleanup old audit logs (SuperAdmin only)
    /// </summary>
    /// <param name="retentionDays">Number of days to retain logs (default: 90)</param>
    /// <returns>Success status</returns>
    [HttpPost("cleanup")]
    [Authorize(Roles = "SuperAdmin")]
    public async Task<ActionResult<ApiResponse<object>>> CleanupOldLogs([FromQuery] int retentionDays = 90)
    {
        try
        {
            await _auditLogService.CleanupOldLogsAsync(retentionDays);

            var userId = HttpContext.Items["UserId"]?.ToString() ?? "system";
            await _auditLogService.LogAsync(
                tenantId: "SYSTEM",
                userId: userId,
                action: "CLEANUP_AUDIT_LOGS",
                entityType: "AuditLog",
                description: $"Cleaned up audit logs older than {retentionDays} days",
                ipAddress: HttpContext.Connection.RemoteIpAddress?.ToString(),
                userAgent: HttpContext.Request.Headers["User-Agent"].FirstOrDefault()
            );

            return Ok(new ApiResponse<object>
            {
                Success = true,
                Message = "Audit logs cleanup completed successfully"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to cleanup audit logs");
            return BadRequest(new ApiResponse<object>
            {
                Success = false,
                Message = "Failed to cleanup audit logs",
                Errors = new List<string> { ex.Message }
            });
        }
    }
}
