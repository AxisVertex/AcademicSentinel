using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using SecureAssessmentClient.Models.Monitoring;
using SecureAssessmentClient.Utilities;

namespace SecureAssessmentClient.Services.DetectionService
{
    /// <summary>
    /// Monitors suspicious exam-time behavior to detect potential cheating
    /// Checks for: Window switching, clipboard activity, idle time, unauthorized process launches
    /// Runs continuously during exam and generates MonitoringEvents for violations
    /// </summary>
    public class BehavioralMonitoringService
    {
        [DllImport("user32.dll")]
        private static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll")]
        private static extern int GetWindowText(IntPtr hWnd, System.Text.StringBuilder text, int count);

        [DllImport("user32.dll")]
        private static extern bool OpenClipboard(IntPtr hWndNewOwner);

        [DllImport("user32.dll")]
        private static extern bool CloseClipboard();

        [DllImport("user32.dll")]
        private static extern IntPtr GetClipboardOwner();

        [DllImport("user32.dll")]
        private static extern uint GetClipboardSequenceNumber();

        [DllImport("user32.dll")]
        private static extern bool GetCursorPos(out System.Drawing.Point lpPoint);

        [DllImport("user32.dll")]
        private static extern bool IsClipboardFormatAvailable(uint format);

        [DllImport("user32.dll")]
        private static extern IntPtr GetClipboardData(uint uFormat);

        [DllImport("kernel32.dll")]
        private static extern int GlobalSize(IntPtr hMem);

        // P/Invoke for system-wide input detection (keyboard and mouse)
        [DllImport("user32.dll")]
        private static extern bool GetLastInputInfo(ref LASTINPUTINFO plii);

        [DllImport("kernel32.dll")]
        private static extern uint GetTickCount();

        // Structure for GetLastInputInfo - tracks last input time system-wide
        [System.Runtime.InteropServices.StructLayout(System.Runtime.InteropServices.LayoutKind.Sequential)]
        private struct LASTINPUTINFO
        {
            public uint cbSize;
            public uint dwTime;
        }

        // Clipboard format constants
        private const uint CF_TEXT = 1;
        private const uint CF_BITMAP = 2;
        private const uint CF_DIB = 8;            // Device-independent bitmap (images)
        private const uint CF_UNICODETEXT = 13;
        private const uint CF_HDROP = 15;         // File list
        private const uint CF_HTML = 0xD010;      // HTML format

        // Structure for mouse position
        [System.Runtime.InteropServices.StructLayout(System.Runtime.InteropServices.LayoutKind.Sequential)]
        private struct Point
        {
            public int X;
            public int Y;
        }

        private IntPtr _lastForegroundWindow;
        private string _previousWindowName = "Unknown";
        private int _windowSwitchCount;
        private DateTime _lastActivityTime;
        private DateTime _lastClipboardCheckTime;
        private uint _lastClipboardSequenceNumber;
        private System.Drawing.Point _lastMousePosition;
        private bool _isMonitoring;
        private DetectionSettings _detectionSettings;
        private string _sessionId;

        // Tracks highest idle risk level reported to avoid spamming
        private int _lastReportedIdleLevel = 0; // 0 = none, 1 = warning, 2 = violation, 3 = critical

        // Event tracking for testing harness
        private List<MonitoringEvent> _detectedWindowSwitches = new List<MonitoringEvent>();
        private List<MonitoringEvent> _detectedClipboardAccess = new List<MonitoringEvent>();
        private List<MonitoringEvent> _detectedIdleEvents = new List<MonitoringEvent>();
        private List<MonitoringEvent> _detectedProcesses = new List<MonitoringEvent>();

