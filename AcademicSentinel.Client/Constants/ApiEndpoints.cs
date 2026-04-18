namespace AcademicSentinel.Client.Constants
{
    public static class ApiEndpoints
    {
        // ========================================================
        // MAIN SERVER URL
        // Make sure this port matches your AcademicSentinel.Server
        // ========================================================
        public const string BaseUrl = "https://localhost:7123";

        // ========================================================
        // AUTHENTICATION
        // ========================================================
        public const string AuthRegister = $"{BaseUrl}/api/auth/register";
        public const string AuthLogin = $"{BaseUrl}/api/auth/login";
        public const string AuthProfile = $"{BaseUrl}/api/auth/profile";
        public const string AuthChangePassword = $"{BaseUrl}/api/auth/change-password";

        // We will add the Room, Image, and Report endpoints here later!
        public const string Rooms = $"{BaseUrl}/api/rooms";
    }
}