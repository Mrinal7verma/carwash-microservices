namespace CarWash.Common.Extensions;

using System.Linq;
using CarWash.Common.Middleware;
using CarWash.Common.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Extensions to configure API model validation response format to match standard ErrorResponse.
/// </summary>
public static class ModelStateExtensions
{
    public static IServiceCollection AddCustomValidationErrorResponse(this IServiceCollection services)
    {
        services.Configure<ApiBehaviorOptions>(options =>
        {
            options.InvalidModelStateResponseFactory = actionContext =>
            {
                var errors = actionContext.ModelState
                    .Where(entry => entry.Value != null && entry.Value.Errors.Count > 0)
                    .SelectMany(entry => entry.Value!.Errors.Select(err =>
                        string.IsNullOrWhiteSpace(err.ErrorMessage) ? "Invalid model input." : err.ErrorMessage))
                    .ToArray();

                var message = string.Join("; ", errors);
                var httpContext = actionContext.HttpContext;
                var correlationId = httpContext.Items.TryGetValue(CorrelationIdMiddleware.CorrelationIdItemKey, out var item) && item is string cid
                    ? cid
                    : (httpContext.Request.Headers[CorrelationIdMiddleware.CorrelationIdHeaderName].FirstOrDefault() ?? httpContext.TraceIdentifier);

                var errorResponse = new ErrorResponse
                {
                    Message = message,
                    CorrelationId = correlationId
                };

                return new BadRequestObjectResult(errorResponse);
            };
        });

        return services;
    }
}
