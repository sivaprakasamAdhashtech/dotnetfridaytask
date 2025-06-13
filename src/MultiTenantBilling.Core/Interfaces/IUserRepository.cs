using MultiTenantBilling.Core.Entities;
using MultiTenantBilling.Core.Enums;

namespace MultiTenantBilling.Core.Interfaces;

public interface IUserRepository : IRepository<User>
{
    Task<User?> GetByEmailAsync(string email);
    Task<User?> GetByEmailAndTenantAsync(string email, string tenantId);
    Task<IEnumerable<User>> GetByTenantIdAsync(string tenantId);
    Task<IEnumerable<User>> GetByRoleAsync(UserRole role);
    Task<bool> EmailExistsAsync(string email, string tenantId);
    Task UpdateLastLoginAsync(string userId, DateTime loginTime);
}
