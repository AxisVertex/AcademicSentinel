namespace AcademicSentinel.Client.Models
{
    public class StudentCourseDto
    {
        public int RoomId { get; set; }
        public string SubjectName { get; set; } = string.Empty;
        public string Section { get; set; } = string.Empty;
        public string InstructorName { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty; // e.g., "Active", "Pending"
    }
}