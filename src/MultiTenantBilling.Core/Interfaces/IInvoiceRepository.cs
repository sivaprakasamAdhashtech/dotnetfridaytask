using MultiTenantBilling.Core.Entities;
using MultiTenantBilling.Core.Enums;

namespace MultiTenantBilling.Core.Interfaces;

public interface IInvoiceRepository : IRepository<Invoice>
{
    Task<IEnumerable<Invoice>> GetByTenantIdAsync(string tenantId);
    Task<IEnumerable<Invoice>> GetBySubscriptionIdAsync(string subscriptionId);
    Task<Invoice?> GetByInvoiceNumberAsync(string invoiceNumber);
    Task<IEnumerable<Invoice>> GetByStatusAsync(InvoiceStatus status);
    Task<IEnumerable<Invoice>> GetOverdueInvoicesAsync();
    Task<IEnumerable<Invoice>> GetInvoicesDueForRetryAsync();
    Task<string> GenerateInvoiceNumberAsync();
}
