using MultiTenantBilling.Core.Entities;
using MultiTenantBilling.Core.DTOs;

namespace MultiTenantBilling.Core.Interfaces;

public interface IAuditLogRepository : IRepository<AuditLog>
{
    Task<IEnumerable<AuditLog>> GetByTenantIdAsync(string tenantId, int page = 1, int pageSize = 50);
    Task<IEnumerable<AuditLog>> GetByUserIdAsync(string userId, int page = 1, int pageSize = 50);
    Task<IEnumerable<AuditLog>> SearchAsync(AuditLogQueryRequest query);
    Task<long> GetCountByTenantAsync(string tenantId);
    Task CleanupOldLogsAsync(DateTime cutoffDate);
}
