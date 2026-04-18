using AcademicSentinel.Client.Models;

namespace AcademicSentinel.Client.Services
{
    public static class SessionManager
    {
        // This will hold our JWT Token while the app is running
        public static string JwtToken { get; set; } = string.Empty;

        // This holds the info of the currently logged-in Teacher
        public static UserResponseDto CurrentUser { get; set; } = null;

        // A quick check to see if we are logged in
        public static bool IsLoggedIn => !string.IsNullOrEmpty(JwtToken);

        // Clears the data when logging out
        public static void Logout()
        {
            JwtToken = string.Empty;
            CurrentUser = null;
        }
    }
}