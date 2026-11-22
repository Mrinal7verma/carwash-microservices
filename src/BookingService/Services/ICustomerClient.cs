namespace BookingService.Services;

using BookingService.DTOs;

/// <summary>
/// Client contract for communicating with CustomerService.
/// </summary>
public interface ICustomerClient
{
    Task<CustomerCarResponse> ValidateCustomerCarAsync(long carId, CancellationToken cancellationToken);

    Task<string> GetCustomerEmailAsync(long customerId, CancellationToken cancellationToken);
}
