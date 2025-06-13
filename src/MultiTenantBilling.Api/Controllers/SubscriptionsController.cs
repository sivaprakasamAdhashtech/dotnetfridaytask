using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MultiTenantBilling.Core.DTOs;
using MultiTenantBilling.Core.Interfaces;

namespace MultiTenantBilling.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class SubscriptionsController : ControllerBase
{
    private readonly ISubscriptionService _subscriptionService;
    private readonly IAuditLogService _auditLogService;
    private readonly ILogger<SubscriptionsController> _logger;

    public SubscriptionsController(
        ISubscriptionService subscriptionService,
        IAuditLogService auditLogService,
        ILogger<SubscriptionsController> logger)
    {
        _subscriptionService = subscriptionService;
        _auditLogService = auditLogService;
        _logger = logger;
    }

    /// <summary>
    /// Create a new subscription for a tenant (SuperAdmin only)
    /// </summary>
    /// <param name="request">Subscription creation request</param>
    /// <returns>Created subscription information</returns>
    [HttpPost]
    [Authorize(Roles = "SuperAdmin")]
    public async Task<ActionResult<ApiResponse<SubscriptionDto>>> CreateSubscription([FromBody] CreateSubscriptionRequest request)
    {
        try
        {
            var userId = HttpContext.Items["UserId"]?.ToString() ?? "system";
            var subscription = await _subscriptionService.CreateSubscriptionAsync(request, userId);

            await _auditLogService.LogAsync(
                tenantId: subscription.TenantId,
                userId: userId,
                action: "CREATE_SUBSCRIPTION",
                entityType: "Subscription",
                entityId: subscription.Id,
                description: $"Created subscription for tenant: {subscription.TenantId}",
                ipAddress: HttpContext.Connection.RemoteIpAddress?.ToString(),
                userAgent: HttpContext.Request.Headers["User-Agent"].FirstOrDefault()
            );

            return CreatedAtAction(nameof(GetSubscription), new { id = subscription.Id }, 
                new ApiResponse<SubscriptionDto>
                {
                    Success = true,
                    Message = "Subscription created successfully",
                    Data = subscription
                });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create subscription");
            return BadRequest(new ApiResponse<SubscriptionDto>
            {
                Success = false,
                Message = "Failed to create subscription",
                Errors = new List<string> { ex.Message }
            });
        }
    }

