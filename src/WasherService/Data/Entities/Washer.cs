namespace WasherService.Data.Entities;

/// <summary>
/// Entity representing a registered car washer service provider.
/// </summary>
public class Washer
{
    public long WasherId { get; set; }

    public string FirstName { get; set; } = string.Empty;

    public string LastName { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public string Phone { get; set; } = string.Empty;

    public string PasswordHash { get; set; } = string.Empty;

    public bool IsAvailable { get; set; } = true;
}
