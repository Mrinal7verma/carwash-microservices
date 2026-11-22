namespace AuthService.Services;

/// <summary>
/// Service contract for generating signed JWT tokens for admin users.
/// </summary>
public interface IAdminTokenService
{
    string GenerateToken(string email);
}
