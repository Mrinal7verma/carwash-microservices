namespace BookingService.Configuration;

/// <summary>
/// Configuration options for the pending booking cleanup background service.
/// </summary>
public class CleanupOptions
{
    public const string SectionName = "Cleanup";

    public int IntervalMinutes { get; set; } = 60;
}
