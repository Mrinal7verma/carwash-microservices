namespace CarWash.Common.Exceptions;

/// <summary>
/// Base exception for domain and application-level errors in CarWash services.
/// </summary>
public abstract class CarWashException : Exception
{
    protected CarWashException(string message, int statusCode)
        : base(message)
    {
        StatusCode = statusCode;
    }

    public int StatusCode { get; }
}
