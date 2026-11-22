namespace BookingService.DTOs;

using System.ComponentModel.DataAnnotations;

/// <summary>
/// DTO representing a customer request to create a new booking.
/// </summary>
public class CreateBookingRequest
{
    [Required(ErrorMessage = "Package ID is required.")]
    public long PackageId { get; set; }

    [Required(ErrorMessage = "Car ID is required.")]
    public long CarId { get; set; }

    [Required(ErrorMessage = "Scheduled time is required.")]
    public DateTime ScheduledTime { get; set; }
}
