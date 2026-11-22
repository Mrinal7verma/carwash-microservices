namespace CarWash.Common.Http;

using CarWash.Common.Middleware;
using Microsoft.AspNetCore.Http;

/// <summary>
/// DelegatingHandler for outgoing HttpClient calls that propagates the current X-Correlation-Id header.
/// </summary>
public class CorrelationIdHandler : DelegatingHandler
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CorrelationIdHandler(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var httpContext = _httpContextAccessor.HttpContext;
        if (httpContext != null &&
            httpContext.Items.TryGetValue(CorrelationIdMiddleware.CorrelationIdItemKey, out var item) &&
            item is string correlationId &&
            !string.IsNullOrWhiteSpace(correlationId))
        {
            if (!request.Headers.Contains(CorrelationIdMiddleware.CorrelationIdHeaderName))
            {
                request.Headers.Add(CorrelationIdMiddleware.CorrelationIdHeaderName, correlationId);
            }
        }

        return await base.SendAsync(request, cancellationToken);
    }
}
