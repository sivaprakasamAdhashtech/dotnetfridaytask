using FluentAssertions;
using Moq;
using MultiTenantBilling.Core.DTOs;
using MultiTenantBilling.Core.Entities;
using MultiTenantBilling.Core.Exceptions;
using MultiTenantBilling.Core.Interfaces;
using MultiTenantBilling.Infrastructure.Services;
using Xunit;

namespace MultiTenantBilling.Tests.Services;

public class TenantServiceTests
{
    private readonly Mock<ITenantRepository> _tenantRepositoryMock;
    private readonly Mock<IAuditLogService> _auditLogServiceMock;
    private readonly TenantService _tenantService;

    public TenantServiceTests()
    {
        _tenantRepositoryMock = new Mock<ITenantRepository>();
        _auditLogServiceMock = new Mock<IAuditLogService>();
        _tenantService = new TenantService(_tenantRepositoryMock.Object, _auditLogServiceMock.Object);
    }

    [Fact]
    public async Task CreateTenantAsync_WithValidRequest_ShouldCreateTenant()
    {
        // Arrange
        var request = new CreateTenantRequest
        {
            Name = "Test Company",
            TenantId = "TEST-001",
            ContactEmail = "test@company.com",
            ContactPhone = "+1-555-0123",
            Address = "123 Test St",
            MaxRequestsPerMinute = 100,
            Settings = new Dictionary<string, object> { ["timezone"] = "UTC" }
        };

        _tenantRepositoryMock.Setup(x => x.TenantIdExistsAsync(request.TenantId))
            .ReturnsAsync(false);

        _tenantRepositoryMock.Setup(x => x.CreateAsync(It.IsAny<Tenant>()))
            .ReturnsAsync((Tenant tenant) => tenant);

        // Act
        var result = await _tenantService.CreateTenantAsync(request, "test-user");

        // Assert
        result.Should().NotBeNull();
        result.Name.Should().Be(request.Name);
        result.TenantId.Should().Be(request.TenantId);
        result.ContactEmail.Should().Be(request.ContactEmail.ToLowerInvariant());
        result.IsActive.Should().BeTrue();

        _tenantRepositoryMock.Verify(x => x.CreateAsync(It.IsAny<Tenant>()), Times.Once);
    }

    [Fact]
    public async Task CreateTenantAsync_WithDuplicateTenantId_ShouldThrowDuplicateException()
    {
        // Arrange
        var request = new CreateTenantRequest
        {
            Name = "Test Company",
            TenantId = "TEST-001",
            ContactEmail = "test@company.com"
        };

        _tenantRepositoryMock.Setup(x => x.TenantIdExistsAsync(request.TenantId))
            .ReturnsAsync(true);

        // Act & Assert
        await Assert.ThrowsAsync<DuplicateException>(() => 
            _tenantService.CreateTenantAsync(request, "test-user"));

        _tenantRepositoryMock.Verify(x => x.CreateAsync(It.IsAny<Tenant>()), Times.Never);
    }

    [Fact]
    public async Task GetTenantAsync_WithExistingTenant_ShouldReturnTenant()
    {
        // Arrange
        var tenantId = "TEST-001";
        var tenant = new Tenant
        {
            Id = "507f1f77bcf86cd799439011",
            Name = "Test Company",
            TenantId = tenantId,
            ContactEmail = "test@company.com",
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _tenantRepositoryMock.Setup(x => x.GetByTenantIdAsync(tenantId))
            .ReturnsAsync(tenant);

        // Act
        var result = await _tenantService.GetTenantAsync(tenantId);

        // Assert
        result.Should().NotBeNull();
        result!.TenantId.Should().Be(tenantId);
        result.Name.Should().Be(tenant.Name);
    }

    [Fact]
    public async Task GetTenantAsync_WithNonExistentTenant_ShouldReturnNull()
    {
        // Arrange
        var tenantId = "NON-EXISTENT";

        _tenantRepositoryMock.Setup(x => x.GetByTenantIdAsync(tenantId))
            .ReturnsAsync((Tenant?)null);

        // Act
        var result = await _tenantService.GetTenantAsync(tenantId);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task UpdateTenantAsync_WithValidRequest_ShouldUpdateTenant()
    {
        // Arrange
        var tenantId = "TEST-001";
        var existingTenant = new Tenant
        {
            Id = "507f1f77bcf86cd799439011",
            Name = "Old Name",
            TenantId = tenantId,
            ContactEmail = "old@company.com",
            IsActive = true,
            CreatedAt = DateTime.UtcNow.AddDays(-1),
            UpdatedAt = DateTime.UtcNow.AddDays(-1)
        };

        var updateRequest = new UpdateTenantRequest
        {
            Name = "New Name",
            ContactEmail = "new@company.com",
            IsActive = true,
            MaxRequestsPerMinute = 200,
            Settings = new Dictionary<string, object> { ["timezone"] = "EST" }
        };

        _tenantRepositoryMock.Setup(x => x.GetByTenantIdAsync(tenantId))
            .ReturnsAsync(existingTenant);

        _tenantRepositoryMock.Setup(x => x.UpdateAsync(It.IsAny<Tenant>()))
            .ReturnsAsync((Tenant tenant) => tenant);

        // Act
        var result = await _tenantService.UpdateTenantAsync(tenantId, updateRequest, "test-user");

        // Assert
        result.Should().NotBeNull();
        result.Name.Should().Be(updateRequest.Name);
        result.ContactEmail.Should().Be(updateRequest.ContactEmail.ToLowerInvariant());
        result.MaxRequestsPerMinute.Should().Be(updateRequest.MaxRequestsPerMinute);

        _tenantRepositoryMock.Verify(x => x.UpdateAsync(It.IsAny<Tenant>()), Times.Once);
    }

    [Fact]
    public async Task UpdateTenantAsync_WithNonExistentTenant_ShouldThrowNotFoundException()
    {
        // Arrange
        var tenantId = "NON-EXISTENT";
        var updateRequest = new UpdateTenantRequest
        {
            Name = "New Name",
            ContactEmail = "new@company.com",
            IsActive = true,
            MaxRequestsPerMinute = 200,
            Settings = new Dictionary<string, object>()
        };

        _tenantRepositoryMock.Setup(x => x.GetByTenantIdAsync(tenantId))
            .ReturnsAsync((Tenant?)null);

        // Act & Assert
        await Assert.ThrowsAsync<NotFoundException>(() => 
            _tenantService.UpdateTenantAsync(tenantId, updateRequest, "test-user"));

        _tenantRepositoryMock.Verify(x => x.UpdateAsync(It.IsAny<Tenant>()), Times.Never);
    }
}
