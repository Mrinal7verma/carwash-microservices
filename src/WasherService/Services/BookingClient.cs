namespace WasherService.Services;

using System.Net.Http.Json;
using CarWash.Common.Exceptions;
using global::WasherService.Configuration;
using global::WasherService.DTOs;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

/// <summary>
/// Typed HTTP client for calling internal BookingService endpoints from WasherService.
/// Maps network/timeout/5xx issues to ServiceUnavailableException (503).
/// </summary>
public class BookingClient : IBookingClient
{
    private const string InternalApiKeyHeaderName = "X-Internal-Api-Key";

    private readonly HttpClient _httpClient;
    private readonly InternalApiOptions _internalApiOptions;
    private readonly ILogger<BookingClient> _logger;

    public BookingClient(
        HttpClient httpClient,
        IOptions<InternalApiOptions> internalApiOptions,
        ILogger<BookingClient> logger)
    {
        _httpClient = httpClient;
        _internalApiOptions = internalApiOptions.Value;
        _logger = logger;
    }

    public async Task<bool> HasActiveWasherBookingsAsync(long washerId, CancellationToken cancellationToken)
    {
        var relativeUri = $"api/bookings/internal/washers/{washerId}/active";
        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, relativeUri);
            if (!string.IsNullOrWhiteSpace(_internalApiOptions.Key))
            {
                request.Headers.Add(InternalApiKeyHeaderName, _internalApiOptions.Key);
            }

            using var response = await _httpClient.SendAsync(request, cancellationToken);

            if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
            {
                _logger.LogWarning("BookingService rejected internal API key for {Uri}", relativeUri);
                throw new UnauthorizedException("Invalid internal API key provided to BookingService.");
            }

            if ((int)response.StatusCode >= 500)
            {
                _logger.LogWarning("BookingService responded with server error {StatusCode} for {Uri}", response.StatusCode, relativeUri);
                throw new ServiceUnavailableException("BookingService is currently experiencing server errors.");
            }

            if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                return false;
            }

            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadFromJsonAsync<ActiveBookingCheckResponse>(cancellationToken: cancellationToken);
            return content?.HasActiveBookings == true || content?.HasActive == true;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP connection error while calling BookingService at {Uri}", relativeUri);
            throw new ServiceUnavailableException("Unable to contact BookingService.");
        }
        catch (TaskCanceledException ex) when (!cancellationToken.IsCancellationRequested)
        {
            _logger.LogError(ex, "Timeout occurred while waiting for BookingService at {Uri}", relativeUri);
            throw new ServiceUnavailableException("BookingService request timed out.");
        }
        catch (TimeoutException ex)
        {
            _logger.LogError(ex, "Timeout occurred while waiting for BookingService at {Uri}", relativeUri);
            throw new ServiceUnavailableException("BookingService request timed out.");
        }
    }
}
