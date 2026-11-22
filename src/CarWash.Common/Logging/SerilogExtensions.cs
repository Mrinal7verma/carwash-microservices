namespace CarWash.Common.Logging;

using CarWash.Common.Middleware;
using Microsoft.AspNetCore.Builder;
using Serilog;
using Serilog.Events;

/// <summary>
/// Serilog setup helpers configuring console and daily rolling file logging with sensitive data masking.
/// </summary>
public static class SerilogExtensions
{
    private const string OutputTemplate = "[{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz}] [{Level:u3}] [{CorrelationId}] {Message:lj}{NewLine}{Exception}";

    public static WebApplicationBuilder AddCarWashSerilog(this WebApplicationBuilder builder, string serviceName)
    {
        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Information()
            .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
            .MinimumLevel.Override("Microsoft.Hosting.Lifetime", LogEventLevel.Information)
            .MinimumLevel.Override("Microsoft.AspNetCore.Hosting", LogEventLevel.Information)
            .MinimumLevel.Override("Microsoft.AspNetCore.Routing", LogEventLevel.Warning)
            .Enrich.FromLogContext()
            .Enrich.WithProperty("Service", serviceName)
            .Enrich.With<SensitiveDataMaskingEnricher>()
            .WriteTo.Console(outputTemplate: OutputTemplate)
            .WriteTo.File(
                path: $"logs/{serviceName}-.log",
                rollingInterval: RollingInterval.Day,
                outputTemplate: OutputTemplate,
                retainedFileCountLimit: 14)
            .CreateLogger();

        builder.Host.UseSerilog();

        return builder;
    }

    public static IApplicationBuilder UseCarWashRequestLogging(this IApplicationBuilder app)
    {
        return app.UseSerilogRequestLogging(options =>
        {
            options.MessageTemplate = "HTTP {RequestMethod} {RequestPath} responded {StatusCode} in {Elapsed:0.0000} ms";
            options.EnrichDiagnosticContext = (diagnosticContext, httpContext) =>
            {
                if (httpContext.Items.TryGetValue(CorrelationIdMiddleware.CorrelationIdItemKey, out var item) && item is string cid)
                {
                    diagnosticContext.Set("CorrelationId", cid);
                }
                else
                {
                    var headerCid = httpContext.Request.Headers[CorrelationIdMiddleware.CorrelationIdHeaderName].FirstOrDefault();
                    if (!string.IsNullOrWhiteSpace(headerCid))
                    {
                        diagnosticContext.Set("CorrelationId", headerCid);
                    }
                }

                diagnosticContext.Set("Host", httpContext.Request.Host.Value ?? string.Empty);
                diagnosticContext.Set("Scheme", httpContext.Request.Scheme);
            };
        });
    }
}
