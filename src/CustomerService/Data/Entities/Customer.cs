namespace CustomerService.Data.Entities;

/// <summary>
/// Entity representing a registered customer.
/// </summary>
public class Customer
{
    public long CustomerId { get; set; }

    public string FirstName { get; set; } = string.Empty;

    public string LastName { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public string Phone { get; set; } = string.Empty;

    public string PasswordHash { get; set; } = string.Empty;

    public string? Address { get; set; }

    public string? City { get; set; }

    public ICollection<Car> Cars { get; set; } = new List<Car>();
}
