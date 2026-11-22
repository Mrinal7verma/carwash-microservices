namespace BookingService.Services;

/// <summary>
/// Client contract for sending email notifications via NotificationService.
/// </summary>
public interface INotificationClient
{
    Task SendBookingEmailAsync(string to, string subject, string body, CancellationToken cancellationToken);
}
