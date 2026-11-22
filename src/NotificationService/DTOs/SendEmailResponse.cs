namespace NotificationService.DTOs;

using System.Text.Json.Serialization;

/// <summary>
/// DTO returned after successfully processing an email request.
/// </summary>
public class SendEmailResponse
{
    [JsonPropertyName("message")]
    public string Message { get; set; } = "Email accepted for processing.";
}
