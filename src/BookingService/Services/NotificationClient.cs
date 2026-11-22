namespace BookingService.Services;

using System.Net.Http.Json;
using BookingService.DTOs;
using Microsoft.Extensions.Logging;

/// <summary>
/// Typed client for dispatching email requests to NotificationService.
/// Failures are caught and logged as Error, never interrupting booking operations.
/// </summary>
public class NotificationClient : INotificationClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<NotificationClient> _logger;

    public NotificationClient(HttpClient httpClient, ILogger<NotificationClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task SendBookingEmailAsync(string to, string subject, string body, CancellationToken cancellationToken)
    {
        try
        {
            var payload = new SendEmailRequest
            {
                To = to,
                Subject = subject,
                Body = body
            };

            using var response = await _httpClient.PostAsJsonAsync("api/notifications/email", payload, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("NotificationService responded with non-success status code {StatusCode} for email to {To}", response.StatusCode, to);
            }
        }
        catch (Exception ex)
        {
            // Email dispatch must never fail or roll back the booking operation
            _logger.LogError(ex, "Failed to send notification email to {To} with subject '{Subject}'", to, subject);
        }
    }
}
