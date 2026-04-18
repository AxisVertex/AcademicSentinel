using System;
using System.Collections.Generic;
using System.Linq;
using SecureAssessmentClient.Models.Monitoring;
using SecureAssessmentClient.Utilities;

namespace SecureAssessmentClient.Services.DetectionService
{
    /// <summary>
    /// Decision engine for aggregating detection events and generating risk assessments
    /// Combines environment (Phase 6) and behavioral (Phase 7) detection data
    /// Performs real-time risk scoring with pattern recognition and escalation logic
    /// </summary>
    public class DecisionEngineService
    {
        // Risk score thresholds (0-100 scale)
        private int _suspiciousThreshold = 30;      // >= 30: Suspicious
        private int _cheatingThreshold = 70;        // >= 70: Cheating

        // Time window for pattern detection (in seconds)
        private const int PATTERN_WINDOW_SECONDS = 300;  // 5 minutes

        // Event history for pattern analysis
        private List<MonitoringEvent> _eventHistory;
        private DateTime _sessionStartTime;
        private string _sessionId;
        private bool _strictMode;

        // Weights for different violation types (configurable)
        private Dictionary<string, int> _eventTypeWeights = new Dictionary<string, int>
        {
            // Critical violations (high weight)
            { Constants.EVENT_VM_DETECTED, 40 },
            { Constants.EVENT_PROCESS_DETECTED, 35 },
            { Constants.EVENT_HAS_DETECTED, 30 },
            
            // High-risk behaviors (medium-high weight)
            { Constants.EVENT_WINDOW_SWITCH, 8 },
            { Constants.EVENT_CLIPBOARD_COPY, 10 },
            { Constants.EVENT_IDLE, 5 },
            
            // Default weight for unknown events
            { "DEFAULT", 15 }
        };

        // Pattern thresholds for escalation
        private Dictionary<string, int> _patternThresholds = new Dictionary<string, int>
        {
            { Constants.EVENT_WINDOW_SWITCH, 3 },     // 3+ excessive switches in window = pattern
            { Constants.EVENT_CLIPBOARD_COPY, 5 },    // 5+ clipboard accesses = suspicious pattern
            { Constants.EVENT_PROCESS_DETECTED, 1 },  // Any process detection = immediate escalation
        };

        public DecisionEngineService(string sessionId, bool strictMode = false)
        {
            _sessionId = sessionId;
            _strictMode = strictMode;
            _eventHistory = new List<MonitoringEvent>();
            _sessionStartTime = DateTime.UtcNow;
            Logger.Info($"Decision Engine initialized for session {sessionId}, StrictMode={strictMode}");
        }

        /// <summary>
        /// Performs comprehensive risk assessment on a single monitoring event
        /// Analyzes event in context of session history and patterns
        /// </summary>
        public RiskAssessment AssessEvent(MonitoringEvent monitoringEvent)
        {
            if (monitoringEvent == null)
            {
                return null;
            }

            try
            {
                var assessment = new RiskAssessment
                {
                    SessionId = _sessionId
                };

                // Add event to history
                _eventHistory.Add(monitoringEvent);

                // Calculate base score from event severity
                int baseScore = CalculateBaseScore(monitoringEvent);

                // Check for patterns in recent history
                int patternScore = AnalyzePatterns(monitoringEvent.EventType);

                // Combine scores
                assessment.RiskScore = Math.Min(100, baseScore + patternScore);

                // Determine risk level based on score
                assessment.RiskLevel = DetermineRiskLevel(assessment.RiskScore);

                // Get recent events for context
                assessment.ContributingEvents = GetRecentEvents(5);

                // Generate decision and rationale
                assessment.RecommendedAction = DetermineAction(assessment.RiskScore, assessment.RiskLevel, monitoringEvent);
                assessment.RationaleDescription = GenerateRationale(monitoringEvent, baseScore, patternScore, assessment.RiskLevel);

                // Check for detected patterns
                assessment.PatternDescription = DetectPatterns();

                Logger.Info($"Risk Assessment: {assessment.RiskLevel} (Score: {assessment.RiskScore}, Event: {monitoringEvent.EventType})");

                return assessment;
            }
            catch (Exception ex)
            {
                Logger.Error("Error during risk assessment", ex);
                return null;
            }
        }

