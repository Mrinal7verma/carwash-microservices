namespace CarWash.Common.Middleware;

using System.Text.Json;
using CarWash.Common.Exceptions;
using CarWash.Common.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

/// <summary>
/// Global exception handling middleware that catches domain and unhandled exceptions,
/// translates them to appropriate HTTP status codes, and outputs the standard ErrorResponse shape.
/// </summary>
public class ExceptionHandlingMiddleware
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(context, ex);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        var correlationId = context.Items.TryGetValue(CorrelationIdMiddleware.CorrelationIdItemKey, out var item) && item is string cid
            ? cid
            : (context.Request.Headers[CorrelationIdMiddleware.CorrelationIdHeaderName].FirstOrDefault() ?? context.TraceIdentifier);

        int statusCode;
        string message;

        switch (exception)
        {
            case CarWashException cwe:
                statusCode = cwe.StatusCode;
                message = cwe.Message;
                _logger.LogWarning(exception, "Handled CarWashException ({StatusCode}): {Message}. CorrelationId: {CorrelationId}", statusCode, message, correlationId);
                break;
            default:
                statusCode = StatusCodes.Status500InternalServerError;
                message = "Internal server error";
                _logger.LogError(exception, "Unhandled exception occurred. CorrelationId: {CorrelationId}", correlationId);
                break;
        }

        if (!context.Response.HasStarted)
        {
            context.Response.Clear();
            context.Response.StatusCode = statusCode;
            context.Response.ContentType = "application/json";

            var response = new ErrorResponse
            {
                Message = message,
                CorrelationId = correlationId
            };

            await context.Response.WriteAsync(JsonSerializer.Serialize(response, JsonOptions));
        }
    }
}
