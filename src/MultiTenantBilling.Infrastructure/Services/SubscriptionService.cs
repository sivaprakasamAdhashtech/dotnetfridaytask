using MultiTenantBilling.Core.DTOs;
using MultiTenantBilling.Core.Entities;
using MultiTenantBilling.Core.Enums;
using MultiTenantBilling.Core.Exceptions;
using MultiTenantBilling.Core.Interfaces;

namespace MultiTenantBilling.Infrastructure.Services;

public class SubscriptionService : ISubscriptionService
{
    private readonly ISubscriptionRepository _subscriptionRepository;
    private readonly ISubscriptionPlanRepository _subscriptionPlanRepository;
    private readonly ITenantRepository _tenantRepository;
    private readonly IInvoiceService _invoiceService;
    private readonly IAuditLogService _auditLogService;

    public SubscriptionService(
        ISubscriptionRepository subscriptionRepository,
        ISubscriptionPlanRepository subscriptionPlanRepository,
        ITenantRepository tenantRepository,
        IInvoiceService invoiceService,
        IAuditLogService auditLogService)
    {
        _subscriptionRepository = subscriptionRepository;
        _subscriptionPlanRepository = subscriptionPlanRepository;
        _tenantRepository = tenantRepository;
        _invoiceService = invoiceService;
        _auditLogService = auditLogService;
    }

    public async Task<SubscriptionDto> CreateSubscriptionAsync(CreateSubscriptionRequest request, string createdBy)
    {
        // Verify tenant exists
        var tenant = await _tenantRepository.GetByTenantIdAsync(request.TenantId);
        if (tenant == null)
        {
            throw new NotFoundException("Tenant", request.TenantId);
        }

        // Verify subscription plan exists
        var plan = await _subscriptionPlanRepository.GetByIdAsync(request.SubscriptionPlanId);
        if (plan == null)
        {
            throw new NotFoundException("SubscriptionPlan", request.SubscriptionPlanId);
        }

        // Check if tenant already has an active subscription
        var existingSubscription = await _subscriptionRepository.GetActiveSubscriptionByTenantAsync(request.TenantId);
        if (existingSubscription != null)
        {
            throw new BusinessException("Tenant already has an active subscription");
        }

        var subscription = new Subscription
        {
            TenantId = request.TenantId,
            SubscriptionPlanId = request.SubscriptionPlanId,
            Status = SubscriptionStatus.Active,
            StartDate = request.StartDate,
            EndDate = request.EndDate,
            CurrentPrice = plan.Price,
            NextBillingDate = CalculateNextBillingDate(request.StartDate, plan.BillingCycle),
            CreatedBy = createdBy,
            UpdatedBy = createdBy,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            Metadata = request.Metadata
        };

        // Handle trial setup
        if (request.StartTrial && plan.TrialDays.HasValue)
        {
            subscription.IsTrialActive = true;
            subscription.TrialEndDate = DateTime.UtcNow.AddDays(plan.TrialDays.Value);
            subscription.NextBillingDate = subscription.TrialEndDate.Value;
        }

        await _subscriptionRepository.CreateAsync(subscription);

        return await MapToDtoAsync(subscription);
    }

    public async Task<SubscriptionDto> UpdateSubscriptionAsync(string subscriptionId, UpdateSubscriptionRequest request, string updatedBy)
    {
        var subscription = await _subscriptionRepository.GetByIdAsync(subscriptionId);
        if (subscription == null)
        {
            throw new NotFoundException("Subscription", subscriptionId);
        }

        subscription.Status = request.Status;
        subscription.EndDate = request.EndDate;
        subscription.UpdatedBy = updatedBy;
        subscription.UpdatedAt = DateTime.UtcNow;
        subscription.Metadata = request.Metadata;

        if (request.Status == SubscriptionStatus.Cancelled)
        {
            subscription.CancelledAt = DateTime.UtcNow;
            subscription.CancelledBy = updatedBy;
            subscription.CancellationReason = request.CancellationReason;
        }

        await _subscriptionRepository.UpdateAsync(subscription);

        return await MapToDtoAsync(subscription);
    }

    public async Task<SubscriptionDto?> GetSubscriptionAsync(string subscriptionId)
    {
        var subscription = await _subscriptionRepository.GetByIdAsync(subscriptionId);
        return subscription != null ? await MapToDtoAsync(subscription) : null;
    }

    public async Task<IEnumerable<SubscriptionDto>> GetSubscriptionsByTenantAsync(string tenantId)
    {
        var subscriptions = await _subscriptionRepository.GetByTenantIdAsync(tenantId);
        var subscriptionDtos = new List<SubscriptionDto>();

        foreach (var subscription in subscriptions)
        {
            subscriptionDtos.Add(await MapToDtoAsync(subscription));
        }

        return subscriptionDtos;
    }

    public async Task<SubscriptionDto?> GetActiveSubscriptionByTenantAsync(string tenantId)
    {
        var subscription = await _subscriptionRepository.GetActiveSubscriptionByTenantAsync(tenantId);
        return subscription != null ? await MapToDtoAsync(subscription) : null;
    }

