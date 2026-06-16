using Microsoft.Win32;
using System.Diagnostics;

namespace LockPig.Services;

public sealed class StartupService
{
    private const string RunKeyPath = @"Software\Microsoft\Windows\CurrentVersion\Run";
    private const string ValueName = "LockCat";
    private const string LegacyValueName = "LockPig";

    public bool IsEnabled()
    {
        using RegistryKey? key = Registry.CurrentUser.OpenSubKey(RunKeyPath, writable: false);
        return key?.GetValue(ValueName) is string;
    }

    public void SetEnabled(bool enabled)
    {
        using RegistryKey key = Registry.CurrentUser.CreateSubKey(RunKeyPath);

        if (!enabled)
        {
            key.DeleteValue(ValueName, throwOnMissingValue: false);
            key.DeleteValue(LegacyValueName, throwOnMissingValue: false);
            return;
        }

        string exePath = Process.GetCurrentProcess().MainModule?.FileName ?? Environment.ProcessPath ?? string.Empty;
        if (!string.IsNullOrWhiteSpace(exePath))
        {
            key.DeleteValue(LegacyValueName, throwOnMissingValue: false);
            key.SetValue(ValueName, $"\"{exePath}\"");
        }
    }
}
