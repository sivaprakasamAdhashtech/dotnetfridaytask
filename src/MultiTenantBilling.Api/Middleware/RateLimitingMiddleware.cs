using MultiTenantBilling.Core.Exceptions;
using MultiTenantBilling.Core.Interfaces;
using System.Collections.Concurrent;

namespace MultiTenantBilling.Api.Middleware;

public class RateLimitingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<RateLimitingMiddleware> _logger;
    private static readonly ConcurrentDictionary<string, TenantRateLimit> _tenantLimits = new();

    public RateLimitingMiddleware(RequestDelegate next, ILogger<RateLimitingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context, ITenantRepository tenantRepository)
    {
        // Skip rate limiting for certain endpoints
        var path = context.Request.Path.Value?.ToLower();
        if (path != null && (path.Contains("/swagger") || path.Contains("/hangfire") || path.Contains("/health")))
        {
            await _next(context);
            return;
        }

        // Get tenant ID from context (set by TenantIsolationMiddleware)
        var tenantId = context.Items["TenantId"]?.ToString();
        
        if (string.IsNullOrEmpty(tenantId))
        {
            // For unauthenticated requests, use IP-based rate limiting
            tenantId = GetClientIdentifier(context);
        }

        // Get or create rate limit tracker for tenant
        var rateLimit = _tenantLimits.GetOrAdd(tenantId, async key =>
        {
            var tenant = await tenantRepository.GetByTenantIdAsync(key);
            var maxRequests = tenant?.MaxRequestsPerMinute ?? 100; // Default limit
            return new TenantRateLimit(maxRequests);
        });

        // If the rate limit is a Task (from async GetOrAdd), await it
        if (rateLimit is Task<TenantRateLimit> rateLimitTask)
        {
            rateLimit = await rateLimitTask;
            _tenantLimits.TryUpdate(tenantId, rateLimit, rateLimitTask);
        }

        // Check rate limit
        if (!rateLimit.TryConsumeRequest())
        {
            _logger.LogWarning("Rate limit exceeded for tenant {TenantId}. Limit: {Limit} requests per minute", 
                tenantId, rateLimit.MaxRequestsPerMinute);

            context.Response.StatusCode = 429;
            context.Response.Headers.Add("Retry-After", "60");
            await context.Response.WriteAsync("Rate limit exceeded. Please try again later.");
            return;
        }

        await _next(context);
    }

    private string GetClientIdentifier(HttpContext context)
    {
        // Try to get real IP address
        var forwardedFor = context.Request.Headers["X-Forwarded-For"].FirstOrDefault();
        if (!string.IsNullOrEmpty(forwardedFor))
        {
            return forwardedFor.Split(',')[0].Trim();
        }

        var realIp = context.Request.Headers["X-Real-IP"].FirstOrDefault();
        if (!string.IsNullOrEmpty(realIp))
        {
            return realIp;
        }

        return context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
    }
}

public class TenantRateLimit
{
    private readonly Queue<DateTime> _requestTimes = new();
    private readonly object _lock = new();

    public int MaxRequestsPerMinute { get; }

    public TenantRateLimit(int maxRequestsPerMinute)
    {
        MaxRequestsPerMinute = maxRequestsPerMinute;
    }

    public bool TryConsumeRequest()
    {
        lock (_lock)
        {
            var now = DateTime.UtcNow;
            var oneMinuteAgo = now.AddMinutes(-1);

            // Remove old requests
            while (_requestTimes.Count > 0 && _requestTimes.Peek() < oneMinuteAgo)
            {
                _requestTimes.Dequeue();
            }

            // Check if we can add a new request
            if (_requestTimes.Count >= MaxRequestsPerMinute)
            {
                return false;
            }

            _requestTimes.Enqueue(now);
            return true;
        }
    }
}
