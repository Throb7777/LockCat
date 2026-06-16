using LockPig.Localization;
using LockPig.Models;
using System.Windows.Input;

namespace LockPig.Controls;

public partial class HotkeyEditor : System.Windows.Controls.UserControl
{
    private readonly System.Windows.Controls.TextBox[] _slots;
    private HotkeyGesture _gesture = HotkeyGesture.DefaultLock;
    private string _language = "zh-CN";
    private bool _rendering;

    public event EventHandler? GestureChanged;

    public HotkeyEditor()
    {
        InitializeComponent();
        _slots = [SlotOne, SlotTwo, SlotThree];
        RenderGesture();
    }

    public HotkeyGesture Gesture
    {
        get => _gesture;
        set
        {
            _gesture = value;
            RenderGesture();
        }
    }

    public void ApplyLanguage(string language)
    {
        _language = Strings.NormalizeLanguage(language);
    }

    public bool CommitPendingText(out string error)
    {
        if (TryParseSlots(out HotkeyGesture parsed, out error))
        {
            _gesture = parsed;
            RenderGesture();
            GestureChanged?.Invoke(this, EventArgs.Empty);
            return true;
        }

        RenderGesture();
        return false;
    }

    private void Slot_PreviewKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
    {
        Key key = e.Key == Key.System ? e.SystemKey : e.Key;

        if (key is Key.Tab)
        {
            return;
        }

        if (key is Key.Back or Key.Delete)
        {
            ((System.Windows.Controls.TextBox)sender).Text = string.Empty;
            GestureChanged?.Invoke(this, EventArgs.Empty);
            e.Handled = true;
            return;
        }

        if (IsModifierKey(key))
        {
            ((System.Windows.Controls.TextBox)sender).Text = NormalizeModifierName(key);
            GestureChanged?.Invoke(this, EventArgs.Empty);
            e.Handled = true;
            return;
        }

        ModifierKeys modifiers = LimitModifiers(Keyboard.Modifiers);
        if (modifiers != ModifierKeys.None)
        {
            _gesture = new HotkeyGesture(key, modifiers);
            RenderGesture();
            GestureChanged?.Invoke(this, EventArgs.Empty);
            e.Handled = true;
        }
    }

    private void Slot_LostKeyboardFocus(object sender, System.Windows.Input.KeyboardFocusChangedEventArgs e)
    {
        if (_rendering)
        {
            return;
        }

        if (TryParseSlots(out HotkeyGesture parsed, out _))
        {
            _gesture = parsed;
            RenderGesture();
            GestureChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    private void HotkeyEditor_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.OriginalSource is System.Windows.Controls.TextBox)
        {
            return;
        }

        SlotOne.Focus();
        SlotOne.SelectAll();
    }

    private void RenderGesture()
    {
        _rendering = true;
        string[] parts = _gesture.Parts().Take(3).ToArray();

        for (int i = 0; i < _slots.Length; i++)
        {
            _slots[i].Text = i < parts.Length ? parts[i] : string.Empty;
        }

        _rendering = false;
    }

    private bool TryParseSlots(out HotkeyGesture gesture, out string error)
    {
        LocalizedStrings strings = Strings.For(_language);
        gesture = _gesture;
        error = string.Empty;

        string[] values = _slots
            .Select(slot => slot.Text.Trim())
            .Where(text => !string.IsNullOrWhiteSpace(text))
            .ToArray();

        if (values.Length == 0)
        {
            error = strings.HotkeyEmpty;
            return false;
        }

        if (values.Length > 3)
        {
            error = strings.HotkeyTooManyKeys;
            return false;
        }

        ModifierKeys modifiers = ModifierKeys.None;
        Key? mainKey = null;

        foreach (string value in values)
        {
            if (TryParseModifier(value, out ModifierKeys modifier))
            {
                modifiers |= modifier;
                continue;
            }

            if (!TryParseKey(value, out Key parsedKey))
            {
                error = string.Format(strings.HotkeyUnknownKey, value);
                return false;
            }

            if (mainKey is not null)
            {
                error = strings.HotkeyOnlyOneMainKey;
                return false;
            }

            mainKey = parsedKey;
        }

        if (modifiers == ModifierKeys.None || mainKey is null)
        {
            error = strings.HotkeyNeedsModifierAndMainKey;
            return false;
        }

        gesture = new HotkeyGesture(mainKey.Value, LimitModifiers(modifiers));
        return true;
    }

