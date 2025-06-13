using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MultiTenantBilling.Core.DTOs;
using MultiTenantBilling.Core.Interfaces;

namespace MultiTenantBilling.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class InvoicesController : ControllerBase
{
    private readonly IInvoiceService _invoiceService;
    private readonly IAuditLogService _auditLogService;
    private readonly ILogger<InvoicesController> _logger;

    public InvoicesController(
        IInvoiceService invoiceService,
        IAuditLogService auditLogService,
        ILogger<InvoicesController> logger)
    {
        _invoiceService = invoiceService;
        _auditLogService = auditLogService;
        _logger = logger;
    }

    /// <summary>
    /// Get invoices for the current tenant
    /// </summary>
    /// <returns>List of tenant invoices</returns>
    [HttpGet]
    [Authorize(Roles = "TenantAdmin,BillingUser")]
    public async Task<ActionResult<ApiResponse<IEnumerable<InvoiceDto>>>> GetTenantInvoices()
    {
        try
        {
            var tenantId = HttpContext.Items["TenantId"]?.ToString();
            if (string.IsNullOrEmpty(tenantId))
            {
                return BadRequest(new ApiResponse<IEnumerable<InvoiceDto>>
                {
                    Success = false,
                    Message = "Tenant context is required"
                });
            }

            var invoices = await _invoiceService.GetInvoicesByTenantAsync(tenantId);
            
            return Ok(new ApiResponse<IEnumerable<InvoiceDto>>
            {
                Success = true,
                Message = "Invoices retrieved successfully",
                Data = invoices
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve tenant invoices");
            return BadRequest(new ApiResponse<IEnumerable<InvoiceDto>>
            {
                Success = false,
                Message = "Failed to retrieve invoices",
                Errors = new List<string> { ex.Message }
            });
        }
    }

    /// <summary>
    /// Get invoice by ID
    /// </summary>
    /// <param name="id">Invoice ID</param>
    /// <returns>Invoice information</returns>
    [HttpGet("{id}")]
    [Authorize(Roles = "TenantAdmin,BillingUser")]
    public async Task<ActionResult<ApiResponse<InvoiceDto>>> GetInvoice(string id)
    {
        try
        {
            var invoice = await _invoiceService.GetInvoiceAsync(id);
            if (invoice == null)
            {
                return NotFound(new ApiResponse<InvoiceDto>
                {
                    Success = false,
                    Message = "Invoice not found"
                });
            }

            return Ok(new ApiResponse<InvoiceDto>
            {
                Success = true,
                Message = "Invoice retrieved successfully",
                Data = invoice
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve invoice {InvoiceId}", id);
            return BadRequest(new ApiResponse<InvoiceDto>
            {
                Success = false,
                Message = "Failed to retrieve invoice",
                Errors = new List<string> { ex.Message }
            });
        }
    }

    /// <summary>
    /// Get invoices for a specific subscription
    /// </summary>
    /// <param name="subscriptionId">Subscription ID</param>
    /// <returns>List of subscription invoices</returns>
    [HttpGet("subscription/{subscriptionId}")]
    [Authorize(Roles = "TenantAdmin,BillingUser")]
    public async Task<ActionResult<ApiResponse<IEnumerable<InvoiceDto>>>> GetSubscriptionInvoices(string subscriptionId)
    {
        try
        {
            var invoices = await _invoiceService.GetInvoicesBySubscriptionAsync(subscriptionId);
            
            return Ok(new ApiResponse<IEnumerable<InvoiceDto>>
            {
                Success = true,
                Message = "Subscription invoices retrieved successfully",
                Data = invoices
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve subscription invoices for {SubscriptionId}", subscriptionId);
            return BadRequest(new ApiResponse<IEnumerable<InvoiceDto>>
            {
                Success = false,
                Message = "Failed to retrieve subscription invoices",
                Errors = new List<string> { ex.Message }
            });
        }
    }

    /// <summary>
    /// Create a manual invoice
    /// </summary>
    /// <param name="request">Invoice creation request</param>
    /// <returns>Created invoice information</returns>
    [HttpPost]
    [Authorize(Roles = "SuperAdmin,TenantAdmin")]
    public async Task<ActionResult<ApiResponse<InvoiceDto>>> CreateInvoice([FromBody] CreateInvoiceRequest request)
    {
        try
        {
            var userId = HttpContext.Items["UserId"]?.ToString() ?? "system";
            var invoice = await _invoiceService.CreateInvoiceAsync(request, userId);

            await _auditLogService.LogAsync(
                tenantId: invoice.TenantId,
                userId: userId,
                action: "CREATE_INVOICE",
                entityType: "Invoice",
                entityId: invoice.Id,
                description: $"Created manual invoice: {invoice.InvoiceNumber}",
                ipAddress: HttpContext.Connection.RemoteIpAddress?.ToString(),
                userAgent: HttpContext.Request.Headers["User-Agent"].FirstOrDefault()
            );

            return CreatedAtAction(nameof(GetInvoice), new { id = invoice.Id }, 
                new ApiResponse<InvoiceDto>
                {
                    Success = true,
                    Message = "Invoice created successfully",
                    Data = invoice
                });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create invoice");
            return BadRequest(new ApiResponse<InvoiceDto>
            {
                Success = false,
                Message = "Failed to create invoice",
                Errors = new List<string> { ex.Message }
            });
        }
    }

    /// <summary>
    /// Update invoice status
    /// </summary>
    /// <param name="id">Invoice ID</param>
    /// <param name="request">Status update request</param>
    /// <returns>Updated invoice information</returns>
    [HttpPut("{id}/status")]
    [Authorize(Roles = "SuperAdmin,TenantAdmin")]
    public async Task<ActionResult<ApiResponse<InvoiceDto>>> UpdateInvoiceStatus(string id, [FromBody] UpdateInvoiceStatusRequest request)
    {
        try
        {
            var userId = HttpContext.Items["UserId"]?.ToString() ?? "system";
            var invoice = await _invoiceService.UpdateInvoiceStatusAsync(id, request, userId);

            await _auditLogService.LogAsync(
                tenantId: invoice.TenantId,
                userId: userId,
                action: "UPDATE_INVOICE_STATUS",
                entityType: "Invoice",
                entityId: invoice.Id,
                description: $"Updated invoice {invoice.InvoiceNumber} status to: {invoice.Status}",
                ipAddress: HttpContext.Connection.RemoteIpAddress?.ToString(),
                userAgent: HttpContext.Request.Headers["User-Agent"].FirstOrDefault()
            );

            return Ok(new ApiResponse<InvoiceDto>
            {
                Success = true,
                Message = "Invoice status updated successfully",
                Data = invoice
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update invoice status for {InvoiceId}", id);
            return BadRequest(new ApiResponse<InvoiceDto>
            {
                Success = false,
                Message = "Failed to update invoice status",
                Errors = new List<string> { ex.Message }
            });
        }
    }

    /// <summary>
    /// Generate invoice for a subscription
    /// </summary>
    /// <param name="subscriptionId">Subscription ID</param>
    /// <returns>Generated invoice information</returns>
    [HttpPost("generate/{subscriptionId}")]
    [Authorize(Roles = "SuperAdmin,TenantAdmin")]
    public async Task<ActionResult<ApiResponse<InvoiceDto>>> GenerateInvoice(string subscriptionId)
    {
        try
        {
            var userId = HttpContext.Items["UserId"]?.ToString() ?? "system";
            var invoice = await _invoiceService.GenerateInvoiceForSubscriptionAsync(subscriptionId, userId);

            return Ok(new ApiResponse<InvoiceDto>
            {
                Success = true,
                Message = "Invoice generated successfully",
                Data = invoice
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate invoice for subscription {SubscriptionId}", subscriptionId);
            return BadRequest(new ApiResponse<InvoiceDto>
            {
                Success = false,
                Message = "Failed to generate invoice",
                Errors = new List<string> { ex.Message }
            });
        }
    }
}
