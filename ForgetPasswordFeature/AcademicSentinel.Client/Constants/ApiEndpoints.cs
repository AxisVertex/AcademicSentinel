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
        public const string AuthForgotPassword = $"{BaseUrl}/api/auth/forgot-password";
        public const string AuthVerifyResetCode = $"{BaseUrl}/api/auth/verify-reset-code";
        public const string AuthResetPassword = $"{BaseUrl}/api/auth/reset-password";

        // We will add the Room, Image, and Report endpoints here later!
        public const string Rooms = $"{BaseUrl}/api/rooms";
    }
}