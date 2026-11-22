namespace WasherService.DTOs;

using System.Text.Json.Serialization;

/// <summary>
/// DTO returned upon successful washer login.
/// </summary>
public class WasherLoginResponse
{
    [JsonPropertyName("token")]
    public string Token { get; set; } = string.Empty;

    [JsonPropertyName("washer")]
    public WasherResponse Washer { get; set; } = null!;
}
