namespace CarWash.Common.OpenApi;

using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.OpenApi;
using Swashbuckle.AspNetCore.SwaggerGen;

/// <summary>
/// OpenAPI/Swagger configuration extensions with Bearer authentication definition adapted for Microsoft.OpenApi.
/// </summary>
public static class SwaggerExtensions
{
    public static IServiceCollection AddCarWashSwagger(this IServiceCollection services, string title, string version = "v1")
    {
        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen(options =>
        {
            options.SwaggerDoc(version, new OpenApiInfo
            {
                Title = title,
                Version = version
            });

            var securityScheme = new OpenApiSecurityScheme
            {
                Name = "Authorization",
                Description = "Enter JWT Bearer token in the format: Bearer {token}",
                In = ParameterLocation.Header,
                Type = SecuritySchemeType.Http,
                Scheme = "bearer",
                BearerFormat = "JWT"
            };

            options.AddSecurityDefinition("Bearer", securityScheme);

            options.AddSecurityRequirement(document => new OpenApiSecurityRequirement
            {
                {
                    new OpenApiSecuritySchemeReference("Bearer", document),
                    new List<string>()
                }
            });
        });

        return services;
    }

    public static IApplicationBuilder UseCarWashSwagger(this IApplicationBuilder app, IHostEnvironment environment)
    {
        // Enabled ONLY in Development
        if (environment.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI();
        }

        return app;
    }

    public static WebApplication UseCarWashSwagger(this WebApplication app)
    {
        return (WebApplication)UseCarWashSwagger((IApplicationBuilder)app, app.Environment);
    }
}
