namespace NotificationService.Services;

using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MimeKit;
using NotificationService.Configuration;

/// <summary>
/// SMTP implementation of IEmailService using MailKit.
/// Skips transmission and logs metadata when Smtp:Enabled is false.
/// </summary>
public class SmtpEmailService : IEmailService
{
    private readonly SmtpOptions _options;
    private readonly ILogger<SmtpEmailService> _logger;

    public SmtpEmailService(
        IOptions<SmtpOptions> options,
        ILogger<SmtpEmailService> logger)
    {
        _options = options.Value;
        _logger = logger;
    }

    public async Task SendAsync(string to, string subject, string body, CancellationToken cancellationToken)
    {
        // When SMTP is disabled (default in Development), log to + subject only and do not send
        if (!_options.Enabled)
        {
            _logger.LogInformation("SMTP dispatch is disabled. Email skipped. To: {To}, Subject: {Subject}", to, subject);
            return;
        }

        var message = new MimeMessage();
        var senderName = string.IsNullOrWhiteSpace(_options.SenderName) ? _options.FromAddress : _options.SenderName;
        message.From.Add(new MailboxAddress(senderName, _options.FromAddress));
        message.To.Add(MailboxAddress.Parse(to));
        message.Subject = subject;

        var bodyBuilder = new BodyBuilder
        {
            TextBody = body
        };
        message.Body = bodyBuilder.ToMessageBody();

        using var client = new SmtpClient();

        // Determine socket options based on standard ports
        var socketOptions = _options.Port switch
        {
            465 => SecureSocketOptions.SslOnConnect,
            587 => SecureSocketOptions.StartTls,
            _ => SecureSocketOptions.Auto
        };

        await client.ConnectAsync(_options.Host, _options.Port, socketOptions, cancellationToken);

        if (!string.IsNullOrWhiteSpace(_options.Username))
        {
            await client.AuthenticateAsync(_options.Username, _options.Password, cancellationToken);
        }

        var sendRequest = new SmtpSendRequest(message);
        await client.SendAsync(sendRequest, cancellationToken);
        await client.DisconnectAsync(true, cancellationToken);

        _logger.LogInformation("Email successfully dispatched via SMTP. To: {To}, Subject: {Subject}", to, subject);
    }
}
