namespace ApiGateway.Middleware;

using CarWash.Common.Middleware;
using CarWash.Common.Models;
using Microsoft.AspNetCore.Http;

/// <summary>
/// Gateway middleware that intercepts incoming requests and returns 404 Not Found
/// for any path containing "/internal/" so internal endpoints are never exposed externally.
/// </summary>
public class InternalEndpointBlockingMiddleware
{
    private readonly RequestDelegate _next;

    public InternalEndpointBlockingMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var path = context.Request.Path.Value;

        if (!string.IsNullOrEmpty(path) &&
            (path.Contains("/internal/", StringComparison.OrdinalIgnoreCase) ||
             path.EndsWith("/internal", StringComparison.OrdinalIgnoreCase)))
        {
            var correlationId = context.Items[CorrelationIdMiddleware.CorrelationIdItemKey]?.ToString()
                ?? context.Request.Headers[CorrelationIdMiddleware.CorrelationIdHeaderName].FirstOrDefault()
                ?? Guid.NewGuid().ToString();

            context.Response.StatusCode = StatusCodes.Status404NotFound;
            context.Response.ContentType = "application/json";

            var errorResponse = new ErrorResponse
            {
                Message = "Endpoint not found.",
                CorrelationId = correlationId
            };

            await context.Response.WriteAsJsonAsync(errorResponse);
            return;
        }

        await _next(context);
    }
}
