namespace BookingService.Services;

using BookingService.Constants;
using BookingService.Data;
using BookingService.Data.Entities;
using BookingService.DTOs;
using CarWash.Common.Exceptions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

/// <summary>
/// Core implementation of booking management, lifecycle state transitions, and package catalogs.
/// </summary>
public class BookingServiceImplementation : IBookingService
{
    private readonly BookingDbContext _dbContext;
    private readonly ICustomerClient _customerClient;
    private readonly IWasherClient _washerClient;
    private readonly INotificationClient _notificationClient;
    private readonly ILogger<BookingServiceImplementation> _logger;
    private readonly IServiceScopeFactory? _serviceScopeFactory;

    public BookingServiceImplementation(
        BookingDbContext dbContext,
        ICustomerClient customerClient,
        IWasherClient washerClient,
        INotificationClient notificationClient,
        ILogger<BookingServiceImplementation> logger,
        IServiceScopeFactory? serviceScopeFactory = null)
    {
        _dbContext = dbContext;
        _customerClient = customerClient;
        _washerClient = washerClient;
        _notificationClient = notificationClient;
        _logger = logger;
        _serviceScopeFactory = serviceScopeFactory;
    }

    public async Task<IEnumerable<ServicePackageResponse>> GetActivePackagesAsync(CancellationToken cancellationToken)
    {
        return await _dbContext.ServicePackages
            .AsNoTracking()
            .Where(p => p.IsActive)
            .Select(p => new ServicePackageResponse
            {
                PackageId = p.PackageId,
                Name = p.Name,
                Description = p.Description,
                Price = p.Price,
                EstimatedDurationMinutes = p.EstimatedDurationMinutes,
                IsActive = p.IsActive
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<ServicePackageResponse>> GetAllPackagesAsync(CancellationToken cancellationToken)
    {
        return await _dbContext.ServicePackages
            .AsNoTracking()
            .Select(p => new ServicePackageResponse
            {
                PackageId = p.PackageId,
                Name = p.Name,
                Description = p.Description,
                Price = p.Price,
                EstimatedDurationMinutes = p.EstimatedDurationMinutes,
                IsActive = p.IsActive
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<ServicePackageResponse> CreatePackageAsync(CreateServicePackageRequest request, CancellationToken cancellationToken)
    {
        var package = new ServicePackage
        {
            Name = request.Name.Trim(),
            Description = request.Description.Trim(),
            Price = request.Price,
            EstimatedDurationMinutes = request.EstimatedDurationMinutes,
            IsActive = request.IsActive
        };

        _dbContext.ServicePackages.Add(package);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return new ServicePackageResponse
        {
            PackageId = package.PackageId,
            Name = package.Name,
            Description = package.Description,
            Price = package.Price,
            EstimatedDurationMinutes = package.EstimatedDurationMinutes,
            IsActive = package.IsActive
        };
    }

    public async Task<ServicePackageResponse> UpdatePackageAsync(long id, UpdateServicePackageRequest request, CancellationToken cancellationToken)
    {
        var package = await _dbContext.ServicePackages
            .SingleOrDefaultAsync(p => p.PackageId == id, cancellationToken);

        if (package == null)
        {
            throw new NotFoundException($"Service package #{id} not found.");
        }

        package.Name = request.Name.Trim();
        package.Description = request.Description.Trim();
        package.Price = request.Price;
        package.EstimatedDurationMinutes = request.EstimatedDurationMinutes;
        package.IsActive = request.IsActive;

        await _dbContext.SaveChangesAsync(cancellationToken);

        return new ServicePackageResponse
        {
            PackageId = package.PackageId,
            Name = package.Name,
            Description = package.Description,
            Price = package.Price,
            EstimatedDurationMinutes = package.EstimatedDurationMinutes,
            IsActive = package.IsActive
        };
    }

    public async Task DeactivatePackageAsync(long id, CancellationToken cancellationToken)
    {
        var package = await _dbContext.ServicePackages
            .SingleOrDefaultAsync(p => p.PackageId == id, cancellationToken);

        if (package == null)
        {
            throw new NotFoundException($"Service package #{id} not found.");
        }

        package.IsActive = false;
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<BookingResponseDto> CreateBookingAsync(long customerId, CreateBookingRequest request, CancellationToken cancellationToken)
    {
        // Scheduled time must be in the future
        if (request.ScheduledTime <= DateTime.UtcNow)
        {
            throw new BadRequestException("Scheduled time must be in the future.");
        }

        // Validate vehicle ownership via CustomerService (throws 404 or 503)
        await _customerClient.ValidateCustomerCarAsync(request.CarId, cancellationToken);

        // Validate active package
        var package = await _dbContext.ServicePackages
            .AsNoTracking()
            .SingleOrDefaultAsync(p => p.PackageId == request.PackageId && p.IsActive, cancellationToken);

        if (package == null)
        {
            throw new BadRequestException("The selected service package does not exist or is inactive.");
        }

        var booking = new Booking
        {
            CustomerId = customerId,
            CarId = request.CarId,
            PackageId = package.PackageId,
            WasherId = null,
            BookingDate = DateTime.UtcNow,
            ScheduledTime = request.ScheduledTime,
            Status = BookingStatus.Pending,
            TotalAmount = package.Price
        };

        _dbContext.Bookings.Add(booking);
        await _dbContext.SaveChangesAsync(cancellationToken);

        SendNotificationSafe(
            customerId,
            booking.BookingId,
            "Car Wash Booking Created",
            $"Hello, your booking #{booking.BookingId} for '{package.Name}' scheduled at {booking.ScheduledTime:u} has been created.");

        return new BookingResponseDto
        {
            BookingId = booking.BookingId,
            CustomerId = booking.CustomerId,
            CarId = booking.CarId,
            WasherId = booking.WasherId,
            PackageName = package.Name,
            TotalAmount = booking.TotalAmount,
            ScheduledTime = booking.ScheduledTime,
            Status = booking.Status
        };
    }

    public async Task<IEnumerable<BookingResponseDto>> GetCustomerBookingsAsync(long customerId, CancellationToken cancellationToken)
    {
        return await _dbContext.Bookings
            .AsNoTracking()
            .Include(b => b.Package)
            .Where(b => b.CustomerId == customerId)
            .OrderByDescending(b => b.BookingDate)
            .Select(b => new BookingResponseDto
            {
                BookingId = b.BookingId,
                CustomerId = b.CustomerId,
                CarId = b.CarId,
                WasherId = b.WasherId,
                PackageName = b.Package.Name,
                TotalAmount = b.TotalAmount,
                ScheduledTime = b.ScheduledTime,
                Status = b.Status
            })
            .ToListAsync(cancellationToken);
    }

    public async Task CancelBookingAsync(long customerId, long bookingId, CancellationToken cancellationToken)
    {
        var booking = await _dbContext.Bookings
            .Include(b => b.Package)
            .SingleOrDefaultAsync(b => b.BookingId == bookingId && b.CustomerId == customerId, cancellationToken);

        if (booking == null)
        {
            throw new NotFoundException("Booking not found or does not belong to the caller.");
        }

        if (!BookingStatus.IsActive(booking.Status))
        {
            throw new ConflictException($"Booking cannot be cancelled. Current status is '{booking.Status}'.");
        }

        booking.Status = BookingStatus.Cancelled;
        await _dbContext.SaveChangesAsync(cancellationToken);

        SendNotificationSafe(
            customerId,
            booking.BookingId,
            "Car Wash Booking Cancelled",
            $"Your booking #{booking.BookingId} for '{booking.Package.Name}' scheduled at {booking.ScheduledTime:u} has been cancelled.");
    }

    public async Task<IEnumerable<BookingResponseDto>> GetPendingBookingsAsync(long washerId, CancellationToken cancellationToken)
    {
        var rejectedBookingIds = _dbContext.BookingRejections
            .Where(r => r.WasherId == washerId)
            .Select(r => r.BookingId);

        return await _dbContext.Bookings
            .AsNoTracking()
            .Include(b => b.Package)
            .Where(b => b.Status == BookingStatus.Pending && !rejectedBookingIds.Contains(b.BookingId))
            .OrderBy(b => b.ScheduledTime)
            .Select(b => new BookingResponseDto
            {
                BookingId = b.BookingId,
                CustomerId = b.CustomerId,
                CarId = b.CarId,
                WasherId = b.WasherId,
                PackageName = b.Package.Name,
                TotalAmount = b.TotalAmount,
                ScheduledTime = b.ScheduledTime,
                Status = b.Status
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<BookingResponseDto>> GetWasherBookingsAsync(long washerId, CancellationToken cancellationToken)
    {
        return await _dbContext.Bookings
            .AsNoTracking()
            .Include(b => b.Package)
            .Where(b => b.WasherId == washerId)
            .OrderByDescending(b => b.ScheduledTime)
            .Select(b => new BookingResponseDto
            {
                BookingId = b.BookingId,
                CustomerId = b.CustomerId,
                CarId = b.CarId,
                WasherId = b.WasherId,
                PackageName = b.Package.Name,
                TotalAmount = b.TotalAmount,
                ScheduledTime = b.ScheduledTime,
                Status = b.Status
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<BookingResponseDto> UpdateBookingStatusAsync(long washerId, long bookingId, string newStatus, CancellationToken cancellationToken)
    {
        var isAccept = string.Equals(newStatus?.Trim(), BookingStatus.Accepted, StringComparison.OrdinalIgnoreCase);
        var isReject = string.Equals(newStatus?.Trim(), "Rejected", StringComparison.OrdinalIgnoreCase);
        var isComplete = string.Equals(newStatus?.Trim(), BookingStatus.Completed, StringComparison.OrdinalIgnoreCase);

        if (!isAccept && !isReject && !isComplete)
        {
            throw new BadRequestException("Action must be 'Accepted', 'Rejected', or 'Completed'.");
        }

        // Validate washer via WasherService
        await _washerClient.ValidateWasherAsync(cancellationToken);

        var booking = await _dbContext.Bookings
            .Include(b => b.Package)
            .SingleOrDefaultAsync(b => b.BookingId == bookingId, cancellationToken);

        if (booking == null)
        {
            throw new NotFoundException($"Booking #{bookingId} not found.");
        }

        if (BookingStatus.IsFinal(booking.Status))
        {
            throw new BadRequestException($"Cannot modify a booking in final status '{booking.Status}'.");
        }

        if (isAccept)
        {
            if (booking.Status != BookingStatus.Pending)
            {
                throw new BadRequestException("Only pending bookings can be accepted.");
            }

            // Atomic conditional update to prevent race conditions between washers
            var affected = await _dbContext.Bookings
                .Where(b => b.BookingId == bookingId && b.Status == BookingStatus.Pending)
                .ExecuteUpdateAsync(
                    s => s
                        .SetProperty(b => b.Status, BookingStatus.Accepted)
                        .SetProperty(b => b.WasherId, washerId),
                    cancellationToken);

            if (affected == 0)
            {
                throw new BadRequestException("Booking is no longer pending or has already been accepted.");
            }

            booking.Status = BookingStatus.Accepted;
            booking.WasherId = washerId;

            SendNotificationSafe(
                booking.CustomerId,
                booking.BookingId,
                "Car Wash Booking Accepted",
                $"Good news! Your booking #{booking.BookingId} has been accepted by a washer.");
        }
        else if (isReject)
        {
            if (booking.Status != BookingStatus.Pending)
            {
                throw new BadRequestException("Only pending bookings can be rejected.");
            }

            var alreadyRejected = await _dbContext.BookingRejections
                .AnyAsync(r => r.BookingId == bookingId && r.WasherId == washerId, cancellationToken);

            if (alreadyRejected)
            {
                throw new BadRequestException("Washer has already rejected this booking.");
            }

            var rejection = new BookingRejection
            {
                BookingId = bookingId,
                WasherId = washerId,
                RejectedAt = DateTime.UtcNow
            };

            _dbContext.BookingRejections.Add(rejection);

            try
            {
                await _dbContext.SaveChangesAsync(cancellationToken);
            }
            catch (DbUpdateException ex)
            {
                _logger.LogWarning(ex, "Duplicate rejection prevented by unique constraint for booking {BookingId}", bookingId);
                throw new BadRequestException("Washer has already rejected this booking.");
            }
        }
        else if (isComplete)
        {
            if (booking.Status != BookingStatus.Accepted)
            {
                throw new BadRequestException("Only accepted bookings can be marked as completed.");
            }

            if (booking.WasherId != washerId)
            {
                throw new ForbiddenException("Only the assigned washer can complete this booking.");
            }

            booking.Status = BookingStatus.Completed;
            await _dbContext.SaveChangesAsync(cancellationToken);

            SendNotificationSafe(
                booking.CustomerId,
                booking.BookingId,
                "Car Wash Completed",
                $"Your car wash service for booking #{booking.BookingId} is now complete. Thank you!");
        }

        return new BookingResponseDto
        {
            BookingId = booking.BookingId,
            CustomerId = booking.CustomerId,
            CarId = booking.CarId,
            WasherId = booking.WasherId,
            PackageName = booking.Package.Name,
            TotalAmount = booking.TotalAmount,
            ScheduledTime = booking.ScheduledTime,
            Status = booking.Status
        };
    }

    public async Task<int> ExpirePendingBookingsAsync(CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;
        var expiredCount = await _dbContext.Bookings
            .Where(b => b.Status == BookingStatus.Pending && b.ScheduledTime < now)
            .ExecuteUpdateAsync(s => s.SetProperty(b => b.Status, BookingStatus.Expired), cancellationToken);

        return expiredCount;
    }

    public async Task<bool> HasActiveBookingsForCustomerAsync(long customerId, CancellationToken cancellationToken)
    {
        return await _dbContext.Bookings
            .AnyAsync(
                b => b.CustomerId == customerId &&
                    (b.Status == BookingStatus.Pending || b.Status == BookingStatus.Accepted),
                cancellationToken);
    }

    public async Task<bool> HasActiveBookingsForCarAsync(long carId, CancellationToken cancellationToken)
    {
        return await _dbContext.Bookings
            .AnyAsync(
                b => b.CarId == carId &&
                    (b.Status == BookingStatus.Pending || b.Status == BookingStatus.Accepted),
                cancellationToken);
    }

    public async Task<bool> HasActiveBookingsForWasherAsync(long washerId, CancellationToken cancellationToken)
    {
        return await _dbContext.Bookings
            .AnyAsync(
                b => b.WasherId == washerId &&
                    (b.Status == BookingStatus.Pending || b.Status == BookingStatus.Accepted),
                cancellationToken);
    }

    private void SendNotificationSafe(long customerId, long bookingId, string subject, string body)
    {
        _ = Task.Run(async () =>
        {
            try
            {
                if (_serviceScopeFactory != null)
                {
                    using var scope = _serviceScopeFactory.CreateScope();
                    var customerClient = scope.ServiceProvider.GetRequiredService<ICustomerClient>();
                    var notificationClient = scope.ServiceProvider.GetRequiredService<INotificationClient>();
                    var email = await customerClient.GetCustomerEmailAsync(customerId, CancellationToken.None);
                    await notificationClient.SendBookingEmailAsync(email, subject, body, CancellationToken.None);
                }
                else
                {
                    var email = await _customerClient.GetCustomerEmailAsync(customerId, CancellationToken.None);
                    await _notificationClient.SendBookingEmailAsync(email, subject, body, CancellationToken.None);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send notification email for booking #{BookingId}", bookingId);
            }
        });
    }
}
