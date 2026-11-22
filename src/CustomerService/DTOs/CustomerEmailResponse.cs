namespace CustomerService.DTOs;

using System.Text.Json.Serialization;

/// <summary>
/// DTO returned for internal customer email query.
/// </summary>
public class CustomerEmailResponse
{
    [JsonPropertyName("email")]
    public string Email { get; set; } = string.Empty;
}
