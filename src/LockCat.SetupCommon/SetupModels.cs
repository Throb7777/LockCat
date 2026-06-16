namespace LockCat.SetupCommon;

public sealed record InstallOptions
{
    public required string InstallDirectory { get; init; }
    public required string PayloadDirectory { get; init; }
    public required string UninstallerPath { get; init; }
    public bool CreateDesktopShortcut { get; init; }
    public bool StartWithWindows { get; init; } = true;
    public bool StartAfterInstall { get; init; } = true;
    public bool AddStartMenuShortcut { get; init; } = true;
}

public sealed record UninstallOptions
{
    public required string InstallDirectory { get; init; }
    public bool KeepSettings { get; init; } = true;
}

public sealed record SetupProgress(int Percent, string Message);

public sealed record InstallManifest
{
    public string ProductId { get; init; } = "LockCat";
    public string Version { get; init; } = SetupText.Version;
    public string InstallDirectory { get; init; } = string.Empty;
    public bool DesktopShortcut { get; init; }
    public bool StartWithWindows { get; init; }
    public DateTimeOffset InstalledAt { get; init; } = DateTimeOffset.Now;
    public IReadOnlyList<string> InstalledFiles { get; init; } = [];
    public IReadOnlyList<string> InstalledDirectories { get; init; } = [];
}
