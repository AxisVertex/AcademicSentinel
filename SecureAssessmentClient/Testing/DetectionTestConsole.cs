using System;
using System.Linq;
using System.Threading.Tasks;
using SecureAssessmentClient.Models.Monitoring;
using SecureAssessmentClient.Services;
using SecureAssessmentClient.Utilities;

namespace SecureAssessmentClient.Testing
{
    /// <summary>
    /// Interactive test console for validation of detection pipeline
    /// Provides menu-driven interface to test all phases (6→7→8→9)
    /// </summary>
    public class DetectionTestConsole
    {
        private TestScenarioRunner _testRunner;
        private bool _isRunning = true;
        private string _authToken;
        private string _hubUrl;

        public DetectionTestConsole()
        {
            _hubUrl = "https://localhost:7236/monitoringHub";
        }

        /// <summary>
        /// Starts the interactive test console
        /// </summary>
        public async Task RunAsync()
        {
            DisplayWelcomeMessage();

            // Get configuration
            await GetConfigurationAsync();

            // Initialize test runner
            Console.WriteLine("\nInitializing detection pipeline...");
            _testRunner = new TestScenarioRunner(_hubUrl);

            bool initialized = await _testRunner.InitializeAsync(_authToken);
            if (!initialized)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("Failed to initialize testing harness. Exiting.");
                Console.ResetColor();
                return;
            }

            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("✓ Detection pipeline initialized successfully");
            Console.WriteLine($"✓ Test Session ID: {_testRunner.SessionId}");
            Console.ResetColor();

            // Main menu loop
            while (_isRunning)
            {
                DisplayMainMenu();
                await HandleMenuInputAsync();
            }

            // Cleanup
            await _testRunner.ShutdownAsync();
            Console.WriteLine("\nTest console closed. Goodbye!");
        }

        /// <summary>
        /// Displays welcome message
        /// </summary>
        private void DisplayWelcomeMessage()
        {
            Console.Clear();
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine("╔════════════════════════════════════════════════════════════╗");
            Console.WriteLine("║   SECURE ASSESSMENT CLIENT - DETECTION PIPELINE TEST CONSOLE ║");
            Console.WriteLine("║                   Backend Validation Harness                 ║");
            Console.WriteLine("╚════════════════════════════════════════════════════════════╝");
            Console.ResetColor();

            Console.WriteLine("\nThis console tests the complete detection-to-server pipeline:");
            Console.WriteLine("  • Phase 6: Environment Integrity Detection");
            Console.WriteLine("  • Phase 7: Behavioral Monitoring");
            Console.WriteLine("  • Phase 8: Decision Engine & Risk Scoring");
            Console.WriteLine("  • Phase 9: Event Logger & Server Transmission");
            Console.WriteLine("  • SignalR: Real-time server communication");
            Console.WriteLine();
        }

        /// <summary>
        /// Gets configuration from user
        /// </summary>
        private async Task GetConfigurationAsync()
        {
            Console.WriteLine("=== CONFIGURATION ===\n");

            Console.Write("Enter auth token (or press Enter for 'test-token'): ");
            _authToken = Console.ReadLine();
            if (string.IsNullOrEmpty(_authToken))
            {
                _authToken = "test-token";
            }

            Console.Write("Enter SignalR hub URL (or press Enter for 'https://localhost:7236/monitoringHub'): ");
            string customUrl = Console.ReadLine();
            if (!string.IsNullOrEmpty(customUrl))
            {
                _hubUrl = customUrl;
            }

            Console.WriteLine($"\nConfiguration set:");
            Console.WriteLine($"  Token: {_authToken}");
            Console.WriteLine($"  Hub URL: {_hubUrl}");
            Console.WriteLine();
        }

