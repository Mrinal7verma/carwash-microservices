namespace BookingService.Configuration;

/// <summary>
/// Configuration options holding base URLs for downstream microservices.
/// </summary>
public class ServicesOptions
{
    public const string SectionName = "Services";

    public string CustomerBaseUrl { get; set; } = "http://localhost:5002";

    public string WasherBaseUrl { get; set; } = "http://localhost:5003";

    public string NotificationBaseUrl { get; set; } = "http://localhost:5005";
}
