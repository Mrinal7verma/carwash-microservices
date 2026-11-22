namespace CustomerService.Services;

using CarWash.Common.Authentication;

/// <summary>
/// Service that generates JWT tokens for customers with role Customer.
/// </summary>
public class JwtService : IJwtService
{
    private const string CustomerRole = "Customer";

    private readonly IJwtTokenHelper _jwtTokenHelper;

    public JwtService(IJwtTokenHelper jwtTokenHelper)
    {
        _jwtTokenHelper = jwtTokenHelper;
    }

    public string GenerateToken(long customerId, string email)
    {
        return _jwtTokenHelper.BuildToken(customerId, email, CustomerRole);
    }
}
