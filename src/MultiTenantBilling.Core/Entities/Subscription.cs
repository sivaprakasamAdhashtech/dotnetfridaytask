using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MultiTenantBilling.Core.Enums;
using System.ComponentModel.DataAnnotations;

namespace MultiTenantBilling.Core.Entities;

public class Subscription
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; } = string.Empty;

    [Required]
    [StringLength(50)]
    public string TenantId { get; set; } = string.Empty;

    [Required]
    [BsonRepresentation(BsonType.ObjectId)]
    public string SubscriptionPlanId { get; set; } = string.Empty;

    [Required]
    public SubscriptionStatus Status { get; set; }

    [Required]
    public DateTime StartDate { get; set; }

    public DateTime? EndDate { get; set; }

    [Required]
    public DateTime NextBillingDate { get; set; }

    [Required]
    [Range(0.01, double.MaxValue)]
    public decimal CurrentPrice { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    [StringLength(100)]
    public string? CreatedBy { get; set; }

    [StringLength(100)]
    public string? UpdatedBy { get; set; }

    // Trial information
    public bool IsTrialActive { get; set; } = false;
    public DateTime? TrialEndDate { get; set; }

    // Billing information
    public DateTime? LastBilledDate { get; set; }
    public int BillingCycleCount { get; set; } = 0;

    // Cancellation information
    public DateTime? CancelledAt { get; set; }
    [StringLength(100)]
    public string? CancelledBy { get; set; }
    [StringLength(500)]
    public string? CancellationReason { get; set; }

    // External references
    [StringLength(100)]
    public string? ExternalSubscriptionId { get; set; } // Stripe, PayPal, etc.

    // Subscription metadata
    public Dictionary<string, object> Metadata { get; set; } = new();

    // Navigation property (not stored in MongoDB)
    [BsonIgnore]
    public SubscriptionPlan? Plan { get; set; }
}
