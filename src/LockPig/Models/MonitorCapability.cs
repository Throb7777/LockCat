namespace LockPig.Models;

public sealed record MonitorCapability(
    string Description,
    string? Model,
    bool SupportsDdc,
    bool SupportsPowerMode,
    uint CurrentPowerMode,
    string? Error);
