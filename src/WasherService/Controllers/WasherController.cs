namespace WasherService.Controllers;

using CarWash.Common.Extensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WasherService.DTOs;
using WasherService.Services;

/// <summary>
/// Controller for managing washer registrations, authentication, profile, and availability.
/// </summary>
[ApiController]
[Route("api/washers")]
public class WasherController : ControllerBase
{
    private readonly IWasherService _washerService;
    private readonly IJwtTokenService _jwtTokenService;

    public WasherController(IWasherService washerService, IJwtTokenService jwtTokenService)
    {
        _washerService = washerService;
        _jwtTokenService = jwtTokenService;
    }

    [HttpPost("register")]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Register([FromBody] WasherRegisterRequest request, CancellationToken cancellationToken)
    {
        await _washerService.RegisterWasherAsync(request, cancellationToken);
        return Ok(new { message = "Washer registered successfully" });
    }

    [HttpPost("login")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(WasherLoginResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Login([FromBody] WasherLoginRequest request, CancellationToken cancellationToken)
    {
        var washer = await _washerService.LoginAsync(request, cancellationToken);
        var token = _jwtTokenService.GenerateToken(washer.WasherId, washer.Email, "Washer");

        var response = new WasherLoginResponse
        {
            Token = token,
            Washer = new WasherResponse
            {
                WasherId = washer.WasherId,
                FirstName = washer.FirstName,
                LastName = washer.LastName,
                Email = washer.Email,
                Phone = washer.Phone,
                IsAvailable = washer.IsAvailable
            }
        };

        return Ok(response);
    }

    [HttpGet("available")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(IEnumerable<WasherResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAvailable(CancellationToken cancellationToken)
    {
        var available = await _washerService.GetAvailableWashersAsync(cancellationToken);
        return Ok(available);
    }

    [HttpGet("admin/all")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(IEnumerable<WasherResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetAllWashers(CancellationToken cancellationToken)
    {
        var all = await _washerService.GetAllWashersAsync(cancellationToken);
        return Ok(all);
    }

    [HttpGet("profile")]
    [Authorize(Roles = "Washer")]
    [ProducesResponseType(typeof(WasherResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetProfile(CancellationToken cancellationToken)
    {
        var washerId = User.GetUserId();
        var profile = await _washerService.GetProfileAsync(washerId, cancellationToken);
        return Ok(profile);
    }

    [HttpPut("availability")]
    [Authorize(Roles = "Washer")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateAvailability([FromBody] WasherAvailabilityRequest request, CancellationToken cancellationToken)
    {
        var washerId = User.GetUserId();
        await _washerService.UpdateAvailabilityAsync(washerId, request.IsAvailable, cancellationToken);
        return Ok(new { message = "Washer availability updated successfully" });
    }

    [HttpDelete("profile")]
    [Authorize(Roles = "Washer")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
    public async Task<IActionResult> Delete(CancellationToken cancellationToken)
    {
        var washerId = User.GetUserId();
        await _washerService.DeleteWasherAsync(washerId, cancellationToken);
        return Ok(new { message = "Washer profile deleted successfully" });
    }
}
