namespace CarWash.Common.Authentication;

using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;

/// <summary>
/// Service collection extension to configure standard JWT authentication for CarWash services.
/// </summary>
public static class JwtAuthenticationExtensions
{
    public static IServiceCollection AddCarWashJwt(this IServiceCollection services, IConfiguration configuration)
    {
        var key = configuration["Jwt:Key"]
            ?? throw new InvalidOperationException("Configuration 'Jwt:Key' is required for JWT authentication.");
        var issuer = configuration["Jwt:Issuer"]
            ?? throw new InvalidOperationException("Configuration 'Jwt:Issuer' is required for JWT authentication.");
        var audience = configuration["Jwt:Audience"]
            ?? throw new InvalidOperationException("Configuration 'Jwt:Audience' is required for JWT authentication.");

        var keyBytes = Encoding.UTF8.GetBytes(key);

        services.AddAuthentication(options =>
        {
            options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
        })
        .AddJwtBearer(options =>
        {
            options.RequireHttpsMetadata = false;
            options.SaveToken = true;
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(keyBytes),
                ValidateIssuer = true,
                ValidIssuer = issuer,
                ValidateAudience = true,
                ValidAudience = audience,
                ValidateLifetime = true,
                ClockSkew = TimeSpan.FromSeconds(30),
                RoleClaimType = ClaimTypes.Role,
                NameClaimType = ClaimTypes.NameIdentifier
            };
        });

        services.AddAuthorization();

        return services;
    }
}
