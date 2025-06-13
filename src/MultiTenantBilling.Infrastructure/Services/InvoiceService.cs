using MultiTenantBilling.Core.DTOs;
using MultiTenantBilling.Core.Entities;
using MultiTenantBilling.Core.Enums;
using MultiTenantBilling.Core.Exceptions;
using MultiTenantBilling.Core.Interfaces;

namespace MultiTenantBilling.Infrastructure.Services;

public class InvoiceService : IInvoiceService
{
    private readonly IInvoiceRepository _invoiceRepository;
    private readonly ISubscriptionRepository _subscriptionRepository;
    private readonly ISubscriptionPlanRepository _subscriptionPlanRepository;
    private readonly IAuditLogService _auditLogService;

    public InvoiceService(
        IInvoiceRepository invoiceRepository,
        ISubscriptionRepository subscriptionRepository,
        ISubscriptionPlanRepository subscriptionPlanRepository,
        IAuditLogService auditLogService)
    {
        _invoiceRepository = invoiceRepository;
        _subscriptionRepository = subscriptionRepository;
        _subscriptionPlanRepository = subscriptionPlanRepository;
        _auditLogService = auditLogService;
    }

    public async Task<InvoiceDto> CreateInvoiceAsync(CreateInvoiceRequest request, string createdBy)
    {
        var invoiceNumber = await _invoiceRepository.GenerateInvoiceNumberAsync();
        
        var invoice = new Invoice
        {
            InvoiceNumber = invoiceNumber,
            TenantId = request.TenantId,
            SubscriptionId = request.SubscriptionId,
            Status = InvoiceStatus.Pending,
            Amount = request.Amount,
            TaxAmount = request.TaxAmount,
            TotalAmount = request.Amount + request.TaxAmount,
            Currency = request.Currency,
            IssueDate = DateTime.UtcNow,
            DueDate = request.DueDate,
            Notes = request.Notes,
            CreatedBy = createdBy,
            UpdatedBy = createdBy,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            LineItems = request.LineItems.Select(item => new InvoiceLineItem
            {
                Description = item.Description,
                Quantity = item.Quantity,
                UnitPrice = item.UnitPrice,
                TotalPrice = item.Quantity * item.UnitPrice,
                PeriodStart = item.PeriodStart,
                PeriodEnd = item.PeriodEnd
            }).ToList()
        };

        await _invoiceRepository.CreateAsync(invoice);

        return MapToDto(invoice);
    }

    public async Task<InvoiceDto> UpdateInvoiceStatusAsync(string invoiceId, UpdateInvoiceStatusRequest request, string updatedBy)
    {
        var invoice = await _invoiceRepository.GetByIdAsync(invoiceId);
        if (invoice == null)
        {
            throw new NotFoundException("Invoice", invoiceId);
        }

        invoice.Status = request.Status;
        invoice.PaymentMethod = request.PaymentMethod;
        invoice.PaymentTransactionId = request.PaymentTransactionId;
        invoice.Notes = request.Notes;
        invoice.UpdatedBy = updatedBy;
        invoice.UpdatedAt = DateTime.UtcNow;

        if (request.Status == InvoiceStatus.Paid)
        {
            invoice.PaidDate = DateTime.UtcNow;
        }

        await _invoiceRepository.UpdateAsync(invoice);

        return MapToDto(invoice);
    }

    public async Task<InvoiceDto?> GetInvoiceAsync(string invoiceId)
    {
        var invoice = await _invoiceRepository.GetByIdAsync(invoiceId);
        return invoice != null ? MapToDto(invoice) : null;
    }

    public async Task<IEnumerable<InvoiceDto>> GetInvoicesByTenantAsync(string tenantId)
    {
        var invoices = await _invoiceRepository.GetByTenantIdAsync(tenantId);
        return invoices.Select(MapToDto);
    }

    public async Task<IEnumerable<InvoiceDto>> GetInvoicesBySubscriptionAsync(string subscriptionId)
    {
        var invoices = await _invoiceRepository.GetBySubscriptionIdAsync(subscriptionId);
        return invoices.Select(MapToDto);
    }

    public async Task<InvoiceDto> GenerateInvoiceForSubscriptionAsync(string subscriptionId, string createdBy)
    {
        var subscription = await _subscriptionRepository.GetByIdAsync(subscriptionId);
        if (subscription == null)
        {
            throw new NotFoundException("Subscription", subscriptionId);
        }

        var plan = await _subscriptionPlanRepository.GetByIdAsync(subscription.SubscriptionPlanId);
        if (plan == null)
        {
            throw new NotFoundException("SubscriptionPlan", subscription.SubscriptionPlanId);
        }

        // Skip billing if still in trial
        if (subscription.IsTrialActive && subscription.TrialEndDate > DateTime.UtcNow)
        {
            throw new BusinessException("Cannot generate invoice during trial period");
        }

        var invoiceNumber = await _invoiceRepository.GenerateInvoiceNumberAsync();
        var billingPeriodStart = subscription.LastBilledDate ?? subscription.StartDate;
        var billingPeriodEnd = subscription.NextBillingDate;

        var invoice = new Invoice
        {
            InvoiceNumber = invoiceNumber,
            TenantId = subscription.TenantId,
            SubscriptionId = subscriptionId,
            Status = InvoiceStatus.Pending,
            Amount = subscription.CurrentPrice,
            TaxAmount = CalculateTax(subscription.CurrentPrice),
            TotalAmount = subscription.CurrentPrice + CalculateTax(subscription.CurrentPrice),
            Currency = "USD",
            IssueDate = DateTime.UtcNow,
            DueDate = DateTime.UtcNow.AddDays(30), // 30 days to pay
            CreatedBy = createdBy,
            UpdatedBy = createdBy,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            LineItems = new List<InvoiceLineItem>
            {
                new InvoiceLineItem
                {
                    Description = $"{plan.Name} - {plan.BillingCycle} Subscription",
                    Quantity = 1,
                    UnitPrice = subscription.CurrentPrice,
                    TotalPrice = subscription.CurrentPrice,
                    PeriodStart = billingPeriodStart,
                    PeriodEnd = billingPeriodEnd
                }
            }
        };

        await _invoiceRepository.CreateAsync(invoice);

        await _auditLogService.LogAsync(
            tenantId: subscription.TenantId,
            userId: createdBy,
            action: "GENERATE_INVOICE",
            entityType: "Invoice",
            entityId: invoice.Id,
            description: $"Generated invoice {invoice.InvoiceNumber} for subscription {subscriptionId}"
        );

        return MapToDto(invoice);
    }

