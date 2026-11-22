namespace CustomerService.Tests;

using System.Collections.Generic;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using CustomerService.Controllers;
using CustomerService.DTOs;
using CustomerService.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

public class CarControllerTests
{
    private readonly Mock<ICustomerService> _mockCustomerService;
    private readonly CarController _controller;

    public CarControllerTests()
    {
        _mockCustomerService = new Mock<ICustomerService>();
        _controller = new CarController(_mockCustomerService.Object);
        SetUserPrincipal(5L, "owner@example.com", "Customer");
    }

    [Fact]
    public async Task AddCar_Returns201Created()
    {
        var request = new CarRequest { Make = "Toyota", Model = "Yaris", RegistrationNo = "KA-01-1111" };
        var response = new CarResponse { CarId = 1L, Make = "Toyota", Model = "Yaris", RegistrationNo = "KA-01-1111" };

        _mockCustomerService.Setup(s => s.AddCarAsync(5L, request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(response);

        var result = await _controller.AddCar(request, CancellationToken.None);

        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(response, okResult.Value);
    }

    [Fact]
    public async Task GetCars_ReturnsListOfCars()
    {
        var list = new List<CarResponse>
        {
            new CarResponse { CarId = 1L, Make = "Honda", Model = "City", RegistrationNo = "KA-02-2222" }
        };
        _mockCustomerService.Setup(s => s.GetCarsAsync(5L, It.IsAny<CancellationToken>()))
            .ReturnsAsync(list);

        var result = await _controller.GetCars(CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(list, ok.Value);
    }

    [Fact]
    public async Task DeleteCar_Returns200()
    {
        var result = await _controller.DeleteCar(1L, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result);
        _mockCustomerService.Verify(s => s.DeleteCarAsync(5L, 1L, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetCar_ReturnsCarResponse()
    {
        var car = new CarResponse { CarId = 1L, Make = "Honda" };
        _mockCustomerService.Setup(s => s.GetCarByIdAsync(5L, 1L, It.IsAny<CancellationToken>())).ReturnsAsync(car);

        var result = await _controller.GetCar(1L, CancellationToken.None);
        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(car, ok.Value);
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
