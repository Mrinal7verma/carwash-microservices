using BookingService.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace BookingService.Services;

/// <summary>
/// Background service that periodically scans for and marks pending bookings whose scheduled time
/// has passed as Expired.
/// </summary>
public class PendingBookingCleanupService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly CleanupOptions _options;
    private readonly ILogger<PendingBookingCleanupService> _logger;

    public PendingBookingCleanupService(
        IServiceScopeFactory scopeFactory,
        IOptions<CleanupOptions> options,
        ILogger<PendingBookingCleanupService> logger)
    {
        _scopeFactory = scopeFactory;
        _options = options.Value;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var intervalMinutes = _options.IntervalMinutes > 0 ? _options.IntervalMinutes : 60;
        var interval = TimeSpan.FromMinutes(intervalMinutes);

        _logger.LogInformation("PendingBookingCleanupService started with interval of {Minutes} minutes.", intervalMinutes);

        using var timer = new PeriodicTimer(interval);

        while (!stoppingToken.IsCancellationRequested && await timer.WaitForNextTickAsync(stoppingToken))
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var bookingService = scope.ServiceProvider.GetRequiredService<IBookingService>();
                var expiredCount = await bookingService.ExpirePendingBookingsAsync(stoppingToken);

                if (expiredCount > 0)
                {
                    _logger.LogInformation("Expired {Count} pending booking(s) whose scheduled time has passed.", expiredCount);
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                // Graceful shutdown requested
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error occurred during pending booking cleanup execution.");
            }
        }

        _logger.LogInformation("PendingBookingCleanupService is stopping.");
    }
}
