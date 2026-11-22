namespace CarWash.Common.Security;

using System.Security.Cryptography;
using System.Text;
using CarWash.Common.Middleware;
using CarWash.Common.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Action filter requiring a valid X-Internal-Api-Key matching InternalApi:Key.
/// Rejects unauthorized requests with HTTP 401 and standard ErrorResponse.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public class InternalApiKeyAttribute : Attribute, IAsyncActionFilter
{
    public const string HeaderName = "X-Internal-Api-Key";

    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var configuration = context.HttpContext.RequestServices.GetRequiredService<IConfiguration>();
        var expectedKey = configuration["InternalApi:Key"];

        if (string.IsNullOrEmpty(expectedKey))
        {
            SetUnauthorizedResult(context, "Internal API key is not configured on the service.");
            return;
        }

        if (!context.HttpContext.Request.Headers.TryGetValue(HeaderName, out var extractedKey) ||
            string.IsNullOrEmpty(extractedKey))
        {
            SetUnauthorizedResult(context, "Missing internal API key header.");
            return;
        }

        var expectedBytes = Encoding.UTF8.GetBytes(expectedKey);
        var actualBytes = Encoding.UTF8.GetBytes(extractedKey.ToString());

        // CryptographicOperations.FixedTimeEquals prevents timing attacks on sensitive key comparison
        if (expectedBytes.Length != actualBytes.Length || !CryptographicOperations.FixedTimeEquals(expectedBytes, actualBytes))
        {
            SetUnauthorizedResult(context, "Invalid internal API key.");
            return;
        }

        await next();
    }

    private static void SetUnauthorizedResult(ActionExecutingContext context, string message)
    {
        var httpContext = context.HttpContext;
        var correlationId = httpContext.Items.TryGetValue(CorrelationIdMiddleware.CorrelationIdItemKey, out var item) && item is string cid
            ? cid
            : (httpContext.Request.Headers[CorrelationIdMiddleware.CorrelationIdHeaderName].FirstOrDefault() ?? httpContext.TraceIdentifier);

        var errorResponse = new ErrorResponse
        {
            Message = message,
            CorrelationId = correlationId
        };

        context.Result = new ObjectResult(errorResponse)
        {
            StatusCode = StatusCodes.Status401Unauthorized
        };
    }
}
