using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MultiTenantBilling.Core.Enums;
using System.ComponentModel.DataAnnotations;

namespace MultiTenantBilling.Core.Entities;

public class Invoice
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; } = string.Empty;

    [Required]
    [StringLength(50)]
    public string InvoiceNumber { get; set; } = string.Empty;

    [Required]
    [StringLength(50)]
    public string TenantId { get; set; } = string.Empty;

    [Required]
    [BsonRepresentation(BsonType.ObjectId)]
    public string SubscriptionId { get; set; } = string.Empty;

    [Required]
    public InvoiceStatus Status { get; set; }

    [Required]
    [Range(0.01, double.MaxValue)]
    public decimal Amount { get; set; }

    [Required]
    [Range(0, double.MaxValue)]
    public decimal TaxAmount { get; set; } = 0;

    [Required]
    [Range(0.01, double.MaxValue)]
    public decimal TotalAmount { get; set; }

    [Required]
    [StringLength(3)]
    public string Currency { get; set; } = "USD";

    [Required]
    public DateTime IssueDate { get; set; }

    [Required]
    public DateTime DueDate { get; set; }

    public DateTime? PaidDate { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    [StringLength(100)]
    public string? CreatedBy { get; set; }

    [StringLength(100)]
    public string? UpdatedBy { get; set; }

    // Payment information
    [StringLength(100)]
    public string? PaymentMethod { get; set; }
    [StringLength(100)]
    public string? PaymentTransactionId { get; set; }
    [StringLength(100)]
    public string? ExternalInvoiceId { get; set; } // Stripe invoice ID

    // Invoice line items
    public List<InvoiceLineItem> LineItems { get; set; } = new();

    // Additional notes
    [StringLength(1000)]
    public string? Notes { get; set; }

    // Retry information for failed payments
    public int PaymentRetryCount { get; set; } = 0;
    public DateTime? NextRetryDate { get; set; }

    // Navigation properties (not stored in MongoDB)
    [BsonIgnore]
    public Subscription? Subscription { get; set; }
}

public class InvoiceLineItem
{
    [Required]
    [StringLength(200)]
    public string Description { get; set; } = string.Empty;

    [Required]
    [Range(1, int.MaxValue)]
    public int Quantity { get; set; } = 1;

    [Required]
    [Range(0.01, double.MaxValue)]
    public decimal UnitPrice { get; set; }

    [Required]
    [Range(0.01, double.MaxValue)]
    public decimal TotalPrice { get; set; }

    public DateTime? PeriodStart { get; set; }
    public DateTime? PeriodEnd { get; set; }
}
