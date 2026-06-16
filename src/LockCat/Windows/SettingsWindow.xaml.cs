using LockPig.Localization;
using LockPig.Models;
using LockPig.Services;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Threading;

namespace LockPig.Windows;

public partial class SettingsWindow : Window
{
    private readonly StartupService _startupService;
    private readonly DdcMonitorService _ddcMonitorService;
    private readonly Action<AppSettings> _saveRequested;
    private readonly Action<double>? _petOpacityPreviewRequested;
    private readonly DispatcherTimer _saveFeedbackTimer = new();
    private readonly DispatcherTimer _blinkTimer = new();
    private AppSettings _baselineSettings;
    private bool _baselineStartWithWindows;
    private double _baselinePetOpacity;
    private MonitorPowerMode _selectedMonitorPowerMode;
    private string _selectedLanguage = "zh-CN";
    private LocalizedStrings _strings = Strings.For("zh-CN");
    private bool _isInitializing;
    private bool _isDirty;
    private int _blinkStep;

    public SettingsWindow(
        AppSettings settings,
        StartupService startupService,
        DdcMonitorService ddcMonitorService,
        Action<AppSettings> saveRequested,
        Action<double>? petOpacityPreviewRequested = null)
    {
        InitializeComponent();
        _startupService = startupService;
        _ddcMonitorService = ddcMonitorService;
        _saveRequested = saveRequested;
        _petOpacityPreviewRequested = petOpacityPreviewRequested;
        _baselineSettings = CopySettings(settings);
        _baselineStartWithWindows = _startupService.IsEnabled();
        _baselinePetOpacity = Math.Clamp(settings.PetOpacity, 0.2, 1.0);
        _selectedLanguage = Strings.NormalizeLanguage(settings.Language);
        _strings = Strings.For(_selectedLanguage);

        LanguagePopup.CustomPopupPlacementCallback = PlaceLanguagePopup;
        MonitorModePopup.CustomPopupPlacementCallback = PlaceMonitorModePopup;

        _saveFeedbackTimer.Interval = TimeSpan.FromMilliseconds(1200);
        _saveFeedbackTimer.Tick += (_, _) =>
        {
            _saveFeedbackTimer.Stop();
            SaveButton.Content = _strings.SaveChanges;
        };

        _blinkTimer.Interval = TimeSpan.FromMilliseconds(180);
        _blinkTimer.Tick += (_, _) => AnimatePreviewBlink();

        _isInitializing = true;
        ApplySettingsToControls(_baselineSettings, _baselineStartWithWindows, markDirty: false);
        _isInitializing = false;
        ApplyLanguage();
        SetDirty(false);
    }

    public void ApplyLanguage(string language)
    {
        _selectedLanguage = Strings.NormalizeLanguage(language);
        _strings = Strings.For(_selectedLanguage);
        ApplyLanguage();
    }

    public void ApplyExternalPetVisible(bool visible)
    {
        _baselineSettings.PetVisible = visible;
        bool wasInitializing = _isInitializing;
        _isInitializing = true;
        ShowPetSwitch.IsChecked = visible;
        _isInitializing = wasInitializing;
    }

    private void ApplyLanguage()
    {
        Title = _strings.SettingsTitle;
        TitleText.Text = _strings.SettingsTitle;
        HeroSubtitleText.Text = _strings.HeroSubtitle;
        ShortcutSectionText.Text = _strings.ShortcutSection;
        ShortcutDescriptionText.Text = _strings.ShortcutDescription;
        LockHotkeyLabelText.Text = _strings.LockHotkey;
        UnlockHotkeyLabelText.Text = _strings.UnlockHotkey;
        LockBehaviorSectionText.Text = _strings.LockBehaviorSection;
        TurnOffMonitorText.Text = _strings.TurnOffMonitor;
        TurnOffMonitorDescriptionText.Text = _strings.TurnOffMonitorDescription;
        CatAppearanceSectionText.Text = _strings.CatAppearanceSection;
        ShowPetText.Text = _strings.ShowPet;
        ShowPetDescriptionText.Text = _strings.ShowPetDescription;
        AlwaysOnTopText.Text = _strings.AlwaysOnTop;
        AlwaysOnTopDescriptionText.Text = _strings.AlwaysOnTopDescription;
        StartupLanguageSectionText.Text = _strings.StartupLanguageSection;
        StartWithWindowsText.Text = _strings.StartWithWindows;
        InterfaceLanguageText.Text = _strings.InterfaceLanguage;
        CancelButton.Content = _strings.Cancel;
        if (!_isDirty)
        {
            SaveButton.Content = _strings.SaveChanges;
        }

        AutomationProperties.SetName(CancelButton, _strings.Cancel);
        AutomationProperties.SetName(SaveButton, _strings.SaveChanges);
        LockHotkeyEditor.ApplyLanguage(_selectedLanguage);
        UnlockHotkeyEditor.ApplyLanguage(_selectedLanguage);

        UpdateLanguageText();
        UpdateMonitorPowerModeText();
        SetDirty(_isDirty);
    }

