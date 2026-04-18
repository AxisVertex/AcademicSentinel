namespace AcademicSentinel.Server.DTOs;

public class RoomStatusUpdateDto
{
    // The new status the instructor wants to set.
    // Expected values: "Pending", "Countdown", "Active", or "Ended"
    public string Status { get; set; } = string.Empty;
}