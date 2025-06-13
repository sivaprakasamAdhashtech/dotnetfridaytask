using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System.ComponentModel.DataAnnotations;

namespace MultiTenantBilling.Core.Entities;

public class AuditLog
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; } = string.Empty;

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

    [Required]
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    [StringLength(500)]
    public string? Description { get; set; }

    // Request information
    [StringLength(50)]
    public string? IpAddress { get; set; }

    [StringLength(500)]
    public string? UserAgent { get; set; }

    // Additional context data
    public Dictionary<string, object> Metadata { get; set; } = new();

    // Changes tracking
    public Dictionary<string, object>? OldValues { get; set; }
    public Dictionary<string, object>? NewValues { get; set; }

    // Severity level
    [StringLength(20)]
    public string Severity { get; set; } = "Info"; // Info, Warning, Error, Critical

    // Correlation ID for tracking related operations
    [StringLength(100)]
    public string? CorrelationId { get; set; }
}
