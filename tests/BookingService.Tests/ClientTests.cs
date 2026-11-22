namespace BookingService.Tests;

using System;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using BookingService.Configuration;
using BookingService.DTOs;
using BookingService.Services;
using BookingService.Tests.Helpers;
using CarWash.Common.Exceptions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

public class ClientTests
{
    [Fact]
    public async Task CustomerClient_ValidateCustomerCarAsync_Success()
    {
        string? requestedPath = null;
        var fakeHandler = new FakeHttpMessageHandler((req, ct) =>
        {
            requestedPath = req.RequestUri?.ToString();
            var json = "{\"carId\":1,\"make\":\"Toyota\",\"model\":\"Corolla\",\"registrationNo\":\"KA-01-1234\"}";
            var response = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(json, System.Text.Encoding.UTF8, "application/json")
            };
            return Task.FromResult(response);
        });

        var httpClient = new HttpClient(fakeHandler) { BaseAddress = new Uri("http://localhost:5002/") };
        var mockContextAccessor = new Mock<IHttpContextAccessor>();
        var options = Options.Create(new InternalApiOptions { Key = "secret" });
        var logger = new Mock<ILogger<CustomerClient>>();

        var client = new CustomerClient(httpClient, mockContextAccessor.Object, options, logger.Object);
        var result = await client.ValidateCustomerCarAsync(1L, CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(1L, result.CarId);
        Assert.Equal("Toyota", result.Make);
        Assert.Contains("api/customers/cars/1", requestedPath);
    }

    [Fact]
    public async Task CustomerClient_ValidateCustomerCarAsync_NotFound_ThrowsNotFoundException()
    {
        var fakeHandler = new FakeHttpMessageHandler((req, ct) =>
        {
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound));
        });

        var httpClient = new HttpClient(fakeHandler) { BaseAddress = new Uri("http://localhost:5002/") };
        var mockContextAccessor = new Mock<IHttpContextAccessor>();
        var options = Options.Create(new InternalApiOptions { Key = "secret" });
        var logger = new Mock<ILogger<CustomerClient>>();

        var client = new CustomerClient(httpClient, mockContextAccessor.Object, options, logger.Object);
        await Assert.ThrowsAsync<NotFoundException>(() =>
            client.ValidateCustomerCarAsync(999L, CancellationToken.None));
    }

    [Fact]
    public async Task CustomerClient_GetCustomerEmailAsync_Success_And_Errors()
    {
        // 1. Success
        string? requestedPath = null;
        var fakeHandler = new FakeHttpMessageHandler((req, ct) =>
        {
            requestedPath = req.RequestUri?.ToString();
            var json = "{\"email\":\"customer@test.com\"}";
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(json, System.Text.Encoding.UTF8, "application/json")
            });
        });
        var httpClient = new HttpClient(fakeHandler) { BaseAddress = new Uri("http://localhost:5002/") };
        var mockContextAccessor = new Mock<IHttpContextAccessor>();
        var options = Options.Create(new InternalApiOptions { Key = "secret" });
        var logger = new Mock<ILogger<CustomerClient>>();

        var client = new CustomerClient(httpClient, mockContextAccessor.Object, options, logger.Object);
        var email = await client.GetCustomerEmailAsync(1L, CancellationToken.None);
        Assert.Equal("customer@test.com", email);
        Assert.Contains("api/customers/internal/1/email", requestedPath);

        // 2. 500 error throws ServiceUnavailableException
        var failHandler = new FakeHttpMessageHandler((req, ct) =>
        {
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.InternalServerError));
        });
        var failClient = new CustomerClient(new HttpClient(failHandler) { BaseAddress = new Uri("http://localhost:5002/") }, mockContextAccessor.Object, options, logger.Object);
        await Assert.ThrowsAsync<ServiceUnavailableException>(() => failClient.GetCustomerEmailAsync(1L, CancellationToken.None));
    }

    [Fact]
    public async Task WasherClient_ValidateWasherAsync_Success_And_Errors()
    {
        // 1. Success
        var fakeHandler = new FakeHttpMessageHandler((req, ct) =>
        {
            var json = "{\"washerId\":1,\"firstName\":\"John\",\"lastName\":\"Doe\",\"isAvailable\":true}";
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(json, System.Text.Encoding.UTF8, "application/json")
            });
        });

        var httpClient = new HttpClient(fakeHandler) { BaseAddress = new Uri("http://localhost:5003/") };
        var mockContextAccessor = new Mock<IHttpContextAccessor>();
        var logger = new Mock<ILogger<WasherClient>>();

        var client = new WasherClient(httpClient, mockContextAccessor.Object, logger.Object);
        await client.ValidateWasherAsync(CancellationToken.None);

        // 2. Unauthorized throws UnauthorizedException
        var unauthHandler = new FakeHttpMessageHandler((req, ct) =>
        {
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.Unauthorized));
        });
        var unauthClient = new WasherClient(new HttpClient(unauthHandler) { BaseAddress = new Uri("http://localhost:5003/") }, mockContextAccessor.Object, logger.Object);
        await Assert.ThrowsAsync<UnauthorizedException>(() => unauthClient.ValidateWasherAsync(CancellationToken.None));
    }

    [Fact]
    public async Task NotificationClient_SendBookingEmailAsync_Success_And_CatchesErrors()
    {
        // 1. Success
        var fakeHandler = new FakeHttpMessageHandler((req, ct) =>
        {
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK));
        });
        var httpClient = new HttpClient(fakeHandler) { BaseAddress = new Uri("http://localhost:5005/") };
        var logger = new Mock<ILogger<NotificationClient>>();

        var client = new NotificationClient(httpClient, logger.Object);
        await client.SendBookingEmailAsync("to@test.com", "Subject", "Body", CancellationToken.None);

        // 2. Failure handled gracefully without unhandled exception
        var failingHandler = new FakeHttpMessageHandler((req, ct) =>
        {
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.InternalServerError));
        });
        var failHttpClient = new HttpClient(failingHandler) { BaseAddress = new Uri("http://localhost:5005/") };
        var failClient = new NotificationClient(failHttpClient, logger.Object);
        await failClient.SendBookingEmailAsync("to@test.com", "Subject", "Body", CancellationToken.None);
    }
}