    private static bool TryParseModifier(string value, out ModifierKeys modifier)
    {
        modifier = value.Trim().ToUpperInvariant() switch
        {
            "CTRL" or "CONTROL" => ModifierKeys.Control,
            "ALT" => ModifierKeys.Alt,
            "SHIFT" => ModifierKeys.Shift,
            "WIN" or "WINDOWS" => ModifierKeys.Windows,
            _ => ModifierKeys.None
        };

        return modifier != ModifierKeys.None;
    }

    private static bool TryParseKey(string value, out Key key)
    {
        string normalized = value.Trim().Replace(" ", string.Empty).ToUpperInvariant();

        Dictionary<string, Key> aliases = new()
        {
            ["ESC"] = Key.Escape,
            ["ESCAPE"] = Key.Escape,
            ["DEL"] = Key.Delete,
            ["DELETE"] = Key.Delete,
            ["INS"] = Key.Insert,
            ["INSERT"] = Key.Insert,
            ["SPACE"] = Key.Space,
            ["TAB"] = Key.Tab,
            ["ENTER"] = Key.Enter,
            ["RETURN"] = Key.Return,
            ["BACKSPACE"] = Key.Back,
            ["PGUP"] = Key.PageUp,
            ["PAGEUP"] = Key.PageUp,
            ["PGDN"] = Key.PageDown,
            ["PAGEDOWN"] = Key.PageDown,
            ["UP"] = Key.Up,
            ["DOWN"] = Key.Down,
            ["LEFT"] = Key.Left,
            ["RIGHT"] = Key.Right,
            ["HOME"] = Key.Home,
            ["END"] = Key.End,
            ["PAUSE"] = Key.Pause
        };

        if (aliases.TryGetValue(normalized, out key))
        {
            return true;
        }

        if (normalized.Length == 1)
        {
            char c = normalized[0];
            if (c is >= 'A' and <= 'Z')
            {
                key = Key.A + (c - 'A');
                return true;
            }

            if (c is >= '0' and <= '9')
            {
                key = Key.D0 + (c - '0');
                return true;
            }
        }

        if (normalized.StartsWith('F') && int.TryParse(normalized[1..], out int functionKey) && functionKey is >= 1 and <= 24)
        {
            key = Key.F1 + (functionKey - 1);
            return true;
        }

        key = Key.None;
        return false;
    }

    private static ModifierKeys LimitModifiers(ModifierKeys modifiers)
    {
        ModifierKeys limited = ModifierKeys.None;
        int count = 0;

        AddIfPresent(ModifierKeys.Control);
        AddIfPresent(ModifierKeys.Alt);
        AddIfPresent(ModifierKeys.Shift);
        AddIfPresent(ModifierKeys.Windows);

        return limited;

        void AddIfPresent(ModifierKeys modifier)
        {
            if (count >= 2 || !modifiers.HasFlag(modifier))
            {
                return;
            }

            limited |= modifier;
            count++;
        }
    }

    private static bool IsModifierKey(Key key)
    {
        return key is Key.LeftCtrl or Key.RightCtrl or Key.LeftAlt or Key.RightAlt or Key.LeftShift or Key.RightShift or Key.LWin or Key.RWin;
    }

    private static string NormalizeModifierName(Key key)
    {
        return key switch
        {
            Key.LeftCtrl or Key.RightCtrl => "Ctrl",
            Key.LeftAlt or Key.RightAlt => "Alt",
            Key.LeftShift or Key.RightShift => "Shift",
            Key.LWin or Key.RWin => "Win",
            _ => key.ToString()
        };
    }
}
