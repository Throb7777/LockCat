using LockPig.Models;
using System.Runtime.InteropServices;
using System.Windows.Input;
using System.Windows.Interop;

namespace LockPig.Services;

public sealed class HotkeyService : IDisposable
{
    private readonly HwndSource _source;
    private readonly Dictionary<int, HotkeyAction> _actions = [];
    private int _nextId = 100;

    public HotkeyService()
    {
        HwndSourceParameters parameters = new("LockCatHotkeys")
        {
            Width = 0,
            Height = 0,
            WindowStyle = 0x800000
        };

        _source = new HwndSource(parameters);
        _source.AddHook(WndProc);
    }

    public event Action<HotkeyAction>? HotkeyPressed;

    public void Register(HotkeyGesture gesture, HotkeyAction action)
    {
        int id = _nextId++;
        uint modifiers = ToNativeModifiers(gesture.Modifiers);
        uint virtualKey = (uint)KeyInterop.VirtualKeyFromKey(gesture.Key);

        if (RegisterHotKey(_source.Handle, id, modifiers, virtualKey))
        {
            _actions[id] = action;
        }
    }

    public void UnregisterAll()
    {
        foreach (int id in _actions.Keys.ToArray())
        {
            UnregisterHotKey(_source.Handle, id);
        }

        _actions.Clear();
    }

    private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        const int WM_HOTKEY = 0x0312;

        if (msg == WM_HOTKEY && _actions.TryGetValue(wParam.ToInt32(), out HotkeyAction action))
        {
            HotkeyPressed?.Invoke(action);
            handled = true;
        }

        return IntPtr.Zero;
    }

    private static uint ToNativeModifiers(ModifierKeys modifiers)
    {
        uint value = 0;

        if (modifiers.HasFlag(ModifierKeys.Alt))
        {
            value |= 0x0001;
        }

        if (modifiers.HasFlag(ModifierKeys.Control))
        {
            value |= 0x0002;
        }

        if (modifiers.HasFlag(ModifierKeys.Shift))
        {
            value |= 0x0004;
        }

        if (modifiers.HasFlag(ModifierKeys.Windows))
        {
            value |= 0x0008;
        }

        return value;
    }

    public void Dispose()
    {
        UnregisterAll();
        _source.RemoveHook(WndProc);
        _source.Dispose();
    }

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool UnregisterHotKey(IntPtr hWnd, int id);
}
