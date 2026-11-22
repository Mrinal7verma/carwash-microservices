namespace CarWash.Common.Extensions;

using CarWash.Common.Authentication;
using CarWash.Common.Http;
using Microsoft.Extensions.DependencyInjection;

/// <summary>
/// General service collection extensions for registering shared CarWash building blocks.
/// </summary>
public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddCarWashCommon(this IServiceCollection services)
    {
        services.AddHttpContextAccessor();
        services.AddTransient<CorrelationIdHandler>();
        services.AddScoped<IJwtTokenHelper, JwtTokenHelper>();
        services.AddCustomValidationErrorResponse();

        return services;
    }
}
