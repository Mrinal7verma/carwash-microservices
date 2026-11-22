namespace NotificationService.Configuration;

/// <summary>
/// Strongly-typed options for SMTP email server configuration.
/// </summary>
public class SmtpOptions
{
    public const string SectionName = "Smtp";

    public string Host { get; set; } = "localhost";

    public int Port { get; set; } = 1025;

    public string Username { get; set; } = string.Empty;

    public string Password { get; set; } = string.Empty;

    public string FromAddress { get; set; } = "no-reply@carwash.local";

    public string SenderName { get; set; } = "On-Demand Car Wash";

    public bool Enabled { get; set; } = false;
}
