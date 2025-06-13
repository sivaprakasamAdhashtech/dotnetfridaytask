using Hangfire;
using MultiTenantBilling.Core.Interfaces;

namespace MultiTenantBilling.Api.BackgroundJobs;

public class BillingBackgroundJobs
{
    private readonly ISubscriptionService _subscriptionService;
    private readonly IInvoiceService _invoiceService;
    private readonly IAuditLogService _auditLogService;
    private readonly ILogger<BillingBackgroundJobs> _logger;

    public BillingBackgroundJobs(
        ISubscriptionService subscriptionService,
        IInvoiceService invoiceService,
        IAuditLogService auditLogService,
        ILogger<BillingBackgroundJobs> logger)
    {
        _subscriptionService = subscriptionService;
        _invoiceService = invoiceService;
        _auditLogService = auditLogService;
        _logger = logger;
    }

    /// <summary>
    /// Daily job to process subscription billing
    /// </summary>
    [AutomaticRetry(Attempts = 3)]
    public async Task ProcessDailyBillingAsync()
    {
        var jobId = Guid.NewGuid().ToString();
        _logger.LogInformation("Starting daily billing process. Job ID: {JobId}", jobId);

        try
        {
            await _auditLogService.LogAsync(
                tenantId: "SYSTEM",
                userId: "hangfire",
                action: "START_DAILY_BILLING",
                entityType: "BackgroundJob",
                description: "Started daily billing process",
                correlationId: jobId
            );

            // Process subscription billing
            await _subscriptionService.ProcessSubscriptionBillingAsync();

            await _auditLogService.LogAsync(
                tenantId: "SYSTEM",
                userId: "hangfire",
                action: "COMPLETE_DAILY_BILLING",
                entityType: "BackgroundJob",
                description: "Completed daily billing process successfully",
                correlationId: jobId
            );

            _logger.LogInformation("Daily billing process completed successfully. Job ID: {JobId}", jobId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Daily billing process failed. Job ID: {JobId}", jobId);

            await _auditLogService.LogAsync(
                tenantId: "SYSTEM",
                userId: "hangfire",
                action: "DAILY_BILLING_ERROR",
                entityType: "BackgroundJob",
                description: $"Daily billing process failed: {ex.Message}",
                severity: "Error",
                correlationId: jobId
            );

            throw; // Re-throw to trigger Hangfire retry
        }
    }

    /// <summary>
    /// Hourly job to process overdue invoices
    /// </summary>
    [AutomaticRetry(Attempts = 2)]
    public async Task ProcessOverdueInvoicesAsync()
    {
        var jobId = Guid.NewGuid().ToString();
        _logger.LogInformation("Starting overdue invoices process. Job ID: {JobId}", jobId);

        try
        {
            await _auditLogService.LogAsync(
                tenantId: "SYSTEM",
                userId: "hangfire",
                action: "START_OVERDUE_PROCESSING",
                entityType: "BackgroundJob",
                description: "Started overdue invoices processing",
                correlationId: jobId
            );

            await _invoiceService.ProcessOverdueInvoicesAsync();

            await _auditLogService.LogAsync(
                tenantId: "SYSTEM",
                userId: "hangfire",
                action: "COMPLETE_OVERDUE_PROCESSING",
                entityType: "BackgroundJob",
                description: "Completed overdue invoices processing",
                correlationId: jobId
            );

            _logger.LogInformation("Overdue invoices process completed. Job ID: {JobId}", jobId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Overdue invoices process failed. Job ID: {JobId}", jobId);

            await _auditLogService.LogAsync(
                tenantId: "SYSTEM",
                userId: "hangfire",
                action: "OVERDUE_PROCESSING_ERROR",
                entityType: "BackgroundJob",
                description: $"Overdue invoices processing failed: {ex.Message}",
                severity: "Error",
                correlationId: jobId
            );

            throw;
        }
    }

    /// <summary>
    /// Daily job to retry failed payments
    /// </summary>
    [AutomaticRetry(Attempts = 2)]
    public async Task ProcessFailedPaymentRetriesAsync()
    {
        var jobId = Guid.NewGuid().ToString();
        _logger.LogInformation("Starting failed payment retries process. Job ID: {JobId}", jobId);

        try
        {
            await _auditLogService.LogAsync(
                tenantId: "SYSTEM",
                userId: "hangfire",
                action: "START_PAYMENT_RETRIES",
                entityType: "BackgroundJob",
                description: "Started failed payment retries processing",
                correlationId: jobId
            );

            await _invoiceService.ProcessFailedPaymentRetriesAsync();

            await _auditLogService.LogAsync(
                tenantId: "SYSTEM",
                userId: "hangfire",
                action: "COMPLETE_PAYMENT_RETRIES",
                entityType: "BackgroundJob",
                description: "Completed failed payment retries processing",
                correlationId: jobId
            );

            _logger.LogInformation("Failed payment retries process completed. Job ID: {JobId}", jobId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed payment retries process failed. Job ID: {JobId}", jobId);

            await _auditLogService.LogAsync(
                tenantId: "SYSTEM",
                userId: "hangfire",
                action: "PAYMENT_RETRIES_ERROR",
                entityType: "BackgroundJob",
                description: $"Failed payment retries processing failed: {ex.Message}",
                severity: "Error",
                correlationId: jobId
            );

            throw;
        }
    }

    /// <summary>
    /// Weekly job to cleanup old audit logs
    /// </summary>
    [AutomaticRetry(Attempts = 1)]
    public async Task CleanupAuditLogsAsync()
    {
        var jobId = Guid.NewGuid().ToString();
        _logger.LogInformation("Starting audit logs cleanup. Job ID: {JobId}", jobId);

        try
        {
            await _auditLogService.LogAsync(
                tenantId: "SYSTEM",
                userId: "hangfire",
                action: "START_AUDIT_CLEANUP",
                entityType: "BackgroundJob",
                description: "Started audit logs cleanup",
                correlationId: jobId
            );

            // Keep logs for 90 days
            await _auditLogService.CleanupOldLogsAsync(90);

            await _auditLogService.LogAsync(
                tenantId: "SYSTEM",
                userId: "hangfire",
                action: "COMPLETE_AUDIT_CLEANUP",
                entityType: "BackgroundJob",
                description: "Completed audit logs cleanup",
                correlationId: jobId
            );

            _logger.LogInformation("Audit logs cleanup completed. Job ID: {JobId}", jobId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Audit logs cleanup failed. Job ID: {JobId}", jobId);

            await _auditLogService.LogAsync(
                tenantId: "SYSTEM",
                userId: "hangfire",
                action: "AUDIT_CLEANUP_ERROR",
                entityType: "BackgroundJob",
                description: $"Audit logs cleanup failed: {ex.Message}",
                severity: "Error",
                correlationId: jobId
            );

            throw;
        }
    }
}

public static class BackgroundJobsExtensions
{
    public static void ScheduleRecurringJobs(this IApplicationBuilder app)
    {
        // Schedule daily billing at 2 AM UTC
        RecurringJob.AddOrUpdate<BillingBackgroundJobs>(
            "daily-billing",
            job => job.ProcessDailyBillingAsync(),
            "0 2 * * *", // Daily at 2 AM
            TimeZoneInfo.Utc);

        // Schedule overdue invoice processing every 6 hours
        RecurringJob.AddOrUpdate<BillingBackgroundJobs>(
            "overdue-invoices",
            job => job.ProcessOverdueInvoicesAsync(),
            "0 */6 * * *", // Every 6 hours
            TimeZoneInfo.Utc);

        // Schedule failed payment retries twice daily
        RecurringJob.AddOrUpdate<BillingBackgroundJobs>(
            "payment-retries",
            job => job.ProcessFailedPaymentRetriesAsync(),
            "0 8,20 * * *", // At 8 AM and 8 PM
            TimeZoneInfo.Utc);

        // Schedule audit logs cleanup weekly
        RecurringJob.AddOrUpdate<BillingBackgroundJobs>(
            "audit-cleanup",
            job => job.CleanupAuditLogsAsync(),
            "0 3 * * 0", // Weekly on Sunday at 3 AM
            TimeZoneInfo.Utc);
    }
}
