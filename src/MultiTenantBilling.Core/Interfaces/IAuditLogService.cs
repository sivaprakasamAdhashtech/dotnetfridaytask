using MultiTenantBilling.Core.DTOs;

namespace MultiTenantBilling.Core.Interfaces;

public interface IAuditLogService
{
    Task LogAsync(CreateAuditLogRequest request);
    Task LogAsync(string tenantId, string userId, string action, string entityType, string? entityId = null, 
        string? description = null, Dictionary<string, object>? oldValues = null, 
        Dictionary<string, object>? newValues = null, string severity = "Info", 
        string? correlationId = null, string? ipAddress = null, string? userAgent = null);
    Task<IEnumerable<AuditLogDto>> GetAuditLogsAsync(AuditLogQueryRequest query);
    Task<long> GetAuditLogCountAsync(string tenantId);
    Task CleanupOldLogsAsync(int retentionDays = 90);
}
