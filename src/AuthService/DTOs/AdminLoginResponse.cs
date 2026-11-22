namespace AuthService.DTOs;

using System.Text.Json.Serialization;

/// <summary>
/// DTO representing a successful admin authentication response.
/// </summary>
public class AdminLoginResponse
{
    [JsonPropertyName("token")]
    public string Token { get; set; } = string.Empty;
}
