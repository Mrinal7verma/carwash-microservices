namespace CustomerService.Tests;

using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading;
using System.Threading.Tasks;
using CarWash.Common.Exceptions;
using CustomerService.Data;
using CustomerService.DTOs;
using CustomerService.Services;
using CustomerService.Tests.Helpers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

public class CustomerServiceUnitTests
{
    private readonly CustomerDbContext _dbContext;
    private readonly Mock<IBookingClient> _mockBookingClient;
    private readonly global::CustomerService.Services.CustomerService _service;

    public CustomerServiceUnitTests()
    {
        var options = new DbContextOptionsBuilder<CustomerDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _dbContext = new CustomerDbContext(options);
        _mockBookingClient = new Mock<IBookingClient>();
        _service = new global::CustomerService.Services.CustomerService(_dbContext, _mockBookingClient.Object);
    }

    [Fact]
    public async Task TC_REG_01_ValidRegistration_Success_And_PasswordStoredAsBCryptHash()
    {
        var request = new CustomerRegisterRequest
        {
            FirstName = "John",
            LastName = "Doe",
            Email = "john.valid@example.com",
            Phone = "1234567890",
            Password = "MyPassword123!",
            Address = "123 Main St",
            City = "Springfield"
        };

        await _service.RegisterCustomerAsync(request, CancellationToken.None);

        var saved = await _dbContext.Customers.SingleOrDefaultAsync(c => c.Email == "john.valid@example.com");
        Assert.NotNull(saved);
        Assert.NotEqual("MyPassword123!", saved.PasswordHash);
        Assert.True(BCrypt.Net.BCrypt.Verify("MyPassword123!", saved.PasswordHash));
    }

    [Fact]
    public async Task TC_REG_02_DuplicateEmail_ThrowsBadRequest()
    {
        var request = new CustomerRegisterRequest
        {
            FirstName = "Alice",
            LastName = "Duplicate",
            Email = "duplicate@example.com",
            Phone = "1234567890",
            Password = "Password123!"
        };

        await _service.RegisterCustomerAsync(request, CancellationToken.None);

        var duplicateRequest = new CustomerRegisterRequest
        {
            FirstName = "Another",
            LastName = "Person",
            Email = "duplicate@example.com",
            Phone = "9876543210",
            Password = "Password456!"
        };

        await Assert.ThrowsAsync<BadRequestException>(() =>
            _service.RegisterCustomerAsync(duplicateRequest, CancellationToken.None));
    }

    [Theory]
    [InlineData("", "Valid", "valid@test.com", "1234567890", "Pass123!")]
    [InlineData("Valid", "", "valid@test.com", "1234567890", "Pass123!")]
    [InlineData("Valid", "Valid", "not-an-email", "1234567890", "Pass123!")]
    [InlineData("Valid", "Valid", "valid@test.com", "", "Pass123!")]
    [InlineData("Valid", "Valid", "valid@test.com", "1234567890", "")]
    public async Task TC_REG_03_InvalidEmailOrMissingField_ThrowsBadRequest(
        string firstName, string lastName, string email, string phone, string password)
    {
        var request = new CustomerRegisterRequest
        {
            FirstName = firstName,
            LastName = lastName,
            Email = email,
            Phone = phone,
            Password = password
        };

        await Assert.ThrowsAsync<BadRequestException>(() =>
            _service.RegisterCustomerAsync(request, CancellationToken.None));
    }

    [Fact]
    public async Task TC_LOG_01_ValidLogin_ReturnsCustomerEntity()
    {
        var registerRequest = new CustomerRegisterRequest
        {
            FirstName = "Login",
            LastName = "User",
            Email = "login.user@example.com",
            Phone = "1234567890",
            Password = "CorrectPassword1!"
        };
        await _service.RegisterCustomerAsync(registerRequest, CancellationToken.None);

        var customer = await _service.ValidateCustomerAsync("login.user@example.com", "CorrectPassword1!", CancellationToken.None);

        Assert.NotNull(customer);
        Assert.Equal("login.user@example.com", customer.Email);
    }

