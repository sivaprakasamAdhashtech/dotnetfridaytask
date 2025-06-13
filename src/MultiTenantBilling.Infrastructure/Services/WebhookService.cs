using Microsoft.Extensions.Configuration;
using MultiTenantBilling.Core.DTOs;
using MultiTenantBilling.Core.Enums;
using MultiTenantBilling.Core.Interfaces;
using System.Security.Cryptography;
using System.Text;

namespace MultiTenantBilling.Infrastructure.Services;

public class WebhookService : IWebhookService
{
    private readonly IInvoiceRepository _invoiceRepository;
    private readonly IAuditLogService _auditLogService;
    private readonly IConfiguration _configuration;

    public WebhookService(
        IInvoiceRepository invoiceRepository,
        IAuditLogService auditLogService,
        IConfiguration configuration)
    {
        _invoiceRepository = invoiceRepository;
        _auditLogService = auditLogService;
        _configuration = configuration;
    }

    public async Task<WebhookResponse> ProcessStripeWebhookAsync(StripeWebhookRequest request, string signature)
    {
        var correlationId = Guid.NewGuid().ToString();

        try
        {
            // Validate signature (simulated)
            if (!await ValidateStripeSignatureAsync(request.ToString(), signature))
            {
                await _auditLogService.LogAsync(
                    tenantId: request.TenantId,
                    userId: "webhook",
                    action: "WEBHOOK_SIGNATURE_INVALID",
                    entityType: "Webhook",
                    description: "Invalid webhook signature",
                    severity: "Warning",
                    correlationId: correlationId
                );

                return new WebhookResponse
                {
                    Success = false,
                    Message = "Invalid signature",
                    CorrelationId = correlationId
                };
            }

            // Find the invoice
            var invoice = await _invoiceRepository.GetByInvoiceNumberAsync(request.InvoiceId);
            if (invoice == null)
            {
                await _auditLogService.LogAsync(
                    tenantId: request.TenantId,
                    userId: "webhook",
                    action: "WEBHOOK_INVOICE_NOT_FOUND",
                    entityType: "Webhook",
                    description: $"Invoice not found: {request.InvoiceId}",
                    severity: "Warning",
                    correlationId: correlationId
                );

                return new WebhookResponse
                {
                    Success = false,
                    Message = "Invoice not found",
                    CorrelationId = correlationId
                };
            }

            // Verify tenant matches
            if (invoice.TenantId != request.TenantId)
            {
                await _auditLogService.LogAsync(
                    tenantId: request.TenantId,
                    userId: "webhook",
                    action: "WEBHOOK_TENANT_MISMATCH",
                    entityType: "Webhook",
                    description: $"Tenant mismatch for invoice {request.InvoiceId}",
                    severity: "Error",
                    correlationId: correlationId
                );

                return new WebhookResponse
                {
                    Success = false,
                    Message = "Tenant mismatch",
                    CorrelationId = correlationId
                };
            }

            // Process the webhook event
            switch (request.Event.ToLower())
            {
                case "invoice.paid":
                    await ProcessInvoicePaidEvent(invoice, request, correlationId);
                    break;

                case "payment_failed":
                case "invoice.payment_failed":
                    await ProcessPaymentFailedEvent(invoice, request, correlationId);
                    break;

                default:
                    await _auditLogService.LogAsync(
                        tenantId: request.TenantId,
                        userId: "webhook",
                        action: "WEBHOOK_UNKNOWN_EVENT",
                        entityType: "Webhook",
                        description: $"Unknown webhook event: {request.Event}",
                        severity: "Warning",
                        correlationId: correlationId
                    );

                    return new WebhookResponse
                    {
                        Success = false,
                        Message = "Unknown event type",
                        CorrelationId = correlationId
                    };
            }

            return new WebhookResponse
            {
                Success = true,
                Message = "Webhook processed successfully",
                ProcessedAt = DateTime.UtcNow.ToString("O"),
                CorrelationId = correlationId
            };
        }
        catch (Exception ex)
        {
            await _auditLogService.LogAsync(
                tenantId: request.TenantId,
                userId: "webhook",
                action: "WEBHOOK_ERROR",
                entityType: "Webhook",
                description: $"Webhook processing error: {ex.Message}",
                severity: "Error",
                correlationId: correlationId
            );

            return new WebhookResponse
            {
                Success = false,
                Message = "Internal processing error",
                CorrelationId = correlationId
            };
        }
    }

