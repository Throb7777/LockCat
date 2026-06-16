using System.Windows.Input;

namespace LockPig.Models;

public sealed record HotkeyGesture(Key Key, ModifierKeys Modifiers)
{
    public static HotkeyGesture DefaultLock { get; } = new(Key.K, ModifierKeys.Control | ModifierKeys.Alt);
    public static HotkeyGesture DefaultUnlock { get; } = new(Key.P, ModifierKeys.Control | ModifierKeys.Alt);

    public override string ToString()
    {
        return string.Join("+", Parts());
    }

    public IReadOnlyList<string> Parts()
    {
        List<string> parts = [];

        if (Modifiers.HasFlag(ModifierKeys.Control))
        {
            parts.Add("Ctrl");
        }

        if (Modifiers.HasFlag(ModifierKeys.Alt))
        {
            parts.Add("Alt");
        }

        if (Modifiers.HasFlag(ModifierKeys.Shift))
        {
            parts.Add("Shift");
        }

        if (Modifiers.HasFlag(ModifierKeys.Windows))
        {
            parts.Add("Win");
        }

        parts.Add(FormatKey(Key));
        return parts;
    }

    private static string FormatKey(Key key)
    {
        if (key >= Key.A && key <= Key.Z)
        {
            return key.ToString().ToUpperInvariant();
        }

        if (key >= Key.D0 && key <= Key.D9)
        {
            return ((int)(key - Key.D0)).ToString();
        }

        return key.ToString();
    }
}
