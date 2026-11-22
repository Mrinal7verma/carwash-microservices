namespace BookingService.Services;

using CarWash.Common.Exceptions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

/// <summary>
/// Typed client for calling WasherService.
/// </summary>
public class WasherClient : IWasherClient
{
    private readonly HttpClient _httpClient;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ILogger<WasherClient> _logger;

    public WasherClient(
        HttpClient httpClient,
        IHttpContextAccessor httpContextAccessor,
        ILogger<WasherClient> logger)
    {
        _httpClient = httpClient;
        _httpContextAccessor = httpContextAccessor;
        _logger = logger;
    }

    public async Task ValidateWasherAsync(CancellationToken cancellationToken)
    {
        var relativeUri = "api/washers/profile";
        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, relativeUri);

            // Forward incoming Bearer token
            var authHeader = _httpContextAccessor.HttpContext?.Request.Headers.Authorization.ToString();
            if (!string.IsNullOrWhiteSpace(authHeader))
            {
                request.Headers.Add("Authorization", authHeader);
            }

            using var response = await _httpClient.SendAsync(request, cancellationToken);

            if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
            {
                throw new UnauthorizedException("Washer token is invalid or unauthorized.");
            }

            if ((int)response.StatusCode >= 500)
            {
                _logger.LogWarning("WasherService returned status {StatusCode} when validating washer profile", response.StatusCode);
                throw new ServiceUnavailableException("WasherService returned a server error.");
            }

            response.EnsureSuccessStatusCode();
        }
        catch (UnauthorizedException)
        {
            throw;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP connection error while validating washer profile at WasherService");
            throw new ServiceUnavailableException("WasherService is currently unavailable.");
        }
        catch (TaskCanceledException ex) when (!cancellationToken.IsCancellationRequested)
        {
            _logger.LogError(ex, "Timeout occurred while validating washer profile at WasherService");
            throw new ServiceUnavailableException("WasherService request timed out.");
        }
        catch (TimeoutException ex)
        {
            _logger.LogError(ex, "Timeout occurred while validating washer profile at WasherService");
            throw new ServiceUnavailableException("WasherService request timed out.");
        }
    }
}
