namespace MultiTenantBilling.Infrastructure.Configuration;

public class MongoDbSettings
{
    public string ConnectionString { get; set; } = string.Empty;
    public string DatabaseName { get; set; } = string.Empty;
    public string TenantsCollection { get; set; } = "tenants";
    public string UsersCollection { get; set; } = "users";
    public string SubscriptionPlansCollection { get; set; } = "subscriptionPlans";
    public string SubscriptionsCollection { get; set; } = "subscriptions";
    public string InvoicesCollection { get; set; } = "invoices";
    public string AuditLogsCollection { get; set; } = "auditLogs";
}
