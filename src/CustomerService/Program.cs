using CarWash.Common.Authentication;
using CarWash.Common.Extensions;
using CarWash.Common.Http;
using CarWash.Common.Logging;
using CarWash.Common.Middleware;
using CarWash.Common.OpenApi;
using CustomerService.Configuration;
using CustomerService.Data;
using CustomerService.Services;
using Microsoft.EntityFrameworkCore;
using Polly;

var builder = WebApplication.CreateBuilder(args);

// Serilog setup
builder.AddCarWashSerilog("CustomerService");

// Options pattern
builder.Services.Configure<ServicesOptions>(builder.Configuration.GetSection(ServicesOptions.SectionName));
builder.Services.Configure<InternalApiOptions>(builder.Configuration.GetSection(InternalApiOptions.SectionName));

// EF Core DbContext
var connectionString = builder.Configuration.GetConnectionString("CustomerDb")
    ?? builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<CustomerDbContext>(options =>
{
    if (!string.IsNullOrWhiteSpace(connectionString))
    {
        options.UseSqlServer(connectionString);
    }
});

// Common services (CorrelationIdHandler, IJwtTokenHelper, HttpContextAccessor, ApiBehaviorOptions custom error formatting)
builder.Services.AddCarWashCommon();
builder.Services.AddCarWashJwt(builder.Configuration);

// Application services
builder.Services.AddScoped<ICustomerService, CustomerService.Services.CustomerService>();
builder.Services.AddScoped<IJwtService, JwtService>();

// Typed BookingClient with resilience and CorrelationId forwarding
builder.Services.AddHttpClient<IBookingClient, BookingClient>(client =>
{
    var bookingBaseUrl = builder.Configuration["Services:BookingBaseUrl"] ?? "http://localhost:5004";
    client.BaseAddress = new Uri(bookingBaseUrl.TrimEnd('/') + "/");
    client.Timeout = TimeSpan.FromSeconds(5);
})
.AddHttpMessageHandler<CorrelationIdHandler>()
.AddStandardResilienceHandler(options =>
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
});

// Controllers & Swagger (Development only)
builder.Services.AddControllers();
builder.Services.AddCustomValidationErrorResponse();
builder.Services.AddCarWashSwagger("CustomerService API");

var app = builder.Build();

// In Development, apply migrations automatically with retry for ~30 seconds
if (app.Environment.IsDevelopment())
{
    using var scope = app.Services.CreateScope();
    var dbContext = scope.ServiceProvider.GetService<CustomerDbContext>();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();

    if (dbContext != null && !string.IsNullOrWhiteSpace(connectionString))
    {
        var maxRetries = 10;
        for (var attempt = 1; attempt <= maxRetries; attempt++)
        {
            try
            {
                logger.LogInformation("Attempting to apply CustomerDb migrations (attempt {Attempt}/{Total})...", attempt, maxRetries);
                dbContext.Database.Migrate();
                logger.LogInformation("CustomerDb migrations applied successfully.");
                break;
            }
            catch (Exception ex) when (attempt < maxRetries)
            {
                logger.LogWarning(ex, "Failed to connect/migrate CustomerDb on attempt {Attempt}. Retrying in 3 seconds...", attempt);
                Thread.Sleep(3000);
            }
        }
    }
}

// Common middlewares (CorrelationIdMiddleware, request logging, ExceptionHandlingMiddleware)
app.UseCarWashCommonMiddlewares();

// Swagger enabled ONLY in Development
app.UseCarWashSwagger();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
