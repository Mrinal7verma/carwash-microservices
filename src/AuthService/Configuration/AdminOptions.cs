namespace AuthService.Configuration;

/// <summary>
/// Strongly-typed options for administrative credentials.
/// </summary>
public class AdminOptions
{
    public const string SectionName = "Admin";

    public string Email { get; set; } = string.Empty;

    public string PasswordHash { get; set; } = string.Empty;
}
