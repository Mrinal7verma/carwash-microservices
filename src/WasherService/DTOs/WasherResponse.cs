namespace WasherService.DTOs;

/// <summary>
/// DTO representing washer profile details without sensitive security attributes.
/// </summary>
public class WasherResponse
{
    public long WasherId { get; set; }

    public string FirstName { get; set; } = string.Empty;

    public string LastName { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public string Phone { get; set; } = string.Empty;

    public bool IsAvailable { get; set; }
}
