using System.Threading.RateLimiting;
using ApiGateway.Middleware;
using CarWash.Common.Extensions;
using CarWash.Common.Logging;
using CarWash.Common.Middleware;
using CarWash.Common.Models;
using Microsoft.AspNetCore.RateLimiting;

var builder = WebApplication.CreateBuilder(args);

// Serilog logging
builder.AddCarWashSerilog("ApiGateway");

// Common building blocks
builder.Services.AddCarWashCommon();

// CORS configuration for supported frontend origins
builder.Services.AddCors(options =>
{
    options.AddPolicy("FrontendCorsPolicy", policy =>
    {
        policy.WithOrigins("http://localhost:4200", "http://localhost:3000", "http://localhost:5173")
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

// Built-in fixed window rate limiter: 100 requests per minute per IP address
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.OnRejected = async (context, token) =>
    {
        context.HttpContext.Response.ContentType = "application/json";
        var correlationId = context.HttpContext.Items[CorrelationIdMiddleware.CorrelationIdItemKey]?.ToString()
            ?? Guid.NewGuid().ToString();

        var error = new ErrorResponse
        {
            Message = "Too many requests. Please try again later.",
            CorrelationId = correlationId
        };

        await context.HttpContext.Response.WriteAsJsonAsync(error, cancellationToken: token);
    };

    options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
    {
        var ipAddress = context.Connection.RemoteIpAddress?.ToString() ?? "anonymous";
        return RateLimitPartition.GetFixedWindowLimiter(ipAddress, _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = 100,
            Window = TimeSpan.FromMinutes(1),
            QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
            QueueLimit = 0
        });
    });
});

// YARP reverse proxy configuration
builder.Services.AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"));

var app = builder.Build();

// Standard CarWash middlewares (Correlation ID, Serilog request logging, exception handling)
app.UseCarWashCommonMiddlewares();

// Apply CORS policy
app.UseCors("FrontendCorsPolicy");

// Apply IP-based Rate Limiter
app.UseRateLimiter();

// Block external access to internal endpoints (paths containing "/internal/")
app.UseMiddleware<InternalEndpointBlockingMiddleware>();

// Route requests through YARP reverse proxy
app.MapReverseProxy();

app.Run();
