namespace WasherService.Configuration;

/// <summary>
/// Options for internal microservice communication secrets.
/// </summary>
public class InternalApiOptions
{
    public const string SectionName = "InternalApi";

    public string Key { get; set; } = string.Empty;
}
