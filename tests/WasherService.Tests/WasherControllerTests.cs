namespace WasherService.Tests;

using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using WasherService.Controllers;
using WasherService.Data.Entities;
using WasherService.DTOs;
using WasherService.Services;
using Xunit;

public class WasherControllerTests
{
    private readonly Mock<IWasherService> _mockWasherService;
    private readonly Mock<IJwtTokenService> _mockJwtService;
    private readonly WasherController _controller;

    public WasherControllerTests()
    {
        _mockWasherService = new Mock<IWasherService>();
        _mockJwtService = new Mock<IJwtTokenService>();
        _controller = new WasherController(_mockWasherService.Object, _mockJwtService.Object);
    }

    [Fact]
    public async Task Register_Returns200()
    {
        var req = new WasherRegisterRequest
        {
            FirstName = "W",
            LastName = "L",
            Email = "w@test.com",
            Phone = "1234567890",
            Password = "Password1!"
        };

        var result = await _controller.Register(req, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(ok.Value);
    }

    [Fact]
    public async Task Login_Returns200AndToken()
    {
        var req = new WasherLoginRequest { Email = "w@test.com", Password = "Pass" };
        var washer = new Washer { WasherId = 20L, Email = "w@test.com" };

        _mockWasherService.Setup(s => s.LoginAsync(req, It.IsAny<CancellationToken>()))
            .ReturnsAsync(washer);
        _mockJwtService.Setup(j => j.GenerateToken(washer.WasherId, washer.Email))
            .Returns("token-xyz");

        var result = await _controller.Login(req, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result);
        var resp = Assert.IsType<WasherLoginResponse>(ok.Value);
        Assert.Equal("token-xyz", resp.Token);
    }

    [Fact]
    public async Task GetProfile_ReturnsProfile()
    {
        SetUserPrincipal(20L, "w@test.com", "Washer");
        var profile = new WasherResponse { WasherId = 20L, Email = "w@test.com", FirstName = "John" };
        _mockWasherService.Setup(s => s.GetProfileAsync(20L, It.IsAny<CancellationToken>()))
            .ReturnsAsync(profile);

        var result = await _controller.GetProfile(CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(profile, ok.Value);
    }

    [Fact]
    public async Task UpdateAvailability_Returns200()
    {
        SetUserPrincipal(20L, "w@test.com", "Washer");
        var req = new WasherAvailabilityRequest { IsAvailable = true };

        var result = await _controller.UpdateAvailability(req, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result);
        _mockWasherService.Verify(s => s.UpdateAvailabilityAsync(20L, true, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetAllWashers_And_GetAvailable_And_Delete_Returns200()
    {
        SetUserPrincipal(1L, "admin@carwash.com", "Admin");
        var list = new System.Collections.Generic.List<WasherResponse> { new WasherResponse { WasherId = 1L } };
        _mockWasherService.Setup(s => s.GetAllWashersAsync(It.IsAny<CancellationToken>())).ReturnsAsync(list);
        _mockWasherService.Setup(s => s.GetAvailableWashersAsync(It.IsAny<CancellationToken>())).ReturnsAsync(list);

        var all = await _controller.GetAllWashers(CancellationToken.None);
        Assert.IsType<OkObjectResult>(all);

        var avail = await _controller.GetAvailable(CancellationToken.None);
        Assert.IsType<OkObjectResult>(avail);

        SetUserPrincipal(20L, "w@test.com", "Washer");
        var del = await _controller.Delete(CancellationToken.None);
        Assert.IsType<OkObjectResult>(del);
    }

    private void SetUserPrincipal(long userId, string email, string role)
    {
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
            new Claim(ClaimTypes.Email, email),
            new Claim(ClaimTypes.Role, role)
        };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(identity) }
        };
    }
}
