namespace WasherService.Tests;

using System;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using CarWash.Common.Exceptions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using WasherService.Configuration;
using WasherService.Data;
using WasherService.DTOs;
using WasherService.Services;
using WasherService.Tests.Helpers;
using Xunit;

public class WasherServiceUnitTests
{
    private readonly WasherDbContext _dbContext;
    private readonly Mock<IBookingClient> _mockBookingClient;
    private readonly global::WasherService.Services.WasherService _service;

    public WasherServiceUnitTests()
    {
        var options = new DbContextOptionsBuilder<WasherDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _dbContext = new WasherDbContext(options);
        _mockBookingClient = new Mock<IBookingClient>();
        _service = new global::WasherService.Services.WasherService(_dbContext, _mockBookingClient.Object);
    }

    [Fact]
    public async Task TC_REG_01_ValidRegistration_Success_AndPasswordStoredAsBCryptHash()
    {
        var request = new WasherRegisterRequest
        {
            FirstName = "Dave",
            LastName = "Miller",
            Email = "dave.valid@example.com",
            Phone = "1234567890",
            Password = "Password123!"
        };

        await _service.RegisterWasherAsync(request, CancellationToken.None);

        var saved = await _dbContext.Washers.SingleOrDefaultAsync(w => w.Email == "dave.valid@example.com");
        Assert.NotNull(saved);
        Assert.True(saved.IsAvailable);
        Assert.NotEqual("Password123!", saved.PasswordHash);
        Assert.True(BCrypt.Net.BCrypt.Verify("Password123!", saved.PasswordHash));
    }

    [Fact]
    public async Task TC_REG_02_DuplicateEmail_ThrowsBadRequest()
    {
        var req1 = new WasherRegisterRequest
        {
            FirstName = "Dave",
            LastName = "Miller",
            Email = "dupe.washer@example.com",
            Phone = "1112223333",
            Password = "Password123!"
        };
        await _service.RegisterWasherAsync(req1, CancellationToken.None);

        var req2 = new WasherRegisterRequest
        {
            FirstName = "Another",
            LastName = "Washer",
            Email = "dupe.washer@example.com",
            Phone = "9998887777",
            Password = "Password456!"
        };

        await Assert.ThrowsAsync<BadRequestException>(() =>
            _service.RegisterWasherAsync(req2, CancellationToken.None));
    }

    [Fact]
    public async Task TC_REG_04_PhoneNot10DigitsOrDuplicatePhone_ThrowsBadRequest()
    {
        // 1. Phone not 10 digits (too short, too long, non-digits)
        var invalidPhoneShort = new WasherRegisterRequest
        {
            FirstName = "Short",
            LastName = "Phone",
            Email = "short@test.com",
            Phone = "12345",
            Password = "Password1!"
        };
        await Assert.ThrowsAsync<BadRequestException>(() =>
            _service.RegisterWasherAsync(invalidPhoneShort, CancellationToken.None));

        var invalidPhoneChars = new WasherRegisterRequest
        {
            FirstName = "Chars",
            LastName = "Phone",
            Email = "chars@test.com",
            Phone = "12345abcde",
            Password = "Password1!"
        };
        await Assert.ThrowsAsync<BadRequestException>(() =>
            _service.RegisterWasherAsync(invalidPhoneChars, CancellationToken.None));

        // 2. Duplicate phone
        var washer1 = new WasherRegisterRequest
        {
            FirstName = "P1",
            LastName = "Washer",
            Email = "p1@test.com",
            Phone = "9876543210",
            Password = "Password1!"
        };
        await _service.RegisterWasherAsync(washer1, CancellationToken.None);

        var duplicatePhone = new WasherRegisterRequest
        {
            FirstName = "P2",
            LastName = "Washer",
            Email = "p2@test.com",
            Phone = "9876543210",
            Password = "Password1!"
        };
        await Assert.ThrowsAsync<BadRequestException>(() =>
            _service.RegisterWasherAsync(duplicatePhone, CancellationToken.None));
    }

