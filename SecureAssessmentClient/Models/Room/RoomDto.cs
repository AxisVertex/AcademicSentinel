namespace SecureAssessmentClient.Models.Room
{
    public class RoomDto
    {
        public string Id { get; set; }
        public string SubjectName { get; set; }
        public string InstructorId { get; set; }
        public RoomStatus Status { get; set; }
        public DateTime CreatedAt { get; set; }
        public bool IsJoined { get; set; }
        public DateTime? JoinedAt { get; set; }
    }
}