        // Common applications mapping for friendly display
        private Dictionary<string, string> _applicationNameMap = new Dictionary<string, string>(System.StringComparer.OrdinalIgnoreCase)
        {
            // Office Applications
            { "notepad", "Notepad" },
            { "notepad++", "Notepad++" },
            { "winword", "Microsoft Word" },
            { "excel", "Microsoft Excel" },
            { "powerpnt", "PowerPoint" },
            { "msaccess", "Microsoft Access" },
            { "mspub", "Microsoft Publisher" },
            { "onenote", "OneNote" },

            // Communication Apps
            { "discord", "Discord" },
            { "skype", "Skype" },
            { "telegram", "Telegram" },
            { "slack", "Slack" },
            { "whatsapp", "WhatsApp" },
            { "teams", "Microsoft Teams" },
            { "zoom", "Zoom" },

            // Browsers
            { "chrome", "Google Chrome" },
            { "firefox", "Mozilla Firefox" },
            { "msedge", "Microsoft Edge" },
            { "opera", "Opera Browser" },
            { "iexplore", "Internet Explorer" },

            // PDF & Document Viewers
            { "adobereader", "Adobe Reader" },
            { "sumatra", "SumatraPDF" },
            { "foxitreader", "Foxit Reader" },

            // Development IDEs
            { "devenv", "Visual Studio" },
            { "code", "Visual Studio Code" },
            { "rider", "JetBrains Rider" },
            { "idea", "IntelliJ IDEA" },
            { "pycharm", "PyCharm" },

            // Media & Image Viewers
            { "vlc", "VLC Media Player" },
            { "foobar2000", "foobar2000" },
            { "mpc-hc", "Media Player Classic" },
            { "photoshop", "Adobe Photoshop" },
            { "paint", "Paint" },
            { "gimp", "GIMP" },

            // System Applications
            { "explorer", "Windows Explorer" },
            { "cmd", "Command Prompt" },
            { "powershell", "PowerShell" },
            { "taskmgr", "Task Manager" },
            { "mstsc", "Remote Desktop" },

            // Debugging & Development Tools (Blacklisted)
            { "windbg", "WinDbg" },
            { "x64dbg", "x64dbg" },
            { "ollydbg", "OllyDbg" },
            { "ida", "IDA Pro" },
            { "dnspy", "dnSpy" },
            { "fiddler", "Fiddler" },
            { "cheatengine", "Cheat Engine" },

            // Remote Access (Blacklisted)
            { "teamviewer", "TeamViewer" },
            { "anydesk", "AnyDesk" },
            { "ultravnc", "UltraVNC" },
            { "gotomypc", "GoToMyPC" },

            // Screen Capture (Blacklisted)
            { "obs", "OBS Studio" },
            { "ffmpeg", "FFmpeg" },
            { "camtasia", "Camtasia" },
            { "snagit", "SnagIt" },
            { "bandicam", "Bandicam" }
        };

        // Configurable blacklist of processes to detect
        private List<string> _blacklistedProcesses = new List<string>
        {
            // Debugging tools
            "windbg", "x64dbg", "ollydbg", "ida", "dnspy", "fiddler", "cheatengine",
            "processmonitor", "procexp",
            
            // Remote access
            "teamviewer", "anydesk", "ultravnc", "gotomypc",
            
            // Communication apps
            "discord", "telegram", "slack", "whatsapp", "skype", "teams", "zoom",
            
            // Screen capture
            "obs", "ffmpeg", "camtasia", "snagit", "bandicam",
            
            // Browsers (when not authorized)
            "chrome", "firefox", "opera"
        };

        public BehavioralMonitoringService(DetectionSettings detectionSettings, string sessionId)
        {
            _detectionSettings = detectionSettings ?? new DetectionSettings();
            _sessionId = sessionId;
            _lastActivityTime = DateTime.UtcNow;
            _lastClipboardCheckTime = DateTime.UtcNow;
            _windowSwitchCount = 0;
            _isMonitoring = false;
            _lastForegroundWindow = GetForegroundWindow();
            _lastClipboardSequenceNumber = GetClipboardSequenceNumber();

            // Initialize mouse position tracking
            GetCursorPos(out _lastMousePosition);
        }

