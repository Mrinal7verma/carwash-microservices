namespace CarWash.Common.Extensions;

using System.Security.Claims;
using CarWash.Common.Exceptions;

/// <summary>
/// Extension methods for extracting authenticated user details from ClaimsPrincipal.
/// </summary>
public static class ClaimsPrincipalExtensions
{
    public static long GetUserId(this ClaimsPrincipal principal)
    {
        var idClaim = principal.FindFirst(ClaimTypes.NameIdentifier)?.Value
            ?? principal.FindFirst("sub")?.Value;

        if (string.IsNullOrWhiteSpace(idClaim) || !long.TryParse(idClaim, out var userId))
        {
            throw new UnauthorizedException("User identifier is missing or invalid in the authentication token.");
        }

        return userId;
    }

    public static string? GetEmail(this ClaimsPrincipal principal)
    {
        return principal.FindFirst(ClaimTypes.Email)?.Value
            ?? principal.FindFirst("email")?.Value;
    }

    public static string? GetRole(this ClaimsPrincipal principal)
    {
        return principal.FindFirst(ClaimTypes.Role)?.Value
            ?? principal.FindFirst("role")?.Value;
    }
}