    private void Window_Loaded(object sender, RoutedEventArgs e)
    {
        Opacity = 0;
        RootTranslate.Y = 8;

        BeginAnimation(OpacityProperty, new DoubleAnimation(1, TimeSpan.FromMilliseconds(200))
        {
            EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
        });

        RootTranslate.BeginAnimation(System.Windows.Media.TranslateTransform.YProperty, new DoubleAnimation(0, TimeSpan.FromMilliseconds(200))
        {
            EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
        });

        _blinkTimer.Start();
    }

    private void Window_Closing(object? sender, System.ComponentModel.CancelEventArgs e)
    {
        ClosePopups();
        _blinkTimer.Stop();
        _saveFeedbackTimer.Stop();

        if (_isDirty)
        {
            _petOpacityPreviewRequested?.Invoke(_baselinePetOpacity);
        }
    }

    private void Minimize_Click(object sender, RoutedEventArgs e)
    {
        WindowState = WindowState.Minimized;
    }

    private void Window_Deactivated(object sender, EventArgs e)
    {
        ClosePopups();
    }

    private void Window_StateChanged(object? sender, EventArgs e)
    {
        if (WindowState == WindowState.Minimized)
        {
            ClosePopups();
        }

        UpdateMaximizeIcon();
    }

    private void Window_PreviewKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
    {
        if (e.Key == Key.Escape && (LanguagePopup.IsOpen || MonitorModePopup.IsOpen))
        {
            ClosePopups();
            e.Handled = true;
        }
    }

    private void Window_PreviewMouseDown(object sender, MouseButtonEventArgs e)
    {
        if (!LanguagePopup.IsOpen && !MonitorModePopup.IsOpen)
        {
            return;
        }

        if (e.OriginalSource is DependencyObject source
            && (IsDescendantOf(source, LanguageAnchor) || IsDescendantOf(source, MonitorModeAnchor)))
        {
            return;
        }

        ClosePopups();
    }

    private void MaximizeRestore_Click(object sender, RoutedEventArgs e)
    {
        WindowState = WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;
        UpdateMaximizeIcon();
    }

