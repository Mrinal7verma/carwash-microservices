namespace BookingService.Services;

/// <summary>
/// Client contract for validating washers via WasherService.
/// </summary>
public interface IWasherClient
{
    Task ValidateWasherAsync(CancellationToken cancellationToken);
}
