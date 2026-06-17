using LockPig.Models;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows.Input;
using System.Windows.Interop;

namespace LockPig.Services;

public sealed class KeyboardHookService : IDisposable
{
    private const int WH_KEYBOARD_LL = 13;
    private const int WM_INPUT = 0x00FF;
    private const int WM_KEYDOWN = 0x0100;
    private const int WM_SYSKEYDOWN = 0x0104;
    private const int RID_INPUT = 0x10000003;
    private const int RIDI_DEVICENAME = 0x20000007;
    private const int RIM_TYPEKEYBOARD = 1;
    private const int RIDEV_INPUTSINK = 0x00000100;
    private const int LLKHF_LOWER_IL_INJECTED = 0x00000002;
    private const int LLKHF_INJECTED = 0x00000010;
    private const int SM_REMOTESESSION = 0x1000;
    private const short KeyPressed = unchecked((short)0x8000);
    private static readonly TimeSpan LocalInputMatchWindow = TimeSpan.FromMilliseconds(350);

    private readonly LowLevelKeyboardProc _proc;
    private readonly Func<HotkeyGesture> _unlockHotkeyProvider;
    private readonly HwndSource _rawInputSource;
    private readonly Dictionary<IntPtr, bool> _localKeyboardDevices = [];
    private readonly Queue<KeyboardInputStamp> _recentLocalInputs = [];
    private readonly object _inputLock = new();
    private IntPtr _hookId = IntPtr.Zero;
    private bool _locked;

    public KeyboardHookService(Func<HotkeyGesture> unlockHotkeyProvider)
    {
        _unlockHotkeyProvider = unlockHotkeyProvider;
        _rawInputSource = CreateRawInputSource();
        _rawInputSource.AddHook(RawInputWndProc);
        RegisterRawKeyboardInput(_rawInputSource.Handle);
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
                KbdLlHookStruct data = Marshal.PtrToStructure<KbdLlHookStruct>(lParam);
                if (!IsAllowedDuringLock(data.VirtualKey)
                    && ShouldBlockDuringLock(data))
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

    private IntPtr RawInputWndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        if (msg != WM_INPUT)
        {
            return IntPtr.Zero;
        }

        RecordRawKeyboardInput(lParam);
        return IntPtr.Zero;
    }

    private void RecordRawKeyboardInput(IntPtr rawInputHandle)
    {
        uint size = 0;
        uint headerSize = (uint)Marshal.SizeOf<RawInputHeader>();
        uint result = GetRawInputData(rawInputHandle, RID_INPUT, IntPtr.Zero, ref size, headerSize);
        if (result == uint.MaxValue || size == 0)
        {
            return;
        }

        IntPtr buffer = Marshal.AllocHGlobal((int)size);
        try
        {
            result = GetRawInputData(rawInputHandle, RID_INPUT, buffer, ref size, headerSize);
            if (result == uint.MaxValue)
            {
                return;
            }

            RawInputHeader header = Marshal.PtrToStructure<RawInputHeader>(buffer);
            if (header.Type != RIM_TYPEKEYBOARD || !IsLocalKeyboardDevice(header.Device))
            {
                return;
            }

            IntPtr keyboardPtr = IntPtr.Add(buffer, Marshal.SizeOf<RawInputHeader>());
            RawKeyboard keyboard = Marshal.PtrToStructure<RawKeyboard>(keyboardPtr);
            if (keyboard.Message != WM_KEYDOWN && keyboard.Message != WM_SYSKEYDOWN)
            {
                return;
            }

            lock (_inputLock)
            {
                TrimRecentInputs(DateTime.UtcNow);
                _recentLocalInputs.Enqueue(new KeyboardInputStamp(
                    keyboard.VirtualKey,
                    keyboard.MakeCode,
                    DateTime.UtcNow));
            }
        }
        finally
        {
            Marshal.FreeHGlobal(buffer);
        }
    }

    private bool ShouldBlockDuringLock(KbdLlHookStruct data)
    {
        if (IsRemoteSession() || IsInjectedInput(data.Flags))
        {
            return false;
        }

        if (MatchesRecentLocalRawInput(data))
        {
            return true;
        }

        // WH_KEYBOARD_LL can arrive before WM_INPUT on some systems. In a local
        // console session, non-injected keyboard input should still stay locked.
        return true;
    }

    private bool MatchesRecentLocalRawInput(KbdLlHookStruct data)
    {
        DateTime now = DateTime.UtcNow;
        lock (_inputLock)
        {
            TrimRecentInputs(now);
            return _recentLocalInputs.Any(input =>
                input.VirtualKey == data.VirtualKey
                && input.ScanCode == data.ScanCode
                && now - input.Timestamp <= LocalInputMatchWindow);
        }
    }