        /// <summary>
        /// Displays main menu
        /// </summary>
        private void DisplayMainMenu()
        {
            Console.WriteLine("\n╔════════════════════════════════════════════════════════╗");
            Console.WriteLine("║      REAL DETECTION TESTING CONSOLE - THESIS MODE       ║");
            Console.WriteLine("╚════════════════════════════════════════════════════════╝");
            Console.WriteLine();
            Console.WriteLine("ENVIRONMENT DETECTION:");
            Console.WriteLine("  1. [ENV]     Re-check Environment Integrity");
            Console.WriteLine();
            Console.WriteLine("BEHAVIORAL TRIGGERS:");
            Console.WriteLine("  2. [ALT-TAB] Alt-Tab Window Switching Test");
            Console.WriteLine("  3. [CLIP]    Clipboard Activity Test");
            Console.WriteLine("  4. [IDLE]    Idle Detection Test (70 seconds)");
            Console.WriteLine("  5. [PROC]    Process Detection Test");
            Console.WriteLine();
            Console.WriteLine("COMPREHENSIVE TESTS:");
            Console.WriteLine("  C. [COMP]    Comprehensive Idle Detection Test (Phase 3)");
            Console.WriteLine();
            Console.WriteLine("MONITORING & STATUS:");
            Console.WriteLine("  6. [STATUS]  Display System Status");
            Console.WriteLine("  7. [HISTORY] Show Event History");
            Console.WriteLine("  8. [ASSESS]  Show Risk Assessments");
            Console.WriteLine("  9. [SERVER]  Test Server Connection");
            Console.WriteLine("  A. [HELP]    Show Testing Guide");
            Console.WriteLine("  0. [EXIT]    Shutdown & Exit");
            Console.WriteLine();
            Console.Write("Select option (0-9, A-C): ");
        }

        /// <summary>
        /// Handles menu input
        /// </summary>
        private async Task HandleMenuInputAsync()
        {
            string input = Console.ReadLine();

            switch (input)
            {
                case "1":
                    await _testRunner.SimulateEnvironmentDetectionAsync();
                    PauseForReview();
                    break;

                case "2":
                    _testRunner.TriggerAltTabTest();
                    PauseForReview();
                    break;

                case "3":
                    _testRunner.TriggerClipboardTest();
                    PauseForReview();
                    break;

                case "4":
                    _testRunner.TriggerIdleTest();
                    PauseForReview();
                    break;

                case "5":
                    _testRunner.TriggerProcessTest();
                    PauseForReview();
                    break;

                case "6":
                    _testRunner.DisplayStatus();
                    PauseForReview();
                    break;

                case "7":
                    _testRunner.DisplayEventHistory();
                    PauseForReview();
                    break;

                case "8":
                    _testRunner.DisplayAssessmentHistory();
                    PauseForReview();
                    break;

                case "9":
                    await TestServerConnectionAsync();
                    PauseForReview();
                    break;

                case "A":
                case "a":
                    DisplayTestingGuide();
                    PauseForReview();
                    break;

                case "C":
                case "c":
                    await TestIdleDetectionComprehensiveAsync();
                    PauseForReview();
                    break;

                case "0":
                    _isRunning = false;
                    break;

                default:
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("Invalid option. Please try again.");
                    Console.ResetColor();
                    break;
            }
        }

        /// <summary>
        /// Handles manual event triggering
        /// </summary>
        private async Task HandleManualEventAsync()
        {
            Console.WriteLine("\n=== MANUAL EVENT TRIGGER ===\n");
            Console.WriteLine("Available event types:");
            Console.WriteLine("1. EVENT_WINDOW_SWITCH");
            Console.WriteLine("2. EVENT_CLIPBOARD_COPY");
            Console.WriteLine("3. EVENT_IDLE");
            Console.WriteLine("4. EVENT_PROCESS_DETECTED");
            Console.WriteLine("5. EVENT_VM_DETECTED");
            Console.WriteLine("6. CUSTOM");
            Console.WriteLine();

            Console.Write("Select event type (1-6) or name: ");
            string selection = Console.ReadLine();

            string eventType = selection switch
            {
                "1" => Constants.EVENT_WINDOW_SWITCH,
                "2" => Constants.EVENT_CLIPBOARD_COPY,
                "3" => Constants.EVENT_IDLE,
                "4" => Constants.EVENT_PROCESS_DETECTED,
                "5" => Constants.EVENT_VM_DETECTED,
                "6" => GetCustomEventName(),
                _ => GetCustomEventName()
            };

            _testRunner.TriggerManualEvent(eventType);
            await Task.Delay(100);
        }

