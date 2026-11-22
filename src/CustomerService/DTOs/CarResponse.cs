namespace CustomerService.DTOs;

/// <summary>
/// DTO representing car details returned to client.
/// </summary>
public class CarResponse
{
    public long CarId { get; set; }

    public string Make { get; set; } = string.Empty;

    public string Model { get; set; } = string.Empty;

    public string RegistrationNo { get; set; } = string.Empty;
}
