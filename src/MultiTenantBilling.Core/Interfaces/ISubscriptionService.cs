using MultiTenantBilling.Core.DTOs;

namespace MultiTenantBilling.Core.Interfaces;

public interface ISubscriptionService
{
    Task<SubscriptionDto> CreateSubscriptionAsync(CreateSubscriptionRequest request, string createdBy);
    Task<SubscriptionDto> UpdateSubscriptionAsync(string subscriptionId, UpdateSubscriptionRequest request, string updatedBy);
    Task<SubscriptionDto?> GetSubscriptionAsync(string subscriptionId);
    Task<IEnumerable<SubscriptionDto>> GetSubscriptionsByTenantAsync(string tenantId);
    Task<SubscriptionDto?> GetActiveSubscriptionByTenantAsync(string tenantId);
    Task<bool> CancelSubscriptionAsync(string subscriptionId, string reason, string cancelledBy);
    Task<IEnumerable<SubscriptionPlanDto>> GetAvailablePlansAsync();
    Task ProcessSubscriptionBillingAsync();
}
