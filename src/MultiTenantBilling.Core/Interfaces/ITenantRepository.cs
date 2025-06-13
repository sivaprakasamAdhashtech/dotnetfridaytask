using MultiTenantBilling.Core.Entities;

namespace MultiTenantBilling.Core.Interfaces;

public interface ITenantRepository : IRepository<Tenant>
{
    Task<Tenant?> GetByTenantIdAsync(string tenantId);
    Task<bool> TenantIdExistsAsync(string tenantId);
    Task<IEnumerable<Tenant>> GetActiveTenants();
}
