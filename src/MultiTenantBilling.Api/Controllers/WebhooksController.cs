using Microsoft.AspNetCore.Mvc;
using MultiTenantBilling.Core.DTOs;
using MultiTenantBilling.Core.Interfaces;

namespace MultiTenantBilling.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class WebhooksController : ControllerBase
{
    private readonly IWebhookService _webhookService;
    private readonly IAuditLogService _auditLogService;
    private readonly ILogger<WebhooksController> _logger;

    public WebhooksController(
        IWebhookService webhookService,
        IAuditLogService auditLogService,
        ILogger<WebhooksController> logger)
    {
        _webhookService = webhookService;
        _auditLogService = auditLogService;
        _logger = logger;
    }

    /// <summary>
    /// Stripe webhook endpoint for payment events
    /// </summary>
    /// <param name="request">Stripe webhook payload</param>
    /// <returns>Webhook processing result</returns>
    [HttpPost("stripe")]
    public async Task<ActionResult<WebhookResponse>> ProcessStripeWebhook([FromBody] StripeWebhookRequest request)
    {
        try
        {
            // Get signature from headers
            var signature = HttpContext.Request.Headers["Stripe-Signature"].FirstOrDefault() ?? 
                           HttpContext.Request.Headers["X-Stripe-Signature"].FirstOrDefault() ?? "";

            _logger.LogInformation("Processing Stripe webhook: {Event} for invoice {InvoiceId}", 
                request.Event, request.InvoiceId);

            var response = await _webhookService.ProcessStripeWebhookAsync(request, signature);

            // Log webhook attempt
            await _auditLogService.LogAsync(
                tenantId: request.TenantId,
                userId: "webhook",
                action: "WEBHOOK_RECEIVED",
                entityType: "Webhook",
                description: $"Stripe webhook received: {request.Event} for invoice {request.InvoiceId}",
                severity: response.Success ? "Info" : "Warning",
                correlationId: response.CorrelationId,
                ipAddress: HttpContext.Connection.RemoteIpAddress?.ToString(),
                userAgent: HttpContext.Request.Headers["User-Agent"].FirstOrDefault()
            );

            if (response.Success)
            {
                return Ok(response);
            }
            else
            {
                return BadRequest(response);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to process Stripe webhook");

            var errorResponse = new WebhookResponse
            {
                Success = false,
                Message = "Internal server error",
                CorrelationId = Guid.NewGuid().ToString()
            };

            return StatusCode(500, errorResponse);
        }
    }

    /// <summary>
    /// Test webhook endpoint for development/testing
    /// </summary>
    /// <param name="request">Test webhook payload</param>
    /// <returns>Test result</returns>
    [HttpPost("test")]
    public async Task<ActionResult<WebhookResponse>> TestWebhook([FromBody] StripeWebhookRequest request)
    {
        try
        {
            _logger.LogInformation("Processing test webhook: {Event} for invoice {InvoiceId}", 
                request.Event, request.InvoiceId);

            // For testing, we'll use a mock signature
            var mockSignature = "sha256=test_signature";
            var response = await _webhookService.ProcessStripeWebhookAsync(request, mockSignature);

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to process test webhook");

            var errorResponse = new WebhookResponse
            {
                Success = false,
                Message = "Test webhook processing failed",
                CorrelationId = Guid.NewGuid().ToString()
            };

            return BadRequest(errorResponse);
        }
    }
}
