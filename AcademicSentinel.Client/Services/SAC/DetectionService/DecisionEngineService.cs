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
                var normalized = (newEvent.EventType ?? string.Empty).Trim().ToUpperInvariant();

                switch (normalized)
                {
                    case "RTFM":
                    case "ALT_TAB":
                    case "WINDOW_SWITCH":
                    case "FOCUS":
                        newEvent.SeverityScore = 10;
                        break;
                    case "IDLE":
                    case "INACTIVITY":
                        newEvent.SeverityScore = 10;
                        break;
                    case "CSAD":
                    case "CLIPBOARD":
                    case "COPY":
                    case "PASTE":
                    case "SCREENSHOT":
                    case "PRINTSCREEN":
                        newEvent.SeverityScore = 20;
                        break;
                    case "PBD":
                        newEvent.SeverityScore = 30;
                        break;
                    case "VAC":
                    case "HAS":
                    case "VM":
                    case "REMOTE":
                    case "PROCESS":
                        newEvent.SeverityScore = 40;
                        break;
                    default:
                        if (normalized.Contains("RTFM") || normalized.Contains("ALT_TAB") || normalized.Contains("WINDOW_SWITCH") || normalized.Contains("FOCUS"))
                            newEvent.SeverityScore = 10;
                        else if (normalized.Contains("IDLE") || normalized.Contains("INACTIVITY"))
                            newEvent.SeverityScore = 10;
                        else if (normalized.Contains("CSAD") || normalized.Contains("CLIPBOARD") || normalized.Contains("COPY") || normalized.Contains("PASTE") || normalized.Contains("SCREENSHOT") || normalized.Contains("PRINTSCREEN"))
                            newEvent.SeverityScore = 20;
                        else if (normalized.Contains("PBD"))
                            newEvent.SeverityScore = 30;
                        else if (normalized.Contains("VAC") || normalized.Contains("HAS") || normalized.Contains("VM") || normalized.Contains("REMOTE") || normalized.Contains("PROCESS"))
                            newEvent.SeverityScore = 40;
                        else
                            newEvent.SeverityScore = 10;
                        break;
                }

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