    public async Task<bool> ValidateStripeSignatureAsync(string payload, string signature)
    {
        // Simulated signature validation
        // In a real implementation, this would use Stripe's webhook secret
        var webhookSecret = _configuration["StripeSettings:WebhookSecret"] ?? "whsec_test_secret";
        
        try
        {
            var expectedSignature = ComputeHmacSha256(payload, webhookSecret);
            return signature.Equals($"sha256={expectedSignature}", StringComparison.OrdinalIgnoreCase);
        }
        catch
        {
            return false;
        }
    }

    private async Task ProcessInvoicePaidEvent(Core.Entities.Invoice invoice, StripeWebhookRequest request, string correlationId)
    {
        // Update invoice status
        invoice.Status = InvoiceStatus.Paid;
        invoice.PaidDate = request.PaymentDate ?? DateTime.UtcNow;
        invoice.PaymentMethod = request.PaymentMethod ?? "stripe";
        invoice.PaymentTransactionId = request.TransactionId;
        invoice.UpdatedAt = DateTime.UtcNow;

        await _invoiceRepository.UpdateAsync(invoice);

        await _auditLogService.LogAsync(
            tenantId: invoice.TenantId,
            userId: "webhook",
            action: "INVOICE_PAID",
            entityType: "Invoice",
            entityId: invoice.Id,
            description: $"Invoice {invoice.InvoiceNumber} marked as paid via webhook. Amount: {request.AmountPaid:C}",
            correlationId: correlationId,
            newValues: new Dictionary<string, object>
            {
                ["Status"] = "Paid",
                ["PaidDate"] = invoice.PaidDate,
                ["PaymentMethod"] = invoice.PaymentMethod ?? "",
                ["TransactionId"] = invoice.PaymentTransactionId ?? "",
                ["AmountPaid"] = request.AmountPaid
            }
        );
    }

    private async Task ProcessPaymentFailedEvent(Core.Entities.Invoice invoice, StripeWebhookRequest request, string correlationId)
    {
        // Update invoice status
        invoice.Status = InvoiceStatus.Failed;
        invoice.PaymentRetryCount++;
        invoice.NextRetryDate = DateTime.UtcNow.AddDays(Math.Pow(2, invoice.PaymentRetryCount)); // Exponential backoff
        invoice.UpdatedAt = DateTime.UtcNow;

        // If max retries reached, mark as permanently failed
        if (invoice.PaymentRetryCount >= 3)
        {
            invoice.NextRetryDate = null;
        }

        await _invoiceRepository.UpdateAsync(invoice);

        await _auditLogService.LogAsync(
            tenantId: invoice.TenantId,
            userId: "webhook",
            action: "PAYMENT_FAILED",
            entityType: "Invoice",
            entityId: invoice.Id,
            description: $"Payment failed for invoice {invoice.InvoiceNumber}. Retry count: {invoice.PaymentRetryCount}",
            severity: "Warning",
            correlationId: correlationId,
            newValues: new Dictionary<string, object>
            {
                ["Status"] = "Failed",
                ["PaymentRetryCount"] = invoice.PaymentRetryCount,
                ["NextRetryDate"] = invoice.NextRetryDate?.ToString("O") ?? "null"
            }
        );
    }

    private string ComputeHmacSha256(string data, string secret)
    {
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret));
        var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(data));
        return Convert.ToHexString(hash).ToLower();
    }
}
