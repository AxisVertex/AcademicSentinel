namespace AcademicSentinel.Server.Models;

/// <summary>
/// Tracks manually assigned students to a room
/// This defines who the instructor expects to see in the room
/// </summary>
public class SessionAssignment
{
    public int Id { get; set; }

    // Links to the Room
    public int RoomId { get; set; }

    // Links to the Student who was manually assigned
    public int StudentId { get; set; }

    public DateTime AssignedAt { get; set; } = DateTime.UtcNow;
}