    [Fact]
    public async Task TC_LOG_02_WrongPasswordOrUnknownEmail_ThrowsUnauthorized()
    {
        var registerRequest = new CustomerRegisterRequest
        {
            FirstName = "User",
            LastName = "Test",
            Email = "user.test@example.com",
            Phone = "1234567890",
            Password = "CorrectPassword1!"
        };
        await _service.RegisterCustomerAsync(registerRequest, CancellationToken.None);

        // Wrong password
        await Assert.ThrowsAsync<UnauthorizedException>(() =>
            _service.ValidateCustomerAsync("user.test@example.com", "WrongPassword!", CancellationToken.None));

        // Unknown email
        await Assert.ThrowsAsync<UnauthorizedException>(() =>
            _service.ValidateCustomerAsync("unknown@example.com", "CorrectPassword1!", CancellationToken.None));
    }

    [Fact]
    public async Task TC_CAR_01_AddCar_Success()
    {
        var customerRequest = new CustomerRegisterRequest
        {
            FirstName = "Car",
            LastName = "Owner",
            Email = "car.owner@example.com",
            Phone = "1234567890",
            Password = "Password123!"
        };
        await _service.RegisterCustomerAsync(customerRequest, CancellationToken.None);
        var customer = await _dbContext.Customers.SingleAsync(c => c.Email == "car.owner@example.com");

        var carRequest = new CarRequest
        {
            Make = "Toyota",
            Model = "Corolla",
            RegistrationNo = "KA-01-MJ-1234"
        };

        var response = await _service.AddCarAsync(customer.CustomerId, carRequest, CancellationToken.None);

        Assert.NotNull(response);
        Assert.Equal("KA-01-MJ-1234", response.RegistrationNo);
        var inDb = await _dbContext.Cars.SingleOrDefaultAsync(c => c.RegistrationNo == "KA-01-MJ-1234");
        Assert.NotNull(inDb);
        Assert.Equal(customer.CustomerId, inDb.CustomerId);
    }

    [Fact]
    public async Task TC_CAR_02_DuplicateOrTooLongRegistrationNo_ThrowsBadRequest()
    {
        var customerRequest = new CustomerRegisterRequest
        {
            FirstName = "Car",
            LastName = "Owner2",
            Email = "car.owner2@example.com",
            Phone = "1234567890",
            Password = "Password123!"
        };
        await _service.RegisterCustomerAsync(customerRequest, CancellationToken.None);
        var customer = await _dbContext.Customers.SingleAsync(c => c.Email == "car.owner2@example.com");

        var car1 = new CarRequest
        {
            Make = "Ford",
            Model = "Focus",
            RegistrationNo = "REG-DUPE-1"
        };
        await _service.AddCarAsync(customer.CustomerId, car1, CancellationToken.None);

        // Duplicate registration
        await Assert.ThrowsAsync<BadRequestException>(() =>
            _service.AddCarAsync(customer.CustomerId, car1, CancellationToken.None));

        // Registration number too long (> 20 chars)
        var tooLong = new CarRequest
        {
            Make = "Ford",
            Model = "Focus",
            RegistrationNo = new string('X', 25)
        };
        await Assert.ThrowsAsync<BadRequestException>(() =>
            _service.AddCarAsync(customer.CustomerId, tooLong, CancellationToken.None));
    }

    [Fact]
    public async Task TC_CAR_03_AnotherCustomersCar_Returns404()
    {
        var cust1 = new CustomerRegisterRequest { FirstName = "C1", LastName = "L1", Email = "c1@test.com", Phone = "111", Password = "Password1!" };
        var cust2 = new CustomerRegisterRequest { FirstName = "C2", LastName = "L2", Email = "c2@test.com", Phone = "222", Password = "Password1!" };
        await _service.RegisterCustomerAsync(cust1, CancellationToken.None);
        await _service.RegisterCustomerAsync(cust2, CancellationToken.None);

        var c1Entity = await _dbContext.Customers.SingleAsync(c => c.Email == "c1@test.com");
        var c2Entity = await _dbContext.Customers.SingleAsync(c => c.Email == "c2@test.com");

        var carResponse = await _service.AddCarAsync(c1Entity.CustomerId, new CarRequest { Make = "Honda", Model = "City", RegistrationNo = "C1-CAR" }, CancellationToken.None);

        // C2 requests C1's car
        await Assert.ThrowsAsync<NotFoundException>(() =>
            _service.GetCarByIdAsync(c2Entity.CustomerId, carResponse.CarId, CancellationToken.None));
    }

