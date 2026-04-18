namespace AcademicSentinel.Client.Services.SAC.Models
{
    internal sealed class DetectionSettings
    {
        public bool EnableClipboardMonitoring { get; set; }
        public bool EnableProcessDetection { get; set; }
        public bool EnableIdleDetection { get; set; }
        public bool EnableFocusDetection { get; set; }

        public int IdleWarningThresholdSeconds { get; set; } = 30;
        public int IdleViolationThresholdSeconds { get; set; } = 120;
        public int IdleCriticalThresholdSeconds { get; set; } = 300;
    }
}
