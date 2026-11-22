namespace CarWash.Common.Middleware;

using Microsoft.AspNetCore.Http;
using Serilog.Context;

/// <summary>
/// Middleware to read or generate correlation IDs, assign to request and response headers,
/// and enrich the Serilog LogContext with property CorrelationId.
/// </summary>
public class CorrelationIdMiddleware
{
    public const string CorrelationIdHeaderName = "X-Correlation-Id";
    public const string CorrelationIdItemKey = "CorrelationId";

    private readonly RequestDelegate _next;

    public CorrelationIdMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var correlationId = context.Request.Headers[CorrelationIdHeaderName].FirstOrDefault();
        if (string.IsNullOrWhiteSpace(correlationId))
        {
            correlationId = Guid.NewGuid().ToString();
            context.Request.Headers[CorrelationIdHeaderName] = correlationId;
        }

        context.Items[CorrelationIdItemKey] = correlationId;

        // Ensure header is attached to outgoing response headers
        context.Response.OnStarting(() =>
        {
            if (!context.Response.Headers.ContainsKey(CorrelationIdHeaderName))
            {
                context.Response.Headers.Append(CorrelationIdHeaderName, correlationId);
            }

            return Task.CompletedTask;
        });

        using (LogContext.PushProperty("CorrelationId", correlationId))
        {
            await _next(context);
        }
    }
}
