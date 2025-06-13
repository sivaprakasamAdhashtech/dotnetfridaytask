using MultiTenantBilling.Core.DTOs;
using MultiTenantBilling.Core.Entities;
using MultiTenantBilling.Core.Interfaces;

namespace MultiTenantBilling.Infrastructure.Services;

public class AuditLogService : IAuditLogService
{
    private readonly IAuditLogRepository _auditLogRepository;

    public AuditLogService(IAuditLogRepository auditLogRepository)
    {
        _auditLogRepository = auditLogRepository;
    }

    public async Task LogAsync(CreateAuditLogRequest request)
    {
        var auditLog = new AuditLog
        {
            TenantId = request.TenantId,
            UserId = request.UserId,
            Action = request.Action,
            EntityType = request.EntityType,
            EntityId = request.EntityId,
            Description = request.Description,
            IpAddress = request.IpAddress,
            UserAgent = request.UserAgent,
            Metadata = request.Metadata,
            OldValues = request.OldValues,
            NewValues = request.NewValues,
            Severity = request.Severity,
            CorrelationId = request.CorrelationId,
            Timestamp = DateTime.UtcNow
        };

        await _auditLogRepository.CreateAsync(auditLog);
    }

    public async Task LogAsync(string tenantId, string userId, string action, string entityType, 
        string? entityId = null, string? description = null, Dictionary<string, object>? oldValues = null, 
        Dictionary<string, object>? newValues = null, string severity = "Info", 
        string? correlationId = null, string? ipAddress = null, string? userAgent = null)
    {
        var request = new CreateAuditLogRequest
        {
            TenantId = tenantId,
            UserId = userId,
            Action = action,
            EntityType = entityType,
            EntityId = entityId,
            Description = description,
            OldValues = oldValues,
            NewValues = newValues,
            Severity = severity,
            CorrelationId = correlationId,
            IpAddress = ipAddress,
            UserAgent = userAgent
        };

        await LogAsync(request);
    }

    public async Task<IEnumerable<AuditLogDto>> GetAuditLogsAsync(AuditLogQueryRequest query)
    {
        var auditLogs = await _auditLogRepository.SearchAsync(query);
        
        return auditLogs.Select(log => new AuditLogDto
        {
            Id = log.Id,
            TenantId = log.TenantId,
            UserId = log.UserId,
            Action = log.Action,
            EntityType = log.EntityType,
            EntityId = log.EntityId,
            Timestamp = log.Timestamp,
            Description = log.Description,
            IpAddress = log.IpAddress,
            UserAgent = log.UserAgent,
            Metadata = log.Metadata,
            OldValues = log.OldValues,
            NewValues = log.NewValues,
            Severity = log.Severity,
            CorrelationId = log.CorrelationId
        });
    }

    public async Task<long> GetAuditLogCountAsync(string tenantId)
    {
        return await _auditLogRepository.GetCountByTenantAsync(tenantId);
    }

    public async Task CleanupOldLogsAsync(int retentionDays = 90)
    {
        var cutoffDate = DateTime.UtcNow.AddDays(-retentionDays);
        await _auditLogRepository.CleanupOldLogsAsync(cutoffDate);
    }
}