    [Fact]
    public async Task TC_LOG_01_ValidLogin_ReturnsWasher()
    {
        var request = new WasherRegisterRequest
        {
            FirstName = "Log",
            LastName = "Washer",
            Email = "log.washer@example.com",
            Phone = "1234567890",
            Password = "SecurePass123!"
        };
        await _service.RegisterWasherAsync(request, CancellationToken.None);

        var loginReq = new WasherLoginRequest
        {
            Email = "log.washer@example.com",
            Password = "SecurePass123!"
        };

        var washer = await _service.LoginAsync(loginReq, CancellationToken.None);
        Assert.NotNull(washer);
        Assert.Equal("log.washer@example.com", washer.Email);
    }

    [Fact]
    public async Task TC_LOG_02_WrongPasswordOrUnknownEmail_ThrowsUnauthorized()
    {
        var request = new WasherRegisterRequest
        {
            FirstName = "Log",
            LastName = "Washer",
            Email = "auth.test@example.com",
            Phone = "1234567890",
            Password = "SecurePass123!"
        };
        await _service.RegisterWasherAsync(request, CancellationToken.None);

        // Wrong password
        var wrongPassReq = new WasherLoginRequest
        {
            Email = "auth.test@example.com",
            Password = "WrongPassword!"
        };
        await Assert.ThrowsAsync<UnauthorizedException>(() =>
            _service.LoginAsync(wrongPassReq, CancellationToken.None));

        // Unknown email
        var unknownEmailReq = new WasherLoginRequest
        {
            Email = "ghost@example.com",
            Password = "SecurePass123!"
        };
        await Assert.ThrowsAsync<UnauthorizedException>(() =>
            _service.LoginAsync(unknownEmailReq, CancellationToken.None));
    }

