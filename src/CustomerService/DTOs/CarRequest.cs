namespace CustomerService.DTOs;

using System.ComponentModel.DataAnnotations;

/// <summary>
/// DTO representing payload to add a new car for a customer.
/// </summary>
public class CarRequest
{
    [Required(ErrorMessage = "Make is required.")]
    [MaxLength(100, ErrorMessage = "Make cannot exceed 100 characters.")]
    public string Make { get; set; } = string.Empty;

    [Required(ErrorMessage = "Model is required.")]
    [MaxLength(100, ErrorMessage = "Model cannot exceed 100 characters.")]
    public string Model { get; set; } = string.Empty;

    [Required(ErrorMessage = "Registration number is required.")]
    [MaxLength(20, ErrorMessage = "Registration number cannot exceed 20 characters.")]
    public string RegistrationNo { get; set; } = string.Empty;
}
