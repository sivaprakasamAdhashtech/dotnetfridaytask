using System.ComponentModel.DataAnnotations;

namespace MultiTenantBilling.Core.DTOs;

public class AuditLogDto
{
    public string Id { get; set; } = string.Empty;
    public string TenantId { get; set; } = string.Empty;
    public string UserId { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty;
    public string EntityType { get; set; } = string.Empty;
    public string? EntityId { get; set; }
    public DateTime Timestamp { get; set; }
    public string? Description { get; set; }
    public string? IpAddress { get; set; }
    public string? UserAgent { get; set; }
    public Dictionary<string, object> Metadata { get; set; } = new();
    public Dictionary<string, object>? OldValues { get; set; }
    public Dictionary<string, object>? NewValues { get; set; }
    public string Severity { get; set; } = string.Empty;
    public string? CorrelationId { get; set; }
}

public class AuditLogQueryRequest
{
    public string? TenantId { get; set; }
    public string? UserId { get; set; }
    public string? Action { get; set; }
    public string? EntityType { get; set; }
    public string? EntityId { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public string? Severity { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 50;
}

public class CreateAuditLogRequest
{
    [Required]
    [StringLength(50)]
    public string TenantId { get; set; } = string.Empty;

    [Required]
    [StringLength(100)]
    public string UserId { get; set; } = string.Empty;

    [Required]
    [StringLength(100)]
    public string Action { get; set; } = string.Empty;

    [Required]
    [StringLength(100)]
    public string EntityType { get; set; } = string.Empty;

    [StringLength(100)]
    public string? EntityId { get; set; }

    [StringLength(500)]
    public string? Description { get; set; }

    [StringLength(50)]
    public string? IpAddress { get; set; }

    [StringLength(500)]
    public string? UserAgent { get; set; }

    public Dictionary<string, object> Metadata { get; set; } = new();

    public Dictionary<string, object>? OldValues { get; set; }

    public Dictionary<string, object>? NewValues { get; set; }

    [StringLength(20)]
    public string Severity { get; set; } = "Info";

    [StringLength(100)]
    public string? CorrelationId { get; set; }
}
