namespace MultiTenantBilling.Core.Exceptions;

public class BusinessException : Exception
{
    public BusinessException(string message) : base(message) { }
    public BusinessException(string message, Exception innerException) : base(message, innerException) { }
}

public class NotFoundException : BusinessException
{
    public NotFoundException(string message) : base(message) { }
    public NotFoundException(string entityType, string id) : base($"{entityType} with ID '{id}' was not found.") { }
}

public class DuplicateException : BusinessException
{
    public DuplicateException(string message) : base(message) { }
    public DuplicateException(string entityType, string field, string value) 
        : base($"{entityType} with {field} '{value}' already exists.") { }
}

public class UnauthorizedException : BusinessException
{
    public UnauthorizedException(string message) : base(message) { }
    public UnauthorizedException() : base("Access denied. You don't have permission to perform this action.") { }
}

public class TenantIsolationException : BusinessException
{
    public TenantIsolationException(string message) : base(message) { }
    public TenantIsolationException() : base("Access denied. Resource belongs to a different tenant.") { }
}

public class ValidationException : BusinessException
{
    public List<string> ValidationErrors { get; }

    public ValidationException(string message) : base(message)
    {
        ValidationErrors = new List<string> { message };
    }

    public ValidationException(List<string> errors) : base("Validation failed.")
    {
        ValidationErrors = errors;
    }
}

public class RateLimitExceededException : BusinessException
{
    public RateLimitExceededException(string message) : base(message) { }
    public RateLimitExceededException() : base("Rate limit exceeded. Please try again later.") { }
}
