using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MultiTenantBilling.Core.Enums;
using System.ComponentModel.DataAnnotations;

namespace MultiTenantBilling.Core.Entities;

public class SubscriptionPlan
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; } = string.Empty;

    [Required]
    [StringLength(100)]
    public string Name { get; set; } = string.Empty;

    [StringLength(500)]
    public string? Description { get; set; }

    [Required]
    [Range(0.01, double.MaxValue, ErrorMessage = "Price must be greater than 0")]
    public decimal Price { get; set; }

    [Required]
    public BillingCycle BillingCycle { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    [StringLength(100)]
    public string? CreatedBy { get; set; }

    [StringLength(100)]
    public string? UpdatedBy { get; set; }

    // Plan features and limits
    public Dictionary<string, object> Features { get; set; } = new();

    // Usage limits
    public int? MaxUsers { get; set; }
    public long? MaxStorageGB { get; set; }
    public int? MaxApiCallsPerMonth { get; set; }

    // Trial settings
    public int? TrialDays { get; set; }

    // Plan metadata
    [StringLength(50)]
    public string? PlanCode { get; set; } // External system reference

    public int SortOrder { get; set; } = 0;
}
