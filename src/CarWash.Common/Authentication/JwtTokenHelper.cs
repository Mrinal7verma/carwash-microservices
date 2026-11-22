namespace CarWash.Common.Authentication;

using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

/// <summary>
/// Helper to construct signed JWT bearer tokens for users across the microservices.
/// </summary>
public class JwtTokenHelper : IJwtTokenHelper
{
    private readonly IConfiguration _configuration;

    public JwtTokenHelper(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public static string BuildToken(long id, string email, string role, IConfiguration configuration)
    {
        var key = configuration["Jwt:Key"]
            ?? throw new InvalidOperationException("Configuration 'Jwt:Key' is required to generate tokens.");
        var issuer = configuration["Jwt:Issuer"]
            ?? throw new InvalidOperationException("Configuration 'Jwt:Issuer' is required to generate tokens.");
        var audience = configuration["Jwt:Audience"]
            ?? throw new InvalidOperationException("Configuration 'Jwt:Audience' is required to generate tokens.");

        var expiryMinutesStr = configuration["Jwt:ExpiryMinutes"] ?? "60";
        if (!int.TryParse(expiryMinutesStr, out var expiryMinutes) || expiryMinutes <= 0)
        {
            expiryMinutes = 60;
        }

        var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key));
        var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, id.ToString()),
            new Claim(ClaimTypes.Email, email),
            new Claim(ClaimTypes.Role, role),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        var token = new JwtSecurityToken(
            issuer: issuer,
            audience: audience,
            claims: claims,
            notBefore: DateTime.UtcNow,
            expires: DateTime.UtcNow.AddMinutes(expiryMinutes),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public string BuildToken(long id, string email, string role)
    {
        return BuildToken(id, email, role, _configuration);
    }
}