    [Fact]
    public async Task TC_DEL_01_NoneActive_DeleteWasher_Success()
    {
        var request = new WasherRegisterRequest
        {
            FirstName = "Del",
            LastName = "Washer",
            Email = "del.washer@example.com",
            Phone = "1234567890",
            Password = "Password123!"
        };
        await _service.RegisterWasherAsync(request, CancellationToken.None);
        var washer = await _dbContext.Washers.SingleAsync(w => w.Email == "del.washer@example.com");

        _mockBookingClient
            .Setup(b => b.HasActiveWasherBookingsAsync(washer.WasherId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        await _service.DeleteWasherAsync(washer.WasherId, CancellationToken.None);

        var deleted = await _dbContext.Washers.FindAsync(washer.WasherId);
        Assert.Null(deleted);
    }

    [Fact]
    public async Task TC_DEL_02_ActiveBookings_DeleteWasher_ThrowsConflict()
    {
        var request = new WasherRegisterRequest
        {
            FirstName = "Active",
            LastName = "Washer",
            Email = "active.washer@example.com",
            Phone = "1234567890",
            Password = "Password123!"
        };
        await _service.RegisterWasherAsync(request, CancellationToken.None);
        var washer = await _dbContext.Washers.SingleAsync(w => w.Email == "active.washer@example.com");

        _mockBookingClient
            .Setup(b => b.HasActiveWasherBookingsAsync(washer.WasherId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        await Assert.ThrowsAsync<ConflictException>(() =>
            _service.DeleteWasherAsync(washer.WasherId, CancellationToken.None));

        var stillExists = await _dbContext.Washers.FindAsync(washer.WasherId);
        Assert.NotNull(stillExists);
    }

    [Fact]
    public async Task TC_DEL_03_BookingServiceDown_ThrowsServiceUnavailable_And_NothingDeleted()
    {
        var request = new WasherRegisterRequest
        {
            FirstName = "Down",
            LastName = "Washer",
            Email = "down.washer@example.com",
            Phone = "1234567890",
            Password = "Password123!"
        };
        await _service.RegisterWasherAsync(request, CancellationToken.None);
        var washer = await _dbContext.Washers.SingleAsync(w => w.Email == "down.washer@example.com");

        _mockBookingClient
            .Setup(b => b.HasActiveWasherBookingsAsync(washer.WasherId, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new ServiceUnavailableException("BookingService unavailable"));

        await Assert.ThrowsAsync<ServiceUnavailableException>(() =>
            _service.DeleteWasherAsync(washer.WasherId, CancellationToken.None));

        var stillExists = await _dbContext.Washers.FindAsync(washer.WasherId);
        Assert.NotNull(stillExists);
    }

    [Fact]
    public async Task TC_DEL_04_BadInternalKey_BookingClientThrowsUnauthorized()
    {
        var fakeHandler = new FakeHttpMessageHandler((req, ct) =>
        {
            var response = new HttpResponseMessage(HttpStatusCode.Unauthorized);
            return Task.FromResult(response);
        });

        var client = new HttpClient(fakeHandler)
        {
            BaseAddress = new Uri("http://localhost:5004/")
        };

        var options = Options.Create(new InternalApiOptions
        {
            Key = "invalid-key"
        });

        var logger = new Mock<ILogger<BookingClient>>();
        var bookingClient = new BookingClient(client, options, logger.Object);

        await Assert.ThrowsAsync<UnauthorizedException>(() =>
            bookingClient.HasActiveWasherBookingsAsync(1L, CancellationToken.None));
    }

    [Fact]
    public async Task UpdateAvailability_TogglesIsAvailable()
    {
        var request = new WasherRegisterRequest
        {
            FirstName = "Toggle",
            LastName = "Washer",
            Email = "toggle@example.com",
            Phone = "1234567890",
            Password = "Password123!"
        };
        await _service.RegisterWasherAsync(request, CancellationToken.None);
        var washer = await _dbContext.Washers.SingleAsync(w => w.Email == "toggle@example.com");

        await _service.UpdateAvailabilityAsync(washer.WasherId, false, CancellationToken.None);
        var updated = await _dbContext.Washers.FindAsync(washer.WasherId);
        Assert.False(updated!.IsAvailable);

        await _service.UpdateAvailabilityAsync(washer.WasherId, true, CancellationToken.None);
        var toggledBack = await _dbContext.Washers.FindAsync(washer.WasherId);
        Assert.True(toggledBack!.IsAvailable);
    }

    [Fact]
    public async Task GetProfileAsync_And_GetAvailable_And_GetAll_ReturnsWashers()
    {
        var req = new WasherRegisterRequest
        {
            FirstName = "List",
            LastName = "Washer",
            Email = "list.washer@example.com",
            Phone = "1234509876",
            Password = "Password123!"
        };
        await _service.RegisterWasherAsync(req, CancellationToken.None);
        var washer = await _dbContext.Washers.SingleAsync(w => w.Email == "list.washer@example.com");

        var profile = await _service.GetProfileAsync(washer.WasherId, CancellationToken.None);
        Assert.Equal("List", profile.FirstName);

        var available = await _service.GetAvailableWashersAsync(CancellationToken.None);
        Assert.Contains(available, w => w.WasherId == washer.WasherId);

        var all = await _service.GetAllWashersAsync(CancellationToken.None);
        Assert.Contains(all, w => w.WasherId == washer.WasherId);
    }

    [Fact]
    public void JwtTokenService_GenerateToken_ReturnsToken()
    {
        var mockHelper = new Mock<CarWash.Common.Authentication.IJwtTokenHelper>();
        mockHelper.Setup(h => h.BuildToken(10L, "w@test.com", "Washer")).Returns("washer-jwt");

        var jwtService = new JwtTokenService(mockHelper.Object);
        var token = jwtService.GenerateToken(10L, "w@test.com");
        Assert.Equal("washer-jwt", token);
    }
}
