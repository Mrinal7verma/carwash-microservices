namespace CustomerService.Tests;

using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using CustomerService.Controllers;
using CustomerService.Data.Entities;
using CustomerService.DTOs;
using CustomerService.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

public class CustomerControllerTests
{
    private readonly Mock<ICustomerService> _mockCustomerService;
    private readonly Mock<IJwtService> _mockJwtService;
    private readonly CustomerController _controller;

    public CustomerControllerTests()
    {
        _mockCustomerService = new Mock<ICustomerService>();
        _mockJwtService = new Mock<IJwtService>();
        _controller = new CustomerController(_mockCustomerService.Object, _mockJwtService.Object);
    }

    [Fact]
    public async Task Register_Returns200_WhenValid()
    {
        var request = new CustomerRegisterRequest
        {
            FirstName = "A",
            LastName = "B",
            Email = "a.b@example.com",
            Phone = "1234567890",
            Password = "Password1!"
        };

        var result = await _controller.Register(request, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(ok.Value);
    }

    [Fact]
    public async Task Login_Returns200AndToken_WhenValid()
    {
        var request = new CustomerLoginRequest { Email = "test@example.com", Password = "Pass" };
        var customer = new Customer { CustomerId = 10L, Email = "test@example.com" };

        _mockCustomerService.Setup(s => s.ValidateCustomerAsync(request.Email, request.Password, It.IsAny<CancellationToken>()))
            .ReturnsAsync(customer);
        _mockJwtService.Setup(j => j.GenerateToken(customer.CustomerId, customer.Email))
            .Returns("fake-jwt");

        var result = await _controller.Login(request, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result);
        var resp = Assert.IsType<CustomerLoginResponse>(ok.Value);
        Assert.Equal("fake-jwt", resp.Token);
    }

    [Fact]
    public async Task GetProfile_ReturnsCustomerProfile()
    {
        SetUserPrincipal(10L, "test@example.com", "Customer");
        var profile = new CustomerProfileResponse { CustomerId = 10L, Email = "test@example.com", FirstName = "John" };
        _mockCustomerService.Setup(s => s.GetProfileAsync(10L, It.IsAny<CancellationToken>()))
            .ReturnsAsync(profile);

        var result = await _controller.GetProfile(CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(profile, ok.Value);
    }

    [Fact]
    public async Task DeleteProfile_Returns200()
    {
        SetUserPrincipal(10L, "test@example.com", "Customer");

        var result = await _controller.DeleteProfile(CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result);
        _mockCustomerService.Verify(s => s.DeleteCustomerAsync(10L, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetAllCustomers_And_GetCustomerEmail_Returns200()
    {
        SetUserPrincipal(1L, "admin@carwash.com", "Admin");
        var list = new System.Collections.Generic.List<CustomerProfileResponse> { new CustomerProfileResponse { CustomerId = 10L } };
        _mockCustomerService.Setup(s => s.GetAllCustomersAsync(It.IsAny<CancellationToken>())).ReturnsAsync(list);

        var allResult = await _controller.GetAllCustomers(CancellationToken.None);
        Assert.IsType<OkObjectResult>(allResult);

        _mockCustomerService.Setup(s => s.GetCustomerEmailAsync(10L, It.IsAny<CancellationToken>())).ReturnsAsync("email@test.com");
        var emailResult = await _controller.GetCustomerEmail(10L, CancellationToken.None);
        var ok = Assert.IsType<OkObjectResult>(emailResult);
        var emailResp = Assert.IsType<CustomerEmailResponse>(ok.Value);
        Assert.Equal("email@test.com", emailResp.Email);
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
