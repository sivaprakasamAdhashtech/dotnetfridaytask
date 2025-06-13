using MultiTenantBilling.Core.DTOs;

namespace MultiTenantBilling.Core.Interfaces;

public interface IInvoiceService
{
    Task<InvoiceDto> CreateInvoiceAsync(CreateInvoiceRequest request, string createdBy);
    Task<InvoiceDto> UpdateInvoiceStatusAsync(string invoiceId, UpdateInvoiceStatusRequest request, string updatedBy);
    Task<InvoiceDto?> GetInvoiceAsync(string invoiceId);
    Task<IEnumerable<InvoiceDto>> GetInvoicesByTenantAsync(string tenantId);
    Task<IEnumerable<InvoiceDto>> GetInvoicesBySubscriptionAsync(string subscriptionId);
    Task<InvoiceDto> GenerateInvoiceForSubscriptionAsync(string subscriptionId, string createdBy);
    Task ProcessOverdueInvoicesAsync();
    Task ProcessFailedPaymentRetriesAsync();
}
