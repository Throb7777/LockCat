using LockPig.Models;
using LockPig.Services;

namespace LockPig;

public sealed class AppState
{
    private AppSettings _settings;
    private readonly KeyboardHookService _keyboardHookService;
    private readonly MonitorService _monitorService;
    private readonly DdcMonitorService _ddcMonitorService;
    private bool _hardwareMonitorOffActive;

    public AppState(AppSettings settings, KeyboardHookService keyboardHookService, MonitorService monitorService, DdcMonitorService ddcMonitorService)
    {
        _settings = settings;
        _keyboardHookService = keyboardHookService;
        _monitorService = monitorService;
        _ddcMonitorService = ddcMonitorService;
    }

    public LockState CurrentState { get; private set; } = LockState.Normal;

    public event Action<LockState>? StateChanged;

    public void ApplySettings(AppSettings settings)
    {
        _settings = settings;
    }

    public void Lock()
    {
        if (CurrentState == LockState.Locked)
        {
            return;
        }

        CurrentState = LockState.Locked;
        _keyboardHookService.SetLocked(true);
        StateChanged?.Invoke(CurrentState);

        if (_settings.TurnOffMonitorOnLock)
        {
            if (_settings.MonitorPowerMode == MonitorPowerMode.HardwareDdc)
            {
                _hardwareMonitorOffActive = _ddcMonitorService.TryTurnOffHardware();
                if (!_hardwareMonitorOffActive)
                {
                    _monitorService.TurnOff();
                }
            }
            else
            {
                _monitorService.TurnOff();
            }
        }
    }

    public void Unlock()
    {
        if (CurrentState == LockState.Normal)
        {
            return;
        }

        if (_hardwareMonitorOffActive || _settings.MonitorPowerMode == MonitorPowerMode.HardwareDdc)
        {
            _ddcMonitorService.TurnOnAndRelease();
            _hardwareMonitorOffActive = false;
        }

        _monitorService.TurnOn();
        _keyboardHookService.SetLocked(false);
        CurrentState = LockState.Normal;
        StateChanged?.Invoke(CurrentState);
    }
}