    [Fact]
    public async Task TC_CAR_04_DeleteCar_WithActiveBooking_ThrowsConflictException()
    {
        var cust = new CustomerRegisterRequest { FirstName = "Car", LastName = "Active", Email = "car.act@test.com", Phone = "111", Password = "Password1!" };
        await _service.RegisterCustomerAsync(cust, CancellationToken.None);
        var cEntity = await _dbContext.Customers.SingleAsync(c => c.Email == "car.act@test.com");

        var car = await _service.AddCarAsync(cEntity.CustomerId, new CarRequest { Make = "BMW", Model = "320d", RegistrationNo = "ACT-CAR" }, CancellationToken.None);

        _mockBookingClient
            .Setup(b => b.HasActiveCarBookingsAsync(car.CarId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        await Assert.ThrowsAsync<ConflictException>(() =>
            _service.DeleteCarAsync(cEntity.CustomerId, car.CarId, CancellationToken.None));
    }

    [Fact]
    public async Task TC_DEL_01_NoneActive_DeleteCustomer_Success()
    {
        var cust = new CustomerRegisterRequest { FirstName = "Clean", LastName = "Delete", Email = "clean@test.com", Phone = "111", Password = "Password1!" };
        await _service.RegisterCustomerAsync(cust, CancellationToken.None);
        var cEntity = await _dbContext.Customers.SingleAsync(c => c.Email == "clean@test.com");

        _mockBookingClient
            .Setup(b => b.HasActiveCustomerBookingsAsync(cEntity.CustomerId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        await _service.DeleteCustomerAsync(cEntity.CustomerId, CancellationToken.None);

        var deleted = await _dbContext.Customers.FindAsync(cEntity.CustomerId);
        Assert.Null(deleted);
    }

    [Fact]
    public async Task TC_DEL_02_ActiveBookings_DeleteCustomer_ThrowsConflict()
    {
        var cust = new CustomerRegisterRequest { FirstName = "Active", LastName = "User", Email = "active.del@test.com", Phone = "111", Password = "Password1!" };
        await _service.RegisterCustomerAsync(cust, CancellationToken.None);
        var cEntity = await _dbContext.Customers.SingleAsync(c => c.Email == "active.del@test.com");

        _mockBookingClient
            .Setup(b => b.HasActiveCustomerBookingsAsync(cEntity.CustomerId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        await Assert.ThrowsAsync<ConflictException>(() =>
            _service.DeleteCustomerAsync(cEntity.CustomerId, CancellationToken.None));

        // Ensure customer was NOT deleted
        var stillExists = await _dbContext.Customers.FindAsync(cEntity.CustomerId);
        Assert.NotNull(stillExists);
    }

    [Fact]
    public async Task TC_DEL_03_BookingServiceDown_ThrowsServiceUnavailable_And_NothingDeleted()
    {
        var cust = new CustomerRegisterRequest { FirstName = "Down", LastName = "Booking", Email = "down.book@test.com", Phone = "111", Password = "Password1!" };
        await _service.RegisterCustomerAsync(cust, CancellationToken.None);
        var cEntity = await _dbContext.Customers.SingleAsync(c => c.Email == "down.book@test.com");

        _mockBookingClient
            .Setup(b => b.HasActiveCustomerBookingsAsync(cEntity.CustomerId, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new ServiceUnavailableException("BookingService is down"));

        await Assert.ThrowsAsync<ServiceUnavailableException>(() =>
            _service.DeleteCustomerAsync(cEntity.CustomerId, CancellationToken.None));

        // Nothing deleted
        var stillExists = await _dbContext.Customers.FindAsync(cEntity.CustomerId);
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

        var options = Options.Create(new global::CustomerService.Configuration.InternalApiOptions
        {
            Key = "invalid-key"
        });

        var logger = new Mock<ILogger<BookingClient>>();
        var bookingClient = new BookingClient(client, options, logger.Object);

        await Assert.ThrowsAsync<UnauthorizedException>(() =>
            bookingClient.HasActiveCustomerBookingsAsync(1L, CancellationToken.None));
    }

    [Fact]
    public async Task GetProfileAsync_ReturnsCustomerProfile()
    {
        var cust = new CustomerRegisterRequest { FirstName = "Prof", LastName = "User", Email = "prof@test.com", Phone = "111", Password = "Password1!" };
        await _service.RegisterCustomerAsync(cust, CancellationToken.None);
        var c = await _dbContext.Customers.SingleAsync(x => x.Email == "prof@test.com");

        var profile = await _service.GetProfileAsync(c.CustomerId, CancellationToken.None);
        Assert.NotNull(profile);
        Assert.Equal("Prof", profile.FirstName);
    }

    [Fact]
    public async Task GetAllCustomersAsync_ReturnsAllRegisteredCustomers()
    {
        var list = await _service.GetAllCustomersAsync(CancellationToken.None);
        Assert.NotNull(list);
    }

    [Fact]
    public async Task GetCustomerEmailAsync_ReturnsEmail()
    {
        var cust = new CustomerRegisterRequest { FirstName = "Em", LastName = "User", Email = "em@test.com", Phone = "111", Password = "Password1!" };
        await _service.RegisterCustomerAsync(cust, CancellationToken.None);
        var c = await _dbContext.Customers.SingleAsync(x => x.Email == "em@test.com");

        var email = await _service.GetCustomerEmailAsync(c.CustomerId, CancellationToken.None);
        Assert.Equal("em@test.com", email);
    }

    [Fact]
    public async Task GetCarsAsync_And_GetCarByIdAsync_And_DeleteCarAsync_Success()
    {
        var cust = new CustomerRegisterRequest { FirstName = "Cars", LastName = "User", Email = "cars@test.com", Phone = "111", Password = "Password1!" };
        await _service.RegisterCustomerAsync(cust, CancellationToken.None);
        var c = await _dbContext.Customers.SingleAsync(x => x.Email == "cars@test.com");

        var carResp = await _service.AddCarAsync(c.CustomerId, new CarRequest { Make = "Hyundai", Model = "i20", RegistrationNo = "KA-99-9999" }, CancellationToken.None);
        var cars = await _service.GetCarsAsync(c.CustomerId, CancellationToken.None);
        Assert.Single(cars);

        var carById = await _service.GetCarByIdAsync(c.CustomerId, carResp.CarId, CancellationToken.None);
        Assert.Equal("Hyundai", carById.Make);

        _mockBookingClient.Setup(b => b.HasActiveCarBookingsAsync(carResp.CarId, It.IsAny<CancellationToken>())).ReturnsAsync(false);
        await _service.DeleteCarAsync(c.CustomerId, carResp.CarId, CancellationToken.None);

        var deletedCar = await _dbContext.Cars.FindAsync(carResp.CarId);
        Assert.Null(deletedCar);
    }

    [Fact]
    public void JwtService_GenerateToken_ReturnsToken()
    {
        var mockHelper = new Mock<CarWash.Common.Authentication.IJwtTokenHelper>();
        mockHelper.Setup(h => h.BuildToken(10L, "test@test.com", "Customer")).Returns("jwt-token");
        var jwtService = new JwtService(mockHelper.Object);
        var token = jwtService.GenerateToken(10L, "test@test.com");
        Assert.Equal("jwt-token", token);
    }

    [Fact]
    public async Task BookingClient_HasActiveCarBookingsAsync_ReturnsTrue()
    {
        var fakeHandler = new FakeHttpMessageHandler((req, ct) =>
        {
            var response = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("{\"hasActiveBookings\":true}", System.Text.Encoding.UTF8, "application/json")
            };
            return Task.FromResult(response);
        });

        var client = new HttpClient(fakeHandler) { BaseAddress = new Uri("http://localhost:5004/") };
        var options = Options.Create(new global::CustomerService.Configuration.InternalApiOptions { Key = "k" });
        var logger = new Mock<ILogger<BookingClient>>();
        var bookingClient = new BookingClient(client, options, logger.Object);

        var result = await bookingClient.HasActiveCarBookingsAsync(1L, CancellationToken.None);
        Assert.True(result);
    }
}
