namespace BookingService.DTOs;

/// <summary>
/// DTO representing payload to update the status of a booking.
/// Accepts either 'status' or 'action' ("Accepted" | "Rejected" | "Completed").
/// </summary>
public class UpdateBookingStatusRequest
{
    public string? Action { get; set; }

    public string? Status { get; set; }

    public string GetEffectiveAction()
    {
        if (!string.IsNullOrWhiteSpace(Action))
        {
            return Action.Trim();
        }

        if (!string.IsNullOrWhiteSpace(Status))
        {
            return Status.Trim();
        }

        return string.Empty;
    }
}
