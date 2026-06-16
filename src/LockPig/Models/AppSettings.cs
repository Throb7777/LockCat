namespace LockPig.Models;

public sealed class AppSettings
{
    public HotkeyGesture LockHotkey { get; set; } = HotkeyGesture.DefaultLock;
    public HotkeyGesture UnlockHotkey { get; set; } = HotkeyGesture.DefaultUnlock;
    public bool TurnOffMonitorOnLock { get; set; } = true;
    public MonitorPowerMode MonitorPowerMode { get; set; } = MonitorPowerMode.HardwareDdc;
    public bool PetVisible { get; set; } = true;
    public bool PetAlwaysOnTop { get; set; } = true;
    public double PetOpacity { get; set; } = 1.0;
    public bool StartWithWindows { get; set; }
    public string Language { get; set; } = "zh-CN";
}