        /// <summary>
        /// Performs batch assessment on multiple events
        /// Returns assessment based on highest-risk event
        /// </summary>
        public RiskAssessment AssessEvents(List<MonitoringEvent> events)
        {
            if (events == null || events.Count == 0)
            {
                return null;
            }

            RiskAssessment highestRiskAssessment = null;
            int highestScore = 0;

            foreach (var evt in events)
            {
                var assessment = AssessEvent(evt);
                if (assessment != null && assessment.RiskScore > highestScore)
                {
                    highestScore = assessment.RiskScore;
                    highestRiskAssessment = assessment;
                }
            }

            return highestRiskAssessment;
        }

        /// <summary>
        /// Performs periodic full assessment of entire session
        /// Called periodically to update overall risk status
        /// </summary>
        public RiskAssessment PerformFullSessionAssessment()
        {
            if (_eventHistory.Count == 0)
            {
                return new RiskAssessment
                {
                    SessionId = _sessionId,
                    RiskScore = 0,
                    RiskLevel = RiskLevel.Safe,
                    RationaleDescription = "No violations detected",
                    RecommendedAction = "none"
                };
            }

            try
            {
                var assessment = new RiskAssessment
                {
                    SessionId = _sessionId
                };

                // Calculate aggregate risk from all events
                int aggregateScore = 0;
                var recentEvents = GetRecentEvents(20);

                foreach (var evt in recentEvents)
                {
                    aggregateScore += CalculateBaseScore(evt);
                }

                // Apply pattern penalties
                aggregateScore += AnalyzePatterns(null);  // null = analyze all patterns

                assessment.RiskScore = Math.Min(100, aggregateScore);
                assessment.RiskLevel = DetermineRiskLevel(assessment.RiskScore);
                assessment.ContributingEvents = recentEvents;
                assessment.PatternDescription = DetectPatterns();
                assessment.RecommendedAction = DetermineAction(assessment.RiskScore, assessment.RiskLevel, null);
                assessment.RationaleDescription = $"Session assessment: {_eventHistory.Count} total events analyzed. Pattern status: {assessment.PatternDescription}";

                return assessment;
            }
            catch (Exception ex)
            {
                Logger.Error("Error during full session assessment", ex);
                return null;
            }
        }

        /// <summary>
        /// Calculates base risk score from a single event's severity
        /// Maps severity (1-3) to risk points
        /// </summary>
        private int CalculateBaseScore(MonitoringEvent evt)
        {
            if (evt == null)
                return 0;

            // Get weight for this event type
            int weight = _eventTypeWeights.ContainsKey(evt.EventType)
                ? _eventTypeWeights[evt.EventType]
                : _eventTypeWeights["DEFAULT"];

            // Adjust weight based on violation type
            if (evt.ViolationType == ViolationType.Aggressive)
            {
                weight = (int)(weight * 1.5);  // Aggressive violations weighted 50% higher
            }

            // Multiply by event severity (1-3)
            int baseScore = weight * evt.SeverityScore;

            // In strict mode, increase all scores by 25%
            if (_strictMode)
            {
                baseScore = (int)(baseScore * 1.25);
            }

            return baseScore;
        }

