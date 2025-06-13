using MultiTenantBilling.Core.Exceptions;
using System.Security.Claims;

namespace MultiTenantBilling.Api.Middleware;

public class TenantIsolationMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<TenantIsolationMiddleware> _logger;

    public TenantIsolationMiddleware(RequestDelegate next, ILogger<TenantIsolationMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Skip tenant isolation for auth endpoints and public endpoints
        var path = context.Request.Path.Value?.ToLower();
        if (path != null && (path.Contains("/auth/") || path.Contains("/swagger") || 
            path.Contains("/hangfire") || path.Contains("/health")))
        {
            await _next(context);
            return;
        }

        // Skip for OPTIONS requests (CORS preflight)
        if (context.Request.Method == "OPTIONS")
        {
            await _next(context);
            return;
        }

        // Check if user is authenticated
        if (!context.User.Identity?.IsAuthenticated == true)
        {
            await _next(context);
            return;
        }

        // Extract tenant ID from JWT claims
        var userTenantId = context.User.FindFirst("TenantId")?.Value;
        var userRole = context.User.FindFirst(ClaimTypes.Role)?.Value;

        if (string.IsNullOrEmpty(userTenantId))
        {
            _logger.LogWarning("User authenticated but no TenantId claim found");
            throw new TenantIsolationException("Invalid tenant information in token");
        }

        // Add tenant context to HttpContext for use in controllers
        context.Items["TenantId"] = userTenantId;
        context.Items["UserRole"] = userRole;
        context.Items["UserId"] = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        // SuperAdmin can access all tenants
        if (userRole == "SuperAdmin")
        {
            await _next(context);
            return;
        }

        // For tenant-specific endpoints, validate tenant access
        if (path != null && (path.Contains("/tenants/") || path.Contains("/users/") || 
            path.Contains("/subscriptions/") || path.Contains("/invoices/")))
        {
            // Extract tenant ID from route if present
            var routeTenantId = ExtractTenantIdFromRoute(context);
            
            if (!string.IsNullOrEmpty(routeTenantId) && routeTenantId != userTenantId)
            {
                _logger.LogWarning("User {UserId} from tenant {UserTenant} attempted to access tenant {RouteTenant}", 
                    context.Items["UserId"], userTenantId, routeTenantId);
                throw new TenantIsolationException();
            }
        }

        await _next(context);
    }

    private string? ExtractTenantIdFromRoute(HttpContext context)
    {
        // Try to extract tenant ID from route values
        if (context.Request.RouteValues.TryGetValue("tenantId", out var tenantId))
        {
            return tenantId?.ToString();
        }

        // Try to extract from query parameters
        if (context.Request.Query.TryGetValue("tenantId", out var queryTenantId))
        {
            return queryTenantId.FirstOrDefault();
        }

        return null;
    }
}
