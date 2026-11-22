namespace BookingService.Data.Entities;

/// <summary>
/// Entity representing an explicit rejection action by a washer for a specific booking.
/// </summary>
public class BookingRejection
{
    public long BookingRejectionId { get; set; }

    public long BookingId { get; set; }

    public long WasherId { get; set; }

    public DateTime RejectedAt { get; set; }

    public Booking Booking { get; set; } = null!;
}
