namespace CustomerService.DTOs;

using System.Text.Json.Serialization;

/// <summary>
/// DTO returned upon successful customer authentication.
/// </summary>
public class CustomerLoginResponse
{
    [JsonPropertyName("token")]
    public string Token { get; set; } = string.Empty;
}