        /// <summary>
        /// Analyzes event history for patterns indicating coordinated cheating
        /// Returns additional risk points based on pattern severity
        /// </summary>
        private int AnalyzePatterns(string specificEventType)
        {
            int patternScore = 0;

            try
            {
                var recentEvents = GetRecentEvents(int.MaxValue);  // All events in window

                // Check for repeated violations of same type
                var eventGrouping = recentEvents
                    .GroupBy(e => e.EventType)
                    .Where(g => _patternThresholds.ContainsKey(g.Key));

                foreach (var group in eventGrouping)
                {
                    // Skip if checking specific type and this isn't it
                    if (specificEventType != null && specificEventType != group.Key)
                        continue;

                    int threshold = _patternThresholds[group.Key];
                    int eventCount = group.Count();

                    // Pattern detected: multiple violations of same type
                    if (eventCount >= threshold)
                    {
                        // Calculate escalation: each violation beyond threshold adds more risk
                        int escalation = (eventCount - threshold) * 10;
                        patternScore += escalation;

                        Logger.Warn($"Pattern detected: {group.Key} occurred {eventCount} times (threshold: {threshold})");
                    }
                }

                // Check for mixed violation patterns (environment + behavior)
                bool hasEnvironmentViolation = recentEvents.Any(e => 
                    e.EventType == Constants.EVENT_VM_DETECTED ||
                    e.EventType == Constants.EVENT_HAS_DETECTED);

                bool hasBehaviorViolations = recentEvents.Any(e => 
                    e.EventType == Constants.EVENT_WINDOW_SWITCH ||
                    e.EventType == Constants.EVENT_CLIPBOARD_COPY ||
                    e.EventType == Constants.EVENT_PROCESS_DETECTED);

                // Combination of environment + behavioral violations = high risk
                if (hasEnvironmentViolation && hasBehaviorViolations)
                {
                    patternScore += 20;
                    Logger.Error("Mixed violation pattern detected (environment + behavior)");
                }

                return patternScore;
            }
            catch (Exception ex)
            {
                Logger.Error("Error analyzing patterns", ex);
                return 0;
            }
        }

        /// <summary>
        /// Maps numeric risk score (0-100) to RiskLevel enum
        /// </summary>
        private RiskLevel DetermineRiskLevel(int riskScore)
        {
            if (riskScore >= _cheatingThreshold)
                return RiskLevel.Cheating;
            
            if (riskScore >= _suspiciousThreshold)
                return RiskLevel.Suspicious;
            
            return RiskLevel.Safe;
        }

        /// <summary>
        /// Determines recommended action based on risk assessment
        /// </summary>
        private string DetermineAction(int riskScore, RiskLevel riskLevel, MonitoringEvent triggeringEvent)
        {
            // Immediate block for critical violations
            if (triggeringEvent != null && triggeringEvent.ViolationType == ViolationType.Aggressive)
            {
                return "escalate";  // Server decides block vs warn
            }

            return riskLevel switch
            {
                RiskLevel.Safe => "none",
                RiskLevel.Suspicious => "warn",
                RiskLevel.Cheating => "escalate",
                _ => "none"
            };
        }

        /// <summary>
        /// Generates human-readable explanation of risk assessment
        /// </summary>
        private string GenerateRationale(MonitoringEvent evt, int baseScore, int patternScore, RiskLevel level)
        {
            var parts = new List<string>();

            parts.Add($"Event: {evt.EventType}");
            parts.Add($"Severity: {evt.SeverityScore}/3");
            parts.Add($"Type: {evt.ViolationType}");
            parts.Add($"Base Score: {baseScore}");

            if (patternScore > 0)
            {
                parts.Add($"Pattern Penalty: +{patternScore}");
            }

            parts.Add($"Result: {level}");

            return string.Join(" | ", parts);
        }

        /// <summary>
        /// Analyzes event history to identify suspicious patterns
        /// Returns description of detected patterns
        /// </summary>
        private string DetectPatterns()
        {
            var recentEvents = GetRecentEvents(int.MaxValue);
            
            if (recentEvents.Count == 0)
                return "No patterns detected";

            var patterns = new List<string>();

            // Count by event type
            var eventCounts = recentEvents
                .GroupBy(e => e.EventType)
                .OrderByDescending(g => g.Count());

            foreach (var group in eventCounts)
            {
                int threshold = _patternThresholds.ContainsKey(group.Key)
                    ? _patternThresholds[group.Key]
                    : 0;

                if (threshold > 0 && group.Count() >= threshold)
                {
                    patterns.Add($"{group.Key}: {group.Count()} (threshold: {threshold})");
                }
            }

            if (patterns.Count == 0)
                return "No suspicious patterns";

            return "Detected: " + string.Join("; ", patterns);
        }

