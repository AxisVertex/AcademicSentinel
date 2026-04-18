using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using SecureAssessmentClient.Models.Monitoring;
using SecureAssessmentClient.Services;
using SecureAssessmentClient.Services.DetectionService;
using SecureAssessmentClient.Utilities;

namespace SecureAssessmentClient.Testing
{
    /// <summary>
    /// Test scenario runner for validating complete detection pipeline
    /// Simulates exam conditions and demonstrates Phase 6→7→8→9 integration
    /// </summary>
    public class TestScenarioRunner
    {
        private readonly string _sessionId;
        private readonly string _hubUrl;
        
        private EnvironmentIntegrityService _environmentService;
        private BehavioralMonitoringService _behaviorService;
        private DecisionEngineService _decisionEngine;
        private EventLoggerService _eventLogger;
        private SignalRService _signalRService;
        
        private DetectionSettings _detectionSettings;
        private bool _isRunning;
        private List<RiskAssessment> _assessmentHistory;
        private List<MonitoringEvent> _eventHistory;

        public TestScenarioRunner(string hubUrl = "http://localhost:5000/exam-hub")
        {
            _sessionId = Guid.NewGuid().ToString().Substring(0, 8);
            _hubUrl = hubUrl;
            _isRunning = false;
            _assessmentHistory = new List<RiskAssessment>();
            _eventHistory = new List<MonitoringEvent>();
            
            Logger.Info($"Test Runner initialized with SessionID: {_sessionId}");
        }

        /// <summary>
        /// Initializes all services in the detection pipeline
        /// </summary>
        public async Task<bool> InitializeAsync(string authToken)
        {
            try
            {
                Logger.Info("Initializing detection pipeline for testing...");

                // Setup detection settings
                _detectionSettings = new DetectionSettings
                {
                    RoomId = "TEST_ROOM",
                    EnableClipboardMonitoring = true,
                    EnableProcessDetection = true,
                    EnableIdleDetection = true,
                    IdleWarningThresholdSeconds = 30,    // Configurable warning limit
                    IdleViolationThresholdSeconds = 60,  // Configurable violation limit
                    IdleCriticalThresholdSeconds = 90,   // Configurable critical limit
                    EnableFocusDetection = true,
                    EnableVirtualizationCheck = true,
                    StrictMode = false
                };

                // Phase 6: Environment Integrity Service (run once at startup)
                _environmentService = new EnvironmentIntegrityService();
                Logger.Info("✓ Environment Integrity Service initialized");

                // Note: Skip automatic environment detection at startup in test mode
                // This avoids "dummy data" from testing environment artifacts (e.g., Hyper-V detection)
                // User can manually run environment check using Option 1
                Console.WriteLine("\n[STARTUP] Environment integrity service ready");
                Console.WriteLine("[INFO] Use menu option 1 to manually check environment");
                Console.WriteLine("[INFO] (Skipping automatic check to avoid testing environment artifacts)");


                // Phase 7: Behavioral Monitoring Service (runs continuously)
                _behaviorService = new BehavioralMonitoringService(_detectionSettings, _sessionId);
                _behaviorService.StartMonitoring();
                Logger.Info("✓ Behavioral Monitoring Service initialized (continuous monitoring active)");

                // Phase 8: Decision Engine Service
                _decisionEngine = new DecisionEngineService(_sessionId, strictMode: false);
                Logger.Info("✓ Decision Engine Service initialized");

                // Phase 9: Event Logger Service
                _signalRService = new SignalRService(_hubUrl);
                _eventLogger = new EventLoggerService(_signalRService, _sessionId);

                try
                {
                    await _signalRService.ConnectAsync(authToken, _sessionId);
                    Logger.Info("✓ SignalR connection established");
                }
                catch (Exception ex)
                {
                    Logger.Warn($"SignalR connection failed (continuing with offline testing): {ex.Message}");
                }

                _eventLogger.Start();
                Logger.Info("✓ Event Logger Service initialized");

                _isRunning = true;
                return true;
            }
            catch (Exception ex)
            {
                Logger.Error("Failed to initialize detection pipeline", ex);
                return false;
            }
        }

