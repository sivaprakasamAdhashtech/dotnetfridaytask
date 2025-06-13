using MultiTenantBilling.Core.DTOs;
using MultiTenantBilling.Core.Entities;
using MultiTenantBilling.Core.Exceptions;
using MultiTenantBilling.Core.Interfaces;

namespace MultiTenantBilling.Infrastructure.Services;

public class TenantService : ITenantService
{
    private readonly ITenantRepository _tenantRepository;
    private readonly IAuditLogService _auditLogService;

    public TenantService(ITenantRepository tenantRepository, IAuditLogService auditLogService)
    {
        _tenantRepository = tenantRepository;
        _auditLogService = auditLogService;
    }

    public async Task<TenantDto> CreateTenantAsync(CreateTenantRequest request, string createdBy)
    {
        // Check if tenant ID already exists
        if (await _tenantRepository.TenantIdExistsAsync(request.TenantId))
        {
            throw new DuplicateException("Tenant", "TenantId", request.TenantId);
        }

        var tenant = new Tenant
        {
            Name = request.Name,
            TenantId = request.TenantId,
            ContactEmail = request.ContactEmail.ToLowerInvariant(),
            ContactPhone = request.ContactPhone,
            Address = request.Address,
            MaxRequestsPerMinute = request.MaxRequestsPerMinute,
            Settings = request.Settings,
            CreatedBy = createdBy,
            UpdatedBy = createdBy,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        await _tenantRepository.CreateAsync(tenant);

        return MapToDto(tenant);
    }

    public async Task<TenantDto> UpdateTenantAsync(string tenantId, UpdateTenantRequest request, string updatedBy)
    {
        var tenant = await _tenantRepository.GetByTenantIdAsync(tenantId);
        if (tenant == null)
        {
            throw new NotFoundException("Tenant", tenantId);
        }

        tenant.Name = request.Name;
        tenant.ContactEmail = request.ContactEmail.ToLowerInvariant();
        tenant.ContactPhone = request.ContactPhone;
        tenant.Address = request.Address;
        tenant.IsActive = request.IsActive;
        tenant.MaxRequestsPerMinute = request.MaxRequestsPerMinute;
        tenant.Settings = request.Settings;
        tenant.UpdatedBy = updatedBy;
        tenant.UpdatedAt = DateTime.UtcNow;

        await _tenantRepository.UpdateAsync(tenant);

        return MapToDto(tenant);
    }

    public async Task<TenantDto?> GetTenantAsync(string tenantId)
    {
        var tenant = await _tenantRepository.GetByTenantIdAsync(tenantId);
        return tenant != null ? MapToDto(tenant) : null;
    }

    public async Task<IEnumerable<TenantDto>> GetAllTenantsAsync()
    {
        var tenants = await _tenantRepository.GetAllAsync();
        return tenants.Select(MapToDto);
    }

    public async Task<bool> DeleteTenantAsync(string tenantId)
    {
        var tenant = await _tenantRepository.GetByTenantIdAsync(tenantId);
        if (tenant == null)
        {
            return false;
        }

        return await _tenantRepository.DeleteAsync(tenant.Id);
    }

    public async Task<bool> TenantExistsAsync(string tenantId)
    {
        return await _tenantRepository.TenantIdExistsAsync(tenantId);
    }

    private static TenantDto MapToDto(Tenant tenant)
    {
        return new TenantDto
        {
            Id = tenant.Id,
            Name = tenant.Name,
            TenantId = tenant.TenantId,
            ContactEmail = tenant.ContactEmail,
            ContactPhone = tenant.ContactPhone,
            Address = tenant.Address,
            IsActive = tenant.IsActive,
            CreatedAt = tenant.CreatedAt,
            UpdatedAt = tenant.UpdatedAt,
            CreatedBy = tenant.CreatedBy,
            UpdatedBy = tenant.UpdatedBy,
            MaxRequestsPerMinute = tenant.MaxRequestsPerMinute,
            Settings = tenant.Settings
        };
    }
}
