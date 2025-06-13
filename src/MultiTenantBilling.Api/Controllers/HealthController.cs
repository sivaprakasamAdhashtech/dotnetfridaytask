using Microsoft.AspNetCore.Mvc;
using MultiTenantBilling.Core.DTOs;
using MultiTenantBilling.Infrastructure.Configuration;

namespace MultiTenantBilling.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class HealthController : ControllerBase
{
    private readonly MongoDbContext _mongoDbContext;
    private readonly ILogger<HealthController> _logger;

    public HealthController(MongoDbContext mongoDbContext, ILogger<HealthController> logger)
    {
        _mongoDbContext = mongoDbContext;
        _logger = logger;
    }

    /// <summary>
    /// Health check endpoint
    /// </summary>
    /// <returns>System health status</returns>
    [HttpGet]
    public async Task<ActionResult<ApiResponse<object>>> GetHealth()
    {
        try
        {
            // Check MongoDB connection
            var mongoHealthy = await CheckMongoDbHealthAsync();
            
            var healthData = new
            {
                Status = mongoHealthy ? "Healthy" : "Unhealthy",
                Timestamp = DateTime.UtcNow,
                Version = "1.0.0",
                Environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Unknown",
                Services = new
                {
                    MongoDB = mongoHealthy ? "Connected" : "Disconnected",
                    API = "Running"
                }
            };

            if (mongoHealthy)
            {
                return Ok(new ApiResponse<object>
                {
                    Success = true,
                    Message = "System is healthy",
                    Data = healthData
                });
            }
            else
            {
                return StatusCode(503, new ApiResponse<object>
                {
                    Success = false,
                    Message = "System is unhealthy",
                    Data = healthData
                });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Health check failed");
            
            return StatusCode(503, new ApiResponse<object>
            {
                Success = false,
                Message = "Health check failed",
                Errors = new List<string> { ex.Message }
            });
        }
    }

    private async Task<bool> CheckMongoDbHealthAsync()
    {
        try
        {
            // Simple ping to check MongoDB connectivity
            await _mongoDbContext.Tenants.CountDocumentsAsync(_ => true);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "MongoDB health check failed");
            return false;
        }
    }
}
