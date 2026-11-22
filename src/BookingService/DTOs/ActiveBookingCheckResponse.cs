namespace BookingService.DTOs;

using System.Text.Json.Serialization;

/// <summary>
/// DTO returned by internal active booking verification endpoints.
/// </summary>
public class ActiveBookingCheckResponse
{
    public ActiveBookingCheckResponse()
    {
    }

    public ActiveBookingCheckResponse(bool hasActiveBookings)
    {
        HasActiveBookings = hasActiveBookings;
    }

    [JsonPropertyName("hasActiveBookings")]
    public bool HasActiveBookings { get; set; }
}
