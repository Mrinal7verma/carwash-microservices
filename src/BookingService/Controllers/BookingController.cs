namespace BookingService.Controllers;

using BookingService.DTOs;
using BookingService.Services;
using CarWash.Common.Exceptions;
using CarWash.Common.Extensions;
using CarWash.Common.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

/// <summary>
/// Handles package management, booking life cycle operations, and internal active booking checks.
/// </summary>
[ApiController]
[Route("api/bookings")]
public class BookingController : ControllerBase
{
    private readonly IBookingService _bookingService;

    public BookingController(IBookingService bookingService)
    {
        _bookingService = bookingService;
    }

    /// <summary>
    /// Retrieves all active service packages available for customers.
    /// </summary>
    [HttpGet("packages")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(IEnumerable<ServicePackageResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetActivePackages(CancellationToken cancellationToken)
    {
        var packages = await _bookingService.GetActivePackagesAsync(cancellationToken);
        return Ok(packages);
    }

    /// <summary>
    /// Creates a new booking for the authenticated customer.
    /// </summary>
    [HttpPost]
    [Authorize(Roles = "Customer")]
    [ProducesResponseType(typeof(BookingResponseDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> CreateBooking([FromBody] CreateBookingRequest request, CancellationToken cancellationToken)
    {
        var customerId = User.GetUserId();
        var booking = await _bookingService.CreateBookingAsync(customerId, request, cancellationToken);
        return StatusCode(StatusCodes.Status201Created, booking);
    }

    /// <summary>
    /// Retrieves all bookings belonging to the authenticated customer.
    /// </summary>
    [HttpGet("my")]
    [Authorize(Roles = "Customer")]
    [ProducesResponseType(typeof(IEnumerable<BookingResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetMyBookings(CancellationToken cancellationToken)
    {
        var customerId = User.GetUserId();
        var bookings = await _bookingService.GetCustomerBookingsAsync(customerId, cancellationToken);
        return Ok(bookings);
    }

    /// <summary>
    /// Cancels a pending or accepted booking owned by the authenticated customer.
    /// </summary>
    [HttpPut("{id:long}/cancel")]
    [Authorize(Roles = "Customer")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> CancelBooking([FromRoute] long id, CancellationToken cancellationToken)
    {
        var customerId = User.GetUserId();
        await _bookingService.CancelBookingAsync(customerId, id, cancellationToken);
        return Ok(new { message = "Booking cancelled successfully." });
    }

    /// <summary>
    /// Retrieves available pending bookings not rejected by the authenticated washer.
    /// </summary>
    [HttpGet("pending")]
    [Authorize(Roles = "Washer")]
    [ProducesResponseType(typeof(IEnumerable<BookingResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetPendingBookings(CancellationToken cancellationToken)
    {
        var washerId = User.GetUserId();
        var bookings = await _bookingService.GetPendingBookingsAsync(washerId, cancellationToken);
        return Ok(bookings);
    }

    /// <summary>
    /// Retrieves all accepted or completed bookings assigned to the authenticated washer.
    /// </summary>
    [HttpGet("washer/my")]
    [Authorize(Roles = "Washer")]
    [ProducesResponseType(typeof(IEnumerable<BookingResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetWasherBookings(CancellationToken cancellationToken)
    {
        var washerId = User.GetUserId();
        var bookings = await _bookingService.GetWasherBookingsAsync(washerId, cancellationToken);
        return Ok(bookings);
    }

    /// <summary>
    /// Updates status of a booking (Accepted, Rejected, Completed) by an authenticated washer.
    /// </summary>
    [HttpPut("{id:long}/status")]
    [Authorize(Roles = "Washer")]
    [ProducesResponseType(typeof(BookingResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateBookingStatus(
        [FromRoute] long id,
        [FromBody] UpdateBookingStatusRequest request,
        CancellationToken cancellationToken)
    {
        var washerId = User.GetUserId();
        var action = request.GetEffectiveAction();
        if (string.IsNullOrWhiteSpace(action))
        {
            throw new BadRequestException("Status is required ('Accepted', 'Rejected', or 'Completed').");
        }

        var result = await _bookingService.UpdateBookingStatusAsync(washerId, id, action, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Retrieves all service packages, including inactive ones (Admin only).
    /// </summary>
    [HttpGet("admin/packages")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(IEnumerable<ServicePackageResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetAllPackages(CancellationToken cancellationToken)
    {
        var packages = await _bookingService.GetAllPackagesAsync(cancellationToken);
        return Ok(packages);
    }

    /// <summary>
    /// Creates a new service package (Admin only).
    /// </summary>
    [HttpPost("admin/packages")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(ServicePackageResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> CreatePackage(
        [FromBody] CreateServicePackageRequest request,
        CancellationToken cancellationToken)
    {
        var created = await _bookingService.CreatePackageAsync(request, cancellationToken);
        return StatusCode(StatusCodes.Status201Created, created);
    }

    /// <summary>
    /// Updates an existing service package (Admin only).
    /// </summary>
    [HttpPut("admin/packages/{id:long}")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(ServicePackageResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdatePackage(
        [FromRoute] long id,
        [FromBody] UpdateServicePackageRequest request,
        CancellationToken cancellationToken)
    {
        var updated = await _bookingService.UpdatePackageAsync(id, request, cancellationToken);
        return Ok(updated);
    }

    /// <summary>
    /// Soft-deletes (deactivates) a service package (Admin only).
    /// </summary>
    [HttpDelete("admin/packages/{id:long}")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeactivatePackage([FromRoute] long id, CancellationToken cancellationToken)
    {
        await _bookingService.DeactivatePackageAsync(id, cancellationToken);
        return Ok(new { message = "Package deactivated successfully." });
    }

    /// <summary>
    /// Internal endpoint: checks if a customer has active (Pending or Accepted) bookings.
    /// </summary>
    [HttpGet("internal/customers/{id:long}/active")]
    [InternalApiKey]
    [ProducesResponseType(typeof(ActiveBookingCheckResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> CheckActiveCustomerBookings([FromRoute] long id, CancellationToken cancellationToken)
    {
        var hasActive = await _bookingService.HasActiveBookingsForCustomerAsync(id, cancellationToken);
        return Ok(new ActiveBookingCheckResponse(hasActive));
    }

    /// <summary>
    /// Internal endpoint: checks if a car has active (Pending or Accepted) bookings.
    /// </summary>
    [HttpGet("internal/cars/{id:long}/active")]
    [InternalApiKey]
    [ProducesResponseType(typeof(ActiveBookingCheckResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> CheckActiveCarBookings([FromRoute] long id, CancellationToken cancellationToken)
    {
        var hasActive = await _bookingService.HasActiveBookingsForCarAsync(id, cancellationToken);
        return Ok(new ActiveBookingCheckResponse(hasActive));
    }

    /// <summary>
    /// Internal endpoint: checks if a washer has active (Accepted) bookings.
    /// </summary>
    [HttpGet("internal/washers/{id:long}/active")]
    [InternalApiKey]
    [ProducesResponseType(typeof(ActiveBookingCheckResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> CheckActiveWasherBookings([FromRoute] long id, CancellationToken cancellationToken)
    {
        var hasActive = await _bookingService.HasActiveBookingsForWasherAsync(id, cancellationToken);
        return Ok(new ActiveBookingCheckResponse(hasActive));
    }
}
