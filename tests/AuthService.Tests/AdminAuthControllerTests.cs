namespace AuthService.Tests;

using AuthService.Configuration;
using AuthService.Controllers;
using AuthService.DTOs;
using AuthService.Services;
using CarWash.Common.Exceptions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

public class AdminAuthControllerTests
{
    private const string ConfiguredPasswordHash = "$2b$11$qK4XTDyLDDdbEPZRd673IOY8ey/2JbOr9Yw.X.iefXbJmIjEADnqy";
    private readonly IOptions<AdminOptions> _adminOptions;
    private readonly Mock<IAdminTokenService> _mockTokenService;

    public AdminAuthControllerTests()
    {
        _adminOptions = Options.Create(new AdminOptions
        {
            Email = "admin@carwash.com",
            PasswordHash = ConfiguredPasswordHash
        });

        _mockTokenService = new Mock<IAdminTokenService>();
        _mockTokenService
            .Setup(s => s.GenerateToken(It.IsAny<string>()))
            .Returns("mock-valid-jwt-token");
    }

    [Fact]
    public async Task TC_LOG_01_Admin_ValidLogin_ReturnsJwtToken()
    {
        // Arrange
        var controller = new AdminAuthController(_mockTokenService.Object, _adminOptions);
        var request = new AdminLoginRequest
        {
            Email = "admin@carwash.com",
            Password = "Admin@123"
        };

        // Act
        var result = await controller.Login(request, CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<AdminLoginResponse>(okResult.Value);
        Assert.Equal("mock-valid-jwt-token", response.Token);
    }

    [Fact]
    public async Task TC_LOG_02_Admin_WrongPasswordOrUnknownEmail_Returns401()
    {
        // Arrange
        var controller = new AdminAuthController(_mockTokenService.Object, _adminOptions);

        var wrongPasswordReq = new AdminLoginRequest
        {
            Email = "admin@carwash.com",
            Password = "WrongPassword999!"
        };

        var wrongEmailReq = new AdminLoginRequest
        {
            Email = "stranger@carwash.com",
            Password = "Admin@123"
        };

        // Act & Assert
        await Assert.ThrowsAsync<UnauthorizedException>(() => controller.Login(wrongPasswordReq, CancellationToken.None));
        await Assert.ThrowsAsync<UnauthorizedException>(() => controller.Login(wrongEmailReq, CancellationToken.None));
    }

    [Fact]
    public async Task TC_LOG_03_Admin_Valid_Returns200_Invalid_Returns401_And_VerifiesConfiguredHash()
    {
        // 1. Verify configured hash against "Admin@123"
        var matches = BCrypt.Net.BCrypt.Verify("Admin@123", ConfiguredPasswordHash);
        Assert.True(matches, "Configured Admin:PasswordHash does not match 'Admin@123'");

        // 2. Test valid admin returns 200
        var controller = new AdminAuthController(_mockTokenService.Object, _adminOptions);
        var validReq = new AdminLoginRequest
        {
            Email = "admin@carwash.com",
            Password = "Admin@123"
        };
        var validResult = await controller.Login(validReq, CancellationToken.None);
        var okResult = Assert.IsType<OkObjectResult>(validResult);
        var response = Assert.IsType<AdminLoginResponse>(okResult.Value);
        Assert.NotNull(response.Token);

        // 3. Test invalid credentials return 401
        var invalidReq = new AdminLoginRequest
        {
            Email = "admin@carwash.com",
            Password = "BadPassword"
        };
        await Assert.ThrowsAsync<UnauthorizedException>(() => controller.Login(invalidReq, CancellationToken.None));
    }

    [Fact]
    public async Task Login_ShouldThrowBadRequest_WhenRequestOrFieldsMissing()
    {
        var controller = new AdminAuthController(_mockTokenService.Object, _adminOptions);

        var emptyEmail = new AdminLoginRequest { Email = string.Empty, Password = "Password" };
        var emptyPass = new AdminLoginRequest { Email = "admin@carwash.com", Password = string.Empty };

        await Assert.ThrowsAsync<BadRequestException>(() => controller.Login(emptyEmail, CancellationToken.None));
        await Assert.ThrowsAsync<BadRequestException>(() => controller.Login(emptyPass, CancellationToken.None));
    }
}
