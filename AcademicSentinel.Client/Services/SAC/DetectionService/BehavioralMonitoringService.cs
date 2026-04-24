using AcademicSentinel.Client.Services.SAC.Models;
using AcademicSentinel.Client.Services.SAC.Utilities;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Microsoft.Win32;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace AcademicSentinel.Client.Services.SAC.DetectionService
{
    internal sealed class BehavioralMonitoringService
    {
        [DllImport("user32.dll")]
        private static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        private static extern int GetWindowText(IntPtr hWnd, StringBuilder text, int count);

        [DllImport("user32.dll")]
        private static extern short GetAsyncKeyState(int vKey);

        [DllImport("user32.dll")]
        private static extern uint GetClipboardSequenceNumber();

        [DllImport("user32.dll")]
        private static extern bool GetLastInputInfo(ref LASTINPUTINFO plii);

        [StructLayout(LayoutKind.Sequential)]
        private struct LASTINPUTINFO
        {
            public static readonly int SizeOf = Marshal.SizeOf(typeof(LASTINPUTINFO));

            [MarshalAs(UnmanagedType.U4)]
            public uint cbSize;

            [MarshalAs(UnmanagedType.U4)]
            public uint dwTime;
        }

        private const int VK_CONTROL = 0x11;
        private const int VK_C = 0x43;
        private const int VK_V = 0x56;

        private readonly DetectionSettings _settings;
        private readonly HashSet<string> _blacklistedProcessNames;
        private readonly Dictionary<string, DateTime> _lastReportedAtByEvent = new(StringComparer.OrdinalIgnoreCase);
        private readonly object _syncRoot = new();
        private readonly Queue<MonitoringDetectionEvent> _pendingBackgroundEvents = new();
        private readonly uint _idleThresholdMs = 180000;
        private readonly string[] _blacklistedApps = { "discord", "obs32", "obs64", "vmware", "virtualbox", "taskmgr", "TeamViewer", "AnyDesk" };
        private readonly Dictionary<string, DateTime> _lastPbdReportedAtByApp = new(StringComparer.OrdinalIgnoreCase);

        private IntPtr _lastForegroundWindow;
        private bool _lastForegroundWasSac;
        private string _lastWindowName = "Unknown";
        private IntPtr _temporarilyExemptWindow;
        private DateTime _lastProcessScanAt = DateTime.MinValue;
        private HashSet<string> _lastReportedProcesses = new(StringComparer.OrdinalIgnoreCase);

        private bool _copyDown;
        private bool _pasteDown;
        private uint _lastClipboardSequenceNumber;

        private int _lastReportedIdleLevel;
        private bool _isMonitoring;
        private bool _isCurrentlyIdle;
        private DateTime _monitoringStartedAtUtc;
        private CancellationTokenSource? _idleLoopCts;
        private Task? _idleLoopTask;

        public BehavioralMonitoringService(DetectionSettings settings, IEnumerable<string> blacklistedProcessNames)
        {
            _settings = settings ?? new DetectionSettings();
            _blacklistedProcessNames = blacklistedProcessNames?.ToHashSet(StringComparer.OrdinalIgnoreCase)
                ?? new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            _lastForegroundWindow = GetForegroundWindow();
            _lastClipboardSequenceNumber = GetClipboardSequenceNumber();
        }

        public void StartMonitoring()
        {
            StopIdleLoop();

            _isMonitoring = true;
            _isCurrentlyIdle = false;
            _lastReportedIdleLevel = 0;
            _monitoringStartedAtUtc = DateTime.UtcNow;
            _lastClipboardSequenceNumber = GetClipboardSequenceNumber();

            _idleLoopCts = new CancellationTokenSource();
            _idleLoopTask = RunIdleTrackingLoopAsync(_idleLoopCts.Token);

            DisableTaskManager();
        }

        public void StopMonitoring()
        {
            _isMonitoring = false;
            _isCurrentlyIdle = false;
            _copyDown = false;
            _pasteDown = false;
            _temporarilyExemptWindow = IntPtr.Zero;
            _lastReportedIdleLevel = 0;
            _lastReportedProcesses.Clear();
            _lastReportedAtByEvent.Clear();

            lock (_syncRoot)
            {
                _pendingBackgroundEvents.Clear();
                _lastPbdReportedAtByApp.Clear();
            }

            StopIdleLoop();
            EnableTaskManager();
        }

        public IReadOnlyList<MonitoringDetectionEvent> Poll(bool isSacWindowActive)
        {
            if (!_isMonitoring)
                return Array.Empty<MonitoringDetectionEvent>();

            var findings = new List<MonitoringDetectionEvent>();

            DrainBackgroundEvents(findings);

            DetectFocus(isSacWindowActive, findings);
            DetectClipboardAndScreenshot(findings);
            DetectIdle(findings);
            DetectBlacklistedProcesses(findings);

            _lastForegroundWasSac = isSacWindowActive;

            return findings;
        }

        private async Task RunIdleTrackingLoopAsync(CancellationToken cancellationToken)
        {
            try
            {
                while (!cancellationToken.IsCancellationRequested)
                {
                    if (_isMonitoring && _settings.EnableIdleDetection)
                    {
                        EvaluateHardIdle();
                    }

                    if (_isMonitoring && _settings.EnableProcessDetection)
                    {
                        ScanAndHandleBlacklistedProcesses();
                    }

                    await Task.Delay(1000, cancellationToken).ConfigureAwait(false);
                }
            }
            catch (OperationCanceledException)
            {
            }
        }

        private void DrainBackgroundEvents(ICollection<MonitoringDetectionEvent> findings)
        {
            lock (_syncRoot)
            {
                while (_pendingBackgroundEvents.Count > 0)
                {
                    findings.Add(_pendingBackgroundEvents.Dequeue());
                }
            }
        }

        private void EvaluateHardIdle()
        {
            uint idleTime = GetIdleTimeMs();
            bool shouldReportIdle = false;

            lock (_syncRoot)
            {
                if (idleTime >= _idleThresholdMs && !_isCurrentlyIdle)
                {
                    _isCurrentlyIdle = true;
                    shouldReportIdle = true;
                }
                else if (idleTime < _idleThresholdMs && _isCurrentlyIdle)
                {
                    _isCurrentlyIdle = false;
                }

                if (shouldReportIdle)
                {
                    _pendingBackgroundEvents.Enqueue(new MonitoringDetectionEvent
                    {
                        EventType = DetectionConstants.EventIdle,
                        Description = "Student has been inactive for over 3 minutes.",
                        SeverityScore = 10,
                        Timestamp = DateTime.UtcNow
                    });
                }
            }
        }

        private void ScanAndHandleBlacklistedProcesses()
        {
            Process[] activeProcesses;
            try
            {
                activeProcesses = Process.GetProcesses();
            }
            catch
            {
                return;
            }

            DateTime now = DateTime.UtcNow;
            foreach (var process in activeProcesses)
            {
                using (process)
                {
                    string processName;
                    try
                    {
                        processName = process.ProcessName;
                    }
                    catch
                    {
                        continue;
                    }

                    if (string.IsNullOrWhiteSpace(processName) || !IsBlacklistedApp(processName))
                        continue;

                    bool shouldReport;
                    lock (_syncRoot)
                    {
                        shouldReport = !_lastPbdReportedAtByApp.TryGetValue(processName, out DateTime lastReportedAt)
                            || (now - lastReportedAt).TotalSeconds >= 30;

                        if (shouldReport)
                        {
                            _lastPbdReportedAtByApp[processName] = now;
                        }
                    }

                    try
                    {
                        process.Kill();
                    }
                    catch
                    {
                    }

                    if (!shouldReport)
                        continue;

                    lock (_syncRoot)
                    {
                        _pendingBackgroundEvents.Enqueue(new MonitoringDetectionEvent
                        {
                            EventType = "PBD",
                            Description = $"Restricted Application Opened: {processName}",
                            SeverityScore = 30,
                            Timestamp = now
                        });
                    }
                }
            }
        }

        private bool IsBlacklistedApp(string processName)
        {
            return _blacklistedApps.Any(app => app.Equals(processName, StringComparison.OrdinalIgnoreCase));
        }

        private static void DisableTaskManager()
        {
            try
            {
                using RegistryKey? systemPolicies = Registry.CurrentUser.CreateSubKey(@"Software\Microsoft\Windows\CurrentVersion\Policies\System", true);
                systemPolicies?.SetValue("DisableTaskMgr", 1, RegistryValueKind.DWord);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Failed to disable Task Manager: {ex.Message}");
            }
        }

        private static void EnableTaskManager()
        {
            try
            {
                using RegistryKey? systemPolicies = Registry.CurrentUser.CreateSubKey(@"Software\Microsoft\Windows\CurrentVersion\Policies\System", true);
                if (systemPolicies?.GetValue("DisableTaskMgr") is not null)
                {
                    systemPolicies.DeleteValue("DisableTaskMgr", false);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Failed to enable Task Manager: {ex.Message}");
            }
        }

        private void StopIdleLoop()
        {
            _idleLoopCts?.Cancel();
            _idleLoopCts?.Dispose();
            _idleLoopCts = null;
            _idleLoopTask = null;
        }

        private void DetectFocus(bool isSacWindowActive, ICollection<MonitoringDetectionEvent> findings)
        {
            if (!_settings.EnableFocusDetection)
                return;

            var foreground = GetForegroundWindow();
            if (foreground != _lastForegroundWindow)
            {
                IntPtr previousForeground = _lastForegroundWindow;
                bool previousWasSac = _lastForegroundWasSac;
                string previous = _lastWindowName;
                string current = GetWindowName(foreground);

                if (isSacWindowActive && !previousWasSac)
                {
                    _temporarilyExemptWindow = previousForeground;
                }
                else if (!isSacWindowActive && previousWasSac)
                {
                    bool isReturningToExemptWindow = _temporarilyExemptWindow != IntPtr.Zero && foreground == _temporarilyExemptWindow;
                    if (isReturningToExemptWindow)
                    {
                        ClearTemporaryExemptWindow();
                    }
                    else
                    {
                        AddEvent(findings, DetectionConstants.EventWindowSwitch, 2,
                            $"Window switched from '{previous}' to '{current}' while monitoring is active.", 0);
                        ClearTemporaryExemptWindow();
                    }
                }
                else if (!isSacWindowActive)
                {
                    AddEvent(findings, DetectionConstants.EventWindowSwitch, 2,
                        $"Window switched from '{previous}' to '{current}' while monitoring is active.", 0);
                    ClearTemporaryExemptWindow();
                }

                _lastForegroundWindow = foreground;
                _lastWindowName = current;
                _lastForegroundWasSac = isSacWindowActive;
            }
        }

        private void ClearTemporaryExemptWindow()
        {
            _temporarilyExemptWindow = IntPtr.Zero;
        }

        private void DetectClipboardAndScreenshot(ICollection<MonitoringDetectionEvent> findings)
        {
            if (!_settings.EnableClipboardMonitoring)
                return;

            uint currentSequence = GetClipboardSequenceNumber();
            bool clipboardChanged = currentSequence != _lastClipboardSequenceNumber;
            if (clipboardChanged)
            {
                _lastClipboardSequenceNumber = currentSequence;

                if (TryClipboardContainsImage())
                {
                    AddEvent(findings, DetectionConstants.EventScreenshot, 3,
                        "Screenshot or image capture detected in clipboard while monitoring is active.", 1);
                }
                else
                {
                    AddEvent(findings, DetectionConstants.EventClipboardCopy, 2,
                        "Clipboard content changed while monitoring is active.", 1);
                }
            }

            bool ctrlPressed = IsKeyDown(VK_CONTROL);

            bool copyPressed = ctrlPressed && IsKeyDown(VK_C);
            if (copyPressed && !_copyDown)
            {
                AddEvent(findings, DetectionConstants.EventClipboardCopy, 2,
                    "Copy command (Ctrl+C) detected while monitoring is active.", 2);
            }
            _copyDown = copyPressed;

            bool pastePressed = ctrlPressed && IsKeyDown(VK_V);
            if (pastePressed && !_pasteDown)
            {
                AddEvent(findings, DetectionConstants.EventClipboardPaste, 2,
                    "Paste command (Ctrl+V) detected while monitoring is active.", 2);
            }
            _pasteDown = pastePressed;
        }

        private void DetectIdle(ICollection<MonitoringDetectionEvent> findings)
        {
            if (!_settings.EnableIdleDetection)
                return;

            int idleSecondsFromSystem = GetSystemIdleSeconds();
            int monitoringElapsedSeconds = (int)Math.Max(0, (DateTime.UtcNow - _monitoringStartedAtUtc).TotalSeconds);
            int idleSeconds = Math.Min(idleSecondsFromSystem, monitoringElapsedSeconds);
            int warningThreshold = Math.Max(5, _settings.IdleWarningThresholdSeconds);
            int violationThreshold = Math.Max(warningThreshold + 1, _settings.IdleViolationThresholdSeconds);
            int criticalThreshold = Math.Max(violationThreshold + 1, _settings.IdleCriticalThresholdSeconds);

            if (idleSeconds < warningThreshold)
            {
                _lastReportedIdleLevel = 0;
                return;
            }

            if (idleSeconds >= criticalThreshold && _lastReportedIdleLevel < 3)
            {
                _lastReportedIdleLevel = 3;
                AddEvent(findings, DetectionConstants.EventIdle, 3,
                    $"Critical inactivity detected ({idleSeconds}s). Mouse and keyboard appear idle.", 10);
                return;
            }

            if (idleSeconds >= violationThreshold && _lastReportedIdleLevel < 2)
            {
                _lastReportedIdleLevel = 2;
                AddEvent(findings, DetectionConstants.EventIdle, 2,
                    $"Inactivity detected ({idleSeconds}s). Mouse and keyboard appear idle.", 10);
                return;
            }

            if (idleSeconds >= warningThreshold && _lastReportedIdleLevel < 1)
            {
                _lastReportedIdleLevel = 1;
                AddEvent(findings, DetectionConstants.EventIdle, 1,
                    $"Idle warning: no keyboard/mouse input for {idleSeconds}s.", 10);
            }
        }

        private void DetectBlacklistedProcesses(ICollection<MonitoringDetectionEvent> findings)
        {
            if (!_settings.EnableProcessDetection)
                return;

            var now = DateTime.UtcNow;
            if ((now - _lastProcessScanAt).TotalSeconds < 5)
                return;

            _lastProcessScanAt = now;

            var running = Process.GetProcesses()
                .Select(p =>
                {
                    try { return p.ProcessName; }
                    catch { return string.Empty; }
                })
                .Where(n => !string.IsNullOrWhiteSpace(n))
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            var detected = _blacklistedProcessNames
                .Where(running.Contains)
                .OrderBy(p => p)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            if (detected.Count == 0)
            {
                _lastReportedProcesses.Clear();
                return;
            }

            if (_lastReportedProcesses.SetEquals(detected))
                return;

            _lastReportedProcesses = detected;

            AddEvent(findings, DetectionConstants.EventProcessDetected, 3,
                $"Unauthorized process detected: {string.Join(", ", detected.Take(5))}", 5);
        }

        private void AddEvent(ICollection<MonitoringDetectionEvent> findings, string eventType, int severity, string description, int cooldownSeconds)
        {
            var now = DateTime.UtcNow;
            if (_lastReportedAtByEvent.TryGetValue(eventType, out var lastReportedAt) && (now - lastReportedAt).TotalSeconds < cooldownSeconds)
                return;

            _lastReportedAtByEvent[eventType] = now;
            findings.Add(new MonitoringDetectionEvent
            {
                EventType = eventType,
                SeverityScore = severity,
                Description = description,
                Timestamp = now
            });
        }

        private static bool IsKeyDown(int vKey)
        {
            return (GetAsyncKeyState(vKey) & 0x8000) != 0;
        }

        private static bool TryClipboardContainsImage()
        {
            try
            {
                return Clipboard.ContainsImage();
            }
            catch
            {
                return false;
            }
        }

        private static string GetWindowName(IntPtr handle)
        {
            if (handle == IntPtr.Zero)
                return "Unknown";

            var title = new StringBuilder(256);
            if (GetWindowText(handle, title, title.Capacity) <= 0)
                return "Unknown";

            string value = title.ToString().Trim();
            return string.IsNullOrWhiteSpace(value) ? "Unknown" : value;
        }

        private static int GetSystemIdleSeconds()
        {
            return (int)(GetIdleTimeMs() / 1000);
        }

        private static uint GetIdleTimeMs()
        {
            LASTINPUTINFO lastInPut = new LASTINPUTINFO
            {
                cbSize = (uint)Marshal.SizeOf<LASTINPUTINFO>()
            };

            if (!GetLastInputInfo(ref lastInPut))
                return 0;

            return (uint)Environment.TickCount - lastInPut.dwTime;
        }
    }
}
