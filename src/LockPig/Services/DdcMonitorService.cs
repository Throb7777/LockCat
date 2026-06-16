using LockPig.Models;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;

namespace LockPig.Services;

public sealed class DdcMonitorService : IDisposable
{
    private const byte PowerModeVcpCode = 0xD6;
    private const uint PowerOn = 0x01;
    private const uint PowerOff = 0x04;
    private readonly List<PHYSICAL_MONITOR> _lockedMonitors = [];

    public IReadOnlyList<MonitorCapability> Detect()
    {
        List<MonitorCapability> capabilities = [];

        foreach (IntPtr hMonitor in EnumerateDisplayMonitors())
        {
            if (!TryGetPhysicalMonitors(hMonitor, out PHYSICAL_MONITOR[] monitors, out string? error))
            {
                capabilities.Add(new MonitorCapability("Unknown display", null, false, false, 0, error));
                continue;
            }

            try
            {
                foreach (PHYSICAL_MONITOR monitor in monitors)
                {
                    capabilities.Add(ReadCapability(monitor));
                }
            }
            finally
            {
                DestroyPhysicalMonitors((uint)monitors.Length, monitors);
            }
        }

        return capabilities;
    }

    public bool TryTurnOffHardware()
    {
        ReleaseLockedMonitors(turnOnFirst: false);

        foreach (IntPtr hMonitor in EnumerateDisplayMonitors())
        {
            if (!TryGetPhysicalMonitors(hMonitor, out PHYSICAL_MONITOR[] monitors, out _))
            {
                continue;
            }

            foreach (PHYSICAL_MONITOR monitor in monitors)
            {
                MonitorCapability capability = ReadCapability(monitor);
                if (!capability.SupportsPowerMode)
                {
                    continue;
                }

                if (SetVCPFeature(monitor.hPhysicalMonitor, PowerModeVcpCode, PowerOff))
                {
                    _lockedMonitors.Add(monitor);
                }
                else
                {
                    DestroyPhysicalMonitors(1, [monitor]);
                }
            }
        }

        return _lockedMonitors.Count > 0;
    }

    public void TurnOnAndRelease()
    {
        ReleaseLockedMonitors(turnOnFirst: true);
    }

    public void Dispose()
    {
        ReleaseLockedMonitors(turnOnFirst: true);
    }

    private void ReleaseLockedMonitors(bool turnOnFirst)
    {
        if (_lockedMonitors.Count == 0)
        {
            return;
        }

        PHYSICAL_MONITOR[] monitors = _lockedMonitors.ToArray();
        _lockedMonitors.Clear();

        if (turnOnFirst)
        {
            foreach (PHYSICAL_MONITOR monitor in monitors)
            {
                TryTurnOn(monitor);
            }
        }

        DestroyPhysicalMonitors((uint)monitors.Length, monitors);
    }

    private static void TryTurnOn(PHYSICAL_MONITOR monitor)
    {
        for (int attempt = 0; attempt < 5; attempt++)
        {
            if (SetVCPFeature(monitor.hPhysicalMonitor, PowerModeVcpCode, PowerOn))
            {
                return;
            }

            Thread.Sleep(350);
        }
    }

    private static MonitorCapability ReadCapability(PHYSICAL_MONITOR monitor)
    {
        string? capabilityText = TryReadCapabilities(monitor.hPhysicalMonitor);
        bool supportsD6 = capabilityText?.Contains("D6", StringComparison.OrdinalIgnoreCase) == true;
        bool readD6 = GetVCPFeatureAndVCPFeatureReply(monitor.hPhysicalMonitor, PowerModeVcpCode, out _, out uint current, out _);
        string? model = ParseModel(capabilityText);

        return new MonitorCapability(
            monitor.szPhysicalMonitorDescription,
            model,
            capabilityText is not null || readD6,
            supportsD6 || readD6,
            current,
            readD6 || supportsD6 ? null : "未检测到 DDC/CI 电源模式 D6");
    }