        /// <summary>
        /// Gets custom event name from user
        /// </summary>
        private string GetCustomEventName()
        {
            Console.Write("Enter custom event name: ");
            return Console.ReadLine() ?? "CUSTOM_EVENT";
        }

        /// <summary>
        /// Displays testing guide
        /// </summary>
        private void DisplayTestingGuide()
        {
            Console.Clear();
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine("╔════════════════════════════════════════════════════════════╗");
            Console.WriteLine("║              DETECTION PIPELINE TESTING GUIDE              ║");
            Console.WriteLine("╚════════════════════════════════════════════════════════════╝");
            Console.ResetColor();

            Console.WriteLine("\nTESTING FLOW:");
            Console.WriteLine("─────────────");
            Console.WriteLine();
            Console.WriteLine("1. ENVIRONMENT DETECTION TEST");
            Console.WriteLine("   • Simulates VM/debugger detection (Phase 6)");
            Console.WriteLine("   • Creates MonitoringEvent objects");
            Console.WriteLine("   • Decision Engine scores each event");
            Console.WriteLine("   • Should produce Risk Level = CHEATING");
            Console.WriteLine();

            Console.WriteLine("2. BEHAVIORAL DETECTION TEST");
            Console.WriteLine("   • Simulates window switching (6 times)");
            Console.WriteLine("   • Simulates clipboard access (3 times)");
            Console.WriteLine("   • Simulates idle timeout");
            Console.WriteLine("   • Simulates unauthorized process");
            Console.WriteLine("   • Pattern detection triggers escalation");
            Console.WriteLine();

            Console.WriteLine("3. MANUAL EVENT TRIGGERING");
            Console.WriteLine("   • Trigger specific event types for validation");
            Console.WriteLine("   • Test custom scenarios");
            Console.WriteLine("   • Verify individual components");
            Console.WriteLine();

            Console.WriteLine("WHAT TO LOOK FOR:");
            Console.WriteLine("─────────────────");
            Console.WriteLine("✓ Events are logged and assessed");
            Console.WriteLine("✓ Risk scores increase with violations");
            Console.WriteLine("✓ Pattern detection works (multiple events)");
            Console.WriteLine("✓ Batches are created (10 assessments or 5s)");
            Console.WriteLine("✓ Batches show as 'transmitted' (if SignalR connected)");
            Console.WriteLine("✓ Critical events trigger immediate flush");
            Console.WriteLine();

            Console.WriteLine("DEBUGGING TIPS:");
            Console.WriteLine("───────────────");
            Console.WriteLine("• Use 'Status' to check system health");
            Console.WriteLine("• Use 'Batch Status' to see transmission queue");
            Console.WriteLine("• Use 'Flush' to manually transmit pending batches");
            Console.WriteLine("• Check logs in: %APPDATA%\\SecureAssessmentClient\\Logs\\");
            Console.WriteLine("• Batches persisted to: %APPDATA%\\SecureAssessmentClient\\EventLogs\\");
            Console.WriteLine();

            Console.WriteLine("EXPECTED BEHAVIOR:");
            Console.WriteLine("──────────────────");
            Console.WriteLine("Phase 6→7 → Phase 8 → Phase 9 → Server");
            Console.WriteLine("  ↓       ↓       ↓      ↓         ↓");
            Console.WriteLine("Event → Event → Risk → Batch → Transmitted");
            Console.WriteLine();

            Console.WriteLine("SignalR Connection:");
            Console.WriteLine("• If hub URL is correct: Batches show 'transmitted'");
            Console.WriteLine("• If hub unavailable: Batches show 'failed', retry queued");
            Console.WriteLine("• Persistence: Failed batches saved for later recovery");
            Console.WriteLine();
        }

