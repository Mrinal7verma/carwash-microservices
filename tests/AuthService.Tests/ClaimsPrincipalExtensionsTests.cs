namespace AuthService.Tests;

using System.Security.Claims;
using CarWash.Common.Exceptions;
using CarWash.Common.Extensions;
using Xunit;

public class ClaimsPrincipalExtensionsTests
{
    [Fact]
    public void GetUserId_ShouldReturnParsedLong_WhenValidClaimExists()
    {
        // Arrange
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, "456"),
            new Claim(ClaimTypes.Email, "test@carwash.local"),
            new Claim(ClaimTypes.Role, "Customer")
        };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        var principal = new ClaimsPrincipal(identity);

        // Act
        var userId = principal.GetUserId();
        var email = principal.GetEmail();
        var role = principal.GetRole();

        // Assert
        Assert.Equal(456L, userId);
        Assert.Equal("test@carwash.local", email);
        Assert.Equal("Customer", role);
    }

    [Fact]
    public void GetUserId_ShouldThrowUnauthorizedException_WhenClaimIsMissing()
    {
        // Arrange
        var identity = new ClaimsIdentity(Array.Empty<Claim>(), "TestAuth");
        var principal = new ClaimsPrincipal(identity);

        // Act & Assert
        Assert.Throws<UnauthorizedException>(() => principal.GetUserId());
    }
}
