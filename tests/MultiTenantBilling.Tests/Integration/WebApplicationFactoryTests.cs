using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MultiTenantBilling.Api;
using MultiTenantBilling.Infrastructure.Configuration;
using Testcontainers.MongoDb;
using Xunit;

namespace MultiTenantBilling.Tests.Integration;

public class CustomWebApplicationFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly MongoDbContainer _mongoContainer = new MongoDbBuilder()
        .WithImage("mongo:7.0")
        .WithPortBinding(27017, true)
        .WithEnvironment("MONGO_INITDB_ROOT_USERNAME", "admin")
        .WithEnvironment("MONGO_INITDB_ROOT_PASSWORD", "password123")
        .WithEnvironment("MONGO_INITDB_DATABASE", "MultiTenantBilling_Test")
        .Build();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            // Remove the existing MongoDB configuration
            var descriptor = services.SingleOrDefault(d => d.ServiceType == typeof(MongoDbContext));
            if (descriptor != null)
            {
                services.Remove(descriptor);
            }

            // Add test MongoDB configuration
            services.Configure<MongoDbSettings>(options =>
            {
                options.ConnectionString = _mongoContainer.GetConnectionString();
                options.DatabaseName = "MultiTenantBilling_Test";
            });

            services.AddSingleton<MongoDbContext>();
        });

        builder.UseEnvironment("Testing");
    }

    public async Task InitializeAsync()
    {
        await _mongoContainer.StartAsync();
    }

    public new async Task DisposeAsync()
    {
        await _mongoContainer.StopAsync();
        await base.DisposeAsync();
    }
}

public class IntegrationTestBase : IClassFixture<CustomWebApplicationFactory>
{
    protected readonly CustomWebApplicationFactory Factory;
    protected readonly HttpClient Client;

    public IntegrationTestBase(CustomWebApplicationFactory factory)
    {
        Factory = factory;
        Client = factory.CreateClient();
    }
}
