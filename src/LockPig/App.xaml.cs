using LockPig.Models;
using LockPig.Services;
using LockPig.Windows;
using Microsoft.Win32;
using System.IO;
using System.IO.Pipes;
using System.Text;
using System.Threading;
using System.Windows;

namespace LockPig;

public partial class App : System.Windows.Application
{
    private const string SingleInstanceMutexName = @"Global\LockCat.SingleInstance";
    private const string SingleInstancePipeName = "LockCat.SingleInstance.Pipe";
    private Mutex? _singleInstanceMutex;
    private CancellationTokenSource? _singleInstanceCts;
    private bool _ownsSingleInstance;
    private AppSettings _settings = new();
    private SettingsStore? _settingsStore;
    private StartupService? _startupService;
    private HotkeyService? _hotkeyService;
    private KeyboardHookService? _keyboardHookService;
    private MonitorService? _monitorService;
    private DdcMonitorService? _ddcMonitorService;
    private TrayService? _trayService;
    private AppState? _appState;
    private PetWindow? _petWindow;
    private SettingsWindow? _settingsWindow;

    protected override void OnStartup(StartupEventArgs e)
    {
        try
        {
            _singleInstanceMutex = new Mutex(initiallyOwned: true, SingleInstanceMutexName, out bool createdNew);
            if (!createdNew)
            {
                NotifyExistingInstance(e.Args);
                Shutdown(0);
                return;
            }
            _ownsSingleInstance = true;

            ApplySystemTheme();
            SystemEvents.UserPreferenceChanged += SystemEvents_UserPreferenceChanged;

            base.OnStartup(e);
            StartSingleInstanceServer();

            _settingsStore = new SettingsStore();
            _settings = _settingsStore.Load();
            if (e.Args.Any(arg => string.Equals(arg, "--no-monitor-off", StringComparison.OrdinalIgnoreCase)))
            {
                _settings.TurnOffMonitorOnLock = false;
            }

            _startupService = new StartupService();
            _monitorService = new MonitorService();
            _ddcMonitorService = new DdcMonitorService();
            _keyboardHookService = new KeyboardHookService(() => _settings.UnlockHotkey);
            _appState = new AppState(_settings, _keyboardHookService, _monitorService, _ddcMonitorService);

            _petWindow = new PetWindow(_appState, _settings, ShowSettings, HidePet, ExitApplication);
            if (_settings.PetVisible)
            {
                _petWindow.Show();
            }
            _keyboardHookService.KeyboardActivityDetected += OnKeyboardActivityDetected;

            _hotkeyService = new HotkeyService();
            _hotkeyService.HotkeyPressed += OnHotkeyPressed;
            RegisterHotkeys();

            _trayService = new TrayService(
                lockRequested: () => _appState.Lock(),
                unlockRequested: () => _appState.Unlock(),
                settingsRequested: ShowSettings,
                showPetRequested: ShowPet,
                hidePetRequested: HidePet,
                exitRequested: ExitApplication,
                language: _settings.Language,
                petVisible: _settings.PetVisible);
            _trayService.UpdateState(_appState.CurrentState);

            _appState.StateChanged += state =>
            {
                _trayService.UpdateState(state);
                _petWindow.SetMood(state == LockState.Locked ? PetMood.Locked : PetMood.Recovered);
            };

            if (e.Args.Any(arg => string.Equals(arg, "--open-settings", StringComparison.OrdinalIgnoreCase)))
            {
                ShowSettings();
            }
        }
        catch (Exception ex)
        {
            File.WriteAllText(Path.Combine(Path.GetTempPath(), "LockCatStartup.log"), ex.ToString());
            Shutdown(1);
        }
    }

    private static void NotifyExistingInstance(string[] args)
    {
        try
        {
            using NamedPipeClientStream pipe = new(".", SingleInstancePipeName, PipeDirection.Out);
            pipe.Connect(600);
            string message = args.Length == 0 ? "--open-settings" : string.Join('\n', args);
            byte[] payload = Encoding.UTF8.GetBytes(message);
            pipe.Write(payload, 0, payload.Length);
        }
        catch
        {
            // The existing instance is starting or shutting down. In either case, do not launch a duplicate.
        }
    }

