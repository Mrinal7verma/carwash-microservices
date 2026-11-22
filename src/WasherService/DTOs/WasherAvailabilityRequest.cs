namespace WasherService.DTOs;

/// <summary>
/// DTO representing payload to update washer availability status.
/// </summary>
public class WasherAvailabilityRequest
{
    public bool IsAvailable { get; set; }
}