        /// <summary>
        /// Gets recent events within time window
        /// </summary>
        private List<MonitoringEvent> GetRecentEvents(int maxCount)
        {
            var cutoffTime = DateTime.UtcNow.AddSeconds(-PATTERN_WINDOW_SECONDS);
            
            return _eventHistory
                .Where(e => e.Timestamp >= cutoffTime)
                .OrderByDescending(e => e.Timestamp)
                .Take(maxCount)
                .ToList();
        }

        /// <summary>
        /// Updates risk thresholds at runtime
        /// Called when server sends configuration updates
        /// </summary>
        public void UpdateThresholds(int suspiciousLevel, int cheatingLevel)
        {
            if (suspiciousLevel >= 0 && suspiciousLevel < 100)
                _suspiciousThreshold = suspiciousLevel;
            
            if (cheatingLevel >= 0 && cheatingLevel < 100)
                _cheatingThreshold = cheatingLevel;

            Logger.Info($"Risk thresholds updated: Suspicious={_suspiciousThreshold}, Cheating={_cheatingThreshold}");
        }

        /// <summary>
        /// Updates event type weights for scoring
        /// Allows server-side tuning of detection effectiveness
        /// </summary>
        public void UpdateEventWeights(Dictionary<string, int> newWeights)
        {
            if (newWeights != null && newWeights.Count > 0)
            {
                foreach (var kvp in newWeights)
                {
                    if (_eventTypeWeights.ContainsKey(kvp.Key))
                        _eventTypeWeights[kvp.Key] = kvp.Value;
                }
                Logger.Info("Event weights updated");
            }
        }

        /// <summary>
        /// Updates pattern detection thresholds
        /// </summary>
        public void UpdatePatternThresholds(Dictionary<string, int> newThresholds)
        {
            if (newThresholds != null && newThresholds.Count > 0)
            {
                foreach (var kvp in newThresholds)
                {
                    if (_patternThresholds.ContainsKey(kvp.Key))
                        _patternThresholds[kvp.Key] = kvp.Value;
                }
                Logger.Info("Pattern thresholds updated");
            }
        }

        /// <summary>
        /// Clears event history (e.g., for new exam room)
        /// </summary>
        public void ClearHistory()
        {
            _eventHistory.Clear();
            Logger.Info("Event history cleared");
        }

        /// <summary>
        /// Gets current session statistics
        /// </summary>
        public Dictionary<string, int> GetSessionStatistics()
        {
            var stats = new Dictionary<string, int>
            {
                { "total_events", _eventHistory.Count },
                { "session_duration_seconds", (int)(DateTime.UtcNow - _sessionStartTime).TotalSeconds },
                { "unique_event_types", _eventHistory.Select(e => e.EventType).Distinct().Count() },
                { "aggressive_violations", _eventHistory.Count(e => e.ViolationType == ViolationType.Aggressive) },
                { "passive_violations", _eventHistory.Count(e => e.ViolationType == ViolationType.Passive) }
            };

            return stats;
        }

        /// <summary>
        /// Gets current suspicious threshold
        /// </summary>
        public int SuspiciousThreshold
        {
            get { return _suspiciousThreshold; }
        }

        /// <summary>
        /// Gets current cheating threshold
        /// </summary>
        public int CheatingThreshold
        {
            get { return _cheatingThreshold; }
        }

        /// <summary>
        /// Gets total events in history
        /// </summary>
        public int EventCount
        {
            get { return _eventHistory.Count; }
        }

        /// <summary>
        /// Gets session ID
        /// </summary>
        public string SessionId
        {
            get { return _sessionId; }
        }

        /// <summary>
        /// Gets time elapsed since session start
        /// </summary>
        public TimeSpan SessionDuration
        {
            get { return DateTime.UtcNow - _sessionStartTime; }
        }
    }
}
