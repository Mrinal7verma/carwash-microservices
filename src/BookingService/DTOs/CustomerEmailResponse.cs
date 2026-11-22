namespace BookingService.DTOs;

using System.Text.Json.Serialization;

/// <summary>
/// DTO representing email returned by CustomerService internal lookup.
/// </summary>
public class CustomerEmailResponse
{
    [JsonPropertyName("email")]
    public string Email { get; set; } = string.Empty;
}
