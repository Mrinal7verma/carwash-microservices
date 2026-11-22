namespace CarWash.Common.Exceptions;

/// <summary>
/// Thrown when an operation conflicts with the current state of a resource (HTTP 409).
/// </summary>
public class ConflictException : CarWashException
{
    public ConflictException(string message)
        : base(message, 409)
    {
    }
}
