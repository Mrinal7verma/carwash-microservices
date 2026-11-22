namespace CustomerService.DTOs;

using System.ComponentModel.DataAnnotations;

/// <summary>
/// DTO for customer registration payload.
/// </summary>
public class CustomerRegisterRequest
{
    [Required(ErrorMessage = "First name is required.")]
    [MaxLength(50, ErrorMessage = "First name cannot exceed 50 characters.")]
    public string FirstName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Last name is required.")]
    [MaxLength(50, ErrorMessage = "Last name cannot exceed 50 characters.")]
    public string LastName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Email is required.")]
    [EmailAddress(ErrorMessage = "A valid email address is required.")]
    [MaxLength(150, ErrorMessage = "Email cannot exceed 150 characters.")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Phone is required.")]
    [MaxLength(15, ErrorMessage = "Phone cannot exceed 15 characters.")]
    public string Phone { get; set; } = string.Empty;

    [Required(ErrorMessage = "Password is required.")]
    [MinLength(8, ErrorMessage = "Password must be at least 8 characters long.")]
    public string Password { get; set; } = string.Empty;

    [MaxLength(255, ErrorMessage = "Address cannot exceed 255 characters.")]
    public string? Address { get; set; }

    [MaxLength(100, ErrorMessage = "City cannot exceed 100 characters.")]
    public string? City { get; set; }
}
