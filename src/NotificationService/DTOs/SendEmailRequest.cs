namespace NotificationService.DTOs;

using System.ComponentModel.DataAnnotations;

/// <summary>
/// DTO representing an outgoing email notification request.
/// </summary>
public class SendEmailRequest
{
    [Required(ErrorMessage = "The 'to' email address is required.")]
    [EmailAddress(ErrorMessage = "A valid 'to' email address is required.")]
    public string To { get; set; } = string.Empty;

    [Required(ErrorMessage = "The 'subject' is required.")]
    public string Subject { get; set; } = string.Empty;

    [Required(ErrorMessage = "The 'body' is required.")]
    public string Body { get; set; } = string.Empty;
}