    private void CloseWindow_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }

    private void Save_Click(object sender, RoutedEventArgs e)
    {
        if (!LockHotkeyEditor.CommitPendingText(out string lockError))
        {
            System.Windows.MessageBox.Show(this, lockError, _strings.LockHotkeyInvalidTitle, MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (!UnlockHotkeyEditor.CommitPendingText(out string unlockError))
        {
            System.Windows.MessageBox.Show(this, unlockError, _strings.UnlockHotkeyInvalidTitle, MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (LockHotkeyEditor.Gesture == UnlockHotkeyEditor.Gesture)
        {
            System.Windows.MessageBox.Show(this, _strings.HotkeyConflictMessage, _strings.HotkeyConflictTitle, MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        AppSettings settings = new()
        {
            LockHotkey = LockHotkeyEditor.Gesture,
            UnlockHotkey = UnlockHotkeyEditor.Gesture,
            TurnOffMonitorOnLock = TurnOffMonitorSwitch.IsChecked,
            MonitorPowerMode = _selectedMonitorPowerMode,
            PetVisible = ShowPetSwitch.IsChecked,
            PetAlwaysOnTop = AlwaysOnTopSwitch.IsChecked,
            PetOpacity = Math.Clamp(PetOpacitySlider.Value / 100, 0.2, 1.0),
            StartWithWindows = StartWithWindowsSwitch.IsChecked,
            Language = _selectedLanguage,
            PetTripleClickLockPromptCount = _baselineSettings.PetTripleClickLockPromptCount
        };

        _saveRequested(settings);
        _baselineSettings = CopySettings(settings);
        _baselineStartWithWindows = settings.StartWithWindows;
        _baselinePetOpacity = settings.PetOpacity;
        SaveButton.Content = _strings.Saved;
        _saveFeedbackTimer.Stop();
        _saveFeedbackTimer.Start();
        SetDirty(false);
    }

    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        ClosePopups();
        _saveFeedbackTimer.Stop();
        _petOpacityPreviewRequested?.Invoke(_baselinePetOpacity);
        ApplySettingsToControls(_baselineSettings, _baselineStartWithWindows, markDirty: false);
        ApplyLanguage();
        SetDirty(false);
    }

    private void SettingsValue_Changed(object? sender, EventArgs e)
    {
        MarkDirty();
    }

    private void PetOpacitySlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        UpdatePetOpacityText();

        if (!_isInitializing)
        {
            _petOpacityPreviewRequested?.Invoke(Math.Clamp(PetOpacitySlider.Value / 100, 0.2, 1.0));
        }

        MarkDirty();
    }

    private void LanguageAnchor_Click(object sender, RoutedEventArgs e)
    {
        MonitorModePopup.IsOpen = false;
        LanguagePopup.IsOpen = !LanguagePopup.IsOpen;
        e.Handled = true;
    }

    private void MonitorModeAnchor_Click(object sender, RoutedEventArgs e)
    {
        LanguagePopup.IsOpen = false;
        MonitorModePopup.IsOpen = !MonitorModePopup.IsOpen;
        e.Handled = true;
    }

    private void ClosePopups()
    {
        LanguagePopup.IsOpen = false;
        MonitorModePopup.IsOpen = false;
    }

    private void ChineseLanguage_Click(object sender, RoutedEventArgs e)
    {
        SelectLanguage("zh-CN", markDirty: true);
        LanguagePopup.IsOpen = false;
    }

    private void EnglishLanguage_Click(object sender, RoutedEventArgs e)
    {
        SelectLanguage("en-US", markDirty: true);
        LanguagePopup.IsOpen = false;
    }

    private void JapaneseLanguage_Click(object sender, RoutedEventArgs e)
    {
        SelectLanguage("ja-JP", markDirty: true);
        LanguagePopup.IsOpen = false;
    }

    private void HardwareDdc_Click(object sender, RoutedEventArgs e)
    {
        SelectMonitorPowerMode(MonitorPowerMode.HardwareDdc, markDirty: true);
        MonitorModePopup.IsOpen = false;
    }

    private void WindowsPowerMessage_Click(object sender, RoutedEventArgs e)
    {
        SelectMonitorPowerMode(MonitorPowerMode.WindowsPowerMessage, markDirty: true);
        MonitorModePopup.IsOpen = false;
    }

    private void DetectMonitor_Click(object sender, RoutedEventArgs e)
    {
        IReadOnlyList<MonitorCapability> capabilities = _ddcMonitorService.Detect();
        MonitorCapability? supported = capabilities.FirstOrDefault(capability => capability.SupportsPowerMode);
        string message = supported is not null
            ? string.Format(_strings.MonitorAvailable, supported.Model ?? supported.Description, supported.CurrentPowerMode)
            : _strings.MonitorUnavailable;

        System.Windows.MessageBox.Show(this, message, _strings.MonitorDetectTitle, MessageBoxButton.OK, MessageBoxImage.Information);
        MonitorModePopup.IsOpen = false;
    }

    private void SelectLanguage(string language, bool markDirty)
    {
        _selectedLanguage = Strings.NormalizeLanguage(language);
        _strings = Strings.For(_selectedLanguage);
        ApplyLanguage();

        if (markDirty)
        {
            MarkDirty();
        }
    }

    private void SelectMonitorPowerMode(MonitorPowerMode mode, bool markDirty)
    {
        _selectedMonitorPowerMode = mode;
        UpdateMonitorPowerModeText();

        if (markDirty)
        {
            MarkDirty();
        }
    }

    private void ApplySettingsToControls(AppSettings settings, bool startWithWindows, bool markDirty)
    {
        bool wasInitializing = _isInitializing;
        _isInitializing = true;

        LockHotkeyEditor.Gesture = settings.LockHotkey;
        UnlockHotkeyEditor.Gesture = settings.UnlockHotkey;
        TurnOffMonitorSwitch.IsChecked = settings.TurnOffMonitorOnLock;
        _selectedMonitorPowerMode = settings.MonitorPowerMode;
        ShowPetSwitch.IsChecked = settings.PetVisible;
        AlwaysOnTopSwitch.IsChecked = settings.PetAlwaysOnTop;
        StartWithWindowsSwitch.IsChecked = startWithWindows;
        PetOpacitySlider.Value = Math.Clamp(settings.PetOpacity, 0.2, 1.0) * 100;
        SelectLanguage(settings.Language, markDirty: false);
        UpdateMonitorPowerModeText();
        UpdatePetOpacityText();

        _isInitializing = wasInitializing;

        if (markDirty)
        {
            MarkDirty();
        }
    }

    private static AppSettings CopySettings(AppSettings settings)
    {
        return new AppSettings
        {
            LockHotkey = settings.LockHotkey,
            UnlockHotkey = settings.UnlockHotkey,
            TurnOffMonitorOnLock = settings.TurnOffMonitorOnLock,
            MonitorPowerMode = settings.MonitorPowerMode,
            PetVisible = settings.PetVisible,
            PetAlwaysOnTop = settings.PetAlwaysOnTop,
            PetOpacity = Math.Clamp(settings.PetOpacity, 0.2, 1.0),
            StartWithWindows = settings.StartWithWindows,
            Language = Strings.NormalizeLanguage(settings.Language),
            PetTripleClickLockPromptCount = settings.PetTripleClickLockPromptCount
        };
    }

    private void UpdateLanguageText()
    {
        LanguageValueText.Text = _selectedLanguage switch
        {
            "en-US" => _strings.English,
            "ja-JP" => _strings.Japanese,
            _ => _strings.Chinese
        };

        ChineseLanguageButton.Content = CheckedText(_selectedLanguage == "zh-CN", _strings.Chinese);
        EnglishLanguageButton.Content = CheckedText(_selectedLanguage == "en-US", _strings.English);
        JapaneseLanguageButton.Content = CheckedText(_selectedLanguage == "ja-JP", _strings.Japanese);
    }

    private void UpdateMonitorPowerModeText()
    {
        bool hardware = _selectedMonitorPowerMode == MonitorPowerMode.HardwareDdc;
        MonitorPowerModeValueText.Text = _strings.Advanced;
        HardwareDdcButton.Content = CheckedText(hardware, _strings.HardwareDdc);
        WindowsPowerMessageButton.Content = CheckedText(!hardware, _strings.WindowsPowerMessage);
        DetectMonitorButton.Content = $"  {_strings.RedetectMonitor}";
    }

    private static string CheckedText(bool selected, string text)
    {
        return selected ? $"✓ {text}" : $"  {text}";
    }

    private void UpdatePetOpacityText()
    {
        if (PetOpacityValueText is not null && PetOpacitySlider is not null)
        {
            PetOpacityValueText.Text = $"{Math.Round(PetOpacitySlider.Value):0}%";
        }
    }

    private void UpdateMaximizeIcon()
    {
        if (MaximizeIcon is not null)
        {
            MaximizeIcon.Kind = WindowState == WindowState.Maximized ? "WindowRestore" : "WindowMaximize";
        }
    }

    private void MarkDirty()
    {
        if (_isInitializing || DirtyStateText is null || SaveButton is null)
        {
            return;
        }

        SetDirty(true);
    }

    private void SetDirty(bool dirty)
    {
        if (DirtyStateText is null || SaveButton is null)
        {
            return;
        }

        _isDirty = dirty;
        DirtyStateText.Text = _isDirty ? _strings.UnsavedChanges : _strings.Synced;
        SaveButton.Content = _isDirty ? _strings.SaveChanges : SaveButton.Content;
        SaveButton.IsEnabled = _isDirty;
    }

    private void AnimatePreviewBlink()
    {
        _blinkStep = (_blinkStep + 1) % 18;

        if (_blinkStep == 0)
        {
            // Reserved for a future two-frame preview blink.
        }
    }

    private CustomPopupPlacement[] PlaceLanguagePopup(System.Windows.Size popupSize, System.Windows.Size targetSize, System.Windows.Point offset)
    {
        return PlaceFloatingMenu(LanguageAnchor, popupSize, targetSize);
    }

    private CustomPopupPlacement[] PlaceMonitorModePopup(System.Windows.Size popupSize, System.Windows.Size targetSize, System.Windows.Point offset)
    {
        return PlaceFloatingMenu(MonitorModeAnchor, popupSize, targetSize);
    }

    private static CustomPopupPlacement[] PlaceFloatingMenu(FrameworkElement anchor, System.Windows.Size popupSize, System.Windows.Size targetSize)
    {
        const double margin = 10;
        double x = Math.Min(0, targetSize.Width - popupSize.Width);
        double y = targetSize.Height + 6;

        PresentationSource? source = PresentationSource.FromVisual(anchor);
        if (source?.CompositionTarget is not null)
        {
            System.Windows.Point screenPixels = anchor.PointToScreen(new System.Windows.Point(0, 0));
            System.Windows.Point screen = source.CompositionTarget.TransformFromDevice.Transform(screenPixels);
            Rect workArea = SystemParameters.WorkArea;

            double popupBottom = screen.Y + y + popupSize.Height;
            if (popupBottom > workArea.Bottom - margin)
            {
                y -= popupBottom - (workArea.Bottom - margin);
            }

            double popupRight = screen.X + x + popupSize.Width;
            if (popupRight > workArea.Right - margin)
            {
                x -= popupRight - (workArea.Right - margin);
            }

            if (screen.X + x < workArea.Left + margin)
            {
                x += (workArea.Left + margin) - (screen.X + x);
            }
        }

        return
        [
            new CustomPopupPlacement(new System.Windows.Point(x, y), PopupPrimaryAxis.Horizontal)
        ];
    }

    private static bool IsDescendantOf(DependencyObject source, DependencyObject ancestor)
    {
        DependencyObject? current = source;
        while (current is not null)
        {
            if (ReferenceEquals(current, ancestor))
            {
                return true;
            }

            current = VisualTreeHelper.GetParent(current);
        }

        return false;
    }
}
