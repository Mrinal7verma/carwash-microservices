namespace BookingService.Configuration;

/// <summary>
/// Configuration options holding the internal shared secret key.
/// </summary>
public class InternalApiOptions
{
    public const string SectionName = "InternalApi";

    public string Key { get; set; } = string.Empty;
}
