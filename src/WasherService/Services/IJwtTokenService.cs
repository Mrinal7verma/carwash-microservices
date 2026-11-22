namespace WasherService.Services;

/// <summary>
/// Contract for issuing washer JWT authentication tokens.
/// </summary>
public interface IJwtTokenService
{
    string GenerateToken(long washerId, string email, string role = "Washer");
}
