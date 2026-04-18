namespace SecureAssessmentClient.Models.Authentication
{
    public class AuthToken
    {
        public string AccessToken { get; set; }
        public string TokenType { get; set; }
        public int ExpiresIn { get; set; }
    }
}
