using BookingService.Configuration;
using BookingService.Data;
using BookingService.Services;
using CarWash.Common.Authentication;
using CarWash.Common.Extensions;
using CarWash.Common.Http;
using CarWash.Common.Logging;
using CarWash.Common.Middleware;
using CarWash.Common.OpenApi;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Http.Resilience;
using Polly;

var builder = WebApplication.CreateBuilder(args);

// Serilog setup
builder.AddCarWashSerilog("BookingService");

// Options pattern
builder.Services.Configure<ServicesOptions>(builder.Configuration.GetSection(ServicesOptions.SectionName));
builder.Services.Configure<InternalApiOptions>(builder.Configuration.GetSection(InternalApiOptions.SectionName));
builder.Services.Configure<CleanupOptions>(builder.Configuration.GetSection(CleanupOptions.SectionName));

// EF Core DbContext
var connectionString = builder.Configuration.GetConnectionString("BookingDb")
    ?? builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<BookingDbContext>(options =>
{
    if (!string.IsNullOrWhiteSpace(connectionString))
    {
        options.UseSqlServer(connectionString);
    }
});

// Common building blocks
builder.Services.AddCarWashCommon();
builder.Services.AddCarWashJwt(builder.Configuration);

// Application services & background cleanup
builder.Services.AddScoped<IBookingService, BookingServiceImplementation>();
builder.Services.AddHostedService<PendingBookingCleanupService>();

// Resilience configuration helper for inter-service HTTP clients:
// Retries GET requests up to 2 times on transient failures or 5xx responses with 200ms delay.
// Never retries POST/PUT/DELETE requests.
Action<HttpStandardResilienceOptions> configureResilience = options =>
{
    options.AttemptTimeout.Timeout = TimeSpan.FromSeconds(5);
    options.TotalRequestTimeout.Timeout = TimeSpan.FromSeconds(10);
    options.Retry.MaxRetryAttempts = 2;
    options.Retry.BackoffType = DelayBackoffType.Constant;
    options.Retry.Delay = TimeSpan.FromMilliseconds(200);
    options.Retry.ShouldHandle = new PredicateBuilder<HttpResponseMessage>()
        .Handle<HttpRequestException>()
        .Handle<TimeoutException>()
        .HandleResult(response => response.RequestMessage?.Method == HttpMethod.Get && (int)response.StatusCode >= 500);
};

// Typed CustomerClient
builder.Services.AddHttpClient<ICustomerClient, CustomerClient>(client =>
{
    var customerBaseUrl = builder.Configuration["Services:CustomerBaseUrl"] ?? "http://localhost:5002";
    client.BaseAddress = new Uri(customerBaseUrl.TrimEnd('/') + "/");
    client.Timeout = TimeSpan.FromSeconds(5);
})
.AddHttpMessageHandler<CorrelationIdHandler>()
.AddStandardResilienceHandler(configureResilience);

// Typed WasherClient
builder.Services.AddHttpClient<IWasherClient, WasherClient>(client =>
{
    var washerBaseUrl = builder.Configuration["Services:WasherBaseUrl"] ?? "http://localhost:5003";
    client.BaseAddress = new Uri(washerBaseUrl.TrimEnd('/') + "/");
    client.Timeout = TimeSpan.FromSeconds(5);
})
.AddHttpMessageHandler<CorrelationIdHandler>()
.AddStandardResilienceHandler(configureResilience);

// Typed NotificationClient
builder.Services.AddHttpClient<INotificationClient, NotificationClient>(client =>
{
    var notificationBaseUrl = builder.Configuration["Services:NotificationBaseUrl"] ?? "http://localhost:5005";
    client.BaseAddress = new Uri(notificationBaseUrl.TrimEnd('/') + "/");
    client.Timeout = TimeSpan.FromSeconds(5);
})
.AddHttpMessageHandler<CorrelationIdHandler>()
.AddStandardResilienceHandler(configureResilience);

// Controllers & Swagger
builder.Services.AddControllers();
builder.Services.AddCustomValidationErrorResponse();
builder.Services.AddCarWashSwagger("BookingService API");

var app = builder.Build();

// In Development, apply migrations automatically with retry for ~30 seconds
if (app.Environment.IsDevelopment())
{
    using var scope = app.Services.CreateScope();
    var dbContext = scope.ServiceProvider.GetService<BookingDbContext>();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();

    if (dbContext != null && !string.IsNullOrWhiteSpace(connectionString))
    {
        var maxRetries = 10;
        for (var attempt = 1; attempt <= maxRetries; attempt++)
        {
            try
            {
                logger.LogInformation("Attempting to apply BookingDb migrations (attempt {Attempt}/{Total})...", attempt, maxRetries);
                dbContext.Database.Migrate();
                logger.LogInformation("BookingDb migrations applied successfully.");
                break;
            }
            catch (Exception ex) when (attempt < maxRetries)
            {
                logger.LogWarning(ex, "Failed to connect/migrate BookingDb on attempt {Attempt}. Retrying in 3 seconds...", attempt);
                Thread.Sleep(3000);
            }
        }
    }
}

// Common middlewares (CorrelationIdMiddleware, request logging, ExceptionHandlingMiddleware)
app.UseCarWashCommonMiddlewares();

// Swagger enabled ONLY in Development
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "BookingService API v1");
    });
}

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