        /// <summary>
        /// Starts continuous behavioral monitoring
        /// Should be called when exam session begins
        /// </summary>
        public void StartMonitoring()
        {
            if (!_isMonitoring)
            {
                _isMonitoring = true;
                _lastActivityTime = DateTime.UtcNow;
                _windowSwitchCount = 0;
                Logger.Info("Behavioral monitoring started");
            }
        }

        /// <summary>
        /// Stops behavioral monitoring
        /// Should be called when exam session ends
        /// </summary>
        public void StopMonitoring()
        {
            _isMonitoring = false;
            Logger.Info("Behavioral monitoring stopped");
        }

        /// <summary>
        /// Updates activity timestamp (call on keyboard/mouse events)
        /// Resets idle detection timer
        /// </summary>
        public void UpdateActivity()
        {
            _lastActivityTime = DateTime.UtcNow;
            _lastReportedIdleLevel = 0; // Reset idle tracking
        }

        /// <summary>
        /// Checks if mouse has moved and updates activity timestamp
        /// Used by idle detection to reset timer on mouse movement
        /// </summary>
        private bool DetectMouseMovement()
        {
            try
            {
                System.Drawing.Point currentMousePosition;
                GetCursorPos(out currentMousePosition);

                // Check if mouse position changed
                if (currentMousePosition.X != _lastMousePosition.X || currentMousePosition.Y != _lastMousePosition.Y)
                {
                    _lastMousePosition = currentMousePosition;
                    _lastActivityTime = DateTime.UtcNow;
                    _lastReportedIdleLevel = 0; // Reset idle tracking
                    return true;
                }

                return false;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Checks for keyboard/mouse activity via GetLastInputInfo
        /// Detects ALL system-wide input (keyboard AND mouse combined)
        /// This is more reliable than checking individual key states
        /// </summary>
        private bool DetectKeyboardActivity()
        {
            try
            {
                LASTINPUTINFO lastInputInfo = new LASTINPUTINFO();
                lastInputInfo.cbSize = (uint)System.Runtime.InteropServices.Marshal.SizeOf(lastInputInfo);

                // GetLastInputInfo returns the tick count of the last input event
                if (!GetLastInputInfo(ref lastInputInfo))
                {
                    return false;
                }

                // Get current system tick count
                uint currentTickCount = GetTickCount();

                // Get time elapsed since last input (in milliseconds)
                // This handles tick count overflow (wraps around every ~49 days)
                uint timeSinceLastInput = currentTickCount - lastInputInfo.dwTime;

                // If last input was very recent (within 1 second), there's activity
                const uint RECENT_INPUT_THRESHOLD = 1000; // milliseconds

                if (timeSinceLastInput < RECENT_INPUT_THRESHOLD)
                {
                    // Activity detected - update our tracking time
                    _lastActivityTime = DateTime.UtcNow;
                    _lastReportedIdleLevel = 0; // Reset idle tracking
                    return true;
                }

                return false;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Detects window focus changes (Alt+Tab switching)
        /// Returns MonitoringEvent if suspicious switching detected
        /// </summary>
        public MonitoringEvent DetectWindowFocus()
        {
            if (!_detectionSettings.EnableFocusDetection || !_isMonitoring)
            {
                return null;
            }

            try
            {
                IntPtr currentForegroundWindow = GetForegroundWindow();

                // Check if window has changed
                if (currentForegroundWindow != _lastForegroundWindow)
                {
                    _lastForegroundWindow = currentForegroundWindow;
                    _windowSwitchCount++;

                    // Get window title for identification
                    System.Text.StringBuilder windowTitle = new System.Text.StringBuilder(256);
                    GetWindowText(currentForegroundWindow, windowTitle, 256);
                    string windowName = windowTitle.ToString();

                    // Clean up window name: extract app name from full path/title
                    string cleanWindowName = ExtractApplicationName(windowName);

                    // Get friendly name if available in mapping
                    string friendlyCurrentApp = GetFriendlyApplicationName(cleanWindowName);

                    // Track previous window name for display
                    string fromApp = _previousWindowName;
                    string toApp = friendlyCurrentApp;
                    _previousWindowName = friendlyCurrentApp;

                    Logger.Warn($"Window switch detected: {fromApp} → {toApp} (Total switches: {_windowSwitchCount})");

                    // Determine severity based on switch frequency
                    int severity = 1;
                    if (_windowSwitchCount > 10)
                        severity = 3;
                    else if (_windowSwitchCount > 5)
                        severity = 2;

                    // Only generate event if excessive switching
                    if (_windowSwitchCount > 5)
                    {
                        var evt = new MonitoringEvent
                        {
                            EventType = Constants.EVENT_WINDOW_SWITCH,
                            ViolationType = ViolationType.Passive,
                            SeverityScore = severity,
                            Timestamp = DateTime.UtcNow,
                            Details = $"From: {fromApp} → To: {toApp}\nExcessive window switching detected ({_windowSwitchCount} switches)",
                            SessionId = _sessionId
                        };
                        TrackDetectedEvent(evt);
                        return evt;
                    }
                }

                return null;
            }
            catch (Exception ex)
            {
                Logger.Error("Error detecting window focus", ex);
                return null;
            }
        }

        /// <summary>
        /// Gets friendly application name from mapping dictionary
        /// Returns mapped name if found, otherwise returns the provided name as-is
        /// </summary>
        private string GetFriendlyApplicationName(string appName)
        {
            if (string.IsNullOrWhiteSpace(appName))
                return "Unknown Application";

            // Clean up the app name first (remove .exe, lowercase for lookup)
            string cleanedName = appName.Replace(".exe", "").Replace(".EXE", "").ToLower().Trim();

            // Try to find friendly name in mapping
            if (_applicationNameMap.TryGetValue(cleanedName, out var friendlyName))
            {
                return friendlyName;
            }

            // If not found, return original (already cleaned)
            return appName.Trim();
        }

        /// <summary>
        /// Identifies which application is the clipboard source (which app copied)
        /// Returns application name that currently has focus (likely the source)
        /// </summary>
        private string GetClipboardSourceApplication()
        {
            try
            {
                IntPtr currentForegroundWindow = GetForegroundWindow();
                System.Text.StringBuilder windowTitle = new System.Text.StringBuilder(256);
                GetWindowText(currentForegroundWindow, windowTitle, 256);
                string windowName = windowTitle.ToString();

                string cleanWindowName = ExtractApplicationName(windowName);
                return GetFriendlyApplicationName(cleanWindowName);
            }
            catch
            {
                return "Unknown Source";
            }
        }

        /// <summary>
        /// Detects clipboard content type (Text, Image, Files, etc.)
        /// Returns information about what was copied
        /// </summary>
        private string GetClipboardTypeInfo()
        {
            try
            {
                // Must open clipboard before accessing data
                if (!OpenClipboard(IntPtr.Zero))
                {
                    return "Unknown Content";
                }

                try
                {
                    // Check for Unicode text (most common)
                    if (IsClipboardFormatAvailable(CF_UNICODETEXT))
                    {
                        try
                        {
                            IntPtr hClipboardData = GetClipboardData(CF_UNICODETEXT);
                            if (hClipboardData != IntPtr.Zero)
                            {
                                int size = GlobalSize(hClipboardData);
                                if (size > 0)
                                {
                                    // Each character is 2 bytes in Unicode, minus null terminator
                                    int charCount = Math.Max(0, (size / 2) - 1);
                                    return $"Text | {charCount} characters";
                                }
                            }
                        }
                        catch { }
                        return "Text";
                    }

                    // Check for standard text (ANSI)
                    if (IsClipboardFormatAvailable(CF_TEXT))
                    {
                        try
                        {
                            IntPtr hClipboardData = GetClipboardData(CF_TEXT);
                            if (hClipboardData != IntPtr.Zero)
                            {
                                int size = GlobalSize(hClipboardData);
                                if (size > 0)
                                {
                                    int charCount = Math.Max(0, size - 1);
                                    return $"Text | {charCount} characters";
                                }
                            }
                        }
                        catch { }
                        return "Text";
                    }

                    // Check for image (DIB format - Device-Independent Bitmap)
                    if (IsClipboardFormatAvailable(CF_DIB))
                    {
                        try
                        {
                            IntPtr hClipboardData = GetClipboardData(CF_DIB);
                            if (hClipboardData != IntPtr.Zero)
                            {
                                int size = GlobalSize(hClipboardData);
                                if (size > 0)
                                {
                                    // DIB header is 40 bytes, can extract dimensions
                                    return "Image (Bitmap)";
                                }
                            }
                        }
                        catch { }
                        return "Image";
                    }

                    // Check for standard bitmap
                    if (IsClipboardFormatAvailable(CF_BITMAP))
                    {
                        return "Image (Bitmap)";
                    }

                    // Check for files
                    if (IsClipboardFormatAvailable(CF_HDROP))
                    {
                        try
                        {
                            IntPtr hClipboardData = GetClipboardData(CF_HDROP);
                            if (hClipboardData != IntPtr.Zero)
                            {
                                return "Files";
                            }
                        }
                        catch { }
                        return "Files";
                    }

                    // Check for HTML
                    if (IsClipboardFormatAvailable(CF_HTML))
                    {
                        return "HTML Content";
                    }

                    return "Mixed/Unknown Content";
                }
                finally
                {
                    // Always close clipboard
                    CloseClipboard();
                }
            }
            catch
            {
                return "Unknown Content";
            }
        }

        /// <summary>
        /// Extracts clean application name from window title
        /// Handles file paths, executable names, and browser tabs
        /// </summary>
        private string ExtractApplicationName(string windowTitle)
        {
            if (string.IsNullOrWhiteSpace(windowTitle))
                return "Unknown Application";

            // If it contains file path separators, extract just the filename
            if (windowTitle.Contains("\\"))
            {
                try
                {
                    string[] parts = windowTitle.Split('\\');
                    string filename = parts[parts.Length - 1];
                    // Remove .exe extension
                    filename = filename.Replace(".exe", "").Trim();
                    return filename;
                }
                catch { }
            }

            // If it contains common browser/IDE indicators, clean them up
            if (windowTitle.Contains(" - "))
            {
                // Extract just the app part (before the dash)
                string[] parts = windowTitle.Split(new[] { " - " }, System.StringSplitOptions.None);
                if (parts.Length > 0)
                    return parts[parts.Length - 1].Trim(); // Last part is usually the app name
            }

            // Return as-is if already clean
            return windowTitle.Trim();
        }

        /// <summary>
        /// Detects clipboard activity (copy/paste operations)
        /// Returns MonitoringEvent if clipboard content changed
        /// </summary>
        public MonitoringEvent DetectClipboardActivity()
        {
            if (!_detectionSettings.EnableClipboardMonitoring || !_isMonitoring)
            {
                return null;
            }

            // Only check clipboard periodically (every 500ms) to avoid performance impact
            if ((DateTime.UtcNow - _lastClipboardCheckTime).TotalMilliseconds < 500)
            {
                return null;
            }

            _lastClipboardCheckTime = DateTime.UtcNow;

            try
            {
                uint currentSequenceNumber = GetClipboardSequenceNumber();

                // Check if clipboard content has changed (sequence number increments)
                if (currentSequenceNumber != _lastClipboardSequenceNumber)
                {
                    _lastClipboardSequenceNumber = currentSequenceNumber;

                    // Get the source application that likely performed the copy
                    string sourceApp = GetClipboardSourceApplication();

                    // Get clipboard type and info
                    string clipboardTypeInfo = GetClipboardTypeInfo();

                    Logger.Warn($"Clipboard activity detected: {clipboardTypeInfo} from {sourceApp}");

                    var evt = new MonitoringEvent
                    {
                        EventType = Constants.EVENT_CLIPBOARD_COPY,
                        ViolationType = ViolationType.Passive,
                        SeverityScore = 2,
                        Timestamp = DateTime.UtcNow,
                        Details = $"Copy from: {sourceApp}\nType: {clipboardTypeInfo}\nClipboard activity detected: content copied/accessed",
                        SessionId = _sessionId
                    };
                    TrackDetectedEvent(evt);
                    return evt;
                }

                return null;
            }
            catch (Exception ex)
            {
                Logger.Error("Error detecting clipboard activity", ex);
                return null;
            }
        }

        /// <summary>
        /// Detects idle time (no keyboard/mouse activity)
        /// Returns MonitoringEvent if idle time exceeds threshold
        /// </summary>
        public MonitoringEvent DetectIdleActivity()
        {
            if (!_detectionSettings.EnableIdleDetection || !_isMonitoring)
            {
                return null;
            }

            try
            {
                // Check for BOTH mouse movement AND keyboard activity
                // GetLastInputInfo is more reliable as it tracks ALL system input
                DetectMouseMovement();        // Backup check for mouse position
                DetectKeyboardActivity();     // Primary check for any input (keyboard or mouse)

                // Setup thresholds with backward compatibility
                int warningThreshold = _detectionSettings.IdleWarningThresholdSeconds > 0 
                    ? _detectionSettings.IdleWarningThresholdSeconds : 30; 
                int violationThreshold = _detectionSettings.IdleViolationThresholdSeconds > 0 
                    ? _detectionSettings.IdleViolationThresholdSeconds : 120; 
                int criticalThreshold = _detectionSettings.IdleCriticalThresholdSeconds > 0 
                    ? _detectionSettings.IdleCriticalThresholdSeconds : 300; 

                // Fallback for single old 'IdleThresholdSeconds' if new ones aren't set
                if (_detectionSettings.IdleThresholdSeconds > 0 && _detectionSettings.IdleViolationThresholdSeconds == 0)
                {
                    violationThreshold = _detectionSettings.IdleThresholdSeconds;
                    warningThreshold = violationThreshold / 2;
                    criticalThreshold = violationThreshold * 2;
                }

                TimeSpan idleTime = DateTime.UtcNow - _lastActivityTime;

                // 3: Critical Level
                if (idleTime.TotalSeconds > criticalThreshold && _lastReportedIdleLevel < 3)
                {
                    _lastReportedIdleLevel = 3;
                    Logger.Warn($"Critical idle activity registered: {idleTime.TotalSeconds} seconds of inactivity");

                    var evt = new MonitoringEvent
                    {
                        EventType = Constants.EVENT_IDLE,
                        ViolationType = ViolationType.Aggressive,
                        SeverityScore = 3,
                        Timestamp = DateTime.UtcNow,
                        Details = $"CRITICAL IDLE: Student idle for {(int)idleTime.TotalSeconds} seconds (threshold: {criticalThreshold}s)",
                        SessionId = _sessionId
                    };
                    TrackDetectedEvent(evt);
                    return evt;
                }
                // 2: Violation Level
                else if (idleTime.TotalSeconds > violationThreshold && _lastReportedIdleLevel < 2)
                {
                    _lastReportedIdleLevel = 2;
                    Logger.Warn($"Violation idle activity registered: {idleTime.TotalSeconds} seconds of inactivity");

                    var evt = new MonitoringEvent
                    {
                        EventType = Constants.EVENT_IDLE,
                        ViolationType = ViolationType.Passive,
                        SeverityScore = 2,
                        Timestamp = DateTime.UtcNow,
                        Details = $"VIOLATION IDLE: Student idle for {(int)idleTime.TotalSeconds} seconds (threshold: {violationThreshold}s)",
                        SessionId = _sessionId
                    };
                    TrackDetectedEvent(evt);
                    return evt;
                }
                // 1: Warning Level
                else if (idleTime.TotalSeconds > warningThreshold && _lastReportedIdleLevel < 1)
                {
                    _lastReportedIdleLevel = 1;
                    Logger.Warn($"Warning idle activity registered: {idleTime.TotalSeconds} seconds of inactivity");

                    var evt = new MonitoringEvent
                    {
                        EventType = Constants.EVENT_IDLE,
                        ViolationType = ViolationType.Passive, // Only Passive/Aggressive exist
                        SeverityScore = 1,
                        Timestamp = DateTime.UtcNow,
                        Details = $"WARNING IDLE: Student idle for {(int)idleTime.TotalSeconds} seconds (threshold: {warningThreshold}s)",
                        SessionId = _sessionId
                    };
                    TrackDetectedEvent(evt);
                    return evt;
                }

                return null;
            }
            catch (Exception ex)
            {
                Logger.Error("Error detecting idle activity", ex);
                return null;
            }
        }

        /// <summary>
        /// Detects blacklisted/unauthorized processes running
        /// Returns MonitoringEvent if suspicious process detected
        /// </summary>
        public MonitoringEvent DetectBlacklistedProcesses()
        {
            if (!_detectionSettings.EnableProcessDetection || !_isMonitoring)
            {
                return null;
            }

            try
            {
                foreach (var processName in _blacklistedProcesses)
                {
                    try
                    {
                        var processes = Process.GetProcessesByName(processName);
                        if (processes.Length > 0)
                        {
                            // Additional check: exclude processes running from System32 (may be legitimate)
                            foreach (var process in processes)
                            {
                                try
                                {
                                    string processPath = process.MainModule?.FileName ?? "";

                                    // Flag if running outside system directories
                                    if (!processPath.Contains("System32") && !processPath.Contains("SysWOW64"))
                                    {
                                        // Get friendly name for the process
                                        string friendlyName = GetFriendlyApplicationName(processName);

                                        Logger.Error($"Blacklisted process detected: {friendlyName} at {processPath}");

                                        var evt = new MonitoringEvent
                                        {
                                            EventType = Constants.EVENT_PROCESS_DETECTED,
                                            ViolationType = ViolationType.Aggressive,
                                            SeverityScore = 3,
                                            Timestamp = DateTime.UtcNow,
                                            Details = $"Unauthorized process detected: {friendlyName}",
                                            SessionId = _sessionId
                                        };
                                        TrackDetectedEvent(evt);
                                        return evt;
                                    }
                                }
                                catch
                                {
                                    // Ignore access denied errors for specific processes
                                }
                            }
                        }
                    }
                    catch
                    {
                        // Ignore permission errors per process
                    }
                }

                return null;
            }
            catch (Exception ex)
            {
                Logger.Error("Error detecting blacklisted processes", ex);
                return null;
            }
        }

        /// <summary>
        /// Performs comprehensive behavioral check
        /// Checks all enabled detections in sequence
        /// Returns first detected violation or null
        /// </summary>
        public MonitoringEvent PerformBehavioralCheck()
        {
            if (!_isMonitoring)
            {
                return null;
            }

            try
            {
                // Check in priority order (most severe first)
                var processEvent = DetectBlacklistedProcesses();
                if (processEvent != null)
                    return processEvent;

                var focusEvent = DetectWindowFocus();
                if (focusEvent != null)
                    return focusEvent;

                var clipboardEvent = DetectClipboardActivity();
                if (clipboardEvent != null)
                    return clipboardEvent;

                var idleEvent = DetectIdleActivity();
                if (idleEvent != null)
                    return idleEvent;

                return null;
            }
            catch (Exception ex)
            {
                Logger.Error("Error during behavioral check", ex);
                return null;
            }
        }

        /// <summary>
        /// Resets window switch counter
        /// Call periodically (e.g., every minute) to measure switching rate
        /// </summary>
        public void ResetWindowSwitchCounter()
        {
            _windowSwitchCount = 0;
        }

        /// <summary>
        /// Updates detection settings at runtime
        /// Allows enabling/disabling specific checks during exam
        /// </summary>
        public void UpdateDetectionSettings(DetectionSettings newSettings)
        {
            if (newSettings != null)
            {
                _detectionSettings = newSettings;
                Logger.Info("Detection settings updated");
            }
        }

        /// <summary>
        /// Updates the blacklist of processes to detect
        /// Allows server-side configuration of blocked apps
        /// </summary>
        public void UpdateBlacklistedProcesses(List<string> processes)
        {
            if (processes != null && processes.Count > 0)
            {
                _blacklistedProcesses = processes;
                Logger.Info($"Blacklisted processes updated: {processes.Count} items");
            }
        }

        /// <summary>
        /// Gets current monitoring state
        /// </summary>
        public bool IsMonitoring
        {
            get { return _isMonitoring; }
        }

        /// <summary>
        /// Gets current window switch count
        /// </summary>
        public int WindowSwitchCount
        {
            get { return _windowSwitchCount; }
        }

        /// <summary>
        /// Gets time elapsed since last user activity
        /// </summary>
        public TimeSpan IdleTime
        {
            get { return DateTime.UtcNow - _lastActivityTime; }
        }

        /// <summary>
        /// Gets detection settings currently in use
        /// </summary>
        public DetectionSettings CurrentDetectionSettings
        {
            get { return _detectionSettings; }
        }

        /// <summary>
        /// Gets all detected window switch events since monitoring started
        /// Used by testing harness to retrieve real detection results
        /// </summary>
        public List<MonitoringEvent> GetWindowSwitchEvents()
        {
            return _detectedWindowSwitches ?? new List<MonitoringEvent>();
        }

        /// <summary>
        /// Gets all detected clipboard access events since monitoring started
        /// Used by testing harness to retrieve real detection results
        /// </summary>
        public List<MonitoringEvent> GetClipboardEvents()
        {
            return _detectedClipboardAccess ?? new List<MonitoringEvent>();
        }

        /// <summary>
        /// Gets all detected idle events since monitoring started
        /// Used by testing harness to retrieve real detection results
        /// </summary>
        public List<MonitoringEvent> GetIdleEvents()
        {
            return _detectedIdleEvents ?? new List<MonitoringEvent>();
        }

        /// <summary>
        /// Gets all detected suspicious process events since monitoring started
        /// Used by testing harness to retrieve real detection results
        /// </summary>
        public List<MonitoringEvent> GetProcessDetectionEvents()
        {
            return _detectedProcesses ?? new List<MonitoringEvent>();
        }

        /// <summary>
        /// Clears all tracked events
        /// Used by testing harness to reset between tests
        /// </summary>
        public void ClearDetectedEvents()
        {
            _detectedWindowSwitches.Clear();
            _detectedClipboardAccess.Clear();
            _detectedIdleEvents.Clear();
            _detectedProcesses.Clear();
        }

        /// <summary>
        /// Adds event to tracking history based on event type
        /// Called internally when events are detected
        /// </summary>
        private void TrackDetectedEvent(MonitoringEvent evt)
        {
            if (evt == null) return;

            if (evt.EventType == Constants.EVENT_WINDOW_SWITCH)
                _detectedWindowSwitches.Add(evt);
            else if (evt.EventType == Constants.EVENT_CLIPBOARD_COPY)
                _detectedClipboardAccess.Add(evt);
            else if (evt.EventType == Constants.EVENT_IDLE)
                _detectedIdleEvents.Add(evt);
            else if (evt.EventType == Constants.EVENT_PROCESS_DETECTED)
                _detectedProcesses.Add(evt);
        }
    }
}