    /// <summary>
    /// Get subscription by ID
    /// </summary>
    /// <param name="id">Subscription ID</param>
    /// <returns>Subscription information</returns>
    [HttpGet("{id}")]
    [Authorize(Roles = "SuperAdmin,TenantAdmin")]
    public async Task<ActionResult<ApiResponse<SubscriptionDto>>> GetSubscription(string id)
    {
        try
        {
            var subscription = await _subscriptionService.GetSubscriptionAsync(id);
            if (subscription == null)
            {
                return NotFound(new ApiResponse<SubscriptionDto>
                {
                    Success = false,
                    Message = "Subscription not found"
                });
            }

            return Ok(new ApiResponse<SubscriptionDto>
            {
                Success = true,
                Message = "Subscription retrieved successfully",
                Data = subscription
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve subscription {SubscriptionId}", id);
            return BadRequest(new ApiResponse<SubscriptionDto>
            {
                Success = false,
                Message = "Failed to retrieve subscription",
                Errors = new List<string> { ex.Message }
            });
        }
    }

    /// <summary>
    /// Get subscriptions for the current tenant
    /// </summary>
    /// <returns>List of tenant subscriptions</returns>
    [HttpGet]
    [Authorize(Roles = "TenantAdmin,BillingUser")]
    public async Task<ActionResult<ApiResponse<IEnumerable<SubscriptionDto>>>> GetTenantSubscriptions()
    {
        try
        {
            var tenantId = HttpContext.Items["TenantId"]?.ToString();
            if (string.IsNullOrEmpty(tenantId))
            {
                return BadRequest(new ApiResponse<IEnumerable<SubscriptionDto>>
                {
                    Success = false,
                    Message = "Tenant context is required"
                });
            }

            var subscriptions = await _subscriptionService.GetSubscriptionsByTenantAsync(tenantId);
            
            return Ok(new ApiResponse<IEnumerable<SubscriptionDto>>
            {
                Success = true,
                Message = "Subscriptions retrieved successfully",
                Data = subscriptions
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve tenant subscriptions");
            return BadRequest(new ApiResponse<IEnumerable<SubscriptionDto>>
            {
                Success = false,
                Message = "Failed to retrieve subscriptions",
                Errors = new List<string> { ex.Message }
            });
        }
    }

    /// <summary>
    /// Get active subscription for the current tenant
    /// </summary>
    /// <returns>Active subscription information</returns>
    [HttpGet("active")]
    [Authorize(Roles = "TenantAdmin,BillingUser")]
    public async Task<ActionResult<ApiResponse<SubscriptionDto>>> GetActiveSubscription()
    {
        try
        {
            var tenantId = HttpContext.Items["TenantId"]?.ToString();
            if (string.IsNullOrEmpty(tenantId))
            {
                return BadRequest(new ApiResponse<SubscriptionDto>
                {
                    Success = false,
                    Message = "Tenant context is required"
                });
            }

            var subscription = await _subscriptionService.GetActiveSubscriptionByTenantAsync(tenantId);
            if (subscription == null)
            {
                return NotFound(new ApiResponse<SubscriptionDto>
                {
                    Success = false,
                    Message = "No active subscription found"
                });
            }

            return Ok(new ApiResponse<SubscriptionDto>
            {
                Success = true,
                Message = "Active subscription retrieved successfully",
                Data = subscription
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve active subscription");
            return BadRequest(new ApiResponse<SubscriptionDto>
            {
                Success = false,
                Message = "Failed to retrieve active subscription",
                Errors = new List<string> { ex.Message }
            });
        }
    }

    /// <summary>
    /// Update subscription status
    /// </summary>
    /// <param name="id">Subscription ID</param>
    /// <param name="request">Update request</param>
    /// <returns>Updated subscription information</returns>
    [HttpPut("{id}")]
    [Authorize(Roles = "SuperAdmin,TenantAdmin")]
    public async Task<ActionResult<ApiResponse<SubscriptionDto>>> UpdateSubscription(string id, [FromBody] UpdateSubscriptionRequest request)
    {
        try
        {
            var userId = HttpContext.Items["UserId"]?.ToString() ?? "system";
            var subscription = await _subscriptionService.UpdateSubscriptionAsync(id, request, userId);

            await _auditLogService.LogAsync(
                tenantId: subscription.TenantId,
                userId: userId,
                action: "UPDATE_SUBSCRIPTION",
                entityType: "Subscription",
                entityId: subscription.Id,
                description: $"Updated subscription status to: {subscription.Status}",
                ipAddress: HttpContext.Connection.RemoteIpAddress?.ToString(),
                userAgent: HttpContext.Request.Headers["User-Agent"].FirstOrDefault()
            );

            return Ok(new ApiResponse<SubscriptionDto>
            {
                Success = true,
                Message = "Subscription updated successfully",
                Data = subscription
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update subscription {SubscriptionId}", id);
            return BadRequest(new ApiResponse<SubscriptionDto>
            {
                Success = false,
                Message = "Failed to update subscription",
                Errors = new List<string> { ex.Message }
            });
        }
    }

    /// <summary>
    /// Cancel subscription
    /// </summary>
    /// <param name="id">Subscription ID</param>
    /// <param name="reason">Cancellation reason</param>
    /// <returns>Success status</returns>
    [HttpPost("{id}/cancel")]
    [Authorize(Roles = "SuperAdmin,TenantAdmin")]
    public async Task<ActionResult<ApiResponse<object>>> CancelSubscription(string id, [FromBody] string reason)
    {
        try
        {
            var userId = HttpContext.Items["UserId"]?.ToString() ?? "system";
            var success = await _subscriptionService.CancelSubscriptionAsync(id, reason, userId);

            if (!success)
            {
                return NotFound(new ApiResponse<object>
                {
                    Success = false,
                    Message = "Subscription not found"
                });
            }

            await _auditLogService.LogAsync(
                tenantId: HttpContext.Items["TenantId"]?.ToString() ?? "unknown",
                userId: userId,
                action: "CANCEL_SUBSCRIPTION",
                entityType: "Subscription",
                entityId: id,
                description: $"Cancelled subscription. Reason: {reason}",
                ipAddress: HttpContext.Connection.RemoteIpAddress?.ToString(),
                userAgent: HttpContext.Request.Headers["User-Agent"].FirstOrDefault()
            );

            return Ok(new ApiResponse<object>
            {
                Success = true,
                Message = "Subscription cancelled successfully"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to cancel subscription {SubscriptionId}", id);
            return BadRequest(new ApiResponse<object>
            {
                Success = false,
                Message = "Failed to cancel subscription",
                Errors = new List<string> { ex.Message }
            });
        }
    }
}
