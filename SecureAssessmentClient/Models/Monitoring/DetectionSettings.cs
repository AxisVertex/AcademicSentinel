namespace SecureAssessmentClient.Models.Monitoring
{
    public class DetectionSettings
    {
        public string RoomId { get; set; }
        public bool EnableClipboardMonitoring { get; set; }
        public bool EnableProcessDetection { get; set; }
        public bool EnableIdleDetection { get; set; }

        // Configurable Idle Thresholds
        public int IdleWarningThresholdSeconds { get; set; } = 30; // Default 30s
        public int IdleViolationThresholdSeconds { get; set; } = 120; // Default 2 minutes
        public int IdleCriticalThresholdSeconds { get; set; } = 300; // Default 5 minutes

        // Backward compatibility
        public int IdleThresholdSeconds { get; set; }

        public bool EnableFocusDetection { get; set; }
        public bool EnableVirtualizationCheck { get; set; }
        public bool StrictMode { get; set; }
    }
}
