using MongoDB.Driver;
using MultiTenantBilling.Core.Entities;
using MultiTenantBilling.Core.Enums;
using MultiTenantBilling.Core.Interfaces;
using MultiTenantBilling.Infrastructure.Configuration;

namespace MultiTenantBilling.Infrastructure.Repositories;

public class UserRepository : BaseRepository<User>, IUserRepository
{
    public UserRepository(MongoDbContext context) : base(context.Users)
    {
    }

    public async Task<User?> GetByEmailAsync(string email)
    {
        var filter = Builders<User>.Filter.Eq(x => x.Email, email.ToLowerInvariant());
        return await _collection.Find(filter).FirstOrDefaultAsync();
    }

    public async Task<User?> GetByEmailAndTenantAsync(string email, string tenantId)
    {
        var filter = Builders<User>.Filter.And(
            Builders<User>.Filter.Eq(x => x.Email, email.ToLowerInvariant()),
            Builders<User>.Filter.Eq(x => x.TenantId, tenantId)
        );
        return await _collection.Find(filter).FirstOrDefaultAsync();
    }

    public async Task<IEnumerable<User>> GetByTenantIdAsync(string tenantId)
    {
        var filter = Builders<User>.Filter.Eq(x => x.TenantId, tenantId);
        return await _collection.Find(filter).ToListAsync();
    }

    public async Task<IEnumerable<User>> GetByRoleAsync(UserRole role)
    {
        var filter = Builders<User>.Filter.Eq(x => x.Role, role);
        return await _collection.Find(filter).ToListAsync();
    }

    public async Task<bool> EmailExistsAsync(string email, string tenantId)
    {
        var filter = Builders<User>.Filter.And(
            Builders<User>.Filter.Eq(x => x.Email, email.ToLowerInvariant()),
            Builders<User>.Filter.Eq(x => x.TenantId, tenantId)
        );
        var count = await _collection.CountDocumentsAsync(filter);
        return count > 0;
    }

    public async Task UpdateLastLoginAsync(string userId, DateTime loginTime)
    {
        var filter = Builders<User>.Filter.Eq("_id", userId);
        var update = Builders<User>.Update
            .Set(x => x.LastLoginAt, loginTime)
            .Set(x => x.UpdatedAt, DateTime.UtcNow);
        
        await _collection.UpdateOneAsync(filter, update);
    }
}