    private void StartSingleInstanceServer()
    {
        _singleInstanceCts = new CancellationTokenSource();
        _ = Task.Run(async () =>
        {
            while (!_singleInstanceCts.IsCancellationRequested)
            {
                try
                {
                    using NamedPipeServerStream pipe = new(
                        SingleInstancePipeName,
                        PipeDirection.In,
                        1,
                        PipeTransmissionMode.Byte,
                        PipeOptions.Asynchronous);
                    await pipe.WaitForConnectionAsync(_singleInstanceCts.Token);
                    using MemoryStream buffer = new();
                    await pipe.CopyToAsync(buffer, _singleInstanceCts.Token);
                    string message = Encoding.UTF8.GetString(buffer.ToArray());
                    _ = Dispatcher.BeginInvoke(() => HandleSecondInstanceActivation(message));
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch
                {
                    await Task.Delay(250);
                }
            }
        });
    }

    private void HandleSecondInstanceActivation(string message)
    {
        ShowPet();
        ShowSettings();
    }

    private void OnHotkeyPressed(HotkeyAction action)
    {
        if (_appState is null)
        {
            return;
        }

        if (action == HotkeyAction.Lock)
        {
            _appState.Lock();
        }
        else
        {
            _appState.Unlock();
        }
    }

    private void OnKeyboardActivityDetected()
    {
        Dispatcher.BeginInvoke(() => _petWindow?.NotifyKeyboardActivity());
    }

    private void RegisterHotkeys()
    {
        if (_hotkeyService is null)
        {
            return;
        }

        _hotkeyService.Register(_settings.LockHotkey, HotkeyAction.Lock);
        _hotkeyService.Register(_settings.UnlockHotkey, HotkeyAction.Unlock);
    }

    private void ShowSettings()
    {
        if (_settingsWindow is { IsVisible: true })
        {
            _settingsWindow.Activate();
            _petWindow?.NotifySettingsOpened();
            return;
        }

        _settingsWindow = new SettingsWindow(
            _settings,
            _startupService!,
            _ddcMonitorService!,
            SaveSettings,
            opacity => _petWindow!.Opacity = opacity);
        _settingsWindow.Show();
        _settingsWindow.Activate();
        _petWindow?.NotifySettingsOpened();
    }

    private void ShowPet()
    {
        if (_petWindow is null || _settingsStore is null)
        {
            return;
        }

        _settings.PetVisible = true;
        _settingsStore.Save(_settings);
        _petWindow.ApplySettings(_settings);
        _petWindow.Show();
        _petWindow.Activate();
        _trayService?.UpdatePetVisibility(true);
        _settingsWindow?.ApplyExternalPetVisible(true);
    }

    private void HidePet()
    {
        if (_petWindow is null || _settingsStore is null)
        {
            return;
        }

        _settings.PetVisible = false;
        _settingsStore.Save(_settings);
        _petWindow.Hide();
        _trayService?.UpdatePetVisibility(false);
        _settingsWindow?.ApplyExternalPetVisible(false);
    }

    private void SaveSettings(AppSettings updated)
    {
        _settings = updated;
        _settingsStore!.Save(_settings);
        _startupService!.SetEnabled(_settings.StartWithWindows);
        _petWindow!.ApplySettings(_settings);
        if (_settings.PetVisible)
        {
            _petWindow.Show();
        }
        else
        {
            _petWindow.Hide();
        }

        _trayService!.ApplyLanguage(_settings.Language);
        _trayService.UpdatePetVisibility(_settings.PetVisible);
        _appState!.ApplySettings(_settings);
        _hotkeyService!.UnregisterAll();
        RegisterHotkeys();
        _petWindow!.NotifySettingsSaved();
    }

    private void ExitApplication()
    {
        _appState?.Unlock();
        _hotkeyService?.Dispose();
        _keyboardHookService?.Dispose();
        _ddcMonitorService?.Dispose();
        _trayService?.Dispose();
        _settingsWindow?.Close();
        _petWindow?.Close();
        Shutdown();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _singleInstanceCts?.Cancel();
        _singleInstanceCts?.Dispose();
        SystemEvents.UserPreferenceChanged -= SystemEvents_UserPreferenceChanged;
        _appState?.Unlock();
        _hotkeyService?.Dispose();
        _keyboardHookService?.Dispose();
        _ddcMonitorService?.Dispose();
        _trayService?.Dispose();
        if (_ownsSingleInstance)
        {
            _singleInstanceMutex?.ReleaseMutex();
        }
        _singleInstanceMutex?.Dispose();
        base.OnExit(e);
    }

    private void SystemEvents_UserPreferenceChanged(object sender, UserPreferenceChangedEventArgs e)
    {
        if (e.Category is UserPreferenceCategory.General or UserPreferenceCategory.Color)
        {
            Dispatcher.Invoke(ApplySystemTheme);
        }
    }

    private void ApplySystemTheme()
    {
        bool useLightTheme = true;

        try
        {
            using RegistryKey? key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize");
            object? value = key?.GetValue("AppsUseLightTheme");
            useLightTheme = value is not int intValue || intValue != 0;
        }
        catch
        {
            useLightTheme = true;
        }

        string themeSource = useLightTheme
            ? "Themes/LockCat.Light.xaml"
            : "Themes/LockCat.Dark.xaml";

        ResourceDictionary dictionaries = Resources;
        ResourceDictionary? existingTheme = dictionaries.MergedDictionaries
            .FirstOrDefault(dictionary => dictionary.Source is not null && dictionary.Source.OriginalString.Contains("LockCat."));

        ResourceDictionary newTheme = new()
        {
            Source = new Uri(themeSource, UriKind.Relative)
        };

        if (existingTheme is null)
        {
            dictionaries.MergedDictionaries.Insert(0, newTheme);
        }
        else
        {
            int index = dictionaries.MergedDictionaries.IndexOf(existingTheme);
            dictionaries.MergedDictionaries[index] = newTheme;
        }
    }
}
