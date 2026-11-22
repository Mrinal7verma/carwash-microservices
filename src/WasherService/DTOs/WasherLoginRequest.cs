namespace WasherService.DTOs;

using System.ComponentModel.DataAnnotations;

/// <summary>
/// DTO representing washer login credentials.
/// </summary>
public class WasherLoginRequest
{
    [Required(ErrorMessage = "Email is required.")]
    [EmailAddress(ErrorMessage = "A valid email address is required.")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Password is required.")]
    public string Password { get; set; } = string.Empty;
}
