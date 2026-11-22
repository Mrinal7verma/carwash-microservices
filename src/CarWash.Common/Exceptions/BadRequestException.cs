namespace CarWash.Common.Exceptions;

/// <summary>
/// Thrown when client input or request parameters are invalid (HTTP 400).
/// </summary>
public class BadRequestException : CarWashException
{
    public BadRequestException(string message)
        : base(message, 400)
    {
    }
}
