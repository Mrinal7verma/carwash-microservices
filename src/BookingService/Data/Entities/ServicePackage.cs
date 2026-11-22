namespace BookingService.Data.Entities;

/// <summary>
/// Entity representing a selectable car wash service package.
/// </summary>
public class ServicePackage
{
    public long PackageId { get; set; }

    public string Name { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public decimal Price { get; set; }

    public int EstimatedDurationMinutes { get; set; }

    public bool IsActive { get; set; } = true;

    public ICollection<Booking> Bookings { get; set; } = new List<Booking>();
}
