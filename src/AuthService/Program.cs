using AuthService.Configuration;
using AuthService.Services;
using CarWash.Common.Authentication;
using CarWash.Common.Extensions;
using CarWash.Common.Logging;
using CarWash.Common.Middleware;
using CarWash.Common.OpenApi;

var builder = WebApplication.CreateBuilder(args);

// Serilog setup
builder.AddCarWashSerilog("AuthService");

// Options pattern
builder.Services.Configure<AdminOptions>(builder.Configuration.GetSection(AdminOptions.SectionName));

// Common building blocks (CorrelationIdHandler, IJwtTokenHelper, HttpContextAccessor, ApiBehaviorOptions custom error formatting)
builder.Services.AddCarWashCommon();
builder.Services.AddCarWashJwt(builder.Configuration);

// Application services
builder.Services.AddScoped<IAdminTokenService, AdminTokenService>();

// Controllers & Swagger (Development only)
builder.Services.AddControllers();
builder.Services.AddCustomValidationErrorResponse();
builder.Services.AddCarWashSwagger("AuthService API");

var app = builder.Build();

// Common middlewares (CorrelationIdMiddleware, request logging, ExceptionHandlingMiddleware)
app.UseCarWashCommonMiddlewares();

// Swagger enabled ONLY in Development
app.UseCarWashSwagger();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
