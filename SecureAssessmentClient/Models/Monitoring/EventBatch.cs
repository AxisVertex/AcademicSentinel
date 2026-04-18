namespace SecureAssessmentClient.Models.Monitoring
{
    /// <summary>
    /// Represents a batch of risk assessments bundled for transmission to server
    /// Enables efficient batching of multiple assessments into single transmission
    /// Tracks batch status and server acknowledgments
    /// </summary>
    public class EventBatch
    {
        /// <summary>
        /// Unique identifier for this batch
        /// </summary>
        public string BatchId { get; set; }

        /// <summary>
        /// Session ID for correlation with exam session
        /// </summary>
        public string SessionId { get; set; }

        /// <summary>
        /// Time when batch was created
        /// </summary>
        public DateTime CreatedAt { get; set; }

        /// <summary>
        /// Time when batch was transmitted to server
        /// </summary>
        public DateTime? TransmittedAt { get; set; }

        /// <summary>
        /// Risk assessments included in this batch
        /// </summary>
        public List<RiskAssessment> Assessments { get; set; }

        /// <summary>
        /// Number of transmission attempts made
        /// </summary>
        public int TransmissionAttempts { get; set; }

        /// <summary>
        /// Current status: pending, transmitted, acknowledged, failed
        /// </summary>
        public string Status { get; set; }  // pending, transmitted, acknowledged, failed

        /// <summary>
        /// Server's acknowledgment message (if received)
        /// </summary>
        public string AcknowledgmentMessage { get; set; }

        /// <summary>
        /// Priority level for transmission (0=normal, 1=high)
        /// High priority batches may contain critical violations
        /// </summary>
        public int Priority { get; set; }

        public EventBatch()
        {
            BatchId = Guid.NewGuid().ToString();
            CreatedAt = DateTime.UtcNow;
            Assessments = new List<RiskAssessment>();
            TransmissionAttempts = 0;
            Status = "pending";
            Priority = 0;
        }

        /// <summary>
        /// Checks if batch is ready for transmission (has assessments and correct status)
        /// </summary>
        public bool IsReadyForTransmission()
        {
            return Status == "pending" && Assessments.Count > 0;
        }

        /// <summary>
        /// Checks if batch contains any high-severity assessments
        /// </summary>
        public bool HasCriticalAssessments()
        {
            return Assessments.Any(a => a.RiskLevel == RiskLevel.Cheating);
        }

        /// <summary>
        /// Gets highest risk score in batch
        /// </summary>
        public int GetMaxRiskScore()
        {
            return Assessments.Count > 0 ? Assessments.Max(a => a.RiskScore) : 0;
        }

        /// <summary>
        /// Gets highest risk level in batch
        /// </summary>
        public RiskLevel GetMaxRiskLevel()
        {
            if (Assessments.Count == 0)
                return RiskLevel.Safe;

            var maxLevel = Assessments.Max(a => a.RiskLevel);
            return maxLevel;
        }

        /// <summary>
        /// Gets summary statistics for batch
        /// </summary>
        public Dictionary<string, int> GetStatistics()
        {
            return new Dictionary<string, int>
            {
                { "total_assessments", Assessments.Count },
                { "safe_count", Assessments.Count(a => a.RiskLevel == RiskLevel.Safe) },
                { "suspicious_count", Assessments.Count(a => a.RiskLevel == RiskLevel.Suspicious) },
                { "cheating_count", Assessments.Count(a => a.RiskLevel == RiskLevel.Cheating) },
                { "avg_risk_score", Assessments.Count > 0 ? (int)Assessments.Average(a => a.RiskScore) : 0 }
            };
        }

        public override string ToString()
        {
            var stats = GetStatistics();
            return $"EventBatch(Id={BatchId}, Status={Status}, Assessments={stats["total_assessments"]}, MaxLevel={GetMaxRiskLevel()})";
        }
    }
}
