namespace WasherService.Services;

/// <summary>
/// Typed client contract for communicating with BookingService for washer operations.
/// </summary>
public interface IBookingClient
{
    Task<bool> HasActiveWasherBookingsAsync(long washerId, CancellationToken cancellationToken);
}
