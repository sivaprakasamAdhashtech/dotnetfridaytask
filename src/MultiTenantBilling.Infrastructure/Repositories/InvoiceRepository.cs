using MongoDB.Driver;
using MultiTenantBilling.Core.Entities;
using MultiTenantBilling.Core.Enums;
using MultiTenantBilling.Core.Interfaces;
using MultiTenantBilling.Infrastructure.Configuration;

namespace MultiTenantBilling.Infrastructure.Repositories;

public class InvoiceRepository : BaseRepository<Invoice>, IInvoiceRepository
{
    public InvoiceRepository(MongoDbContext context) : base(context.Invoices)
    {
    }

    public async Task<IEnumerable<Invoice>> GetByTenantIdAsync(string tenantId)
    {
        var filter = Builders<Invoice>.Filter.Eq(x => x.TenantId, tenantId);
        var sort = Builders<Invoice>.Sort.Descending(x => x.CreatedAt);
        return await _collection.Find(filter).Sort(sort).ToListAsync();
    }

    public async Task<IEnumerable<Invoice>> GetBySubscriptionIdAsync(string subscriptionId)
    {
        var filter = Builders<Invoice>.Filter.Eq(x => x.SubscriptionId, subscriptionId);
        var sort = Builders<Invoice>.Sort.Descending(x => x.CreatedAt);
        return await _collection.Find(filter).Sort(sort).ToListAsync();
    }

    public async Task<Invoice?> GetByInvoiceNumberAsync(string invoiceNumber)
    {
        var filter = Builders<Invoice>.Filter.Eq(x => x.InvoiceNumber, invoiceNumber);
        return await _collection.Find(filter).FirstOrDefaultAsync();
    }

    public async Task<IEnumerable<Invoice>> GetByStatusAsync(InvoiceStatus status)
    {
        var filter = Builders<Invoice>.Filter.Eq(x => x.Status, status);
        return await _collection.Find(filter).ToListAsync();
    }

    public async Task<IEnumerable<Invoice>> GetOverdueInvoicesAsync()
    {
        var now = DateTime.UtcNow;
        var filter = Builders<Invoice>.Filter.And(
            Builders<Invoice>.Filter.In(x => x.Status, new[] { InvoiceStatus.Pending, InvoiceStatus.Failed }),
            Builders<Invoice>.Filter.Lt(x => x.DueDate, now)
        );
        return await _collection.Find(filter).ToListAsync();
    }

    public async Task<IEnumerable<Invoice>> GetInvoicesDueForRetryAsync()
    {
        var now = DateTime.UtcNow;
        var filter = Builders<Invoice>.Filter.And(
            Builders<Invoice>.Filter.Eq(x => x.Status, InvoiceStatus.Failed),
            Builders<Invoice>.Filter.Lte(x => x.NextRetryDate, now),
            Builders<Invoice>.Filter.Lt(x => x.PaymentRetryCount, 3) // Max 3 retries
        );
        return await _collection.Find(filter).ToListAsync();
    }

    public async Task<string> GenerateInvoiceNumberAsync()
    {
        var now = DateTime.UtcNow;
        var prefix = $"INV-{now:yyyyMM}";
        
        // Find the highest invoice number for this month
        var filter = Builders<Invoice>.Filter.Regex(x => x.InvoiceNumber, $"^{prefix}");
        var sort = Builders<Invoice>.Sort.Descending(x => x.InvoiceNumber);
        var lastInvoice = await _collection.Find(filter).Sort(sort).FirstOrDefaultAsync();
        
        int nextNumber = 1;
        if (lastInvoice != null)
        {
            var lastNumberPart = lastInvoice.InvoiceNumber.Substring(prefix.Length + 1);
            if (int.TryParse(lastNumberPart, out int lastNumber))
            {
                nextNumber = lastNumber + 1;
            }
        }
        
        return $"{prefix}-{nextNumber:D4}";
    }
}
