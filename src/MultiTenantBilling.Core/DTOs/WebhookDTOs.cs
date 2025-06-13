using System.ComponentModel.DataAnnotations;

namespace MultiTenantBilling.Core.DTOs;

public class StripeWebhookRequest
{
    [Required]
    public string Event { get; set; } = string.Empty;

    [Required]
    public string InvoiceId { get; set; } = string.Empty;

    [Required]
    [Range(0.01, double.MaxValue)]
    public decimal AmountPaid { get; set; }

    [Required]
    public string TenantId { get; set; } = string.Empty;

    public string? PaymentMethod { get; set; }
    public string? TransactionId { get; set; }
    public string? Currency { get; set; } = "USD";
    public DateTime? PaymentDate { get; set; }
    public Dictionary<string, object> Metadata { get; set; } = new();
}

public class WebhookResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public string? ProcessedAt { get; set; }
    public string? CorrelationId { get; set; }
}