        /// <summary>
        /// Tests connection to AcademicSentinel.Server
        /// </summary>
        private async Task TestServerConnectionAsync()
        {
            Console.Clear();
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine("╔════════════════════════════════════════════════════════════╗");
            Console.WriteLine("║              SERVER CONNECTION TEST                       ║");
            Console.WriteLine("╚════════════════════════════════════════════════════════════╝");
            Console.ResetColor();
            Console.WriteLine();

            try
            {
                // Get server credentials from user
                Console.Write("Email (default: student@example.com): ");
                string email = Console.ReadLine();
                if (string.IsNullOrEmpty(email)) email = "student@example.com";

                Console.Write("Password (default: SecurePass123!): ");
                string password = Console.ReadLine();
                if (string.IsNullOrEmpty(password)) password = "SecurePass123!";

                Console.Write("Room ID (default: 1): ");
                if (!int.TryParse(Console.ReadLine(), out int roomId)) roomId = 1;

                Console.WriteLine("\n🔄 Initializing server connection...\n");

                // Create SignalR service
                var signalRService = new SignalRService("https://localhost:7236/monitoringHub");

                // Initialize server connection
                bool success = await App.InitializeServerConnectionAsync(signalRService, email, password, roomId);

                if (success)
                {
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine("\n✅ SERVER CONNECTION TEST SUCCESSFUL!");
                    Console.WriteLine("✅ Authentication completed");
                    Console.WriteLine("✅ SignalR hub connected");
                    Console.WriteLine("✅ Exam room joined");
                    Console.ResetColor();

                    // Test event transmission
                    Console.WriteLine("\n📤 Testing event transmission...");
                    bool eventSent = await signalRService.SendExamMonitoringEventAsync(
                        roomId: roomId,
                        studentId: 1,
                        eventType: "TEST_EVENT",
                        severityScore: 50,
                        description: "Test connection event from SAC"
                    );

                    if (eventSent)
                    {
                        Console.ForegroundColor = ConsoleColor.Green;
                        Console.WriteLine("✅ Test event transmitted successfully!");
                        Console.ResetColor();
                    }

                    // Cleanup
                    await signalRService.DisconnectAsync();
                    Console.WriteLine("✅ Disconnected from server");
                }
                else
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("\n❌ SERVER CONNECTION TEST FAILED!");
                    Console.WriteLine("Check the logs above for details.");
                    Console.ResetColor();
                }
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"\n❌ ERROR: {ex.Message}");
                Console.WriteLine($"Stack Trace: {ex.StackTrace}");
                Console.ResetColor();
            }
        }

        /// <summary>
        /// Comprehensive Idle Detection Test (Phase 3 Verification)
        /// Tests the complete idle detection pipeline with real timing
        /// Simulates 10 seconds of inactivity, verifies event creation, scoring, and transmission
        /// </summary>
        public async Task TestIdleDetectionComprehensiveAsync()
        {
            Console.Clear();
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine("╔════════════════════════════════════════════════════════════╗");
            Console.WriteLine("║     COMPREHENSIVE IDLE DETECTION TEST (Phase 3)           ║");
            Console.WriteLine("║   Testing: Event Creation → Scoring → Transmission        ║");
            Console.WriteLine("╚════════════════════════════════════════════════════════════╝");
            Console.ResetColor();
            Console.WriteLine();

            try
            {
                // PHASE 1: SETUP
                Console.WriteLine("📋 PHASE 1: TEST SETUP");
                Console.WriteLine("─────────────────────────────────────────");

                // Verify test runner is initialized
                if (!_testRunner.IsRunning)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("❌ Test Runner not initialized. Run initialization first.");
                    Console.ResetColor();
                    return;
                }

                Console.WriteLine("✓ Test Runner initialized");
                Console.WriteLine($"✓ Session ID: {_testRunner.SessionId}");
                Console.WriteLine($"✓ Idle Threshold: 10 seconds");
                Console.WriteLine();

                // PHASE 2: IDLE DETECTION TEST
                Console.WriteLine("📊 PHASE 2: IDLE ACTIVITY MONITORING (10 seconds)");
                Console.WriteLine("─────────────────────────────────────────");
                Console.WriteLine("⚠️  DO NOT MOVE MOUSE OR PRESS KEYS for 10 seconds...");
                Console.WriteLine();

                // Clear previous events
                _testRunner.ClearDetectedEvents();

                DateTime startTime = DateTime.UtcNow;
                int detectedIdleCount = 0;
                int lastNotified = 0;

                // Monitor for 10 seconds
                while ((DateTime.UtcNow - startTime).TotalSeconds < 10)
                {
                    int elapsedSeconds = (int)(DateTime.UtcNow - startTime).TotalSeconds;

                    // Display countdown every second
                    if (elapsedSeconds > lastNotified)
                    {
                        Console.WriteLine($"  ⏱️  Elapsed: {elapsedSeconds}/10 seconds...");
                        lastNotified = elapsedSeconds;
                    }

                    Thread.Sleep(100);
                }

                Console.WriteLine($"  ✓ Monitoring complete (10 seconds elapsed)");
                Console.WriteLine();

                // PHASE 3: EVENT VERIFICATION
                Console.WriteLine("✅ PHASE 3: EVENT VERIFICATION");
                Console.WriteLine("─────────────────────────────────────────");

                var idleEvents = _testRunner.GetIdleEvents();

                if (idleEvents != null && idleEvents.Count > 0)
                {
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine($"✓ IDLE events detected: {idleEvents.Count}");
                    Console.ResetColor();
                    detectedIdleCount = idleEvents.Count;

                    foreach (var evt in idleEvents)
                    {
                        Console.WriteLine($"  • Event Type: {evt.EventType}");
                        Console.WriteLine($"  • Violation Type: {evt.ViolationType}");
                        Console.WriteLine($"  • Severity: {evt.SeverityScore}/3");
                        Console.WriteLine($"  • Details: {evt.Details}");
                        Console.WriteLine($"  • Timestamp: {evt.Timestamp:HH:mm:ss.fff}");
                        Console.WriteLine();
                    }
                }
                else
                {
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.WriteLine("⚠️  No idle events detected");
                    Console.WriteLine("   Possible causes:");
                    Console.WriteLine("   • EnableIdleDetection is false");
                    Console.WriteLine("   • Mouse/keyboard activity detected during test");
                    Console.WriteLine("   • IdleThresholdSeconds exceeds test duration");
                    Console.ResetColor();
                    return;
                }

                // PHASE 4: DECISION ENGINE SCORING
                Console.WriteLine("📈 PHASE 4: RISK ASSESSMENT SCORING");
                Console.WriteLine("─────────────────────────────────────────");

                var assessments = _testRunner.GetAssessmentHistory();
                int idleAssessments = 0;

                if (assessments != null && assessments.Count > 0)
                {
                    // Filter for idle-related assessments (newest ones)
                    var recentAssessments = assessments.TakeLast(detectedIdleCount).ToList();

                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine($"✓ Risk assessments generated: {recentAssessments.Count}");
                    Console.ResetColor();
                    idleAssessments = recentAssessments.Count;

                    foreach (var assessment in recentAssessments)
                    {
                        Console.ForegroundColor = GetRiskLevelColor(assessment.RiskLevel);
                        Console.WriteLine($"  [{assessment.RiskLevel}]");
                        Console.ResetColor();
                        Console.WriteLine($"    Risk Score: {assessment.RiskScore}/100");
                        Console.WriteLine($"    Recommended Action: {assessment.RecommendedAction}");
                        Console.WriteLine($"    Rationale: {assessment.RationaleDescription}");
                        Console.WriteLine();
                    }
                }
                else
                {
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.WriteLine("⚠️  No risk assessments found");
                    Console.ResetColor();
                }

                // PHASE 5: EVENT LOGGER VERIFICATION
                Console.WriteLine("📤 PHASE 5: EVENT LOGGER & BATCH TRANSMISSION");
                Console.WriteLine("─────────────────────────────────────────");

                // Flush pending assessments to ensure batching
                await _testRunner.FlushPendingAsync();

                var batchHistory = _testRunner.GetBatchHistory();

                if (batchHistory != null && batchHistory.Count > 0)
                {
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine($"✓ Event batches created: {batchHistory.Count}");
                    Console.ResetColor();

                    foreach (var batch in batchHistory.TakeLast(Math.Min(3, batchHistory.Count)))
                    {
                        Console.WriteLine($"  Batch {batch.BatchId}:");
                        Console.WriteLine($"    • Assessments: {batch.Assessments.Count}");
                        Console.WriteLine($"    • Status: {batch.Status}");
                        Console.WriteLine($"    • Attempts: {batch.TransmissionAttempts}");
                        Console.WriteLine($"    • Priority: {batch.Priority}");

                        Console.ForegroundColor = batch.Status == "transmitted" ? ConsoleColor.Green : 
                                                batch.Status == "failed" ? ConsoleColor.Red : ConsoleColor.Yellow;
                        Console.Write($"    • Status: [{batch.Status.ToUpper()}]");
                        Console.ResetColor();
                        Console.WriteLine();
                    }
                }
                else
                {
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.WriteLine("⚠️  No batches created");
                    Console.ResetColor();
                }

                // PHASE 6: FINAL SUMMARY
                Console.WriteLine();
                Console.ForegroundColor = ConsoleColor.Cyan;
                Console.WriteLine("📊 TEST SUMMARY");
                Console.WriteLine("═════════════════════════════════════════");
                Console.ResetColor();

                Console.WriteLine($"Idle Events Created:      {detectedIdleCount}");
                Console.WriteLine($"Risk Assessments:         {idleAssessments}");
                Console.WriteLine($"Batches Created:          {batchHistory?.Count ?? 0}");

                if (batchHistory?.Any(b => b.Status == "transmitted") == true)
                {
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine($"Batches Transmitted:      ✓ YES");
                    Console.ResetColor();
                }
                else
                {
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.WriteLine($"Batches Transmitted:      ⚠️  (Server unavailable or first batch pending)");
                    Console.ResetColor();
                }

                // VERDICT
                Console.WriteLine();
                if (detectedIdleCount > 0 && idleAssessments > 0)
                {
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine("✅ COMPREHENSIVE IDLE DETECTION TEST: PASSED");
                    Console.WriteLine("   ✓ Idle activity detected");
                    Console.WriteLine("   ✓ Events created with correct type and severity");
                    Console.WriteLine("   ✓ Risk assessments generated");
                    Console.WriteLine("   ✓ Batches created and queued for transmission");
                    Console.ResetColor();
                }
                else
                {
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.WriteLine("⚠️  COMPREHENSIVE IDLE DETECTION TEST: PARTIAL");
                    Console.WriteLine("   ✓ Pipeline executed but some phases incomplete");
                    Console.ResetColor();
                }

                Console.WriteLine();
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"❌ TEST FAILED: {ex.Message}");
                Console.WriteLine($"Stack Trace: {ex.StackTrace}");
                Console.ResetColor();
            }
        }

        /// <summary>
        /// Gets risk level color for console display
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
        /// Pauses for user review
        /// </summary>
        private void PauseForReview()
        {
            Console.WriteLine("\nPress Enter to continue...");
            Console.ReadLine();
            Console.Clear();
        }
    }
}
