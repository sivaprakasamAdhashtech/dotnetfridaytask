using MultiTenantBilling.Core.DTOs;
using MultiTenantBilling.Core.Exceptions;
using System.Net;
using System.Text.Json;

namespace MultiTenantBilling.Api.Middleware;

public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An unhandled exception occurred");
            await HandleExceptionAsync(context, ex);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        context.Response.ContentType = "application/json";

        var response = new ErrorResponse
        {
            TraceId = context.TraceIdentifier,
            Timestamp = DateTime.UtcNow
        };

        switch (exception)
        {
            case NotFoundException notFoundEx:
                context.Response.StatusCode = (int)HttpStatusCode.NotFound;
                response.Message = "Resource not found";
                response.Details = notFoundEx.Message;
                break;

            case DuplicateException duplicateEx:
                context.Response.StatusCode = (int)HttpStatusCode.Conflict;
                response.Message = "Resource already exists";
                response.Details = duplicateEx.Message;
                break;

            case UnauthorizedException unauthorizedEx:
                context.Response.StatusCode = (int)HttpStatusCode.Unauthorized;
                response.Message = "Unauthorized access";
                response.Details = unauthorizedEx.Message;
                break;

            case TenantIsolationException tenantEx:
                context.Response.StatusCode = (int)HttpStatusCode.Forbidden;
                response.Message = "Access denied";
                response.Details = tenantEx.Message;
                break;

            case ValidationException validationEx:
                context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                response.Message = "Validation failed";
                response.Details = validationEx.Message;
                response.Errors = validationEx.ValidationErrors;
                break;

            case RateLimitExceededException rateLimitEx:
                context.Response.StatusCode = 429;
                response.Message = "Rate limit exceeded";
                response.Details = rateLimitEx.Message;
                context.Response.Headers.Add("Retry-After", "60");
                break;

            case BusinessException businessEx:
                context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                response.Message = "Business rule violation";
                response.Details = businessEx.Message;
                break;

            default:
                context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
                response.Message = "An internal server error occurred";
                response.Details = "Please contact support if the problem persists";
                break;
        }

        var jsonResponse = JsonSerializer.Serialize(response, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        await context.Response.WriteAsync(jsonResponse);
    }
}
