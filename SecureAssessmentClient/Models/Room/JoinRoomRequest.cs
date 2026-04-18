namespace SecureAssessmentClient.Models.Room
{
    public class JoinRoomRequest
    {
        public string RoomId { get; set; }
    }

    public class EnrollWithCodeRequest
    {
        public string RoomCode { get; set; }
    }
}
