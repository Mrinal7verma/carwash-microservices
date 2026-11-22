namespace WasherService.Services;

using CarWash.Common.Authentication;

/// <summary>
/// Service that generates JWT tokens for washers with role Washer.
/// </summary>
public class JwtTokenService : IJwtTokenService
{
    private readonly IJwtTokenHelper _jwtTokenHelper;

    public JwtTokenService(IJwtTokenHelper jwtTokenHelper)
    {
        _jwtTokenHelper = jwtTokenHelper;
    }

    public string GenerateToken(long washerId, string email, string role = "Washer")
    {
        return _jwtTokenHelper.BuildToken(washerId, email, role);
    }
}
