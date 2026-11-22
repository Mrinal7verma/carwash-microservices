namespace CustomerService.Services;

using global::CustomerService.Data.Entities;
using global::CustomerService.DTOs;

/// <summary>
/// Service contract for customer management and car operations.
/// </summary>
public interface ICustomerService
{
    Task RegisterCustomerAsync(CustomerRegisterRequest request, CancellationToken cancellationToken);

    Task<Customer> ValidateCustomerAsync(string email, string password, CancellationToken cancellationToken);

    Task<CustomerProfileResponse> GetProfileAsync(long customerId, CancellationToken cancellationToken);

    Task<IEnumerable<CustomerProfileResponse>> GetAllCustomersAsync(CancellationToken cancellationToken);

    Task<string> GetCustomerEmailAsync(long customerId, CancellationToken cancellationToken);

    Task<bool> CanDeleteCustomerAsync(long customerId, CancellationToken cancellationToken);

    Task DeleteCustomerAsync(long customerId, CancellationToken cancellationToken);

    Task<CarResponse> AddCarAsync(long customerId, CarRequest request, CancellationToken cancellationToken);

    Task<IEnumerable<CarResponse>> GetCarsAsync(long customerId, CancellationToken cancellationToken);

    Task<CarResponse> GetCarByIdAsync(long customerId, long carId, CancellationToken cancellationToken);

    Task<bool> CanDeleteCarAsync(long carId, CancellationToken cancellationToken);

    Task DeleteCarAsync(long customerId, long carId, CancellationToken cancellationToken);
}
