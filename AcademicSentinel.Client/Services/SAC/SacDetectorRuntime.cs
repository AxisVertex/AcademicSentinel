using System;
using System.Collections.Generic;
using System.Linq;
using AcademicSentinel.Client.Services.SAC.DetectionService;
using AcademicSentinel.Client.Services.SAC.Models;
// test comment here
namespace AcademicSentinel.Client.Services.SAC
{
    internal sealed class SacDetectorRuntime
    {
        private readonly DetectorRuntimeOptions _options;
        private readonly BehavioralMonitoringService _behavioralMonitoringService;
        private bool _isStarted;
        public bool IsLoggingEnabled { get; private set; } = true;

        public SacDetectorRuntime(DetectorRuntimeOptions options)
        {
            _options = options;

            int idleViolation = Math.Max(1, _options.IdleThresholdSeconds);

            var settings = new DetectionSettings
            {
                EnableFocusDetection = _options.EnableFocusDetection,
                EnableClipboardMonitoring = _options.EnableClipboardMonitoring,
                EnableIdleDetection = _options.EnableIdleDetection,
                EnableProcessDetection = _options.EnableProcessDetection,
                IdleWarningThresholdSeconds = Math.Max(10, idleViolation / 2),
                IdleViolationThresholdSeconds = idleViolation,
                IdleCriticalThresholdSeconds = Math.Max(idleViolation + 10, idleViolation * 2)
            };

            _behavioralMonitoringService = new BehavioralMonitoringService(settings, _options.BlacklistedProcessNames);
        }

        public IReadOnlyList<DetectorFinding> Poll(bool isWindowActive)
        {
            if (!_isStarted)
                return Array.Empty<DetectorFinding>();

            return MapFindings(_behavioralMonitoringService.Poll(isWindowActive));
        }

        public IReadOnlyList<DetectorFinding> RunStartupChecks()
        {
            return Array.Empty<DetectorFinding>();
        }

        public IReadOnlyList<DetectorFinding> OnWindowDeactivated()
        {
            if (!_isStarted)
                return Array.Empty<DetectorFinding>();

            return MapFindings(_behavioralMonitoringService.Poll(false));
        }

        public void SetMonitoringEnabled(bool enabled)
        {
            if (enabled)
            {
                if (_isStarted)
                    return;

                _isStarted = true;
                IsLoggingEnabled = true;
                _behavioralMonitoringService.StartMonitoring();
                return;
            }

            if (!_isStarted)
                return;

            _isStarted = false;
            IsLoggingEnabled = false;
            _behavioralMonitoringService.StopMonitoring();
        }

        public async Task StopMonitoringAsync()
        {
            IsLoggingEnabled = false;

            if (!_isStarted)
                return;

            _isStarted = false;
            _behavioralMonitoringService.StopMonitoring();
            await Task.CompletedTask;
        }

        private static IReadOnlyList<DetectorFinding> MapFindings(IReadOnlyList<MonitoringDetectionEvent> events)
        {
            if (events == null || events.Count == 0)
                return Array.Empty<DetectorFinding>();

            return events
                .Select(e => new DetectorFinding(e.EventType, e.SeverityScore, e.Description))
                .ToList();
        }
    }

    internal sealed class DetectorRuntimeOptions
    {
        public bool EnableFocusDetection { get; set; }
        public bool EnableClipboardMonitoring { get; set; }
        public bool EnableIdleDetection { get; set; }
        public int IdleThresholdSeconds { get; set; }
        public bool EnableProcessDetection { get; set; }
        public bool EnableVirtualizationCheck { get; set; }
        public HashSet<string> BlacklistedProcessNames { get; set; } = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
    }

    internal sealed class DetectorFinding
    {
        public DetectorFinding(string eventType, int severityScore, string description)
        {
            EventType = eventType;
            SeverityScore = severityScore;
            Description = description;
        }

        public string EventType { get; }
        public int SeverityScore { get; }
        public string Description { get; }
    }
}
