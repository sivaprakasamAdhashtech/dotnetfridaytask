using MultiTenantBilling.Core.DTOs;

namespace MultiTenantBilling.Core.Interfaces;

public interface ITenantService
{
    Task<TenantDto> CreateTenantAsync(CreateTenantRequest request, string createdBy);
    Task<TenantDto> UpdateTenantAsync(string tenantId, UpdateTenantRequest request, string updatedBy);
    Task<TenantDto?> GetTenantAsync(string tenantId);
    Task<IEnumerable<TenantDto>> GetAllTenantsAsync();
    Task<bool> DeleteTenantAsync(string tenantId);
    Task<bool> TenantExistsAsync(string tenantId);
}
