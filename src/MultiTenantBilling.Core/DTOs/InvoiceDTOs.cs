using MultiTenantBilling.Core.Enums;
using System.ComponentModel.DataAnnotations;

namespace MultiTenantBilling.Core.DTOs;

public class InvoiceDto
{
    public string Id { get; set; } = string.Empty;
    public string InvoiceNumber { get; set; } = string.Empty;
    public string TenantId { get; set; } = string.Empty;
    public string SubscriptionId { get; set; } = string.Empty;
    public InvoiceStatus Status { get; set; }
    public decimal Amount { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal TotalAmount { get; set; }
    public string Currency { get; set; } = string.Empty;
    public DateTime IssueDate { get; set; }
    public DateTime DueDate { get; set; }
    public DateTime? PaidDate { get; set; }
    public DateTime CreatedAt { get; set; }
    public string? PaymentMethod { get; set; }
    public string? PaymentTransactionId { get; set; }
    public string? ExternalInvoiceId { get; set; }
    public List<InvoiceLineItemDto> LineItems { get; set; } = new();
    public string? Notes { get; set; }
    public int PaymentRetryCount { get; set; }
    public DateTime? NextRetryDate { get; set; }
}

public class InvoiceLineItemDto
{
    public string Description { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal TotalPrice { get; set; }
    public DateTime? PeriodStart { get; set; }
    public DateTime? PeriodEnd { get; set; }
}

public class CreateInvoiceRequest
{
    [Required]
    [StringLength(50)]
    public string TenantId { get; set; } = string.Empty;

    [Required]
    public string SubscriptionId { get; set; } = string.Empty;

    [Required]
    [Range(0.01, double.MaxValue)]
    public decimal Amount { get; set; }

    [Range(0, double.MaxValue)]
    public decimal TaxAmount { get; set; } = 0;

    [Required]
    [StringLength(3)]
    public string Currency { get; set; } = "USD";

    [Required]
    public DateTime DueDate { get; set; }

    public List<CreateInvoiceLineItemRequest> LineItems { get; set; } = new();

    [StringLength(1000)]
    public string? Notes { get; set; }
}

public class CreateInvoiceLineItemRequest
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

    public DateTime? PeriodStart { get; set; }
    public DateTime? PeriodEnd { get; set; }
}

public class UpdateInvoiceStatusRequest
{
    [Required]
    public InvoiceStatus Status { get; set; }

    [StringLength(100)]
    public string? PaymentMethod { get; set; }

    [StringLength(100)]
    public string? PaymentTransactionId { get; set; }

    [StringLength(1000)]
    public string? Notes { get; set; }
}
