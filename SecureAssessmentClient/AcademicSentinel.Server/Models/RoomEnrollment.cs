namespace AcademicSentinel.Server.Models;

public class RoomEnrollment
{
    public int Id { get; set; }
    public int RoomId { get; set; }

    // CHANGED: Now uses StudentId as required by your thesis document
    public int StudentId { get; set; }

    public string EnrollmentSource { get; set; } = "Code";
    public DateTime EnrolledAt { get; set; } = DateTime.UtcNow;
}