        /// <summary>
        /// Runs real environment detection (Phase 6)
        /// Checks for VMs, debuggers, and suspicious processes
        /// </summary>
        public async Task RunEnvironmentDetectionAsync()
        {
            try
            {
                // Check virtualization and hardware artifacts
                var (isVirtual, vmViolations) = _environmentService.CheckVirtualizationArtifacts();

                // Check for debuggers and hardware anomalies
                var (hasAnomalies, anomalies) = _environmentService.ScanHardwareSoftwareArtifacts();

                // Display results in order - show violations first, then passed checks
                if (isVirtual)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine($"⚠️  VIRTUALIZATION DETECTED:");
                    foreach (var violation in vmViolations)
                    {
                        Console.WriteLine($"     • {violation}");
                    }
                    Console.ResetColor();

                    // Create and process event
                    var vmEvent = new MonitoringEvent
                    {
                        EventType = Constants.EVENT_VM_DETECTED,
                        ViolationType = ViolationType.Aggressive,
                        SeverityScore = 3,
                        Timestamp = DateTime.UtcNow,
                        Details = string.Join(" | ", vmViolations),
                        SessionId = _sessionId
                    };
                    ProcessDetectionEvent(vmEvent, "⚠️ VM DETECTED");
                }
                else
                {
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine("[✓] Virtualization Check: PASSED");
                    Console.ResetColor();
                }

                if (hasAnomalies)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine($"⚠️  SUSPICIOUS ARTIFACTS DETECTED:");
                    foreach (var anomaly in anomalies)
                    {
                        Console.WriteLine($"     • {anomaly}");
                    }
                    Console.ResetColor();

                    // Create and process event
                    var anomalyEvent = new MonitoringEvent
                    {
                        EventType = Constants.EVENT_HAS_DETECTED,
                        ViolationType = ViolationType.Aggressive,
                        SeverityScore = 3,
                        Timestamp = DateTime.UtcNow,
                        Details = string.Join(" | ", anomalies),
                        SessionId = _sessionId
                    };
                    ProcessDetectionEvent(anomalyEvent, "⚠️ ANOMALIES DETECTED");
                }
                else
                {
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine("[✓] Debugger/Artifacts Check: PASSED");
                    Console.ResetColor();
                }