    private static string? TryReadCapabilities(IntPtr physicalMonitor)
    {
        if (!GetCapabilitiesStringLength(physicalMonitor, out uint length) || length == 0 || length > 65536)
        {
            return null;
        }

        StringBuilder builder = new((int)length);
        return CapabilitiesRequestAndCapabilitiesReply(physicalMonitor, builder, length) ? builder.ToString() : null;
    }

    private static string? ParseModel(string? capabilityText)
    {
        if (string.IsNullOrWhiteSpace(capabilityText))
        {
            return null;
        }

        Match match = Regex.Match(capabilityText, @"model\(([^)]+)\)", RegexOptions.IgnoreCase);
        return match.Success ? match.Groups[1].Value : null;
    }

    private static bool TryGetPhysicalMonitors(IntPtr hMonitor, out PHYSICAL_MONITOR[] monitors, out string? error)
    {
        monitors = [];
        error = null;

        if (!GetNumberOfPhysicalMonitorsFromHMONITOR(hMonitor, out uint count) || count == 0)
        {
            error = $"无法枚举物理显示器，错误码 {Marshal.GetLastWin32Error()}";
            return false;
        }

        PHYSICAL_MONITOR[] physicalMonitors = new PHYSICAL_MONITOR[count];
        if (!GetPhysicalMonitorsFromHMONITOR(hMonitor, count, physicalMonitors))
        {
            error = $"无法获取物理显示器句柄，错误码 {Marshal.GetLastWin32Error()}";
            return false;
        }

        monitors = physicalMonitors;
        return true;
    }

    private static List<IntPtr> EnumerateDisplayMonitors()
    {
        List<IntPtr> monitors = [];
        MonitorEnumProc proc = (IntPtr hMonitor, IntPtr _, ref RECT _, IntPtr _) =>
        {
            monitors.Add(hMonitor);
            return true;
        };

        EnumDisplayMonitors(IntPtr.Zero, IntPtr.Zero, proc, IntPtr.Zero);
        return monitors;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct RECT
    {
        public int Left;
        public int Top;
        public int Right;
        public int Bottom;
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct PHYSICAL_MONITOR
    {
        public IntPtr hPhysicalMonitor;

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
        public string szPhysicalMonitorDescription;
    }

    private delegate bool MonitorEnumProc(IntPtr hMonitor, IntPtr hdcMonitor, ref RECT lprcMonitor, IntPtr dwData);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool EnumDisplayMonitors(IntPtr hdc, IntPtr lprcClip, MonitorEnumProc lpfnEnum, IntPtr dwData);

    [DllImport("dxva2.dll", SetLastError = true)]
    private static extern bool GetNumberOfPhysicalMonitorsFromHMONITOR(IntPtr hMonitor, out uint pdwNumberOfPhysicalMonitors);

    [DllImport("dxva2.dll", SetLastError = true)]
    private static extern bool GetPhysicalMonitorsFromHMONITOR(IntPtr hMonitor, uint dwPhysicalMonitorArraySize, [Out] PHYSICAL_MONITOR[] pPhysicalMonitorArray);

    [DllImport("dxva2.dll", SetLastError = true)]
    private static extern bool DestroyPhysicalMonitors(uint dwPhysicalMonitorArraySize, [In] PHYSICAL_MONITOR[] pPhysicalMonitorArray);

    [DllImport("dxva2.dll", SetLastError = true)]
    private static extern bool GetCapabilitiesStringLength(IntPtr hMonitor, out uint pdwCapabilitiesStringLengthInCharacters);

    [DllImport("dxva2.dll", SetLastError = true, CharSet = CharSet.Ansi)]
    private static extern bool CapabilitiesRequestAndCapabilitiesReply(IntPtr hMonitor, StringBuilder pszASCIICapabilitiesString, uint dwCapabilitiesStringLengthInCharacters);

    [DllImport("dxva2.dll", SetLastError = true)]
    private static extern bool GetVCPFeatureAndVCPFeatureReply(IntPtr hMonitor, byte bVCPCode, out uint pvct, out uint pdwCurrentValue, out uint pdwMaximumValue);

    [DllImport("dxva2.dll", SetLastError = true)]
    private static extern bool SetVCPFeature(IntPtr hMonitor, byte bVCPCode, uint dwNewValue);
}
