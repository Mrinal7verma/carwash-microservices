using CarWash.Common.Extensions;
using CarWash.Common.Logging;
using CarWash.Common.Middleware;
using CarWash.Common.OpenApi;
using NotificationService.Configuration;
using NotificationService.Services;

var builder = WebApplication.CreateBuilder(args);

// Serilog setup
builder.AddCarWashSerilog("NotificationService");

// Options pattern
builder.Services.Configure<SmtpOptions>(builder.Configuration.GetSection(SmtpOptions.SectionName));

// Common building blocks (CorrelationIdHandler, HttpContextAccessor, ApiBehaviorOptions custom error formatting)
builder.Services.AddCarWashCommon();

// Email services
builder.Services.AddScoped<IEmailService, SmtpEmailService>();

// Controllers & Swagger (Development only)
builder.Services.AddControllers();
builder.Services.AddCustomValidationErrorResponse();
builder.Services.AddCarWashSwagger("NotificationService API");

var app = builder.Build();

// Common middlewares (CorrelationIdMiddleware, request logging, ExceptionHandlingMiddleware)
app.UseCarWashCommonMiddlewares();

// Swagger enabled ONLY in Development
app.UseCarWashSwagger();

app.MapControllers();

app.Run();
