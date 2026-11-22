namespace BookingService.DTOs;

/// <summary>
/// DTO sent to NotificationService to dispatch an email.
/// </summary>
public class SendEmailRequest
{
    public string To { get; set; } = string.Empty;

    public string Subject { get; set; } = string.Empty;

    public string Body { get; set; } = string.Empty;
}
