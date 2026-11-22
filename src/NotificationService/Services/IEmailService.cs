namespace NotificationService.Services;

/// <summary>
/// Service contract for dispatching email messages.
/// </summary>
public interface IEmailService
{
    Task SendAsync(string to, string subject, string body, CancellationToken cancellationToken);
}
