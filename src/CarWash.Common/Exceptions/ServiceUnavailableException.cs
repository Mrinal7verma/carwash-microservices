namespace CarWash.Common.Exceptions;

/// <summary>
/// Thrown when a downstream dependency or service is currently unavailable (HTTP 503).
/// </summary>
public class ServiceUnavailableException : CarWashException
{
    public ServiceUnavailableException(string message = "Service unavailable")
        : base(message, 503)
    {
    }
}
