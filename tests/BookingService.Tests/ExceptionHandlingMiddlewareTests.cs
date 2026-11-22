namespace BookingService.Tests;

using System.Text.Json;
using CarWash.Common.Exceptions;
using CarWash.Common.Middleware;
using CarWash.Common.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

public class ExceptionHandlingMiddlewareTests
{
    [Fact]
    public async Task InvokeAsync_ShouldCatchNotFoundException_AndReturnStandardErrorResponse()
    {
        // Arrange
        var mockLogger = new Mock<ILogger<ExceptionHandlingMiddleware>>();
        var correlationId = "booking-corr-123";

        var middleware = new ExceptionHandlingMiddleware(
            next: _ => throw new NotFoundException("Booking #45 not found"),
            logger: mockLogger.Object);

        var context = new DefaultHttpContext();
        context.Items[CorrelationIdMiddleware.CorrelationIdItemKey] = correlationId;
        context.Response.Body = new MemoryStream();

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        Assert.Equal(404, context.Response.StatusCode);
        Assert.Equal("application/json", context.Response.ContentType);

        context.Response.Body.Seek(0, SeekOrigin.Begin);
        using var reader = new StreamReader(context.Response.Body);
        var responseBody = await reader.ReadToEndAsync();

        var error = JsonSerializer.Deserialize<ErrorResponse>(responseBody, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        Assert.NotNull(error);
        Assert.Equal("Booking #45 not found", error.Message);
        Assert.Equal(correlationId, error.CorrelationId);
    }

    [Fact]
    public async Task InvokeAsync_ShouldCatchGenericException_AndReturn500WithInternalServerError()
    {
        // Arrange
        var mockLogger = new Mock<ILogger<ExceptionHandlingMiddleware>>();
        var correlationId = "generic-corr-456";

        var middleware = new ExceptionHandlingMiddleware(
            next: _ => throw new InvalidOperationException("Unexpected fatal failure"),
            logger: mockLogger.Object);

        var context = new DefaultHttpContext();
        context.Items[CorrelationIdMiddleware.CorrelationIdItemKey] = correlationId;
        context.Response.Body = new MemoryStream();

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        Assert.Equal(500, context.Response.StatusCode);

        context.Response.Body.Seek(0, SeekOrigin.Begin);
        using var reader = new StreamReader(context.Response.Body);
        var responseBody = await reader.ReadToEndAsync();

        var error = JsonSerializer.Deserialize<ErrorResponse>(responseBody, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        Assert.NotNull(error);
        Assert.Equal("Internal server error", error.Message);
        Assert.Equal(correlationId, error.CorrelationId);
    }
}
