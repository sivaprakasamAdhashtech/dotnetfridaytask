using MongoDB.Driver;
using MultiTenantBilling.Core.Entities;
using MultiTenantBilling.Core.Interfaces;
using MultiTenantBilling.Infrastructure.Configuration;

namespace MultiTenantBilling.Infrastructure.Repositories;

public class SubscriptionPlanRepository : BaseRepository<SubscriptionPlan>, ISubscriptionPlanRepository
{
    public SubscriptionPlanRepository(MongoDbContext context) : base(context.SubscriptionPlans)
    {
    }

    public async Task<IEnumerable<SubscriptionPlan>> GetActivePlansAsync()
    {
        var filter = Builders<SubscriptionPlan>.Filter.Eq(x => x.IsActive, true);
        var sort = Builders<SubscriptionPlan>.Sort.Ascending(x => x.SortOrder).Ascending(x => x.Price);
        return await _collection.Find(filter).Sort(sort).ToListAsync();
    }

    public async Task<SubscriptionPlan?> GetByPlanCodeAsync(string planCode)
    {
        var filter = Builders<SubscriptionPlan>.Filter.Eq(x => x.PlanCode, planCode);
        return await _collection.Find(filter).FirstOrDefaultAsync();
    }
}
