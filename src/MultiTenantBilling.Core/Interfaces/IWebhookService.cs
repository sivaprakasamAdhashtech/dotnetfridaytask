using MultiTenantBilling.Core.DTOs;

namespace MultiTenantBilling.Core.Interfaces;

public interface IWebhookService
{
    Task<WebhookResponse> ProcessStripeWebhookAsync(StripeWebhookRequest request, string signature);
    Task<bool> ValidateStripeSignatureAsync(string payload, string signature);
}
