namespace SecureAssessmentClient.Models.Authentication
{
    public class LoginResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; }
        public AuthToken Token { get; set; }
        public UserInfo User { get; set; }
    }

    public class UserInfo
    {
        public string Id { get; set; }
        public string Email { get; set; }
        public string Role { get; set; }
    }
}
