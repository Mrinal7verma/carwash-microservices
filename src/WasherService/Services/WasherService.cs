namespace WasherService.Services;

using CarWash.Common.Exceptions;
using global::WasherService.Data;
using global::WasherService.Data.Entities;
using global::WasherService.DTOs;
using Microsoft.EntityFrameworkCore;

/// <summary>
/// Business logic service handling car washer registration, availability, and active booking validations.
/// </summary>
public class WasherService : IWasherService
{
    private readonly WasherDbContext _dbContext;
    private readonly IBookingClient _bookingClient;

    public WasherService(WasherDbContext dbContext, IBookingClient bookingClient)
    {
        _dbContext = dbContext;
        _bookingClient = bookingClient;
    }

    public async Task RegisterWasherAsync(WasherRegisterRequest request, CancellationToken cancellationToken)
    {
        var emailLower = request.Email.Trim().ToLowerInvariant();
        var phoneClean = request.Phone.Trim();

        if (!System.Text.RegularExpressions.Regex.IsMatch(phoneClean, @"^[0-9]{10}$"))
        {
            throw new BadRequestException("Phone number must be exactly 10 digits.");
        }

        var emailExists = await _dbContext.Washers.AnyAsync(w => w.Email == emailLower, cancellationToken);
        if (emailExists)
        {
            throw new BadRequestException("A washer with this email address already exists.");
        }

        var phoneExists = await _dbContext.Washers.AnyAsync(w => w.Phone == phoneClean, cancellationToken);
        if (phoneExists)
        {
            throw new BadRequestException("A washer with this phone number already exists.");
        }

        var washer = new Washer
        {
            FirstName = request.FirstName.Trim(),
            LastName = request.LastName.Trim(),
            Email = emailLower,
            Phone = phoneClean,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
            IsAvailable = true
        };

        _dbContext.Washers.Add(washer);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<Washer> LoginAsync(WasherLoginRequest request, CancellationToken cancellationToken)
    {
        var emailLower = request.Email.Trim().ToLowerInvariant();
        var washer = await _dbContext.Washers
            .SingleOrDefaultAsync(w => w.Email == emailLower, cancellationToken);

        if (washer == null || !BCrypt.Net.BCrypt.Verify(request.Password, washer.PasswordHash))
        {
            throw new UnauthorizedException("Invalid email or password.");
        }

        return washer;
    }

    public async Task<WasherResponse> GetProfileAsync(long washerId, CancellationToken cancellationToken)
    {
        var washer = await _dbContext.Washers
            .AsNoTracking()
            .SingleOrDefaultAsync(w => w.WasherId == washerId, cancellationToken);

        if (washer == null)
        {
            throw new NotFoundException("Washer profile not found.");
        }

        return new WasherResponse
        {
            WasherId = washer.WasherId,
            FirstName = washer.FirstName,
            LastName = washer.LastName,
            Email = washer.Email,
            Phone = washer.Phone,
            IsAvailable = washer.IsAvailable
        };
    }

    public async Task<IEnumerable<WasherResponse>> GetAvailableWashersAsync(CancellationToken cancellationToken)
    {
        return await _dbContext.Washers
            .AsNoTracking()
            .Where(w => w.IsAvailable)
            .Select(w => new WasherResponse
            {
                WasherId = w.WasherId,
                FirstName = w.FirstName,
                LastName = w.LastName,
                Email = w.Email,
                Phone = w.Phone,
                IsAvailable = w.IsAvailable
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<WasherResponse>> GetAllWashersAsync(CancellationToken cancellationToken)
    {
        return await _dbContext.Washers
            .AsNoTracking()
            .Select(w => new WasherResponse
            {
                WasherId = w.WasherId,
                FirstName = w.FirstName,
                LastName = w.LastName,
                Email = w.Email,
                Phone = w.Phone,
                IsAvailable = w.IsAvailable
            })
            .ToListAsync(cancellationToken);
    }

    public async Task UpdateAvailabilityAsync(long washerId, bool isAvailable, CancellationToken cancellationToken)
    {
        var washer = await _dbContext.Washers
            .SingleOrDefaultAsync(w => w.WasherId == washerId, cancellationToken);

        if (washer == null)
        {
            throw new NotFoundException("Washer profile not found.");
        }

        washer.IsAvailable = isAvailable;
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<bool> CanDeleteWasherAsync(long washerId, CancellationToken cancellationToken)
    {
        var hasActive = await _bookingClient.HasActiveWasherBookingsAsync(washerId, cancellationToken);
        if (hasActive)
        {
            throw new ConflictException("Cannot delete washer account with active bookings.");
        }

        return true;
    }

    public async Task DeleteWasherAsync(long washerId, CancellationToken cancellationToken)
    {
        var washer = await _dbContext.Washers
            .SingleOrDefaultAsync(w => w.WasherId == washerId, cancellationToken);

        if (washer == null)
        {
            throw new NotFoundException("Washer profile not found.");
        }

        // Active booking check across BookingService
        await CanDeleteWasherAsync(washerId, cancellationToken);

        _dbContext.Washers.Remove(washer);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}
