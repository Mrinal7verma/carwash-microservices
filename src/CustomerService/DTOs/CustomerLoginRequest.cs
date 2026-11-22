namespace CustomerService.DTOs;

using System.ComponentModel.DataAnnotations;

/// <summary>
/// DTO for customer authentication payload.
/// </summary>
public class CustomerLoginRequest
{
    [Required(ErrorMessage = "Email is required.")]
    [EmailAddress(ErrorMessage = "A valid email address is required.")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Password is required.")]
    public string Password { get; set; } = string.Empty;
}
