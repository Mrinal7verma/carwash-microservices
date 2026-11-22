namespace BookingService.Data.Entities;

/// <summary>
/// Entity representing a customer's car wash booking.
/// </summary>
public class Booking
{
    public long BookingId { get; set; }

    public long PackageId { get; set; }

    public long CustomerId { get; set; }

    public long CarId { get; set; }

    public long? WasherId { get; set; }

    public DateTime BookingDate { get; set; }

    public DateTime ScheduledTime { get; set; }

    public string Status { get; set; } = string.Empty;

    public decimal TotalAmount { get; set; }

    public ServicePackage Package { get; set; } = null!;

    public ICollection<BookingRejection> Rejections { get; set; } = new List<BookingRejection>();
}
