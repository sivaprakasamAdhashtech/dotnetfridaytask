using MongoDB.Driver;
using MultiTenantBilling.Core.Entities;
using MultiTenantBilling.Core.Interfaces;
using MultiTenantBilling.Infrastructure.Configuration;

namespace MultiTenantBilling.Infrastructure.Repositories;

public class TenantRepository : BaseRepository<Tenant>, ITenantRepository
{
    public TenantRepository(MongoDbContext context) : base(context.Tenants)
    {
    }

    public async Task<Tenant?> GetByTenantIdAsync(string tenantId)
    {
        var filter = Builders<Tenant>.Filter.Eq(x => x.TenantId, tenantId);
        return await _collection.Find(filter).FirstOrDefaultAsync();
    }

    public async Task<bool> TenantIdExistsAsync(string tenantId)
    {
        var filter = Builders<Tenant>.Filter.Eq(x => x.TenantId, tenantId);
        var count = await _collection.CountDocumentsAsync(filter);
        return count > 0;
    }

    public async Task<IEnumerable<Tenant>> GetActiveTenants()
    {
        var filter = Builders<Tenant>.Filter.Eq(x => x.IsActive, true);
        return await _collection.Find(filter).ToListAsync();
    }
}
