using Microsoft.Win32;
using System.Management;
using System.Diagnostics;
using System.Net.NetworkInformation;
using System.Runtime.InteropServices;
using SecureAssessmentClient.Models.Monitoring;
using SecureAssessmentClient.Utilities;

namespace SecureAssessmentClient.Services.DetectionService
{
    /// <summary>
    /// Detects suspicious system environments that indicate potential cheating
    /// Checks for: Virtual machines, debugging tools, remote desktop, suspicious processes
    /// Runs on exam startup and generates MonitoringEvents for violations
    /// </summary>
    public class EnvironmentIntegrityService
    {
        private List<string> _detectedViolations;
        private bool _hasRunInitialCheck;

        public EnvironmentIntegrityService()
        {
            _detectedViolations = new List<string>();
            _hasRunInitialCheck = false;
        }

        public async Task<(bool IsVm, int MonitorCount, bool IsRemote)> PerformFullScanAsync()
        {
            return await Task.Run(() =>
            {
                bool isVm = false;
                int monitorCount = 1;
                bool isRemote = false;

                try
                {
                    isVm = DetectVmFromComputerSystemWmi()
                           || DetectVmFromVideoControllerWmi()
                           || DetectVmFromMacPrefixes();
                }
                catch (Exception ex)
                {
                    Logger.Warn($"VAC full scan encountered an error: {ex.Message}");
                }

                try
                {
                    monitorCount = DetectMonitorCount();
                }
                catch (Exception ex)
                {
                    Logger.Warn($"Monitor count probe failed: {ex.Message}");
                    monitorCount = 1;
                }

                try
                {
                    isRemote = DetectRemoteDesktopSession();
                }
                catch (Exception ex)
                {
                    Logger.Warn($"Remote desktop probe failed: {ex.Message}");
                    isRemote = false;
                }

                return (isVm, Math.Max(1, monitorCount), isRemote);
            });
        }

        private static bool DetectVmFromComputerSystemWmi()
        {
            try
            {
                using var searcher = new ManagementObjectSearcher("SELECT Manufacturer, Model FROM Win32_ComputerSystem");
                foreach (ManagementObject obj in searcher.Get())
                {
                    var manufacturer = Convert.ToString(obj["Manufacturer"]) ?? string.Empty;
                    var model = Convert.ToString(obj["Model"]) ?? string.Empty;
                    var text = $"{manufacturer} {model}";

                    if (ContainsAny(text, "VMware", "VirtualBox", "innotek", "QEMU", "Hyper-V"))
                        return true;
                }
            }
            catch (Exception ex)
            {
                Logger.Warn($"WMI Win32_ComputerSystem query failed: {ex.Message}");
            }

            return false;
        }

        private static bool DetectVmFromVideoControllerWmi()
        {
            try
            {
                using var searcher = new ManagementObjectSearcher("SELECT Name FROM Win32_VideoController");
                foreach (ManagementObject obj in searcher.Get())
                {
                    var name = Convert.ToString(obj["Name"]) ?? string.Empty;
                    if (ContainsAny(name, "VMware SVGA", "VirtualBox Graphics"))
                        return true;
                }
            }
            catch (Exception ex)
            {
                Logger.Warn($"WMI Win32_VideoController query failed: {ex.Message}");
            }

            return false;
        }

        private static bool DetectVmFromMacPrefixes()
        {
            try
            {
                var vmPrefixes = new[]
                {
                    "005056", // VMware
                    "000C29", // VMware
                    "080027"  // VirtualBox
                };

                foreach (var nic in NetworkInterface.GetAllNetworkInterfaces())
                {
                    var mac = nic.GetPhysicalAddress()?.ToString() ?? string.Empty;
                    if (mac.Length < 6)
                        continue;

                    if (vmPrefixes.Any(p => mac.StartsWith(p, StringComparison.OrdinalIgnoreCase)))
                        return true;
                }
            }
            catch (Exception ex)
            {
                Logger.Warn($"MAC address virtualization probe failed: {ex.Message}");
            }

            return false;
        }

        private static int DetectMonitorCount()
        {
            try
            {
                int count = 0;
                using var searcher = new ManagementObjectSearcher("SELECT Name FROM Win32_DesktopMonitor");
                foreach (var _ in searcher.Get())
                    count++;

                return count > 0 ? count : 1;
            }
            catch (Exception ex)
            {
                Logger.Warn($"WMI Win32_DesktopMonitor query failed: {ex.Message}");
                return 1;
            }
        }

