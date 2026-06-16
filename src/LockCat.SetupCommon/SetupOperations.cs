using Microsoft.Win32;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Security.Principal;
using System.Text.Json;

namespace LockCat.SetupCommon;

public static class SetupOperations
{
    public const string ProductName = "LockCat";
    public const string MainExecutable = "LockCat.exe";
    public const string UninstallerExecutable = "LockCatUninstaller.exe";
    public const string ManifestFile = "install.json";
    public const string RepositoryUrl = "https://github.com/Throb7777/LockCat";
    public const string ProjectReadmeUrl = RepositoryUrl + "#readme";
    public const string FeedbackUrl = RepositoryUrl + "/issues/new/choose";
    private const string StartupValueName = "LockCat";
    private const string UninstallKeyPath = @"Software\Microsoft\Windows\CurrentVersion\Uninstall\LockCat";
    private static readonly StringComparer PathComparer = StringComparer.OrdinalIgnoreCase;
    private static readonly string[] LegacyRootFiles =
    [
        MainExecutable,
        "LockCat.dll",
        "LockCat.deps.json",
        "LockCat.runtimeconfig.json",
        "LockCat.pdb",
        UninstallerExecutable
    ];
    private static readonly string[] LegacyAssetFiles =
    [
        @"Assets\Fonts\fusion-pixel-12px-proportional-zh_hans.ttf",
        @"Assets\Fonts\FusionPixelFont-LICENSE-OFL.txt",
        @"Assets\Fonts\FusionPixelFont-OFL.txt",
        @"Assets\Fonts\FusionPixelFont-Upstream-LICENSE\ark-pixel.txt",
        @"Assets\Fonts\FusionPixelFont-Upstream-LICENSE\cubic-11.txt",
        @"Assets\Fonts\FusionPixelFont-Upstream-LICENSE\galmuri.txt",
        @"Assets\Icons\LockCat.ico",
        @"Assets\Icons\LockCatInstaller.ico",
        @"Assets\Icons\LockCatUninstaller.ico",
        @"Assets\Pixel\cat-mini-24.png",
        @"Assets\Pixel\target-logo.png",
        @"Assets\Illustrations\install-complete.png",
        @"Assets\Illustrations\install-options.png",
        @"Assets\Illustrations\install-progress.png",
        @"Assets\Illustrations\install-welcome.png",
        @"Assets\Illustrations\uninstall-complete.png",
        @"Assets\Illustrations\uninstall-confirm.png",
        @"Assets\Illustrations\uninstall-progress.png"
    ];