    public async Task<bool> CancelSubscriptionAsync(string subscriptionId, string reason, string cancelledBy)
    {
        var subscription = await _subscriptionRepository.GetByIdAsync(subscriptionId);
        if (subscription == null)
        {
            return false;
        }

        subscription.Status = SubscriptionStatus.Cancelled;
        subscription.CancelledAt = DateTime.UtcNow;
        subscription.CancelledBy = cancelledBy;
        subscription.CancellationReason = reason;
        subscription.UpdatedAt = DateTime.UtcNow;

        await _subscriptionRepository.UpdateAsync(subscription);
        return true;
    }

    public async Task<IEnumerable<SubscriptionPlanDto>> GetAvailablePlansAsync()
    {
        var plans = await _subscriptionPlanRepository.GetActivePlansAsync();
        return plans.Select(MapPlanToDto);
    }

    public async Task ProcessSubscriptionBillingAsync()
    {
        var today = DateTime.UtcNow.Date;
        var subscriptionsDue = await _subscriptionRepository.GetSubscriptionsDueForBillingAsync(today);

        foreach (var subscription in subscriptionsDue)
        {
            try
            {
                // Generate invoice for the subscription
                await _invoiceService.GenerateInvoiceForSubscriptionAsync(subscription.Id, "system");

                // Update next billing date
                var plan = await _subscriptionPlanRepository.GetByIdAsync(subscription.SubscriptionPlanId);
                if (plan != null)
                {
                    subscription.NextBillingDate = CalculateNextBillingDate(subscription.NextBillingDate, plan.BillingCycle);
                    subscription.LastBilledDate = DateTime.UtcNow;
                    subscription.BillingCycleCount++;
                    subscription.UpdatedAt = DateTime.UtcNow;

                    await _subscriptionRepository.UpdateAsync(subscription);
                }

                await _auditLogService.LogAsync(
                    tenantId: subscription.TenantId,
                    userId: "system",
                    action: "PROCESS_BILLING",
                    entityType: "Subscription",
                    entityId: subscription.Id,
                    description: "Processed subscription billing"
                );
            }
            catch (Exception ex)
            {
                await _auditLogService.LogAsync(
                    tenantId: subscription.TenantId,
                    userId: "system",
                    action: "BILLING_ERROR",
                    entityType: "Subscription",
                    entityId: subscription.Id,
                    description: $"Failed to process billing: {ex.Message}",
                    severity: "Error"
                );
            }
        }
    }

    private DateTime CalculateNextBillingDate(DateTime currentDate, BillingCycle billingCycle)
    {
        return billingCycle switch
        {
            BillingCycle.Monthly => currentDate.AddMonths(1),
            BillingCycle.Quarterly => currentDate.AddMonths(3),
            BillingCycle.SemiAnnually => currentDate.AddMonths(6),
            BillingCycle.Annually => currentDate.AddYears(1),
            _ => currentDate.AddMonths(1)
        };
    }

    private async Task<SubscriptionDto> MapToDtoAsync(Subscription subscription)
    {
        var plan = await _subscriptionPlanRepository.GetByIdAsync(subscription.SubscriptionPlanId);
        
        return new SubscriptionDto
        {
            Id = subscription.Id,
            TenantId = subscription.TenantId,
            SubscriptionPlanId = subscription.SubscriptionPlanId,
            Status = subscription.Status,
            StartDate = subscription.StartDate,
            EndDate = subscription.EndDate,
            NextBillingDate = subscription.NextBillingDate,
            CurrentPrice = subscription.CurrentPrice,
            CreatedAt = subscription.CreatedAt,
            UpdatedAt = subscription.UpdatedAt,
            IsTrialActive = subscription.IsTrialActive,
            TrialEndDate = subscription.TrialEndDate,
            LastBilledDate = subscription.LastBilledDate,
            BillingCycleCount = subscription.BillingCycleCount,
            CancelledAt = subscription.CancelledAt,
            CancelledBy = subscription.CancelledBy,
            CancellationReason = subscription.CancellationReason,
            ExternalSubscriptionId = subscription.ExternalSubscriptionId,
            Metadata = subscription.Metadata,
            Plan = plan != null ? MapPlanToDto(plan) : null
        };
    }

    private static SubscriptionPlanDto MapPlanToDto(SubscriptionPlan plan)
    {
        return new SubscriptionPlanDto
        {
            Id = plan.Id,
            Name = plan.Name,
            Description = plan.Description,
            Price = plan.Price,
            BillingCycle = plan.BillingCycle,
            IsActive = plan.IsActive,
            CreatedAt = plan.CreatedAt,
            Features = plan.Features,
            MaxUsers = plan.MaxUsers,
            MaxStorageGB = plan.MaxStorageGB,
            MaxApiCallsPerMonth = plan.MaxApiCallsPerMonth,
            TrialDays = plan.TrialDays,
            PlanCode = plan.PlanCode,
            SortOrder = plan.SortOrder
        };
    }
}
