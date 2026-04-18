namespace SecureAssessmentClient.Models.Monitoring
{
    /// <summary>
    /// Represents the result of risk assessment on a MonitoringEvent
    /// Contains calculated risk score, level, and decision rationale
    /// </summary>
    public class RiskAssessment
    {
        public string SessionId { get; set; }
        public int RiskScore { get; set; }  // 0-100 scale
        public RiskLevel RiskLevel { get; set; }  // Safe, Suspicious, Cheating
        public DateTime Timestamp { get; set; }
        
        /// <summary>
        /// Human-readable explanation of the risk assessment
        /// </summary>
        public string RationaleDescription { get; set; }
        
        /// <summary>
        /// List of events that contributed to this assessment
        /// </summary>
        public List<MonitoringEvent> ContributingEvents { get; set; }
        
        /// <summary>
        /// Pattern details if multiple violations detected
        /// </summary>
        public string PatternDescription { get; set; }
        
        /// <summary>
        /// Recommended action (none, warn, block, escalate)
        /// </summary>
        public string RecommendedAction { get; set; }

        public RiskAssessment()
        {
            ContributingEvents = new List<MonitoringEvent>();
            Timestamp = DateTime.UtcNow;
        }

        public override string ToString()
        {
            return $"RiskAssessment(Score={RiskScore}, Level={RiskLevel}, Action={RecommendedAction})";
        }
    }
}
