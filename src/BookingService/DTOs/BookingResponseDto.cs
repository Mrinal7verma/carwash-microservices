namespace BookingService.DTOs;

/// <summary>
/// DTO representing complete details of a booking returned to clients.
/// </summary>
public class BookingResponseDto
{
    public long BookingId { get; set; }

    public long CustomerId { get; set; }

    public long CarId { get; set; }

    public long? WasherId { get; set; }

    public string PackageName { get; set; } = string.Empty;

    public decimal TotalAmount { get; set; }

    public DateTime ScheduledTime { get; set; }

    public string Status { get; set; } = string.Empty;
}
