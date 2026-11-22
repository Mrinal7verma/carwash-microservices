namespace BookingService.DTOs;

using System.ComponentModel.DataAnnotations;

/// <summary>
/// DTO representing payload to update an existing service package.
/// </summary>
public class UpdateServicePackageRequest
{
    [Required(ErrorMessage = "Package name is required.")]
    [MaxLength(100, ErrorMessage = "Package name cannot exceed 100 characters.")]
    public string Name { get; set; } = string.Empty;

    [Required(ErrorMessage = "Description is required.")]
    [MaxLength(500, ErrorMessage = "Description cannot exceed 500 characters.")]
    public string Description { get; set; } = string.Empty;

    [Range(0.01, 100000.00, ErrorMessage = "Price must be greater than zero.")]
    public decimal Price { get; set; }

    [Range(1, 1440, ErrorMessage = "Estimated duration must be at least 1 minute.")]
    public int EstimatedDurationMinutes { get; set; }

    public bool IsActive { get; set; }
}
