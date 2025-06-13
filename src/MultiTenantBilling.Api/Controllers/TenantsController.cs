using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MultiTenantBilling.Core.DTOs;
using MultiTenantBilling.Core.Interfaces;

namespace MultiTenantBilling.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class TenantsController : ControllerBase
{
    private readonly ITenantService _tenantService;
    private readonly IAuditLogService _auditLogService;
    private readonly ILogger<TenantsController> _logger;

    public TenantsController(
        ITenantService tenantService,
        IAuditLogService auditLogService,
        ILogger<TenantsController> logger)
    {
        _tenantService = tenantService;
        _auditLogService = auditLogService;
        _logger = logger;
    }

    /// <summary>
    /// Create a new tenant (SuperAdmin only)
    /// </summary>
    /// <param name="request">Tenant creation request</param>
    /// <returns>Created tenant information</returns>
    [HttpPost]
    [Authorize(Roles = "SuperAdmin")]
    public async Task<ActionResult<ApiResponse<TenantDto>>> CreateTenant([FromBody] CreateTenantRequest request)
    {
        try
        {
            var userId = HttpContext.Items["UserId"]?.ToString() ?? "system";
            var tenant = await _tenantService.CreateTenantAsync(request, userId);

            await _auditLogService.LogAsync(
                tenantId: tenant.TenantId,
                userId: userId,
                action: "CREATE_TENANT",
                entityType: "Tenant",
                entityId: tenant.Id,
                description: $"Created tenant: {tenant.Name}",
                ipAddress: HttpContext.Connection.RemoteIpAddress?.ToString(),
                userAgent: HttpContext.Request.Headers["User-Agent"].FirstOrDefault()
            );

            return CreatedAtAction(nameof(GetTenant), new { tenantId = tenant.TenantId }, 
                new ApiResponse<TenantDto>
                {
                    Success = true,
                    Message = "Tenant created successfully",
                    Data = tenant
                });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create tenant");
            return BadRequest(new ApiResponse<TenantDto>
            {
                Success = false,
                Message = "Failed to create tenant",
                Errors = new List<string> { ex.Message }
            });
        }
    }

    /// <summary>
    /// Get all tenants (SuperAdmin only)
    /// </summary>
    /// <returns>List of all tenants</returns>
    [HttpGet]
    [Authorize(Roles = "SuperAdmin")]
    public async Task<ActionResult<ApiResponse<IEnumerable<TenantDto>>>> GetAllTenants()
    {
        try
        {
            var tenants = await _tenantService.GetAllTenantsAsync();
            
            return Ok(new ApiResponse<IEnumerable<TenantDto>>
            {
                Success = true,
                Message = "Tenants retrieved successfully",
                Data = tenants
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve tenants");
            return BadRequest(new ApiResponse<IEnumerable<TenantDto>>
            {
                Success = false,
                Message = "Failed to retrieve tenants",
                Errors = new List<string> { ex.Message }
            });
        }
    }

    /// <summary>
    /// Get tenant by ID
    /// </summary>
    /// <param name="tenantId">Tenant ID</param>
    /// <returns>Tenant information</returns>
    [HttpGet("{tenantId}")]
    public async Task<ActionResult<ApiResponse<TenantDto>>> GetTenant(string tenantId)
    {
        try
        {
            var tenant = await _tenantService.GetTenantAsync(tenantId);
            if (tenant == null)
            {
                return NotFound(new ApiResponse<TenantDto>
                {
                    Success = false,
                    Message = "Tenant not found"
                });
            }

            return Ok(new ApiResponse<TenantDto>
            {
                Success = true,
                Message = "Tenant retrieved successfully",
                Data = tenant
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve tenant {TenantId}", tenantId);
            return BadRequest(new ApiResponse<TenantDto>
            {
                Success = false,
                Message = "Failed to retrieve tenant",
                Errors = new List<string> { ex.Message }
            });
        }
    }

    /// <summary>
    /// Update tenant information
    /// </summary>
    /// <param name="tenantId">Tenant ID</param>
    /// <param name="request">Update request</param>
    /// <returns>Updated tenant information</returns>
    [HttpPut("{tenantId}")]
    [Authorize(Roles = "SuperAdmin,TenantAdmin")]
    public async Task<ActionResult<ApiResponse<TenantDto>>> UpdateTenant(string tenantId, [FromBody] UpdateTenantRequest request)
    {
        try
        {
            var userId = HttpContext.Items["UserId"]?.ToString() ?? "system";
            var tenant = await _tenantService.UpdateTenantAsync(tenantId, request, userId);

            await _auditLogService.LogAsync(
                tenantId: tenantId,
                userId: userId,
                action: "UPDATE_TENANT",
                entityType: "Tenant",
                entityId: tenant.Id,
                description: $"Updated tenant: {tenant.Name}",
                ipAddress: HttpContext.Connection.RemoteIpAddress?.ToString(),
                userAgent: HttpContext.Request.Headers["User-Agent"].FirstOrDefault()
            );

            return Ok(new ApiResponse<TenantDto>
            {
                Success = true,
                Message = "Tenant updated successfully",
                Data = tenant
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update tenant {TenantId}", tenantId);
            return BadRequest(new ApiResponse<TenantDto>
            {
                Success = false,
                Message = "Failed to update tenant",
                Errors = new List<string> { ex.Message }
            });
        }
    }
}
