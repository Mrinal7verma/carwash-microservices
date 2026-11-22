namespace CustomerService.DTOs;

/// <summary>
/// DTO representing customer profile details.
/// </summary>
public class CustomerProfileResponse
{
    public long CustomerId { get; set; }

    public string FirstName { get; set; } = string.Empty;

    public string LastName { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public string Phone { get; set; } = string.Empty;

    public string? Address { get; set; }

    public string? City { get; set; }
}
