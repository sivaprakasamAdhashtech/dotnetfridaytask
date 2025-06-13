using MultiTenantBilling.Core.Enums;
using System.ComponentModel.DataAnnotations;

namespace MultiTenantBilling.Core.DTOs;

public class SubscriptionPlanDto
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public decimal Price { get; set; }
    public BillingCycle BillingCycle { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public Dictionary<string, object> Features { get; set; } = new();
    public int? MaxUsers { get; set; }
    public long? MaxStorageGB { get; set; }
    public int? MaxApiCallsPerMonth { get; set; }
    public int? TrialDays { get; set; }
    public string? PlanCode { get; set; }
    public int SortOrder { get; set; }
}

public class CreateSubscriptionRequest
{
    [Required]
    [StringLength(50)]
    public string TenantId { get; set; } = string.Empty;

    [Required]
    public string SubscriptionPlanId { get; set; } = string.Empty;

    [Required]
    public DateTime StartDate { get; set; }

    public DateTime? EndDate { get; set; }

    public bool StartTrial { get; set; } = false;

    public Dictionary<string, object> Metadata { get; set; } = new();
}

public class UpdateSubscriptionRequest
{
    [Required]
    public SubscriptionStatus Status { get; set; }

    public DateTime? EndDate { get; set; }

    [StringLength(500)]
    public string? CancellationReason { get; set; }

    public Dictionary<string, object> Metadata { get; set; } = new();
}

public class SubscriptionDto
{
    public string Id { get; set; } = string.Empty;
    public string TenantId { get; set; } = string.Empty;
    public string SubscriptionPlanId { get; set; } = string.Empty;
    public SubscriptionStatus Status { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public DateTime NextBillingDate { get; set; }
    public decimal CurrentPrice { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public bool IsTrialActive { get; set; }
    public DateTime? TrialEndDate { get; set; }
    public DateTime? LastBilledDate { get; set; }
    public int BillingCycleCount { get; set; }
    public DateTime? CancelledAt { get; set; }
    public string? CancelledBy { get; set; }
    public string? CancellationReason { get; set; }
    public string? ExternalSubscriptionId { get; set; }
    public Dictionary<string, object> Metadata { get; set; } = new();
    public SubscriptionPlanDto? Plan { get; set; }
}
