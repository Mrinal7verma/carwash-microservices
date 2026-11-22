namespace BookingService.DTOs;

/// <summary>
/// DTO representing a service package definition.
/// </summary>
public class ServicePackageResponse
{
    public long PackageId { get; set; }

    public string Name { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public decimal Price { get; set; }

    public int EstimatedDurationMinutes { get; set; }

    public bool IsActive { get; set; }
}
