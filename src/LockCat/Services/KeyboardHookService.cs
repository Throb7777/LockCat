using LockPig.Models;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows.Input;
using System.Windows.Interop;

namespace LockPig.Services;

public sealed class KeyboardHookService : IDisposable
{
    private const int WH_KEYBOARD_LL = 13;
    private const int WM_KEYDOWN = 0x0100;
    private const int WM_SYSKEYDOWN = 0x0104;
    private const short KeyPressed = unchecked((short)0x8000);

    private readonly LowLevelKeyboardProc _proc;
    private readonly Func<HotkeyGesture> _unlockHotkeyProvider;
    private IntPtr _hookId = IntPtr.Zero;
    private bool _locked;

    public KeyboardHookService(Func<HotkeyGesture> unlockHotkeyProvider)
    {
        _unlockHotkeyProvider = unlockHotkeyProvider;
        _proc = HookCallback;
        _hookId = SetHook(_proc);
    }

    public event Action? KeyboardActivityDetected;

    public void SetLocked(bool locked)
    {
        _locked = locked;
    }

    private IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
    {
        if (nCode >= 0 && (wParam == (IntPtr)WM_KEYDOWN || wParam == (IntPtr)WM_SYSKEYDOWN))
        {
            if (_locked)
            {
                int virtualKey = Marshal.ReadInt32(lParam);
                if (!IsAllowedDuringLock(virtualKey))
                {
                    return (IntPtr)1;
                }
            }
            else
            {
                KeyboardActivityDetected?.Invoke();
            }
        }

        return CallNextHookEx(_hookId, nCode, wParam, lParam);
    }

    private bool IsAllowedDuringLock(int virtualKey)
    {
        HotkeyGesture unlock = _unlockHotkeyProvider();
        Key key = KeyInterop.KeyFromVirtualKey(virtualKey);

        if (IsRequiredModifierKey(key, unlock.Modifiers))
        {
            return true;
        }

        return key == unlock.Key && CurrentModifiersMatch(unlock.Modifiers);
    }

    private static bool CurrentModifiersMatch(ModifierKeys expected)
    {
        return ModifierDownMatches(expected, ModifierKeys.Control, 0x11)
            && ModifierDownMatches(expected, ModifierKeys.Alt, 0x12)
            && ModifierDownMatches(expected, ModifierKeys.Shift, 0x10)
            && ModifierDownMatches(expected, ModifierKeys.Windows, 0x5B, 0x5C);
    }

    private static bool ModifierDownMatches(ModifierKeys expected, ModifierKeys modifier, params int[] virtualKeys)
    {
        bool shouldBeDown = expected.HasFlag(modifier);
        bool isDown = virtualKeys.Any(vk => (GetAsyncKeyState(vk) & KeyPressed) != 0);
        return shouldBeDown == isDown;
    }

    private static bool IsRequiredModifierKey(Key key, ModifierKeys modifiers)
    {
        return modifiers.HasFlag(ModifierKeys.Control) && key is Key.LeftCtrl or Key.RightCtrl
            || modifiers.HasFlag(ModifierKeys.Alt) && key is Key.LeftAlt or Key.RightAlt or Key.System
            || modifiers.HasFlag(ModifierKeys.Shift) && key is Key.LeftShift or Key.RightShift
            || modifiers.HasFlag(ModifierKeys.Windows) && key is Key.LWin or Key.RWin;
    }

    private static IntPtr SetHook(LowLevelKeyboardProc proc)
    {
        using Process currentProcess = Process.GetCurrentProcess();
        using ProcessModule? currentModule = currentProcess.MainModule;
        IntPtr moduleHandle = currentModule is null ? IntPtr.Zero : GetModuleHandle(currentModule.ModuleName);
        return SetWindowsHookEx(WH_KEYBOARD_LL, proc, moduleHandle, 0);
    }

    public void Dispose()
    {
        if (_hookId != IntPtr.Zero)
        {
            UnhookWindowsHookEx(_hookId);
            _hookId = IntPtr.Zero;
        }
    }

    private delegate IntPtr LowLevelKeyboardProc(int nCode, IntPtr wParam, IntPtr lParam);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern IntPtr SetWindowsHookEx(int idHook, LowLevelKeyboardProc lpfn, IntPtr hMod, uint dwThreadId);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool UnhookWindowsHookEx(IntPtr hhk);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

    [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    private static extern IntPtr GetModuleHandle(string? lpModuleName);

    [DllImport("user32.dll")]
    private static extern short GetAsyncKeyState(int vKey);
}
