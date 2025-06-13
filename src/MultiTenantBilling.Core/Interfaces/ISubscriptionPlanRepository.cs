using MultiTenantBilling.Core.Entities;

namespace MultiTenantBilling.Core.Interfaces;

public interface ISubscriptionPlanRepository : IRepository<SubscriptionPlan>
{
    Task<IEnumerable<SubscriptionPlan>> GetActivePlansAsync();
    Task<SubscriptionPlan?> GetByPlanCodeAsync(string planCode);
}
