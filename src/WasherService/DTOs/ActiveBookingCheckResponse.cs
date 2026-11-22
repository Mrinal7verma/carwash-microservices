namespace WasherService.DTOs;

using System.Text.Json.Serialization;

/// <summary>
/// DTO returned by internal BookingService active check endpoint.
/// </summary>
public class ActiveBookingCheckResponse
{
    [JsonPropertyName("hasActiveBookings")]
    public bool HasActiveBookings { get; set; }

    [JsonPropertyName("hasActive")]
    public bool HasActive { get; set; }
}
