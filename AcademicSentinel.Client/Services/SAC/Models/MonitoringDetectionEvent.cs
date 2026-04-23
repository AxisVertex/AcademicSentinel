using System;

namespace AcademicSentinel.Client.Services.SAC.Models
{
    public sealed class MonitoringDetectionEvent
    {
        public string EventType { get; set; } = string.Empty;
        public int SeverityScore { get; set; }
        public string Description { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    }
}