    public static string DefaultInstallDirectory()
    {
        return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), ProductName);
    }

    public static string NormalizeInstallDirectoryForInstall(string selectedDirectory)
    {
        string fullPath = Path.GetFullPath(Environment.ExpandEnvironmentVariables(selectedDirectory))
            .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        string name = Path.GetFileName(fullPath);
        return string.Equals(name, ProductName, StringComparison.OrdinalIgnoreCase)
            ? fullPath
            : Path.Combine(fullPath, ProductName);
    }

    public static bool IsAdministrator()
    {
        using WindowsIdentity identity = WindowsIdentity.GetCurrent();
        WindowsPrincipal principal = new(identity);
        return principal.IsInRole(WindowsBuiltInRole.Administrator);
    }

    public static bool NeedsElevation(string installDirectory)
    {
        string programFiles = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
        string fullInstallDirectory = Path.GetFullPath(installDirectory);
        return IsPathInsideOrEqual(fullInstallDirectory, programFiles) && !IsAdministrator();
    }

    public static void RelaunchElevated(string arguments)
    {
        ProcessStartInfo info = new()
        {
            FileName = Environment.ProcessPath!,
            Arguments = arguments,
            UseShellExecute = true,
            Verb = "runas"
        };
        Process.Start(info);
    }

    public static async Task InstallAsync(InstallOptions options, IProgress<SetupProgress>? progress = null, CancellationToken cancellationToken = default)
    {
        await Task.Run(() =>
        {
            string installDirectory = NormalizeInstallDirectoryForInstall(options.InstallDirectory);
            List<string> installedFiles = [];
            SortedSet<string> installedDirectories = new(PathComparer);

            cancellationToken.ThrowIfCancellationRequested();
            progress?.Report(new SetupProgress(8, "Preparing folders"));
            Directory.CreateDirectory(installDirectory);

            if (!Directory.Exists(options.PayloadDirectory))
            {
                throw new DirectoryNotFoundException($"Payload directory not found: {options.PayloadDirectory}");
            }

            cancellationToken.ThrowIfCancellationRequested();
            progress?.Report(new SetupProgress(24, "Copying files"));
            CopyDirectory(options.PayloadDirectory, installDirectory, installedFiles, installedDirectories, cancellationToken);

            if (File.Exists(options.UninstallerPath))
            {
                File.Copy(options.UninstallerPath, Path.Combine(installDirectory, UninstallerExecutable), overwrite: true);
                AddInstalledFile(installedFiles, UninstallerExecutable);
                CopySetupAssets(options.UninstallerPath, installDirectory, installedFiles, installedDirectories, cancellationToken);
            }

            string installedExe = Path.Combine(installDirectory, MainExecutable);
            if (!File.Exists(installedExe))
            {
                throw new FileNotFoundException("Installed LockCat executable was not found.", installedExe);
            }

            cancellationToken.ThrowIfCancellationRequested();
            progress?.Report(new SetupProgress(52, "Creating shortcuts"));
            if (options.CreateDesktopShortcut)
            {
                CreateShortcut(
                    Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory), "LockCat.lnk"),
                    installedExe,
                    installDirectory,
                    "LockCat");
            }
            else
            {
                DeleteIfExists(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory), "LockCat.lnk"));
            }

            if (options.AddStartMenuShortcut)
            {
                string startMenuDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.StartMenu), "Programs", "LockCat");
                Directory.CreateDirectory(startMenuDir);
                CreateShortcut(Path.Combine(startMenuDir, "LockCat.lnk"), installedExe, installDirectory, "LockCat");
                CreateShortcut(
                    Path.Combine(startMenuDir, "Uninstall LockCat.lnk"),
                    Path.Combine(installDirectory, UninstallerExecutable),
                    installDirectory,
                    "Uninstall LockCat");
            }

            cancellationToken.ThrowIfCancellationRequested();
            progress?.Report(new SetupProgress(72, "Writing settings"));
            SetStartup(options.StartWithWindows, installedExe);
            WriteUninstallEntry(installDirectory);
            WriteManifest(options, installDirectory, installedFiles, installedDirectories);

            progress?.Report(new SetupProgress(100, "Complete"));
        }, cancellationToken);
    }

    public static async Task UninstallAsync(UninstallOptions options, IProgress<SetupProgress>? progress = null, CancellationToken cancellationToken = default)
    {
        await Task.Run(() =>
        {
            cancellationToken.ThrowIfCancellationRequested();
            progress?.Report(new SetupProgress(10, "Closing LockCat"));
            CloseRunningLockCat(options.InstallDirectory);

            cancellationToken.ThrowIfCancellationRequested();
            progress?.Report(new SetupProgress(28, "Removing shortcuts"));
            DeleteIfExists(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory), "LockCat.lnk"));
            string startMenuDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.StartMenu), "Programs", "LockCat");
            if (Directory.Exists(startMenuDir))
            {
                Directory.Delete(startMenuDir, recursive: true);
            }

            cancellationToken.ThrowIfCancellationRequested();
            progress?.Report(new SetupProgress(48, "Removing registry entries"));
            SetStartup(false, string.Empty);
            DeleteUninstallEntry();

            cancellationToken.ThrowIfCancellationRequested();
            progress?.Report(new SetupProgress(72, "Removing files"));
            DeleteInstallDirectory(options.InstallDirectory);

            if (!options.KeepSettings)
            {
                string settingsDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "LockCat");
                if (Directory.Exists(settingsDir))
                {
                    Directory.Delete(settingsDir, recursive: true);
                }
            }

            progress?.Report(new SetupProgress(100, "Complete"));
        }, cancellationToken);
    }

    public static void LaunchInstalledApp(string installDirectory)
    {
        string exe = Path.Combine(installDirectory, MainExecutable);
        if (File.Exists(exe))
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = exe,
                WorkingDirectory = installDirectory,
                UseShellExecute = true
            });
        }
    }

    public static void OpenExternal(string target)
    {
        if (string.IsNullOrWhiteSpace(target))
        {
            return;
        }

        Process.Start(new ProcessStartInfo
        {
            FileName = target,
            UseShellExecute = true
        });
    }

    public static string GuideFileName(string language)
    {
        return language.StartsWith("zh", StringComparison.OrdinalIgnoreCase) ? "README.zh-CN.md" :
            language.StartsWith("ja", StringComparison.OrdinalIgnoreCase) ? "README.ja.md" :
            "README.md";
    }

    public static string GuideUrl(string language)
    {
        if (language.StartsWith("zh", StringComparison.OrdinalIgnoreCase))
        {
            return RepositoryUrl + "/blob/main/README.zh-CN.md";
        }

        if (language.StartsWith("ja", StringComparison.OrdinalIgnoreCase))
        {
            return RepositoryUrl + "/blob/main/README.ja.md";
        }

        return ProjectReadmeUrl;
    }

    public static string ResolveInstallDirectoryFromArgs(string[] args)
    {
        string? fromArgs = ReadOption(args, "--install-dir");
        if (!string.IsNullOrWhiteSpace(fromArgs))
        {
            return ResolveInstallDirectoryCandidate(fromArgs);
        }

        string current = AppContext.BaseDirectory.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        if (File.Exists(Path.Combine(current, MainExecutable)) || File.Exists(Path.Combine(current, ManifestFile)))
        {
            return current;
        }

        return DefaultInstallDirectory();
    }

    private static string ResolveInstallDirectoryCandidate(string candidate)
    {
        string fullCandidate = Path.GetFullPath(Environment.ExpandEnvironmentVariables(candidate))
            .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        string child = Path.Combine(fullCandidate, ProductName);
        if (Directory.Exists(child) &&
            (File.Exists(Path.Combine(child, ManifestFile)) || File.Exists(Path.Combine(child, MainExecutable))))
        {
            return child;
        }

        return fullCandidate;
    }

    public static string? ReadOption(string[] args, string name)
    {
        string prefix = name + "=";
        foreach (string arg in args)
        {
            if (arg.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            {
                return arg[prefix.Length..];
            }
        }

        for (int i = 0; i < args.Length - 1; i++)
        {
            if (string.Equals(args[i], name, StringComparison.OrdinalIgnoreCase))
            {
                return args[i + 1];
            }
        }

        return null;
    }

    public static bool HasFlag(string[] args, string name)
    {
        return args.Any(arg => string.Equals(arg, name, StringComparison.OrdinalIgnoreCase));
    }

    private static void CopyDirectory(
        string source,
        string destination,
        ICollection<string> installedFiles,
        ISet<string> installedDirectories,
        CancellationToken cancellationToken,
        string? relativePrefix = null)
    {
        foreach (string directory in Directory.EnumerateDirectories(source, "*", SearchOption.AllDirectories))
        {
            cancellationToken.ThrowIfCancellationRequested();
            string relative = Path.GetRelativePath(source, directory);
            Directory.CreateDirectory(Path.Combine(destination, relative));
            AddInstalledDirectory(installedDirectories, CombineRelativePath(relativePrefix, relative));
        }

        foreach (string file in Directory.EnumerateFiles(source, "*", SearchOption.AllDirectories))
        {
            cancellationToken.ThrowIfCancellationRequested();
            string relative = Path.GetRelativePath(source, file);
            string target = Path.Combine(destination, relative);
            Directory.CreateDirectory(Path.GetDirectoryName(target)!);
            File.Copy(file, target, overwrite: true);
            AddInstalledFile(installedFiles, CombineRelativePath(relativePrefix, relative));
        }
    }

    private static void CopySetupAssets(
        string setupExecutable,
        string installDirectory,
        ICollection<string> installedFiles,
        ISet<string> installedDirectories,
        CancellationToken cancellationToken)
    {
        string? setupDirectory = Path.GetDirectoryName(setupExecutable);
        if (string.IsNullOrWhiteSpace(setupDirectory))
        {
            return;
        }

        string sourceAssets = Path.Combine(setupDirectory, "Assets");
        string targetAssets = Path.Combine(installDirectory, "Assets");
        if (!Directory.Exists(sourceAssets) ||
            string.Equals(Path.GetFullPath(sourceAssets), Path.GetFullPath(targetAssets), StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        CopyDirectory(sourceAssets, targetAssets, installedFiles, installedDirectories, cancellationToken, "Assets");
        AddInstalledDirectory(installedDirectories, "Assets");
    }

    private static void DeleteInstallDirectory(string installDirectory)
    {
        string fullInstallDirectory = Path.GetFullPath(installDirectory)
            .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        if (!Directory.Exists(fullInstallDirectory))
        {
            return;
        }

        InstallManifest? manifest = ReadManifest(fullInstallDirectory);
        if (manifest is not null &&
            string.Equals(manifest.ProductId, ProductName, StringComparison.OrdinalIgnoreCase) &&
            PathsEqual(manifest.InstallDirectory, fullInstallDirectory) &&
            manifest.InstalledFiles.Count > 0)
        {
            DeleteManifestFiles(fullInstallDirectory, manifest);
            return;
        }

        DeleteLegacyLockCatFiles(fullInstallDirectory);
    }

    private static void DeleteManifestFiles(string installDirectory, InstallManifest manifest)
    {
        string currentExe = Environment.ProcessPath ?? string.Empty;
        foreach (string relative in manifest.InstalledFiles.Distinct(PathComparer))
        {
            string? file = ResolveSafeInstalledPath(installDirectory, relative);
            if (file is null || !File.Exists(file))
            {
                continue;
            }

            if (PathsEqual(file, currentExe))
            {
                continue;
            }

            File.SetAttributes(file, FileAttributes.Normal);
            File.Delete(file);
        }

        DeleteIfSafeInstalledFile(installDirectory, ManifestFile);

        foreach (string relative in manifest.InstalledDirectories.Distinct(PathComparer).OrderByDescending(path => path.Length))
        {
            string? dir = ResolveSafeInstalledPath(installDirectory, relative);
            if (dir is not null)
            {
                DeleteDirectoryIfEmpty(dir);
            }
        }

        DeleteDirectoryIfEmpty(installDirectory);
    }

    private static void DeleteLegacyLockCatFiles(string installDirectory)
    {
        foreach (string relative in LegacyRootFiles.Concat(LegacyAssetFiles))
        {
            DeleteIfSafeInstalledFile(installDirectory, relative);
        }

        string[] legacyDirs =
        [
            @"Assets\Fonts\FusionPixelFont-Upstream-LICENSE",
            @"Assets\Illustrations",
            @"Assets\Icons",
            @"Assets\Pixel",
            @"Assets\Fonts",
            "Assets"
        ];
        foreach (string relative in legacyDirs)
        {
            string? dir = ResolveSafeInstalledPath(installDirectory, relative);
            if (dir is not null)
            {
                DeleteDirectoryIfEmpty(dir);
            }
        }

        DeleteDirectoryIfEmpty(installDirectory);
    }

    private static void WriteManifest(
        InstallOptions options,
        string installDirectory,
        ICollection<string> installedFiles,
        ISet<string> installedDirectories)
    {
        AddInstalledFile(installedFiles, ManifestFile);
        InstallManifest manifest = new()
        {
            InstallDirectory = installDirectory,
            DesktopShortcut = options.CreateDesktopShortcut,
            StartWithWindows = options.StartWithWindows,
            InstalledFiles = installedFiles.Distinct(PathComparer).OrderBy(path => path, PathComparer).ToArray(),
            InstalledDirectories = installedDirectories.Distinct(PathComparer).OrderBy(path => path, PathComparer).ToArray()
        };

        string json = JsonSerializer.Serialize(manifest, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(Path.Combine(installDirectory, ManifestFile), json);
    }

    private static InstallManifest? ReadManifest(string installDirectory)
    {
        string manifestPath = Path.Combine(installDirectory, ManifestFile);
        if (!File.Exists(manifestPath))
        {
            return null;
        }

        try
        {
            string json = File.ReadAllText(manifestPath);
            return JsonSerializer.Deserialize<InstallManifest>(json);
        }
        catch
        {
            return null;
        }
    }

    private static string? ResolveSafeInstalledPath(string installDirectory, string relativePath)
    {
        if (string.IsNullOrWhiteSpace(relativePath) || Path.IsPathRooted(relativePath))
        {
            return null;
        }

        string fullPath = Path.GetFullPath(Path.Combine(installDirectory, relativePath));
        return IsPathInsideOrEqual(fullPath, installDirectory) ? fullPath : null;
    }

    private static void DeleteIfSafeInstalledFile(string installDirectory, string relativePath)
    {
        string? file = ResolveSafeInstalledPath(installDirectory, relativePath);
        if (file is null || !File.Exists(file))
        {
            return;
        }

        string currentExe = Environment.ProcessPath ?? string.Empty;
        if (PathsEqual(file, currentExe))
        {
            return;
        }

        File.SetAttributes(file, FileAttributes.Normal);
        File.Delete(file);
    }

    private static void DeleteDirectoryIfEmpty(string directory)
    {
        try
        {
            if (Directory.Exists(directory) && !Directory.EnumerateFileSystemEntries(directory).Any())
            {
                Directory.Delete(directory);
            }
        }
        catch (IOException)
        {
        }
        catch (UnauthorizedAccessException)
        {
        }
    }

    private static void AddInstalledFile(ICollection<string> installedFiles, string relativePath)
    {
        string normalized = NormalizeRelativePath(relativePath);
        if (!string.IsNullOrWhiteSpace(normalized))
        {
            installedFiles.Add(normalized);
        }
    }

    private static void AddInstalledDirectory(ISet<string> installedDirectories, string relativePath)
    {
        string normalized = NormalizeRelativePath(relativePath);
        if (!string.IsNullOrWhiteSpace(normalized) && normalized != ".")
        {
            installedDirectories.Add(normalized);
        }
    }

    private static string CombineRelativePath(string? prefix, string relativePath)
    {
        return string.IsNullOrWhiteSpace(prefix) ? relativePath : Path.Combine(prefix, relativePath);
    }

    private static string NormalizeRelativePath(string relativePath)
    {
        return relativePath.Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar)
            .TrimStart(Path.DirectorySeparatorChar);
    }

    private static bool PathsEqual(string left, string right)
    {
        if (string.IsNullOrWhiteSpace(left) || string.IsNullOrWhiteSpace(right))
        {
            return false;
        }

        return string.Equals(
            Path.GetFullPath(left).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar),
            Path.GetFullPath(right).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar),
            StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsPathInsideOrEqual(string path, string parent)
    {
        string fullPath = Path.GetFullPath(path).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        string fullParent = Path.GetFullPath(parent).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        return string.Equals(fullPath, fullParent, StringComparison.OrdinalIgnoreCase) ||
            fullPath.StartsWith(fullParent + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase);
    }

    private static void SetStartup(bool enabled, string executable)
    {
        using RegistryKey key = Registry.CurrentUser.CreateSubKey(@"Software\Microsoft\Windows\CurrentVersion\Run");
        if (enabled)
        {
            key.SetValue(StartupValueName, $"\"{executable}\"");
        }
        else
        {
            key.DeleteValue(StartupValueName, throwOnMissingValue: false);
        }
    }

    private static void WriteUninstallEntry(string installDirectory)
    {
        using RegistryKey key = Registry.CurrentUser.CreateSubKey(UninstallKeyPath);
        string uninstaller = Path.Combine(installDirectory, UninstallerExecutable);
        key.SetValue("DisplayName", "LockCat");
        key.SetValue("DisplayVersion", SetupText.Version);
        key.SetValue("Publisher", "LockCat");
        key.SetValue("InstallLocation", installDirectory);
        key.SetValue("DisplayIcon", Path.Combine(installDirectory, MainExecutable));
        key.SetValue("UninstallString", $"\"{uninstaller}\" --install-dir \"{installDirectory}\"");
        key.SetValue("QuietUninstallString", $"\"{uninstaller}\" --silent --install-dir \"{installDirectory}\"");
        key.SetValue("NoModify", 1, RegistryValueKind.DWord);
        key.SetValue("NoRepair", 1, RegistryValueKind.DWord);
    }

    private static void DeleteUninstallEntry()
    {
        Registry.CurrentUser.DeleteSubKeyTree(UninstallKeyPath, throwOnMissingSubKey: false);
    }

    private static void DeleteIfExists(string path)
    {
        if (File.Exists(path))
        {
            File.Delete(path);
        }
    }

    private static void CloseRunningLockCat(string installDirectory)
    {
        foreach (Process process in Process.GetProcessesByName("LockCat"))
        {
            try
            {
                string? path = process.MainModule?.FileName;
                if (path is null || !IsPathInsideOrEqual(path, installDirectory))
                {
                    continue;
                }

                process.CloseMainWindow();
                if (!process.WaitForExit(2500))
                {
                    process.Kill(entireProcessTree: true);
                    process.WaitForExit(2500);
                }
            }
            catch
            {
                // Best effort: a process may exit or deny module inspection between calls.
            }
        }
    }

    private static void CreateShortcut(string shortcutPath, string targetPath, string workingDirectory, string description)
    {
        Type? shellType = Type.GetTypeFromProgID("WScript.Shell");
        if (shellType is null)
        {
            return;
        }

        dynamic shell = Activator.CreateInstance(shellType)!;
        dynamic shortcut = shell.CreateShortcut(shortcutPath);
        shortcut.TargetPath = targetPath;
        shortcut.WorkingDirectory = workingDirectory;
        shortcut.Description = description;
        shortcut.IconLocation = targetPath;
        shortcut.Save();
        Marshal.FinalReleaseComObject(shortcut);
        Marshal.FinalReleaseComObject(shell);
    }
}
