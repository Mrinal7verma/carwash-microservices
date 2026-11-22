namespace CarWash.Common.Models;

using System.Text.Json.Serialization;

/// <summary>
/// Standard API error response shape across all CarWash services.
/// </summary>
public class ErrorResponse
{
    [JsonPropertyName("message")]
    public string Message { get; set; } = string.Empty;

    [JsonPropertyName("correlationId")]
    public string CorrelationId { get; set; } = string.Empty;
}
