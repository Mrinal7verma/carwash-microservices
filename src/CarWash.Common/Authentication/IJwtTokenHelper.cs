namespace CarWash.Common.Authentication;

/// <summary>
/// Defines contract for building signed JWT authentication tokens.
/// </summary>
public interface IJwtTokenHelper
{
    string BuildToken(long id, string email, string role);
}
