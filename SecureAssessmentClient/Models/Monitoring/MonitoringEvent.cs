namespace SecureAssessmentClient.Models.Monitoring
{
    public class MonitoringEvent
    {
        public string EventType { get; set; }
        public ViolationType ViolationType { get; set; }
        public int SeverityScore { get; set; }
        public DateTime Timestamp { get; set; }
        public string Details { get; set; }
        public string SessionId { get; set; }
    }
}
