using System.Runtime.InteropServices;

namespace LockPig.Services;

public sealed class MonitorService
{
    private const int HWND_BROADCAST = 0xFFFF;
    private const int WM_SYSCOMMAND = 0x0112;
    private const int SC_MONITORPOWER = 0xF170;
    private const int MONITOR_OFF = 2;
    private const int MONITOR_ON = -1;

    public void TurnOff()
    {
        SendNotifyMessage((IntPtr)HWND_BROADCAST, WM_SYSCOMMAND, (IntPtr)SC_MONITORPOWER, (IntPtr)MONITOR_OFF);
    }

    public void TurnOn()
    {
        SendNotifyMessage((IntPtr)HWND_BROADCAST, WM_SYSCOMMAND, (IntPtr)SC_MONITORPOWER, (IntPtr)MONITOR_ON);
    }

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool SendNotifyMessage(IntPtr hWnd, int msg, IntPtr wParam, IntPtr lParam);
}