                Console.WriteLine();
                if (!isVirtual && !hasAnomalies)
                {
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine("[✓] Environment Safety: ALL CHECKS PASSED");
                    Console.ResetColor();
                }
                else
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("[✗] Environment Safety: THREATS DETECTED");
                    Console.ResetColor();
                }
                Console.WriteLine();
            }
            catch (Exception ex)
            {
                Logger.Error("Error during environment detection", ex);
            }
        }

        /// <summary>
        /// Re-runs environment detection (Phase 6) - allows manual re-check anytime
        /// </summary>
        public async Task SimulateEnvironmentDetectionAsync()
        {
            Console.WriteLine("\n=== RE-CHECKING ENVIRONMENT INTEGRITY ===\n");
            await RunEnvironmentDetectionAsync();
        }

        /// <summary>
        /// Triggers Alt-Tab detection test
        /// Monitors REAL window switching via Alt+Tab
        /// Displays real-time window focus changes with source and destination applications
        /// </summary>
        public void TriggerAltTabTest()
        {
            Console.WriteLine("\n╔════════════════════════════════════════════════════╗");
            Console.WriteLine("║    ALT-TAB WINDOW SWITCHING DETECTION TEST         ║");
            Console.WriteLine("╚════════════════════════════════════════════════════╝");

            Console.WriteLine("\n⚠️  INSTRUCTIONS:");
            Console.WriteLine("   • The system monitors REAL window switching via Alt+Tab");
            Console.WriteLine("   • Switch windows now with Alt+Tab!");
            Console.WriteLine("   • Each window switch will be detected and logged");
            Console.WriteLine("   • Excessive switching (>5 times) is flagged as suspicious");
            Console.WriteLine("   • Press Enter to stop monitoring\n");

            Console.WriteLine("[MONITORING] Watching for window switching...");
            Console.WriteLine("Switch windows now with Alt+Tab!");
            Console.WriteLine("(Press Enter to stop monitoring)\n");
            Console.WriteLine("═══════════════════════════════════════════════════════");

            // Clear previous events
            _behaviorService.ClearDetectedEvents();

            bool isMonitoring = true;
            int switchCount = 0;

            while (isMonitoring)
            {
                // Call DetectWindowFocus() to detect Alt+Tab switches
                var windowSwitchEvent = _behaviorService.DetectWindowFocus();

                if (windowSwitchEvent != null)
                {
                    switchCount++;
                    Console.ForegroundColor = ConsoleColor.Green;

                    // Extract "From: X → To: Y" from the event details
                    string[] detailLines = windowSwitchEvent.Details.Split(new[] { '\n' }, System.StringSplitOptions.RemoveEmptyEntries);
                    string switchInfo = "";

                    foreach (var line in detailLines)
                    {
                        if (line.Contains("From:") && line.Contains("→"))
                        {
                            // Extract just "FromApp → ToApp" format
                            switchInfo = line.Replace("From: ", "").Trim();
                            break;
                        }
                    }

                    if (string.IsNullOrEmpty(switchInfo))
                    {
                        switchInfo = windowSwitchEvent.Details;
                    }

                    Console.WriteLine($"✓ Window switch detected! {switchInfo}");
                    Console.ResetColor();
                }

                // Check if user pressed Enter to stop monitoring
                if (Console.KeyAvailable)
                {
                    var key = Console.ReadKey(true);
                    if (key.Key == ConsoleKey.Enter)
                    {
                        isMonitoring = false;
                        break;
                    }
                }

                // Sleep to prevent high CPU usage
                Thread.Sleep(100);
            }

            Console.WriteLine("\n\n[STOPPED] Monitoring stopped.");

            // Output final events tracked by the backend
            var windowSwitchEvents = _behaviorService.GetWindowSwitchEvents();
            if (windowSwitchEvents != null && windowSwitchEvents.Count > 0)
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine($"\n✓ Detected {windowSwitchEvents.Count} window switch events in the backend!");
                Console.ResetColor();

                foreach (var evt in windowSwitchEvents)
                {
                    ProcessDetectionEvent(evt, "WINDOW_SWITCH_DETECTED");
                }
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine("\n⚠️  No window switches detected.");
                Console.WriteLine("    Try switching between windows using Alt+Tab.");
                Console.ResetColor();
            }
        }

        private void ConfigureIdleThresholds()
        {
            Console.WriteLine("\n[TEACHER/IMC SETUP] Configure Idle Risk Thresholds");
            Console.WriteLine($"Current Settings: Warning={_detectionSettings.IdleWarningThresholdSeconds}s, Violation={_detectionSettings.IdleViolationThresholdSeconds}s, Critical={_detectionSettings.IdleCriticalThresholdSeconds}s");
            Console.Write("Would you like to customize these thresholds? (y/n): ");
            var response = Console.ReadLine()?.Trim().ToLower();

            if (response == "y" || response == "yes")
            {
                int newWarning = GetThresholdInput("Enter WARNING threshold in seconds (e.g., 10): ", _detectionSettings.IdleWarningThresholdSeconds);
                int newViolation = GetThresholdInput("Enter VIOLATION threshold in seconds (e.g., 30): ", _detectionSettings.IdleViolationThresholdSeconds);
                int newCritical = GetThresholdInput("Enter CRITICAL threshold in seconds (e.g., 60): ", _detectionSettings.IdleCriticalThresholdSeconds);

                if (newWarning < newViolation && newViolation < newCritical)
                {
                    _detectionSettings.IdleWarningThresholdSeconds = newWarning;
                    _detectionSettings.IdleViolationThresholdSeconds = newViolation;
                    _detectionSettings.IdleCriticalThresholdSeconds = newCritical;

                    // Update service with new settings
                    _behaviorService.UpdateDetectionSettings(_detectionSettings);
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine("✓ Custom thresholds saved successfully!");
                    Console.ResetColor();
                }
                else
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("⚠️ Thresholds must be strictly increasing (Warning < Violation < Critical). Using previous defaults.");
                    Console.ResetColor();
                }
            }
        }

        private int GetThresholdInput(string prompt, int defaultVal)
        {
            Console.Write(prompt);
            var input = Console.ReadLine();
            return int.TryParse(input, out int result) && result > 0 ? result : defaultVal;
        }

        /// <summary>
        /// Triggers clipboard activity detection test
        /// Instructs user to copy/paste, captures real events
        /// </summary>
        public void TriggerClipboardTest()
        {
            Console.WriteLine("\n╔════════════════════════════════════════════════════╗");
            Console.WriteLine("║             CLIPBOARD ACTIVITY TEST                  ║");
            Console.WriteLine("╚════════════════════════════════════════════════════╝");
            Console.WriteLine("\nThis test monitors copy/paste clipboard activity.");
            Console.WriteLine("\n⚠️  INSTRUCTIONS:");
            Console.WriteLine("  1. Press Enter to start monitoring");
            Console.WriteLine("  2. Copy text from any application (Ctrl+C)");
            Console.WriteLine("  3. Paste text somewhere (Ctrl+V)");
            Console.WriteLine("  4. Repeat 3-4 times");
            Console.WriteLine("  5. Each clipboard access will be logged as a violation");
            Console.WriteLine("\nPress Enter to start...");
            Console.ReadLine();

            Console.Clear();
            Console.WriteLine("\n[MONITORING] Watching for clipboard activity...");
            Console.WriteLine("Copy and paste text, images, or files now!");
            Console.WriteLine("(Press Enter to stop monitoring)\n");

            // Clear previous events
            _behaviorService.ClearDetectedEvents();

            int accessCount = 0;
            bool monitoringActive = true;

            while (monitoringActive)
            {
                _behaviorService.UpdateActivity();

                // IMPORTANT: Actually call detection method to check for clipboard activity
                var clipboardEvent = _behaviorService.DetectClipboardActivity();
                if (clipboardEvent != null)
                {
                    accessCount++;
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine($"\n  ✓ Clipboard access detected! (#{accessCount})");
                    Console.ResetColor();

                    // Extract and display detailed info
                    ExtractAndDisplayClipboardInfo(clipboardEvent.Details);
                }

                // Check if user pressed Enter to stop monitoring
                if (Console.KeyAvailable)
                {
                    var key = Console.ReadKey(true); // Consume the key
                    if (key.Key == ConsoleKey.Enter)
                    {
                        monitoringActive = false;
                        Console.WriteLine("\n[STOPPED] Monitoring stopped");
                        break;
                    }
                }                Thread.Sleep(500);
            }

            // Check for clipboard activity
            var clipboardEvents = _behaviorService.GetClipboardEvents();
            if (clipboardEvents != null && clipboardEvents.Count > 0)
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine($"\n✓ Detected {clipboardEvents.Count} clipboard access events!");
                Console.ResetColor();

                foreach (var evt in clipboardEvents)
                {
                    ProcessDetectionEvent(evt, "CLIPBOARD_ACCESS");
                }
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine("\n⚠️  No clipboard activity detected.");
                Console.WriteLine("    Try copying and pasting text more frequently.");
                Console.ResetColor();
            }
        }

        /// <summary>
        /// Triggers idle detection test
        /// Monitors REAL system-wide activity (mouse movement, keyboard) via BehavioralMonitoringService
        /// Does NOT simulate activity with console input - lets real P/Invoke detection work
        /// </summary>
        public void TriggerIdleTest()
        {
            Console.WriteLine("\n╔════════════════════════════════════════════════════╗");
            Console.WriteLine("║   IDLE DETECTION SETUP & TEST                      ║");
            Console.WriteLine("╚════════════════════════════════════════════════════╝");

            // Allow user to set thresholds before starting
            ConfigureIdleThresholds();

            Console.WriteLine("\n⚠️  INSTRUCTIONS:");
            Console.WriteLine("   The system monitors your ACTUAL mouse and keyboard activity");
            Console.WriteLine("   You can type anywhere on your system (console, browser, notepad, etc)");
            Console.WriteLine("   Timer displays REAL idle time - not simulated");
            Console.WriteLine("   Activity from ANY application will reset the timer");
            Console.WriteLine("   Flags activate at different idle duration levels:");
            Console.WriteLine($"    - {_detectionSettings.IdleWarningThresholdSeconds}+ seconds:   ⚠️  WARNING LEVEL");
            Console.WriteLine($"    - {_detectionSettings.IdleViolationThresholdSeconds}+ seconds:  ❌ VIOLATION LEVEL");
            Console.WriteLine($"    - {_detectionSettings.IdleCriticalThresholdSeconds}+ seconds:  🚨 CRITICAL LEVEL");
            Console.WriteLine("   Press 'q' anytime to stop monitoring\n");

            Console.WriteLine("[IDLE DETECTION] Starting real system-wide monitoring...");
            Console.WriteLine("Press 'q' to quit (only 'q' is monitored here - other keys reset service timer)\n");
            Console.WriteLine("═══════════════════════════════════════════════════════");

            // Clear previous events
            _behaviorService.ClearDetectedEvents();

            bool isMonitoring = true;
            DateTime testStartTime = DateTime.UtcNow;

            char[] spinner = new char[] { '|', '/', '-', '\\' };
            int spinnerCounter = 0;
            int lastNotifiedSecond = 0;

            while (isMonitoring)
            {
                // Call DetectIdleActivity() which internally:
                // 1. Calls DetectMouseMovement() via P/Invoke GetCursorPos() (system-wide check)
                // 2. Compares idle time vs. threshold
                // 3. Returns event if threshold exceeded
                var idleEvent = _behaviorService.DetectIdleActivity();

                // Get current idle time measured by the service
                TimeSpan currentIdleTime = _behaviorService.IdleTime;

                // For display: calculate how long the test has been running
                TimeSpan testElapsed = DateTime.UtcNow - testStartTime;
                string idleDisplay = string.Format("{0:D2}:{1:D2}", currentIdleTime.Minutes, currentIdleTime.Seconds);

                // Use \r to overwrite the current line continuously for the animation
                Console.Write($"\r{spinner[spinnerCounter % 4]} [MONITORING] Idle: {idleDisplay} √ [MONITORING]");
                spinnerCounter++;

                int currentIdleSeconds = (int)currentIdleTime.TotalSeconds;

                // Backend detection - if service detected idle threshold crossed
                if (idleEvent != null)
                {
                    Console.WriteLine(); // Break the spinner line
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine($"  ⚠️ IDLE THRESHOLD CROSSED: {idleEvent.Details}");
                    Console.ResetColor();
                }

                // If timer resets, reset our notification memory
                if (currentIdleSeconds == 0)
                {
                    lastNotifiedSecond = 0;
                }
                // Check if we hit a 10s increment (10, 20, 30, 40, 50) of IDLE time
                else if (currentIdleSeconds > lastNotifiedSecond)
                {
                    if (currentIdleSeconds % 10 == 0 && currentIdleSeconds < 60)
                    {
                        Console.WriteLine(); // Break the spinner line
                        Console.ForegroundColor = ConsoleColor.Cyan;
                        Console.WriteLine($"  [INFO] User has been idle for {currentIdleSeconds} seconds. Move your mouse or type to reset.");
                        Console.ResetColor();
                    }
                    lastNotifiedSecond = currentIdleSeconds;
                }

                // IMPORTANT: Only check for 'q' to quit - don't interfere with service's real monitoring
                // Any other key the user presses will be detected by the service's real P/Invoke monitoring
                if (Console.KeyAvailable)
                {
                    var key = Console.ReadKey(true);
                    if (key.KeyChar == 'q' || key.KeyChar == 'Q')
                    {
                        isMonitoring = false;
                    }
                    // NOTE: Other keypresses are NOT handled here - they go to active application
                    // The service's real activity detection will pick them up via P/Invoke
                }

                // Sleep for 100ms to allow smooth spinner animation
                Thread.Sleep(100);
            }

            Console.WriteLine("\n\n[STOPPED] Monitoring stopped.");

            // Output final events tracked by the backend
            var idleEvents = _behaviorService.GetIdleEvents();
            if (idleEvents != null && idleEvents.Count > 0)
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine($"\n✓ Detected {idleEvents.Count} idle period events in the backend!");
                Console.ResetColor();

                foreach (var evt in idleEvents)
                {
                    ProcessDetectionEvent(evt, "IDLE_DETECTED");
                }
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine("\n⚠️  No idle events detected by the backend.");
                Console.WriteLine("    Try being completely idle for more than the threshold (currently 1 minute).");
                Console.WriteLine("    Any mouse movement or keyboard activity resets the idle timer.");
                Console.ResetColor();
            }
        }

        /// <summary>
        /// Triggers process detection test
        /// Instructs user to launch suspicious process, captures real event
        /// </summary>
        public void TriggerProcessTest()
        {
            Console.WriteLine("\n╔════════════════════════════════════════════════════╗");
            Console.WriteLine("║            PROCESS DETECTION TEST                    ║");
            Console.WriteLine("╚════════════════════════════════════════════════════╝");
            Console.WriteLine("\nThis test monitors for unauthorized processes.");
            Console.WriteLine("\nDetected Blacklisted Processes:");
            Console.WriteLine("  • Discord.exe");
            Console.WriteLine("  • Slack.exe");
            Console.WriteLine("  • TeamViewer.exe");
            Console.WriteLine("  • AnyDesk.exe");
            Console.WriteLine("  • And 20+ others...");
            Console.WriteLine("\n⚠️  INSTRUCTIONS:");
            Console.WriteLine("  1. Press Enter to start monitoring");
            Console.WriteLine("  2. Launch one of the detected processes (e.g., Discord, Slack)");
            Console.WriteLine("  3. System will detect the process in real-time");
            Console.WriteLine("  4. Event will be logged as aggressive violation");
            Console.WriteLine("\nPress Enter to start...");
            Console.ReadLine();

            Console.Clear();
            Console.WriteLine("\n[MONITORING] Watching for unauthorized processes...");
            Console.WriteLine("Launch a suspicious process now!");
            Console.WriteLine("\nMonitoring for 30 seconds...");

            // Clear previous events
            _behaviorService.ClearDetectedEvents();

            DateTime startTime = DateTime.UtcNow;

            while ((DateTime.UtcNow - startTime).TotalSeconds < 30)
            {
                _behaviorService.UpdateActivity();

                // IMPORTANT: Actually call detection method to check for blacklisted processes
                var processEvent = _behaviorService.DetectBlacklistedProcesses();
                if (processEvent != null)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine($"  ⚠️ SUSPICIOUS PROCESS DETECTED: {processEvent.Details}");
                    Console.ResetColor();
                }

                Thread.Sleep(1000);
            }

            // Check for process events
            var processEvents = _behaviorService.GetProcessDetectionEvents();
            if (processEvents != null && processEvents.Count > 0)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"\n⚠️ DETECTED {processEvents.Count} suspicious process(es)!");
                Console.ResetColor();

                foreach (var evt in processEvents)
                {
                    ProcessDetectionEvent(evt, "PROCESS_DETECTED");
                }
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine("\n⚠️  No suspicious processes detected.");
                Console.WriteLine("    No blacklisted applications were running during test.");
                Console.ResetColor();
            }
        }

        /// <summary>
        /// Processes a detection event through the entire pipeline
        /// Phase 6/7 → Phase 8 (Risk Assessment) → Phase 9 (Logging)
        /// </summary>
        private void ProcessDetectionEvent(MonitoringEvent evt, string label)
        {
            try
            {
                _eventHistory.Add(evt);

                // Display event
                Console.ForegroundColor = ConsoleColor.Cyan;
                Console.WriteLine($"[EVENT] {label}");
                Console.ResetColor();
                Console.WriteLine($"  Type: {evt.EventType}");
                Console.WriteLine($"  Violation: {evt.ViolationType} (Severity: {evt.SeverityScore}/3)");
                Console.WriteLine($"  Details: {evt.Details}");

                // Phase 8: Decision Engine Assessment
                var assessment = _decisionEngine.AssessEvent(evt);

                if (assessment != null)
                {
                    _assessmentHistory.Add(assessment);

                    // Display assessment
                    Console.ForegroundColor = GetRiskLevelColor(assessment.RiskLevel);
                    Console.WriteLine($"[ASSESSMENT] Risk: {assessment.RiskLevel} (Score: {assessment.RiskScore}/100)");
                    Console.ResetColor();
                    Console.WriteLine($"  Action: {assessment.RecommendedAction}");
                    Console.WriteLine($"  Rationale: {assessment.RationaleDescription}");

                    // Phase 9: Event Logger
                    _eventLogger.LogAssessment(assessment);
                    Console.WriteLine($"  → Logged for transmission (Pending: {_eventLogger.PendingAssessmentCount})");
                }

                Console.WriteLine();
            }
            catch (Exception ex)
            {
                Logger.Error($"Error processing event: {label}", ex);
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"ERROR: {ex.Message}");
                Console.ResetColor();
            }
        }

        /// <summary>
        /// Displays current system status
        /// </summary>
        public void DisplayStatus()
        {
            Console.WriteLine("\n=== SYSTEM STATUS ===\n");

            // Detection Pipeline Status
            Console.WriteLine("Detection Pipeline Status:");
            Console.WriteLine($"  Session ID: {_sessionId}");
            Console.WriteLine($"  Running: {_isRunning}");
            Console.WriteLine($"  Behavioral Monitoring: {(_behaviorService?.IsMonitoring ?? false ? "ACTIVE" : "INACTIVE")}");
            Console.WriteLine();

            // Event History
            Console.WriteLine("Event History:");
            Console.WriteLine($"  Total Events: {_eventHistory.Count}");
            Console.WriteLine($"  Total Assessments: {_assessmentHistory.Count}");
            Console.WriteLine();

            // Decision Engine Stats
            if (_decisionEngine != null)
            {
                var stats = _decisionEngine.GetSessionStatistics();
                Console.WriteLine("Decision Engine Statistics:");
                Console.WriteLine($"  Total Events Analyzed: {stats["total_events"]}");
                Console.WriteLine($"  Aggressive Violations: {stats["aggressive_violations"]}");
                Console.WriteLine($"  Passive Violations: {stats["passive_violations"]}");
                Console.WriteLine($"  Session Duration: {stats["session_duration_seconds"]}s");
                Console.WriteLine();
            }

            // Event Logger Stats
            if (_eventLogger != null)
            {
                var loggerStats = _eventLogger.GetStatistics();
                Console.WriteLine("Event Logger Statistics:");
                Console.WriteLine($"  Assessments Logged: {loggerStats["total_assessments_logged"]}");
                Console.WriteLine($"  Pending (Buffer): {loggerStats["pending_assessments"]}");
                Console.WriteLine($"  Batches Created: {loggerStats["total_batches_created"]}");
                Console.WriteLine($"  Batches Transmitted: {loggerStats["total_batches_transmitted"]}");
                Console.WriteLine($"  Failed Batches: {loggerStats["failed_batches_in_queue"]}");
                Console.WriteLine();
            }

            // SignalR Status
            Console.WriteLine("SignalR Connection Status:");
            if (_signalRService != null)
            {
                Console.ForegroundColor = _signalRService.IsConnected ? ConsoleColor.Green : ConsoleColor.Red;
                Console.WriteLine($"  Connected: {(_signalRService.IsConnected ? "YES" : "NO")}");
                Console.ResetColor();
                Console.WriteLine($"  Hub URL: {_hubUrl}");
            }
            Console.WriteLine();
        }

        /// <summary>
        /// Displays event history in detail
        /// </summary>
        public void DisplayEventHistory()
        {
            Console.WriteLine("\n=== EVENT HISTORY ===\n");

            if (_eventHistory.Count == 0)
            {
                Console.WriteLine("No events logged yet.");
                return;
            }

            // Philippine timezone (UTC+8)
            TimeZoneInfo phTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Singapore Standard Time"); // PH uses same offset

            for (int i = 0; i < _eventHistory.Count; i++)
            {
                var evt = _eventHistory[i];
                DateTime phTime = TimeZoneInfo.ConvertTime(evt.Timestamp, phTimeZone);
                Console.WriteLine($"{i + 1}. [{phTime:HH:mm:ss}] {evt.EventType}");
                Console.WriteLine($"   Violation: {evt.ViolationType} (Severity: {evt.SeverityScore})");
                Console.WriteLine($"   {evt.Details}");
                Console.WriteLine();
            }
        }

        /// <summary>
        /// Displays risk assessment history
        /// </summary>
        public void DisplayAssessmentHistory()
        {
            Console.WriteLine("\n=== RISK ASSESSMENT HISTORY ===\n");

            if (_assessmentHistory.Count == 0)
            {
                Console.WriteLine("No assessments generated yet.");
                return;
            }

            var safeCount = 0;
            var suspiciousCount = 0;
            var cheatingCount = 0;

            foreach (var assessment in _assessmentHistory)
            {
                var color = GetRiskLevelColor(assessment.RiskLevel);
                Console.ForegroundColor = color;
                Console.Write($"[{assessment.RiskLevel}]");
                Console.ResetColor();
                Console.WriteLine($" Score: {assessment.RiskScore:D3}/100 | {assessment.RationaleDescription}");

                switch (assessment.RiskLevel)
                {
                    case RiskLevel.Safe: safeCount++; break;
                    case RiskLevel.Suspicious: suspiciousCount++; break;
                    case RiskLevel.Cheating: cheatingCount++; break;
                }
            }

            Console.WriteLine();
            Console.WriteLine("Summary:");
            Console.WriteLine($"  Safe: {safeCount}");
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine($"  Suspicious: {suspiciousCount}");
            Console.ResetColor();
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"  Cheating: {cheatingCount}");
            Console.ResetColor();
        }

        /// <summary>
        /// Displays pending batches in event logger
        /// </summary>
        public void DisplayBatchStatus()
        {
            Console.WriteLine("\n=== BATCH STATUS ===\n");

            if (_eventLogger == null)
            {
                Console.WriteLine("Event Logger not initialized.");
                return;
            }

            var history = _eventLogger.GetBatchHistory();
            var failed = _eventLogger.GetFailedBatches();

            Console.WriteLine($"Total Batches Created: {history.Count}");
            Console.WriteLine($"Failed Batches (Retry Queue): {failed.Count}");
            Console.WriteLine();

            if (history.Count == 0)
            {
                Console.WriteLine("No batches created yet.");
                return;
            }

            Console.WriteLine("Batch Details:");
            for (int i = 0; i < history.Count; i++)
            {
                var batch = history[i];
                var color = batch.Status == "transmitted" ? ConsoleColor.Green : 
                           batch.Status == "failed" ? ConsoleColor.Red : ConsoleColor.Yellow;
                
                Console.ForegroundColor = color;
                Console.Write($"  [{batch.Status.ToUpper()}]");
                Console.ResetColor();
                Console.WriteLine($" Batch {i + 1}: {batch.Assessments.Count} assessments | MaxLevel: {batch.GetMaxRiskLevel()} | Attempts: {batch.TransmissionAttempts}");
            }
        }

        /// <summary>
        /// Triggers a manual test event
        /// </summary>
        public void TriggerManualEvent(string eventType)
        {
            var evt = new MonitoringEvent
            {
                EventType = eventType,
                ViolationType = ViolationType.Passive,
                SeverityScore = 2,
                Timestamp = DateTime.UtcNow,
                Details = $"Manual test event: {eventType}",
                SessionId = _sessionId
            };

            ProcessDetectionEvent(evt, $"Manual: {eventType}");
        }

        /// <summary>
        /// Flushes pending assessments and transmits batches
        /// </summary>
        public async Task FlushPendingAsync()
        {
            Console.WriteLine("\n=== FLUSHING PENDING BATCHES ===\n");

            if (_eventLogger == null)
            {
                Console.WriteLine("Event Logger not initialized.");
                return;
            }

            await _eventLogger.StopAsync();
            Console.WriteLine("All pending assessments flushed and batches transmitted.");

            // Restart logger for continued testing
            _eventLogger = new EventLoggerService(_signalRService, _sessionId);
            _eventLogger.Start();
        }

        /// <summary>
        /// Shuts down all services
        /// </summary>
        public async Task ShutdownAsync()
        {
            Console.WriteLine("\n=== SHUTTING DOWN ===\n");

            if (_eventLogger != null)
            {
                await _eventLogger.StopAsync();
            }

            if (_behaviorService != null)
            {
                _behaviorService.StopMonitoring();
            }

            if (_signalRService != null && _signalRService.IsConnected)
            {
                await _signalRService.DisconnectAsync();
            }

            _isRunning = false;
            Logger.Info("Test scenario runner shutdown complete");
        }

        /// <summary>
        /// Gets color for risk level
        /// </summary>
        private ConsoleColor GetRiskLevelColor(RiskLevel level)
        {
            return level switch
            {
                RiskLevel.Safe => ConsoleColor.Green,
                RiskLevel.Suspicious => ConsoleColor.Yellow,
                RiskLevel.Cheating => ConsoleColor.Red,
                _ => ConsoleColor.White
            };
        }

        /// <summary>
        /// Extracts app names from event details for display
        /// Parses "From: App1 → To: App2" format
        /// </summary>
        private string ExtractAppNameFromEvent(string details)
        {
            if (string.IsNullOrEmpty(details))
                return "Unknown";

            try
            {
                // Look for "From: " pattern
                if (details.Contains("From:"))
                {
                    var startIdx = details.IndexOf("From:") + 5;
                    var arrowIdx = details.IndexOf("→");
                    if (arrowIdx > startIdx)
                    {
                        var fromPart = details.Substring(startIdx, arrowIdx - startIdx).Trim();
                        var endIdx = details.IndexOf("To:");
                        if (endIdx > arrowIdx)
                        {
                            var toPart = details.Substring(endIdx + 3, details.Length - (endIdx + 3)).Split('\n')[0].Trim();
                            return $"{fromPart} → {toPart}";
                        }
                    }
                }
            }
            catch { }

            return "Window switch";
        }

        /// <summary>
        /// Extracts source application from clipboard event details
        /// Parses "Copy from: AppName" format
        /// </summary>
        private string ExtractSourceAppFromEvent(string details)
        {
            if (string.IsNullOrEmpty(details))
                return "Unknown";

            try
            {
                // Look for "Copy from: " pattern
                if (details.Contains("Copy from:"))
                {
                    var startIdx = details.IndexOf("Copy from:") + 10;
                    var endIdx = details.IndexOf("\n", startIdx);
                    if (endIdx == -1)
                        endIdx = details.Length;

                    var sourceApp = details.Substring(startIdx, endIdx - startIdx).Trim();
                    return sourceApp;
                }
            }
            catch { }

            return "Unknown Source";
        }

        /// <summary>
        /// Extracts and displays clipboard info in a formatted way
        /// Shows: Source App, Type, Length/Size information
        /// </summary>
        private void ExtractAndDisplayClipboardInfo(string details)
        {
            if (string.IsNullOrEmpty(details))
                return;

            try
            {
                string sourceApp = "Unknown";
                string clipboardType = "Unknown";

                // Extract source app
                if (details.Contains("Copy from:"))
                {
                    var startIdx = details.IndexOf("Copy from:") + 10;
                    var endIdx = details.IndexOf("\n", startIdx);
                    if (endIdx > startIdx)
                    {
                        sourceApp = details.Substring(startIdx, endIdx - startIdx).Trim();
                    }
                }

                // Extract clipboard type info
                if (details.Contains("Type:"))
                {
                    var startIdx = details.IndexOf("Type:") + 5;
                    var endIdx = details.IndexOf("\n", startIdx);
                    if (endIdx == -1)
                        endIdx = details.Length;
                    if (endIdx > startIdx)
                    {
                        clipboardType = details.Substring(startIdx, endIdx - startIdx).Trim();
                    }
                }

                // Display in formatted way
                Console.ForegroundColor = ConsoleColor.Cyan;
                Console.WriteLine($"    From:  {sourceApp}");
                Console.WriteLine($"    Type:  {clipboardType}");
                Console.ResetColor();
            }
            catch { }
        }

        public string SessionId => _sessionId;
        public bool IsRunning => _isRunning;

        /// <summary>
        /// Gets assessment history for verification in tests
        /// </summary>
        public List<RiskAssessment> GetAssessmentHistory()
        {
            return _assessmentHistory ?? new List<RiskAssessment>();
        }

        /// <summary>
        /// Gets batch history from event logger for verification in tests
        /// </summary>
        public List<EventBatch> GetBatchHistory()
        {
            return _eventLogger?.GetBatchHistory() ?? new List<EventBatch>();
        }

        /// <summary>
        /// Gets idle events from behavioral monitoring service
        /// </summary>
        public List<MonitoringEvent> GetIdleEvents()
        {
            return _behaviorService?.GetIdleEvents() ?? new List<MonitoringEvent>();
        }

        /// <summary>
        /// Clears all detected events in behavioral monitoring service
        /// Used for test isolation
        /// </summary>
        public void ClearDetectedEvents()
        {
            _behaviorService?.ClearDetectedEvents();
            _assessmentHistory.Clear();
        }
    }
}

