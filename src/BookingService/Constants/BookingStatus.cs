namespace BookingService.Constants;

/// <summary>
/// Status constants representing the lifecycle states of a car wash booking.
/// </summary>
public static class BookingStatus
{
    public const string Pending = "Pending";
    public const string Accepted = "Accepted";
    public const string Completed = "Completed";
    public const string Cancelled = "Cancelled";
    public const string Expired = "Expired";

    public static bool IsActive(string status) =>
        status == Pending || status == Accepted;

    public static bool IsFinal(string status) =>
        status == Completed || status == Cancelled || status == Expired;
}
