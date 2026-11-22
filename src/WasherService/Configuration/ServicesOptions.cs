namespace WasherService.Configuration;

/// <summary>
/// Options for downstream service endpoints.
/// </summary>
public class ServicesOptions
{
    public const string SectionName = "Services";

    public string BookingBaseUrl { get; set; } = "http://localhost:5004";
}
