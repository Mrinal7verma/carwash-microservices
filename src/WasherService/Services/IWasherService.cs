namespace WasherService.Services;

using global::WasherService.Data.Entities;
using global::WasherService.DTOs;

/// <summary>
/// Service contract for washer management, authentication, and availability.
/// </summary>
public interface IWasherService
{
    Task RegisterWasherAsync(WasherRegisterRequest request, CancellationToken cancellationToken);

    Task<Washer> LoginAsync(WasherLoginRequest request, CancellationToken cancellationToken);

    Task<WasherResponse> GetProfileAsync(long washerId, CancellationToken cancellationToken);

    Task<IEnumerable<WasherResponse>> GetAvailableWashersAsync(CancellationToken cancellationToken);

    Task<IEnumerable<WasherResponse>> GetAllWashersAsync(CancellationToken cancellationToken);

    Task UpdateAvailabilityAsync(long washerId, bool isAvailable, CancellationToken cancellationToken);

    Task<bool> CanDeleteWasherAsync(long washerId, CancellationToken cancellationToken);

    Task DeleteWasherAsync(long washerId, CancellationToken cancellationToken);
}
