namespace CarWash.Common.Exceptions;

/// <summary>
/// Thrown when an authenticated user lacks permissions for an operation (HTTP 403).
/// </summary>
public class ForbiddenException : CarWashException
{
    public ForbiddenException(string message = "Forbidden")
        : base(message, 403)
    {
    }
}
