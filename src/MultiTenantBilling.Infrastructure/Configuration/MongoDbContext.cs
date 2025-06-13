using MongoDB.Driver;
using MultiTenantBilling.Core.Entities;
using Microsoft.Extensions.Options;

namespace MultiTenantBilling.Infrastructure.Configuration;

public class MongoDbContext
{
    private readonly IMongoDatabase _database;
    private readonly MongoDbSettings _settings;

    public MongoDbContext(IOptions<MongoDbSettings> settings)
    {
        _settings = settings.Value;
        var client = new MongoClient(_settings.ConnectionString);
        _database = client.GetDatabase(_settings.DatabaseName);
        
        CreateIndexes();
    }

    public IMongoCollection<Tenant> Tenants => 
        _database.GetCollection<Tenant>(_settings.TenantsCollection);

    public IMongoCollection<User> Users => 
        _database.GetCollection<User>(_settings.UsersCollection);

    public IMongoCollection<SubscriptionPlan> SubscriptionPlans => 
        _database.GetCollection<SubscriptionPlan>(_settings.SubscriptionPlansCollection);

    public IMongoCollection<Subscription> Subscriptions => 
        _database.GetCollection<Subscription>(_settings.SubscriptionsCollection);

    public IMongoCollection<Invoice> Invoices => 
        _database.GetCollection<Invoice>(_settings.InvoicesCollection);

    public IMongoCollection<AuditLog> AuditLogs => 
        _database.GetCollection<AuditLog>(_settings.AuditLogsCollection);

    private void CreateIndexes()
    {
        // Tenant indexes
        var tenantIndexKeys = Builders<Tenant>.IndexKeys.Ascending(x => x.TenantId);
        var tenantIndexModel = new CreateIndexModel<Tenant>(tenantIndexKeys, 
            new CreateIndexOptions { Unique = true });
        Tenants.Indexes.CreateOne(tenantIndexModel);

        // User indexes
        var userEmailTenantIndexKeys = Builders<User>.IndexKeys
            .Ascending(x => x.Email)
            .Ascending(x => x.TenantId);
        var userEmailTenantIndexModel = new CreateIndexModel<User>(userEmailTenantIndexKeys, 
            new CreateIndexOptions { Unique = true });
        Users.Indexes.CreateOne(userEmailTenantIndexModel);

        var userTenantIndexKeys = Builders<User>.IndexKeys.Ascending(x => x.TenantId);
        var userTenantIndexModel = new CreateIndexModel<User>(userTenantIndexKeys);
        Users.Indexes.CreateOne(userTenantIndexModel);

        // Subscription indexes
        var subscriptionTenantIndexKeys = Builders<Subscription>.IndexKeys.Ascending(x => x.TenantId);
        var subscriptionTenantIndexModel = new CreateIndexModel<Subscription>(subscriptionTenantIndexKeys);
        Subscriptions.Indexes.CreateOne(subscriptionTenantIndexModel);

        var subscriptionBillingIndexKeys = Builders<Subscription>.IndexKeys.Ascending(x => x.NextBillingDate);
        var subscriptionBillingIndexModel = new CreateIndexModel<Subscription>(subscriptionBillingIndexKeys);
        Subscriptions.Indexes.CreateOne(subscriptionBillingIndexModel);

        // Invoice indexes
        var invoiceTenantDueDateIndexKeys = Builders<Invoice>.IndexKeys
            .Ascending(x => x.TenantId)
            .Ascending(x => x.DueDate);
        var invoiceTenantDueDateIndexModel = new CreateIndexModel<Invoice>(invoiceTenantDueDateIndexKeys);
        Invoices.Indexes.CreateOne(invoiceTenantDueDateIndexModel);

        var invoiceNumberIndexKeys = Builders<Invoice>.IndexKeys.Ascending(x => x.InvoiceNumber);
        var invoiceNumberIndexModel = new CreateIndexModel<Invoice>(invoiceNumberIndexKeys, 
            new CreateIndexOptions { Unique = true });
        Invoices.Indexes.CreateOne(invoiceNumberIndexModel);

        // Audit log indexes
        var auditLogTenantTimestampIndexKeys = Builders<AuditLog>.IndexKeys
            .Ascending(x => x.TenantId)
            .Descending(x => x.Timestamp);
        var auditLogTenantTimestampIndexModel = new CreateIndexModel<AuditLog>(auditLogTenantTimestampIndexKeys);
        AuditLogs.Indexes.CreateOne(auditLogTenantTimestampIndexModel);

        var auditLogTimestampIndexKeys = Builders<AuditLog>.IndexKeys.Descending(x => x.Timestamp);
        var auditLogTimestampIndexModel = new CreateIndexModel<AuditLog>(auditLogTimestampIndexKeys);
        AuditLogs.Indexes.CreateOne(auditLogTimestampIndexModel);
    }
}
