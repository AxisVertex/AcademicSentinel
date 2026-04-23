using AcademicSentinel.Client.Services.SAC.Models;
using System;

namespace AcademicSentinel.Client.Services.SAC.DetectionService
{
    public class DecisionEngineService
    {
        private readonly object _syncRoot = new();
        private int _cumulativeScore = 0;
        private RiskLevel _currentLevel = RiskLevel.Safe;

        public RiskAssessment EvaluateEvent(MonitoringDetectionEvent newEvent)
        {
            if (newEvent == null)
                throw new ArgumentNullException(nameof(newEvent));

            lock (_syncRoot)
            {
                newEvent.SeverityScore = ResolveSeverityScore(newEvent.EventType);

                _cumulativeScore += Math.Max(0, newEvent.SeverityScore);

                var previousLevel = _currentLevel;
                _currentLevel = ResolveRiskLevel(_cumulativeScore);

                return new RiskAssessment
                {
                    CurrentScore = _cumulativeScore,
                    CurrentLevel = _currentLevel,
                    HasThresholdChanged = previousLevel != _currentLevel
                };
            }
        }

        private static int ResolveSeverityScore(string eventType)
        {
            var normalized = (eventType ?? string.Empty).Trim().ToUpperInvariant();

            return normalized switch
            {
                "RTFM" or "ALT_TAB" or "WINDOW_SWITCH" or "FOCUS" => 10,
                "IDLE" or "INACTIVITY" => 10,
                "CSAD" or "CLIPBOARD" or "COPY" or "PASTE" or "SCREENSHOT" or "PRINTSCREEN" => 20,
                "VAC" or "HAS" or "PBD" or "VM" or "REMOTE" or "PROCESS" => 40,
                _ when normalized.Contains("RTFM") || normalized.Contains("ALT_TAB") || normalized.Contains("WINDOW_SWITCH") || normalized.Contains("FOCUS") => 10,
                _ when normalized.Contains("IDLE") || normalized.Contains("INACTIVITY") => 10,
                _ when normalized.Contains("CSAD") || normalized.Contains("CLIPBOARD") || normalized.Contains("COPY") || normalized.Contains("PASTE") || normalized.Contains("SCREENSHOT") || normalized.Contains("PRINTSCREEN") => 20,
                _ when normalized.Contains("VAC") || normalized.Contains("HAS") || normalized.Contains("PBD") || normalized.Contains("VM") || normalized.Contains("REMOTE") || normalized.Contains("PROCESS") => 40,
                _ => 10
            };
        }

        private static RiskLevel ResolveRiskLevel(int cumulativeScore)
        {
            if (cumulativeScore < 20)
                return RiskLevel.Safe;

            if (cumulativeScore < 50)
                return RiskLevel.Suspicious;

            return RiskLevel.Cheating;
        }
    }
}