    private void TrimRecentInputs(DateTime now)
    {
        while (_recentLocalInputs.Count > 0 && now - _recentLocalInputs.Peek().Timestamp > LocalInputMatchWindow)
        {
            _recentLocalInputs.Dequeue();
        }
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

    private bool IsLocalKeyboardDevice(IntPtr device)
    {
        if (device == IntPtr.Zero)
        {
            return false;
        }

        lock (_inputLock)
        {
            if (_localKeyboardDevices.TryGetValue(device, out bool isLocal))
            {
                return isLocal;
            }

            string name = GetRawInputDeviceName(device);
            isLocal = IsLocalPhysicalKeyboardName(name);
            _localKeyboardDevices[device] = isLocal;
            return isLocal;
        }
    }

    private static string GetRawInputDeviceName(IntPtr device)
    {
        uint size = 0;
        uint result = GetRawInputDeviceInfo(device, RIDI_DEVICENAME, IntPtr.Zero, ref size);
        if (result == uint.MaxValue || size == 0)
        {
            return string.Empty;
        }

        IntPtr buffer = Marshal.AllocHGlobal((int)size * 2);
        try
        {
            result = GetRawInputDeviceInfo(device, RIDI_DEVICENAME, buffer, ref size);
            if (result == uint.MaxValue)
            {
                return string.Empty;
            }

            return Marshal.PtrToStringUni(buffer) ?? string.Empty;
        }
        finally
        {
            Marshal.FreeHGlobal(buffer);
        }
    }

    private static bool IsLocalPhysicalKeyboardName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return false;
        }

        string normalized = name.ToUpperInvariant();
        if (normalized.Contains("RDP")
            || normalized.Contains("ROOT#RDP_KBD")
            || normalized.Contains("TERMINAL")
            || normalized.Contains("REMOTE")
            || normalized.Contains("VIRTUAL"))
        {
            return false;
        }

        return normalized.Contains("HID#")
            || normalized.Contains("USB#")
            || normalized.Contains("ACPI#")
            || normalized.Contains("PNP0303")
            || normalized.Contains("KEYBOARD");
    }

    private static bool IsRemoteSession()
    {
        return GetSystemMetrics(SM_REMOTESESSION) != 0;
    }

    private static bool IsInjectedInput(int flags)
    {
        return (flags & (LLKHF_INJECTED | LLKHF_LOWER_IL_INJECTED)) != 0;
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

        _rawInputSource.RemoveHook(RawInputWndProc);
        _rawInputSource.Dispose();
    }

    private static HwndSource CreateRawInputSource()
    {
        HwndSourceParameters parameters = new("LockCatRawKeyboardInput")
        {
            Width = 0,
            Height = 0,
            WindowStyle = 0x800000
        };

        return new HwndSource(parameters);
    }

    private static void RegisterRawKeyboardInput(IntPtr handle)
    {
        RawInputDevice[] devices =
        [
            new()
            {
                UsagePage = 0x01,
                Usage = 0x06,
                Flags = RIDEV_INPUTSINK,
                Target = handle
            }
        ];

        RegisterRawInputDevices(devices, (uint)devices.Length, (uint)Marshal.SizeOf<RawInputDevice>());
    }

    private readonly record struct KeyboardInputStamp(int VirtualKey, int ScanCode, DateTime Timestamp);

    [StructLayout(LayoutKind.Sequential)]
    private struct KbdLlHookStruct
    {
        public int VirtualKey;
        public int ScanCode;
        public int Flags;
        public int Time;
        public IntPtr ExtraInfo;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct RawInputDevice
    {
        public ushort UsagePage;
        public ushort Usage;
        public int Flags;
        public IntPtr Target;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct RawInputHeader
    {
        public int Type;
        public int Size;
        public IntPtr Device;
        public IntPtr WParam;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct RawKeyboard
    {
        public ushort MakeCode;
        public ushort Flags;
        public ushort Reserved;
        public ushort VirtualKey;
        public uint Message;
        public uint ExtraInformation;
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

    [DllImport("user32.dll")]
    private static extern int GetSystemMetrics(int nIndex);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool RegisterRawInputDevices(RawInputDevice[] pRawInputDevices, uint uiNumDevices, uint cbSize);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern uint GetRawInputData(IntPtr hRawInput, uint uiCommand, IntPtr pData, ref uint pcbSize, uint cbSizeHeader);

    [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    private static extern uint GetRawInputDeviceInfo(IntPtr hDevice, uint uiCommand, IntPtr pData, ref uint pcbSize);
}
