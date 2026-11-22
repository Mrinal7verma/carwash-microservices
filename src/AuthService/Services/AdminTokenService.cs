namespace AuthService.Services;

using CarWash.Common.Authentication;

/// <summary>
/// Implements token generation for administrative users with the Admin role.
/// </summary>
public class AdminTokenService : IAdminTokenService
{
    private const long AdminUserId = 1L;
    private const string AdminRole = "Admin";

    private readonly IJwtTokenHelper _jwtTokenHelper;

    public AdminTokenService(IJwtTokenHelper jwtTokenHelper)
    {
        _jwtTokenHelper = jwtTokenHelper;
    }

    public string GenerateToken(string email)
    {
        return _jwtTokenHelper.BuildToken(AdminUserId, email, AdminRole);
    }
}
