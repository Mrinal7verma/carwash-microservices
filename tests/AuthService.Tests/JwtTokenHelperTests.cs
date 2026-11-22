namespace AuthService.Tests;

using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using CarWash.Common.Authentication;
using Microsoft.Extensions.Configuration;
using Xunit;

public class JwtTokenHelperTests
{
    private readonly IConfiguration _configuration;

    public JwtTokenHelperTests()
    {
        var settings = new Dictionary<string, string?>
        {
            { "Jwt:Key", "SuperSecretCarWashJwtSigningKeyForMicroservices2026!" },
            { "Jwt:Issuer", "CarWashAuthService" },
            { "Jwt:Audience", "CarWashServices" },
            { "Jwt:ExpiryMinutes", "30" }
        };

        _configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(settings)
            .Build();
    }

    [Fact]
    public void BuildToken_ShouldGenerateValidJwtToken_WithExpectedClaims()
    {
        // Arrange
        var helper = new JwtTokenHelper(_configuration);
        long userId = 101;
        var email = "admin@carwash.local";
        var role = "Admin";

        // Act
        var tokenString = helper.BuildToken(userId, email, role);

        // Assert
        Assert.False(string.IsNullOrWhiteSpace(tokenString));

        var handler = new JwtSecurityTokenHandler();
        var token = handler.ReadJwtToken(tokenString);

        Assert.Equal("CarWashAuthService", token.Issuer);
        Assert.Contains(token.Audiences, aud => aud == "CarWashServices");

        var subClaim = token.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier || c.Type == "sub");
        Assert.NotNull(subClaim);
        Assert.Equal("101", subClaim.Value);

        var emailClaim = token.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Email || c.Type == "email");
        Assert.NotNull(emailClaim);
        Assert.Equal("admin@carwash.local", emailClaim.Value);

        var roleClaim = token.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Role || c.Type == "role");
        Assert.NotNull(roleClaim);
        Assert.Equal("Admin", roleClaim.Value);
    }
}
