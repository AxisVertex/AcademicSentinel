namespace SecureAssessmentClient.Utilities
{
    public static class Constants
    {
        public const string TOKEN_KEY = "auth_token";
        public const string USER_ID_KEY = "user_id";
        public const string USER_ROLE_KEY = "user_role";

        // Event Types
        public const string EVENT_ALT_TAB = "ALT_TAB";
        public const string EVENT_WINDOW_SWITCH = "WINDOW_SWITCH";
        public const string EVENT_PROCESS_DETECTED = "PROCESS_DETECTED";
        public const string EVENT_CLIPBOARD_COPY = "CLIPBOARD_COPY";
        public const string EVENT_CLIPBOARD_PASTE = "CLIPBOARD_PASTE";
        public const string EVENT_SCREENSHOT = "SCREENSHOT";
        public const string EVENT_IDLE = "IDLE";
        public const string EVENT_VM_DETECTED = "VM_DETECTED";
        public const string EVENT_HAS_DETECTED = "HAS_DETECTED";

        // Risk Levels
        public const string RISK_SAFE = "Safe";
        public const string RISK_SUSPICIOUS = "Suspicious";
        public const string RISK_CHEATING = "Cheating";

        // Room Status
        public const string ROOM_PENDING = "Pending";
        public const string ROOM_COUNTDOWN = "Countdown";
        public const string ROOM_ACTIVE = "Active";
        public const string ROOM_ENDED = "Ended";
    }
}
