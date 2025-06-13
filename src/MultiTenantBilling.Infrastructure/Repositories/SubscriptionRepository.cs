using MongoDB.Driver;
using MultiTenantBilling.Core.Entities;
using MultiTenantBilling.Core.Enums;
using MultiTenantBilling.Core.Interfaces;
using MultiTenantBilling.Infrastructure.Configuration;

namespace MultiTenantBilling.Infrastructure.Repositories;

public class SubscriptionRepository : BaseRepository<Subscription>, ISubscriptionRepository
{
    public SubscriptionRepository(MongoDbContext context) : base(context.Subscriptions)
    {
    }

    public async Task<IEnumerable<Subscription>> GetByTenantIdAsync(string tenantId)
    {
        var filter = Builders<Subscription>.Filter.Eq(x => x.TenantId, tenantId);
        var sort = Builders<Subscription>.Sort.Descending(x => x.CreatedAt);
        return await _collection.Find(filter).Sort(sort).ToListAsync();
    }

    public async Task<Subscription?> GetActiveSubscriptionByTenantAsync(string tenantId)
    {
        var filter = Builders<Subscription>.Filter.And(
            Builders<Subscription>.Filter.Eq(x => x.TenantId, tenantId),
            Builders<Subscription>.Filter.Eq(x => x.Status, SubscriptionStatus.Active)
        );
        return await _collection.Find(filter).FirstOrDefaultAsync();
    }

    public async Task<IEnumerable<Subscription>> GetByStatusAsync(SubscriptionStatus status)
    {
        var filter = Builders<Subscription>.Filter.Eq(x => x.Status, status);
        return await _collection.Find(filter).ToListAsync();
    }

    public async Task<IEnumerable<Subscription>> GetSubscriptionsDueForBillingAsync(DateTime date)
    {
        var filter = Builders<Subscription>.Filter.And(
            Builders<Subscription>.Filter.Eq(x => x.Status, SubscriptionStatus.Active),
            Builders<Subscription>.Filter.Lte(x => x.NextBillingDate, date)
        );
        return await _collection.Find(filter).ToListAsync();
    }

    public async Task<IEnumerable<Subscription>> GetExpiredTrialsAsync()
    {
        var now = DateTime.UtcNow;
        var filter = Builders<Subscription>.Filter.And(
            Builders<Subscription>.Filter.Eq(x => x.IsTrialActive, true),
            Builders<Subscription>.Filter.Lte(x => x.TrialEndDate, now)
        );
        return await _collection.Find(filter).ToListAsync();
    }
}
