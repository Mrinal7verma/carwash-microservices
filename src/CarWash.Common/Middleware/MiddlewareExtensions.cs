namespace CarWash.Common.Middleware;

using CarWash.Common.Logging;
using Microsoft.AspNetCore.Builder;

/// <summary>
/// Pipeline extensions to register the standard CarWash middlewares in correct order.
/// </summary>
public static class MiddlewareExtensions
{
    public static IApplicationBuilder UseCarWashCommonMiddlewares(this IApplicationBuilder app)
    {
        // 1. Correlation ID is attached first so it enriches all downstream logs and headers
        app.UseMiddleware<CorrelationIdMiddleware>();

        // 2. Serilog request logging with correlation ID enrichment
        app.UseCarWashRequestLogging();

        // 3. Exception handling catches any unhandled error and returns unified JSON format
        app.UseMiddleware<ExceptionHandlingMiddleware>();

        return app;
    }
}
