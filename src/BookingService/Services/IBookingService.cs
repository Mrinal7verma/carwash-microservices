namespace BookingService.Services;

using BookingService.DTOs;

/// <summary>
/// Business service contract for booking and package operations.
/// </summary>
public interface IBookingService
{
    Task<IEnumerable<ServicePackageResponse>> GetActivePackagesAsync(CancellationToken cancellationToken);

    Task<IEnumerable<ServicePackageResponse>> GetAllPackagesAsync(CancellationToken cancellationToken);

    Task<ServicePackageResponse> CreatePackageAsync(CreateServicePackageRequest request, CancellationToken cancellationToken);

    Task<ServicePackageResponse> UpdatePackageAsync(long id, UpdateServicePackageRequest request, CancellationToken cancellationToken);

    Task DeactivatePackageAsync(long id, CancellationToken cancellationToken);

    Task<BookingResponseDto> CreateBookingAsync(long customerId, CreateBookingRequest request, CancellationToken cancellationToken);

    Task<IEnumerable<BookingResponseDto>> GetCustomerBookingsAsync(long customerId, CancellationToken cancellationToken);

    Task CancelBookingAsync(long customerId, long bookingId, CancellationToken cancellationToken);

    Task<IEnumerable<BookingResponseDto>> GetPendingBookingsAsync(long washerId, CancellationToken cancellationToken);

    Task<IEnumerable<BookingResponseDto>> GetWasherBookingsAsync(long washerId, CancellationToken cancellationToken);

    Task<BookingResponseDto> UpdateBookingStatusAsync(long washerId, long bookingId, string newStatus, CancellationToken cancellationToken);

    Task<int> ExpirePendingBookingsAsync(CancellationToken cancellationToken);

    Task<bool> HasActiveBookingsForCustomerAsync(long customerId, CancellationToken cancellationToken);

    Task<bool> HasActiveBookingsForCarAsync(long carId, CancellationToken cancellationToken);

    Task<bool> HasActiveBookingsForWasherAsync(long washerId, CancellationToken cancellationToken);
}
