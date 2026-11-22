namespace CustomerService.Services;

/// <summary>
/// Typed client contract for communicating with BookingService.
/// </summary>
public interface IBookingClient
{
    Task<bool> HasActiveCustomerBookingsAsync(long customerId, CancellationToken cancellationToken);

    Task<bool> HasActiveCarBookingsAsync(long carId, CancellationToken cancellationToken);
}
