namespace CarWash.Common.Exceptions;

/// <summary>
/// Thrown when a requested resource is not found (HTTP 404).
/// </summary>
public class NotFoundException : CarWashException
{
    public NotFoundException(string message)
        : base(message, 404)
    {
    }
}
