namespace AcademicSentinel.Client.Services.SAC.Models
{
    public enum RiskLevel
    {
        Safe = 0,
        Suspicious = 1,
        Cheating = 2
    }

    public sealed class RiskAssessment
    {
        public int CurrentScore { get; set; }
        public RiskLevel CurrentLevel { get; set; }
        public bool HasThresholdChanged { get; set; }
    }
}
