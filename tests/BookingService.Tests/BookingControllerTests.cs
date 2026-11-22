namespace BookingService.Tests;

using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using BookingService.Controllers;
using BookingService.DTOs;
using BookingService.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

public class BookingControllerTests
{
    private readonly Mock<IBookingService> _mockBookingService;
    private readonly BookingController _controller;

    public BookingControllerTests()
    {
        _mockBookingService = new Mock<IBookingService>();
        _controller = new BookingController(_mockBookingService.Object);
        SetUserPrincipal(50L, "customer@test.com", "Customer");
    }

    [Fact]
    public async Task GetActivePackages_Returns200WithPackages()
    {
        var packages = new List<ServicePackageResponse>
        {
            new ServicePackageResponse { PackageId = 1L, Name = "Basic Wash", Price = 299.00m, IsActive = true }
        };
        _mockBookingService.Setup(s => s.GetActivePackagesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(packages);

        var result = await _controller.GetActivePackages(CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(packages, ok.Value);
    }

    [Fact]
    public async Task CreateBooking_Returns201Created()
    {
        var request = new CreateBookingRequest { PackageId = 1L, CarId = 2L, ScheduledTime = DateTime.UtcNow.AddHours(2) };
        var responseDto = new BookingResponseDto { BookingId = 100L, Status = "Pending", TotalAmount = 299.00m };

        _mockBookingService.Setup(s => s.CreateBookingAsync(50L, request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(responseDto);

        var result = await _controller.CreateBooking(request, CancellationToken.None);

        var objectResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(StatusCodes.Status201Created, objectResult.StatusCode);
        Assert.Equal(responseDto, objectResult.Value);
    }

    [Fact]
    public async Task GetMyBookings_Returns200WithList()
    {
        var list = new List<BookingResponseDto>
        {
            new BookingResponseDto { BookingId = 100L, Status = "Pending" }
        };
        _mockBookingService.Setup(s => s.GetCustomerBookingsAsync(50L, It.IsAny<CancellationToken>()))
            .ReturnsAsync(list);

        var result = await _controller.GetMyBookings(CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(list, ok.Value);
    }

    [Fact]
    public async Task CancelBooking_Returns200()
    {
        var result = await _controller.CancelBooking(100L, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result);
        _mockBookingService.Verify(s => s.CancelBookingAsync(50L, 100L, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UpdateBookingStatus_Returns200()
    {
        SetUserPrincipal(60L, "washer@test.com", "Washer");
        var req = new UpdateBookingStatusRequest { Status = "Accepted" };
        var resp = new BookingResponseDto { BookingId = 100L, Status = "Accepted", WasherId = 60L };

        _mockBookingService.Setup(s => s.UpdateBookingStatusAsync(60L, 100L, "Accepted", It.IsAny<CancellationToken>()))
            .ReturnsAsync(resp);

        var result = await _controller.UpdateBookingStatus(100L, req, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(resp, ok.Value);
    }

    [Fact]
    public async Task Admin_PackageManagementEndpoints_WorkCorrectly()
    {
        SetUserPrincipal(1L, "admin@carwash.com", "Admin");

        // 1. GetAllPackages
        var allList = new List<ServicePackageResponse> { new ServicePackageResponse { PackageId = 1L } };
        _mockBookingService.Setup(s => s.GetAllPackagesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(allList);
        var getAllResult = await _controller.GetAllPackages(CancellationToken.None);
        Assert.IsType<OkObjectResult>(getAllResult);

        // 2. CreatePackage
        var createReq = new CreateServicePackageRequest { Name = "Super Clean", Description = "Desc", Price = 500m, EstimatedDurationMinutes = 60, IsActive = true };
        var createResp = new ServicePackageResponse { PackageId = 5L, Name = "Super Clean" };
        _mockBookingService.Setup(s => s.CreatePackageAsync(createReq, It.IsAny<CancellationToken>())).ReturnsAsync(createResp);
        var createResult = await _controller.CreatePackage(createReq, CancellationToken.None);
        var objResult = Assert.IsType<ObjectResult>(createResult);
        Assert.Equal(StatusCodes.Status201Created, objResult.StatusCode);

        // 3. UpdatePackage
        var updateReq = new UpdateServicePackageRequest { Name = "Super Clean 2", Description = "Desc", Price = 550m, EstimatedDurationMinutes = 65, IsActive = true };
        _mockBookingService.Setup(s => s.UpdatePackageAsync(5L, updateReq, It.IsAny<CancellationToken>())).ReturnsAsync(createResp);
        var updateResult = await _controller.UpdatePackage(5L, updateReq, CancellationToken.None);
        Assert.IsType<OkObjectResult>(updateResult);

        // 4. DeactivatePackage
        var deactivateResult = await _controller.DeactivatePackage(5L, CancellationToken.None);
        Assert.IsType<OkObjectResult>(deactivateResult);
    }

    [Fact]
    public async Task InternalActiveChecks_Return200WithBoolean()
    {
        _mockBookingService.Setup(s => s.HasActiveBookingsForCustomerAsync(10L, It.IsAny<CancellationToken>())).ReturnsAsync(true);
        _mockBookingService.Setup(s => s.HasActiveBookingsForCarAsync(20L, It.IsAny<CancellationToken>())).ReturnsAsync(false);
        _mockBookingService.Setup(s => s.HasActiveBookingsForWasherAsync(30L, It.IsAny<CancellationToken>())).ReturnsAsync(true);

        var custResult = Assert.IsType<OkObjectResult>(await _controller.CheckActiveCustomerBookings(10L, CancellationToken.None));
        var carResult = Assert.IsType<OkObjectResult>(await _controller.CheckActiveCarBookings(20L, CancellationToken.None));
        var washerResult = Assert.IsType<OkObjectResult>(await _controller.CheckActiveWasherBookings(30L, CancellationToken.None));

        Assert.True(((ActiveBookingCheckResponse)custResult.Value!).HasActiveBookings);
        Assert.False(((ActiveBookingCheckResponse)carResult.Value!).HasActiveBookings);
        Assert.True(((ActiveBookingCheckResponse)washerResult.Value!).HasActiveBookings);
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
