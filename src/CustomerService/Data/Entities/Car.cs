namespace CustomerService.Data.Entities;

/// <summary>
/// Entity representing a vehicle registered to a customer.
/// </summary>
public class Car
{
    public long CarId { get; set; }

    public long CustomerId { get; set; }

    public string Make { get; set; } = string.Empty;

    public string Model { get; set; } = string.Empty;

    public string RegistrationNo { get; set; } = string.Empty;

    public Customer Customer { get; set; } = null!;
}