    public async Task ProcessOverdueInvoicesAsync()
    {
        var overdueInvoices = await _invoiceRepository.GetOverdueInvoicesAsync();

        foreach (var invoice in overdueInvoices)
        {
            try
            {
                invoice.Status = InvoiceStatus.Overdue;
                invoice.UpdatedAt = DateTime.UtcNow;
                await _invoiceRepository.UpdateAsync(invoice);

                await _auditLogService.LogAsync(
                    tenantId: invoice.TenantId,
                    userId: "system",
                    action: "MARK_OVERDUE",
                    entityType: "Invoice",
                    entityId: invoice.Id,
                    description: $"Marked invoice {invoice.InvoiceNumber} as overdue",
                    severity: "Warning"
                );
            }
            catch (Exception ex)
            {
                await _auditLogService.LogAsync(
                    tenantId: invoice.TenantId,
                    userId: "system",
                    action: "OVERDUE_ERROR",
                    entityType: "Invoice",
                    entityId: invoice.Id,
                    description: $"Failed to mark invoice as overdue: {ex.Message}",
                    severity: "Error"
                );
            }
        }
    }

    public async Task ProcessFailedPaymentRetriesAsync()
    {
        var invoicesForRetry = await _invoiceRepository.GetInvoicesDueForRetryAsync();

        foreach (var invoice in invoicesForRetry)
        {
            try
            {
                // Simulate payment retry logic
                // In a real implementation, this would integrate with payment processors
                
                invoice.PaymentRetryCount++;
                invoice.NextRetryDate = DateTime.UtcNow.AddDays(Math.Pow(2, invoice.PaymentRetryCount)); // Exponential backoff
                invoice.UpdatedAt = DateTime.UtcNow;

                if (invoice.PaymentRetryCount >= 3)
                {
                    invoice.Status = InvoiceStatus.Failed;
                    invoice.NextRetryDate = null;
                }

                await _invoiceRepository.UpdateAsync(invoice);

                await _auditLogService.LogAsync(
                    tenantId: invoice.TenantId,
                    userId: "system",
                    action: "PAYMENT_RETRY",
                    entityType: "Invoice",
                    entityId: invoice.Id,
                    description: $"Payment retry attempt {invoice.PaymentRetryCount} for invoice {invoice.InvoiceNumber}"
                );
            }
            catch (Exception ex)
            {
                await _auditLogService.LogAsync(
                    tenantId: invoice.TenantId,
                    userId: "system",
                    action: "RETRY_ERROR",
                    entityType: "Invoice",
                    entityId: invoice.Id,
                    description: $"Failed to process payment retry: {ex.Message}",
                    severity: "Error"
                );
            }
        }
    }

    private decimal CalculateTax(decimal amount)
    {
        // Simple tax calculation - 10%
        // In a real implementation, this would be based on tenant location and tax rules
        return Math.Round(amount * 0.10m, 2);
    }

    private static InvoiceDto MapToDto(Invoice invoice)
    {
        return new InvoiceDto
        {
            Id = invoice.Id,
            InvoiceNumber = invoice.InvoiceNumber,
            TenantId = invoice.TenantId,
            SubscriptionId = invoice.SubscriptionId,
            Status = invoice.Status,
            Amount = invoice.Amount,
            TaxAmount = invoice.TaxAmount,
            TotalAmount = invoice.TotalAmount,
            Currency = invoice.Currency,
            IssueDate = invoice.IssueDate,
            DueDate = invoice.DueDate,
            PaidDate = invoice.PaidDate,
            CreatedAt = invoice.CreatedAt,
            PaymentMethod = invoice.PaymentMethod,
            PaymentTransactionId = invoice.PaymentTransactionId,
            ExternalInvoiceId = invoice.ExternalInvoiceId,
            Notes = invoice.Notes,
            PaymentRetryCount = invoice.PaymentRetryCount,
            NextRetryDate = invoice.NextRetryDate,
            LineItems = invoice.LineItems.Select(item => new InvoiceLineItemDto
            {
                Description = item.Description,
                Quantity = item.Quantity,
                UnitPrice = item.UnitPrice,
                TotalPrice = item.TotalPrice,
                PeriodStart = item.PeriodStart,
                PeriodEnd = item.PeriodEnd
            }).ToList()
        };
    }
}
