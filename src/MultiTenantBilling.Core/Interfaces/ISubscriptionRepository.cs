using MultiTenantBilling.Core.Entities;
using MultiTenantBilling.Core.Enums;

namespace MultiTenantBilling.Core.Interfaces;

public interface ISubscriptionRepository : IRepository<Subscription>
{
    Task<IEnumerable<Subscription>> GetByTenantIdAsync(string tenantId);
    Task<Subscription?> GetActiveSubscriptionByTenantAsync(string tenantId);
    Task<IEnumerable<Subscription>> GetByStatusAsync(SubscriptionStatus status);
    Task<IEnumerable<Subscription>> GetSubscriptionsDueForBillingAsync(DateTime date);
    Task<IEnumerable<Subscription>> GetExpiredTrialsAsync();
}
