using System;
using System.Linq;
using System.Management;
using System.Net.NetworkInformation;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace AcademicSentinel.Client.Services.SAC.DetectionService
{
    internal sealed class EnvironmentIntegrityService
    {
        public async Task<(bool IsVm, bool IsRemote)> PerformFullScanAsync()
        {
            return await Task.Run(() =>
            {
                bool isVm = false;
                bool isRemote = false;

                try
                {
                    isVm = DetectVmFromComputerSystemWmi()
                        || DetectVmFromVideoControllerWmi()
                        || DetectVmFromMacPrefixes();
                }
                catch
                {
                    isVm = false;
                }

                try
                {
                    isRemote = DetectRemoteDesktopSession();
                }
                catch
                {
                    isRemote = false;
                }

                return (isVm, isRemote);
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
            catch
            {
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
            catch
            {
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
            catch
            {
            }

            return false;
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
    }
}
