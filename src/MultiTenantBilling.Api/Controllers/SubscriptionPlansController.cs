using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MultiTenantBilling.Core.DTOs;
using MultiTenantBilling.Core.Interfaces;

namespace MultiTenantBilling.Api.Controllers;

[ApiController]
[Route("api/subscription-plans")]
[Authorize]
public class SubscriptionPlansController : ControllerBase
{
    private readonly ISubscriptionService _subscriptionService;
    private readonly ILogger<SubscriptionPlansController> _logger;

    public SubscriptionPlansController(
        ISubscriptionService subscriptionService,
        ILogger<SubscriptionPlansController> logger)
    {
        _subscriptionService = subscriptionService;
        _logger = logger;
    }

    /// <summary>
    /// Get all available subscription plans
    /// </summary>
    /// <returns>List of available subscription plans</returns>
    [HttpGet]
    public async Task<ActionResult<ApiResponse<IEnumerable<SubscriptionPlanDto>>>> GetAvailablePlans()
    {
        try
        {
            var plans = await _subscriptionService.GetAvailablePlansAsync();
            
            return Ok(new ApiResponse<IEnumerable<SubscriptionPlanDto>>
            {
                Success = true,
                Message = "Subscription plans retrieved successfully",
                Data = plans
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve subscription plans");
            return BadRequest(new ApiResponse<IEnumerable<SubscriptionPlanDto>>
            {
                Success = false,
                Message = "Failed to retrieve subscription plans",
                Errors = new List<string> { ex.Message }
            });
        }
    }
}
