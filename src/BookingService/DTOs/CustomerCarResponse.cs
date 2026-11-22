namespace BookingService.DTOs;

/// <summary>
/// DTO representing car details returned by CustomerService.
/// </summary>
public class CustomerCarResponse
{
    public long CarId { get; set; }

    public string Make { get; set; } = string.Empty;

    public string Model { get; set; } = string.Empty;

    public string RegistrationNo { get; set; } = string.Empty;
}
