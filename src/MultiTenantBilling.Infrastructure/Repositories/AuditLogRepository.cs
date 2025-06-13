using MongoDB.Driver;
using MultiTenantBilling.Core.Entities;
using MultiTenantBilling.Core.DTOs;
using MultiTenantBilling.Core.Interfaces;
using MultiTenantBilling.Infrastructure.Configuration;

namespace MultiTenantBilling.Infrastructure.Repositories;

public class AuditLogRepository : BaseRepository<AuditLog>, IAuditLogRepository
{
    public AuditLogRepository(MongoDbContext context) : base(context.AuditLogs)
    {
    }

    public async Task<IEnumerable<AuditLog>> GetByTenantIdAsync(string tenantId, int page = 1, int pageSize = 50)
    {
        var filter = Builders<AuditLog>.Filter.Eq(x => x.TenantId, tenantId);
        var sort = Builders<AuditLog>.Sort.Descending(x => x.Timestamp);
        var skip = (page - 1) * pageSize;
        
        return await _collection
            .Find(filter)
            .Sort(sort)
            .Skip(skip)
            .Limit(pageSize)
            .ToListAsync();
    }

    public async Task<IEnumerable<AuditLog>> GetByUserIdAsync(string userId, int page = 1, int pageSize = 50)
    {
        var filter = Builders<AuditLog>.Filter.Eq(x => x.UserId, userId);
        var sort = Builders<AuditLog>.Sort.Descending(x => x.Timestamp);
        var skip = (page - 1) * pageSize;
        
        return await _collection
            .Find(filter)
            .Sort(sort)
            .Skip(skip)
            .Limit(pageSize)
            .ToListAsync();
    }

    public async Task<IEnumerable<AuditLog>> SearchAsync(AuditLogQueryRequest query)
    {
        var filterBuilder = Builders<AuditLog>.Filter;
        var filters = new List<FilterDefinition<AuditLog>>();

        if (!string.IsNullOrEmpty(query.TenantId))
            filters.Add(filterBuilder.Eq(x => x.TenantId, query.TenantId));

        if (!string.IsNullOrEmpty(query.UserId))
            filters.Add(filterBuilder.Eq(x => x.UserId, query.UserId));

        if (!string.IsNullOrEmpty(query.Action))
            filters.Add(filterBuilder.Eq(x => x.Action, query.Action));

        if (!string.IsNullOrEmpty(query.EntityType))
            filters.Add(filterBuilder.Eq(x => x.EntityType, query.EntityType));

        if (!string.IsNullOrEmpty(query.EntityId))
            filters.Add(filterBuilder.Eq(x => x.EntityId, query.EntityId));

        if (!string.IsNullOrEmpty(query.Severity))
            filters.Add(filterBuilder.Eq(x => x.Severity, query.Severity));

        if (query.StartDate.HasValue)
            filters.Add(filterBuilder.Gte(x => x.Timestamp, query.StartDate.Value));

        if (query.EndDate.HasValue)
            filters.Add(filterBuilder.Lte(x => x.Timestamp, query.EndDate.Value));

        var filter = filters.Count > 0 ? filterBuilder.And(filters) : filterBuilder.Empty;
        var sort = Builders<AuditLog>.Sort.Descending(x => x.Timestamp);
        var skip = (query.Page - 1) * query.PageSize;

        return await _collection
            .Find(filter)
            .Sort(sort)
            .Skip(skip)
            .Limit(query.PageSize)
            .ToListAsync();
    }

    public async Task<long> GetCountByTenantAsync(string tenantId)
    {
        var filter = Builders<AuditLog>.Filter.Eq(x => x.TenantId, tenantId);
        return await _collection.CountDocumentsAsync(filter);
    }

    public async Task CleanupOldLogsAsync(DateTime cutoffDate)
    {
        var filter = Builders<AuditLog>.Filter.Lt(x => x.Timestamp, cutoffDate);
        await _collection.DeleteManyAsync(filter);
    }
}
