namespace NotificationService.Tests;

using CarWash.Common.Models;
using CarWash.Common.Security;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

public class InternalApiKeyAttributeTests
{
    private const string ExpectedKey = "CarWashInternalSecretApiKey2026!";

    private readonly IConfiguration _configuration;

    public InternalApiKeyAttributeTests()
    {
        var dict = new Dictionary<string, string?>
        {
            { "InternalApi:Key", ExpectedKey }
        };

        _configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(dict)
            .Build();
    }

    [Fact]
    public async Task OnActionExecutionAsync_ShouldReturn401_WhenHeaderIsMissing()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(_configuration);
        var serviceProvider = services.BuildServiceProvider();

        var httpContext = new DefaultHttpContext
        {
            RequestServices = serviceProvider
        };

        var actionContext = new ActionContext(httpContext, new RouteData(), new ActionDescriptor());
        var context = new ActionExecutingContext(actionContext, new List<IFilterMetadata>(), new Dictionary<string, object?>(), controller: null!);

        var attribute = new InternalApiKeyAttribute();
        var nextCalled = false;

        // Act
        await attribute.OnActionExecutionAsync(context, () =>
        {
            nextCalled = true;
            return Task.FromResult(new ActionExecutedContext(actionContext, new List<IFilterMetadata>(), controller: null!));
        });

        // Assert
        Assert.False(nextCalled);
        var objectResult = Assert.IsType<ObjectResult>(context.Result);
        Assert.Equal(401, objectResult.StatusCode);
        var error = Assert.IsType<ErrorResponse>(objectResult.Value);
        Assert.Equal("Missing internal API key header.", error.Message);
    }

    [Fact]
    public async Task OnActionExecutionAsync_ShouldReturn401_WhenKeyIsInvalid()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(_configuration);
        var serviceProvider = services.BuildServiceProvider();

        var httpContext = new DefaultHttpContext
        {
            RequestServices = serviceProvider
        };
        httpContext.Request.Headers[InternalApiKeyAttribute.HeaderName] = "WrongKey123";

        var actionContext = new ActionContext(httpContext, new RouteData(), new ActionDescriptor());
        var context = new ActionExecutingContext(actionContext, new List<IFilterMetadata>(), new Dictionary<string, object?>(), controller: null!);

        var attribute = new InternalApiKeyAttribute();
        var nextCalled = false;

        // Act
        await attribute.OnActionExecutionAsync(context, () =>
        {
            nextCalled = true;
            return Task.FromResult(new ActionExecutedContext(actionContext, new List<IFilterMetadata>(), controller: null!));
        });

        // Assert
        Assert.False(nextCalled);
        var objectResult = Assert.IsType<ObjectResult>(context.Result);
        Assert.Equal(401, objectResult.StatusCode);
        var error = Assert.IsType<ErrorResponse>(objectResult.Value);
        Assert.Equal("Invalid internal API key.", error.Message);
    }

    [Fact]
    public async Task OnActionExecutionAsync_ShouldCallNext_WhenKeyIsValid()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(_configuration);
        var serviceProvider = services.BuildServiceProvider();

        var httpContext = new DefaultHttpContext
        {
            RequestServices = serviceProvider
        };
        httpContext.Request.Headers[InternalApiKeyAttribute.HeaderName] = ExpectedKey;

        var actionContext = new ActionContext(httpContext, new RouteData(), new ActionDescriptor());
        var context = new ActionExecutingContext(actionContext, new List<IFilterMetadata>(), new Dictionary<string, object?>(), controller: null!);

        var attribute = new InternalApiKeyAttribute();
        var nextCalled = false;

        // Act
        await attribute.OnActionExecutionAsync(context, () =>
        {
            nextCalled = true;
            return Task.FromResult(new ActionExecutedContext(actionContext, new List<IFilterMetadata>(), controller: null!));
        });

        // Assert
        Assert.True(nextCalled);
        Assert.Null(context.Result);
    }
}
