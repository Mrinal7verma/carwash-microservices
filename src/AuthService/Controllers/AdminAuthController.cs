namespace AuthService.Controllers;

using AuthService.Configuration;
using AuthService.DTOs;
using AuthService.Services;
using CarWash.Common.Exceptions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

/// <summary>
/// Controller for administrative authentication endpoints.
/// </summary>
[ApiController]
[Route("api/auth/admin")]
public class AdminAuthController : ControllerBase
{
    private readonly IAdminTokenService _adminTokenService;
    private readonly IOptions<AdminOptions> _adminOptions;

    public AdminAuthController(
        IAdminTokenService adminTokenService,
        IOptions<AdminOptions> adminOptions)
    {
        _adminTokenService = adminTokenService;
        _adminOptions = adminOptions;
    }

    [HttpPost("login")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(AdminLoginResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Login([FromBody] AdminLoginRequest request, CancellationToken cancellationToken)
    {
        // Model validation is automatically executed by ApiBehaviorOptions.
        // Await yield to observe cancellationToken cooperatively.
        await Task.Yield();
        cancellationToken.ThrowIfCancellationRequested();

        if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
        {
            throw new BadRequestException("Email and password are required.");
        }

        var adminConfig = _adminOptions.Value;
        if (string.IsNullOrWhiteSpace(adminConfig.Email) || string.IsNullOrWhiteSpace(adminConfig.PasswordHash))
        {
            throw new UnauthorizedException("Admin authentication is not configured on the server.");
        }

        var emailMatches = string.Equals(request.Email.Trim(), adminConfig.Email.Trim(), StringComparison.OrdinalIgnoreCase);
        var passwordMatches = emailMatches && BCrypt.Net.BCrypt.Verify(request.Password, adminConfig.PasswordHash);

        if (!passwordMatches)
        {
            throw new UnauthorizedException("Invalid email or password.");
        }

        var token = _adminTokenService.GenerateToken(request.Email);
        return Ok(new AdminLoginResponse { Token = token });
    }
}
