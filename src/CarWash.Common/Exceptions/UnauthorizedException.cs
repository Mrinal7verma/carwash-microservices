namespace CarWash.Common.Exceptions;

/// <summary>
/// Thrown when authentication is missing or invalid (HTTP 401).
/// </summary>
public class UnauthorizedException : CarWashException
{
    public UnauthorizedException(string message = "Unauthorized")
        : base(message, 401)
    {
    }
}
