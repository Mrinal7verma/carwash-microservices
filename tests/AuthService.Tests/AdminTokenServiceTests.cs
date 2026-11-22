namespace AuthService.Tests;

using AuthService.Services;
using CarWash.Common.Authentication;
using Moq;
using Xunit;

public class AdminTokenServiceTests
{
    [Fact]
    public void GenerateToken_ShouldCallJwtTokenHelperWithAdminRoleAndUserIdOne()
    {
        // Arrange
        var mockHelper = new Mock<IJwtTokenHelper>();
        mockHelper
            .Setup(h => h.BuildToken(1L, "admin@carwash.com", "Admin"))
            .Returns("generated-token");

        var service = new AdminTokenService(mockHelper.Object);

        // Act
        var token = service.GenerateToken("admin@carwash.com");

        // Assert
        Assert.Equal("generated-token", token);
        mockHelper.Verify(h => h.BuildToken(1L, "admin@carwash.com", "Admin"), Times.Once);
    }
}