        private static bool DetectRemoteDesktopSession()
        {
            try
            {
                const int SM_REMOTESESSION = 0x1000;
                return GetSystemMetrics(SM_REMOTESESSION) != 0;
            }
            catch
            {
                return false;
            }
        }

        [DllImport("user32.dll")]
        private static extern int GetSystemMetrics(int nIndex);

        private static bool ContainsAny(string source, params string[] probes)
        {
            if (string.IsNullOrWhiteSpace(source) || probes == null || probes.Length == 0)
                return false;

            return probes.Any(p => source.IndexOf(p, StringComparison.OrdinalIgnoreCase) >= 0);
        }

        /// <summary>
        /// Checks for virtualization artifacts in registry and running processes
        /// Only flags VM if it's ACTIVELY RUNNING, not just installed
        /// Detects: VirtualBox, Hyper-V, VMware, QEMU, Parallels, VirtualPC
        /// </summary>
        public (bool IsVirtual, List<string> Details) CheckVirtualizationArtifacts()
        {
            var violations = new List<string>();

            try
            {
                // ONLY check for RUNNING processes - no registry checks
                // Detects: Traditional VMs, Android Emulators, and Mobile Device Simulators
                var vmProcesses = new Dictionary<string, string>
                {
                    // Traditional Virtual Machines
                    { "VBoxService", "VirtualBox Service" },
                    { "VBoxTray", "VirtualBox Tray" },
                    { "vmtoolsd", "VMware Tools" },
                    { "vmware", "VMware Process" },
                    { "parallels", "Parallels Desktop" },
                    { "vpc", "Virtual PC" },
                    { "qemu", "QEMU" },
                    { "mstsc", "Remote Desktop" },

                    // BlueStacks Android Emulator
                    { "HD-Player", "BlueStacks HD-Player" },
                    { "HD-RunCoreService", "BlueStacks Core Service" },
                    { "HD-LogRotateService", "BlueStacks Log Service" },

                    // Nox Player Android Emulator
                    { "Nox", "Nox Player Main Application" },
                    { "noxvms", "Nox Player VM" },
                    { "NoxVMHandle", "Nox Virtual Machine Handler" },
                    { "NoxSrv", "Nox Service" },

                    // LDPlayer Android Emulator
                    { "dnplayer", "LDPlayer Main Process" },
                    { "LdVBoxHeadless", "LDPlayer VM Engine" },
                    { "LdBoxRender", "LDPlayer Graphics Rendering" },

                    // MEmu Play Android Emulator
                    { "MEmu", "MEmu Play Main Process" },
                    { "MEmuConsole", "MEmu Management Console" },
                    { "MEmuHeadless", "MEmu Background Engine" },

                    // Genymotion Android Emulator
                    { "genymotion", "Genymotion Desktop Application" },
                    { "player", "Genymotion Virtual Device Player" },

                    // GameLoop (Tencent) Android Emulator
                    { "AndroidEmulator", "GameLoop Emulator Engine" },
                    { "AppMarket", "GameLoop App Store" },
                    { "Syzs_vbus", "GameLoop Virtual Bus Service" },

                    // MuMu Player Android Emulator
                    { "MuMuPlayer", "MuMu Player Main Application" },
                    { "NemuPlayer", "Nemu Player Engine" },

                    // Google Play Games (PC)
                    { "Crosvm", "Google Play Games VM" },
                    { "Tcore", "Tencent Core Service" },
                };

                foreach (var (processName, description) in vmProcesses)
                {
                    try
                    {
                        // ONLY flag if process is ACTUALLY RUNNING RIGHT NOW
                        if (Process.GetProcessesByName(processName).Length > 0)
                        {
                            violations.Add($"VM Process: {description}");
                            Logger.Warn($"Virtualization process RUNNING: {description}");
                        }
                    }
                    catch
                    {
                        // Ignore permission errors
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Error("Error checking virtualization artifacts", ex);
            }

            return (violations.Count > 0, violations);
        }

        /// <summary>
        /// Scans for debugging tools and remote access methods
        /// ONLY FLAGS CURRENTLY RUNNING TOOLS - no registry artifact checking
        /// Detects: WinDbg, x64dbg, OllyDbg, IDA, dnSpy, Fiddler, Remote Desktop, TeamViewer, AnyDesk, etc.
        /// </summary>
        public (bool HasAnomalies, List<string> Details) ScanHardwareSoftwareArtifacts()
        {
            var violations = new List<string>();

            try
            {
                // Process-based detection ONLY - no registry artifact checking
                // This ensures we only flag tools that are ACTIVELY RUNNING, not installed

            // Process-based detection ONLY - no registry artifact checking
            // This ensures we only flag tools that are ACTIVELY RUNNING, not installed

                // Remote access tools - ONLY FLAG IF ACTUALLY RUNNING
                var remoteAccessTools = new Dictionary<string, string>
                {
                    { "ultravnc", "UltraVNC" },
                    { "teamviewer", "TeamViewer" },
                    { "anydesk", "AnyDesk" },
                    { "gotomypc", "GoToMyPC" },
                    { "mstsc", "Remote Desktop" },
                };

                foreach (var (processName, toolName) in remoteAccessTools)
                {
                    try
                    {
                        if (Process.GetProcessesByName(processName).Length > 0)
                        {
                            violations.Add($"Remote Access App: {toolName}");
                            Logger.Warn($"Remote access tool RUNNING: {toolName}");
                        }
                    }
                    catch
                    {
                        // Ignore permission errors
                    }
                }
                var debuggingTools = new Dictionary<string, string>
                {
                    { "windbg", "WinDbg" },
                    { "x64dbg", "x64dbg" },
                    { "ollydbg", "OllyDbg" },
                    { "ida", "IDA Pro" },
                    { "dnspy", "dnSpy" },
                    { "fiddler", "Fiddler Proxy" },
                    { "cheatengine", "Cheat Engine" },
                    { "processmonitor", "Process Monitor" },
                    { "procexp", "Process Explorer" },
                };

                foreach (var (processName, toolName) in debuggingTools)
                {
                    try
                    {
                        if (Process.GetProcessesByName(processName).Length > 0)
                        {
                            violations.Add($"Debugging Tool: {toolName}");
                            Logger.Error($"Debugging tool detected: {toolName}");
                        }
                    }
                    catch
                    {
                        // Ignore permission errors
                    }
                }

                // Screen recording/capture tools
                var screenCaptureSoftware = new Dictionary<string, string>
                {
                    { "obs", "OBS Studio" },
                    { "ffmpeg", "FFmpeg" },
                    { "vlc", "VLC Media Player" },
                    { "camtasia", "Camtasia" },
                    { "snagit", "SnagIt" },
                    { "geforce", "GeForce Experience" },
                    { "bandicam", "Bandicam" },
                };

                foreach (var (processName, softwareName) in screenCaptureSoftware)
                {
                    try
                    {
                        if (Process.GetProcessesByName(processName).Length > 0)
                        {
                            violations.Add($"Screen Capture Software: {softwareName}");
                            Logger.Warn($"Screen capture tool detected: {softwareName}");
                        }
                    }
                    catch
                    {
                        // Ignore permission errors
                    }
                }

                // Unauthorized communication apps - ONLY FLAG IF ACTUALLY RUNNING
                var communicationApps = new Dictionary<string, string>
                {
                    { "discord", "Discord" },
                    { "telegram", "Telegram" },
                    { "slack", "Slack" },
                    { "whatsapp", "WhatsApp" },
                    { "skype", "Skype" },
                    { "teams", "Microsoft Teams" },
                    { "zoom", "Zoom" },
                };

                foreach (var (processName, appName) in communicationApps)
                {
                    try
                    {
                        // Only flag if process is ACTIVELY RUNNING (not just installed)
                        if (Process.GetProcessesByName(processName).Length > 0)
                        {
                            violations.Add($"Communication App: {appName}");
                            Logger.Warn($"Unauthorized communication app RUNNING: {appName}");
                        }
                    }
                    catch
                    {
                        // Ignore permission errors
                    }
                }

                // Alternative browsers and search tools
                var unauthorizedTools = new Dictionary<string, string>
                {
                    { "chrome", "Google Chrome" },
                    { "firefox", "Mozilla Firefox" },
                    { "opera", "Opera Browser" },
                    { "edge", "Edge Browser" },
                    { "google", "Google Desktop" },
                };

                foreach (var (processName, toolName) in unauthorizedTools)
                {
                    try
                    {
                        var processes = Process.GetProcessesByName(processName);
                        // Only flag if running, but allow some flexibility (some processes are legitimate)
                        if (processes.Length > 1) // More than 1 instance suggests intentional use
                        {
                            violations.Add($"Unauthorized App: {toolName}");
                            Logger.Warn($"Potentially unauthorized app detected: {toolName}");
                        }
                    }
                    catch
                    {
                        // Ignore permission errors
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Error("Error detecting suspicious processes", ex);
            }

            return (violations.Count > 0, violations);
        }

        /// <summary>
        /// Performs comprehensive initial environment check on exam startup
        /// Generates MonitoringEvent if violations detected
        /// </summary>
        public MonitoringEvent PerformInitialEnvironmentCheck()
        {
            try
            {
                Logger.Info("Starting initial environment integrity check");
                _detectedViolations.Clear();

                // Check virtualization
                var (isVirtual, vmViolations) = CheckVirtualizationArtifacts();
                if (isVirtual)
                {
                    _detectedViolations.AddRange(vmViolations);
                    Logger.Error($"Virtualization detected: {string.Join("; ", vmViolations)}");
                }

                // Check hardware/software artifacts
                var (hasAnomalies, artifacts) = ScanHardwareSoftwareArtifacts();
                if (hasAnomalies)
                {
                    _detectedViolations.AddRange(artifacts);
                    Logger.Warn($"Hardware/Software anomalies detected: {string.Join("; ", artifacts)}");
                }

                // Generate event if violations found
                if (_detectedViolations.Count > 0)
                {
                    var violationType = isVirtual ? ViolationType.Aggressive : ViolationType.Passive;
                    var severity = isVirtual ? 3 : (_detectedViolations.Count > 3 ? 2 : 1);

                    var monitoringEvent = new MonitoringEvent
                    {
                        EventType = "ENVIRONMENT_VIOLATION",
                        ViolationType = violationType,
                        SeverityScore = severity,
                        Timestamp = DateTime.UtcNow,
                        Details = string.Join(" | ", _detectedViolations)
                    };

                    Logger.Info($"Generated environment violation event with severity {severity}");
                    _hasRunInitialCheck = true;
                    return monitoringEvent;
                }

                Logger.Info("Environment check completed - no violations detected");
                _hasRunInitialCheck = true;
                return null;
            }
            catch (Exception ex)
            {
                Logger.Error("Unexpected error during environment check", ex);
                return null;
            }
        }

        /// <summary>
        /// Continuous monitoring check (run periodically during exam)
        /// Detects new suspicious processes that may have been launched
        /// </summary>
        public MonitoringEvent ContinuousEnvironmentCheck()
        {
            try
            {
                // Only check for processes that could be launched during exam
                var suspiciousProcesses = new Dictionary<string, (string name, ViolationType type, int severity)>
                {
                    { "windbg", ("WinDbg Debugger", ViolationType.Aggressive, 3) },
                    { "x64dbg", ("x64dbg Debugger", ViolationType.Aggressive, 3) },
                    { "dnspy", ("dnSpy Decompiler", ViolationType.Aggressive, 3) },
                    { "cheatengine", ("Cheat Engine", ViolationType.Aggressive, 3) },
                    { "fiddler", ("Fiddler Proxy", ViolationType.Aggressive, 2) },
                    { "teamviewer", ("TeamViewer Remote", ViolationType.Aggressive, 2) },
                    { "anydesk", ("AnyDesk Remote", ViolationType.Aggressive, 2) },
                    { "discord", ("Discord Chat", ViolationType.Aggressive, 2) },
                    { "telegram", ("Telegram Chat", ViolationType.Aggressive, 2) },
                };

                foreach (var (processName, (displayName, violationType, severity)) in suspiciousProcesses)
                {
                    try
                    {
                        if (Process.GetProcessesByName(processName).Length > 0)
                        {
                            return new MonitoringEvent
                            {
                                EventType = "SUSPICIOUS_PROCESS_DETECTED",
                                ViolationType = violationType,
                                SeverityScore = severity,
                                Timestamp = DateTime.UtcNow,
                                Details = $"Suspicious process detected: {displayName}"
                            };
                        }
                    }
                    catch
                    {
                        // Ignore permission errors
                    }
                }

                return null;
            }
            catch (Exception ex)
            {
                Logger.Error("Error during continuous environment check", ex);
                return null;
            }
        }

        /// <summary>
        /// Gets list of detected violations from last check
        /// </summary>
        public List<string> GetDetectedViolations()
        {
            return new List<string>(_detectedViolations);
        }

        /// <summary>
        /// Gets whether initial check has been completed
        /// </summary>
        public bool HasRunInitialCheck
        {
            get { return _hasRunInitialCheck; }
        }

        /// <summary>
        /// Gets count of detected violations
        /// </summary>
        public int ViolationCount
        {
            get { return _detectedViolations.Count; }
        }
    }
}
