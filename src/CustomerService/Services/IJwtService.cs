namespace CustomerService.Services;

/// <summary>
/// Contract for issuing customer JWT authentication tokens.
/// </summary>
public interface IJwtService
{
    string GenerateToken(long customerId, string email);
}
