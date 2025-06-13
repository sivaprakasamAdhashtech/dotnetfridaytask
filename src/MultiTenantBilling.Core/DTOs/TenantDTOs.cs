using System.ComponentModel.DataAnnotations;

namespace MultiTenantBilling.Core.DTOs;

public class CreateTenantRequest
{
    [Required]
    [StringLength(100)]
    public string Name { get; set; } = string.Empty;

    [Required]
    [StringLength(50)]
    public string TenantId { get; set; } = string.Empty;

    [Required]
    [EmailAddress]
    [StringLength(255)]
    public string ContactEmail { get; set; } = string.Empty;

    [StringLength(20)]
    public string? ContactPhone { get; set; }

    [StringLength(500)]
    public string? Address { get; set; }

    public int MaxRequestsPerMinute { get; set; } = 100;

    public Dictionary<string, object> Settings { get; set; } = new();
}

public class UpdateTenantRequest
{
    [Required]
    [StringLength(100)]
    public string Name { get; set; } = string.Empty;

    [Required]
    [EmailAddress]
    [StringLength(255)]
    public string ContactEmail { get; set; } = string.Empty;

    [StringLength(20)]
    public string? ContactPhone { get; set; }

    [StringLength(500)]
    public string? Address { get; set; }

    public bool IsActive { get; set; }

    public int MaxRequestsPerMinute { get; set; } = 100;

    public Dictionary<string, object> Settings { get; set; } = new();
}

public class TenantDto
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string TenantId { get; set; } = string.Empty;
    public string ContactEmail { get; set; } = string.Empty;
    public string? ContactPhone { get; set; }
    public string? Address { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public string? CreatedBy { get; set; }
    public string? UpdatedBy { get; set; }
    public int MaxRequestsPerMinute { get; set; }
    public Dictionary<string, object> Settings { get; set; } = new();
}
