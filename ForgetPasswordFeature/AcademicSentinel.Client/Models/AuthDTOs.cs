namespace AcademicSentinel.Client.Models
{
    public class UserResponseDto
    {
        public int Id { get; set; }
        public string Email { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public string Token { get; set; } = string.Empty;

        // This must match the property name we added to the Server!
        public string? ProfileImageUrl { get; set; }
    }

    public class UserLoginDto
    {
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }

    public class UserRegisterDto
    {
        public string Email { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty; 
        public string Password { get; set; } = string.Empty;
        public string Role { get; set; } = "Student";
    }

    public class ForgotPasswordRequestDto
    {
        public string Email { get; set; } = string.Empty;
    }

    public class VerifyResetCodeRequestDto
    {
        public string Email { get; set; } = string.Empty;
        public string Code { get; set; } = string.Empty;
    }

    public class VerifyResetCodeResponseDto
    {
        public bool Success { get; set; }
        public string ResetToken { get; set; } = string.Empty;
    }

    public class ResetPasswordRequestDto
    {
        public string Email { get; set; } = string.Empty;
        public string NewPassword { get; set; } = string.Empty;
        public string ResetToken { get; set; } = string.Empty;
    }

    public class StudentListItem
    {
        // These are the exact fields causing your CS1061 errors
        public string StudentId { get; set; } = string.Empty;
        public string StudentName { get; set; } = string.Empty;
        public string StudentYear { get; set; } = string.Empty;
        public string StudentCourse { get; set; } = string.Empty;

        // Keep these for your list views and data tracking
        public string Email { get; set; } = string.Empty;
        public string EnrollmentSource { get; set; } = string.Empty;
        public string ParticipationStatus { get; set; } = string.Empty;
    }
}