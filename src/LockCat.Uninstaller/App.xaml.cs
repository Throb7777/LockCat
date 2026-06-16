using LockCat.SetupCommon;
using System.Diagnostics;
using System.IO;
using System.Windows;

namespace LockCat.Uninstaller;

public partial class App : Application
{
    protected override async void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        SetupTexts texts = SetupText.Current();
        bool silent = SetupOperations.HasFlag(e.Args, "--silent");
        bool qaRender = SetupOperations.HasFlag(e.Args, "--qa-render");
        bool temp = SetupOperations.HasFlag(e.Args, "--temp");

        try
        {
            if (!qaRender &&
                !temp &&
                TryRelaunchFromTemp(e.Args))
            {
                Shutdown(0);
                return;
            }

            if (silent)
            {
                string installDir = SetupOperations.ResolveInstallDirectoryFromArgs(e.Args);
                await SetupOperations.UninstallAsync(new UninstallOptions
                {
                    InstallDirectory = installDir,
                    KeepSettings = !SetupOperations.HasFlag(e.Args, "--remove-settings")
                });
                Shutdown(0);
                return;
            }

            UninstallerWindow window = new(texts, e.Args);
            if (qaRender)
            {
                string output = SetupOperations.ReadOption(e.Args, "--output") ?? "qa-uninstaller-render";
                Directory.CreateDirectory(output);
                window.RenderQa(output);
                Shutdown(0);
                return;
            }

            MainWindow = window;
            window.Show();
        }
        catch (Exception ex)
        {
            File.WriteAllText(Path.Combine(Path.GetTempPath(), "LockCatUninstaller.log"), ex.ToString());
            if (!silent && !qaRender)
            {
                MessageBox.Show(ex.Message, texts.UninstallingTitle, MessageBoxButton.OK, MessageBoxImage.Error);
            }
            Shutdown(1);
        }
    }

    private static bool TryRelaunchFromTemp(string[] args)
    {
        string installDir = SetupOperations.ResolveInstallDirectoryFromArgs(args);
        string baseDir = Path.GetFullPath(AppContext.BaseDirectory);
        string installRoot = Path.GetFullPath(installDir.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) + Path.DirectorySeparatorChar);
        if (!baseDir.StartsWith(installRoot, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        string? currentExe = Environment.ProcessPath;
        if (string.IsNullOrWhiteSpace(currentExe) || !File.Exists(currentExe))
        {
            return false;
        }

        string tempDir = Path.Combine(Path.GetTempPath(), "LockCatUninstaller", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempDir);
        string tempExe = Path.Combine(tempDir, Path.GetFileName(currentExe));
        File.Copy(currentExe, tempExe, overwrite: true);

        string sourceAssets = Path.Combine(AppContext.BaseDirectory, "Assets");
        string targetAssets = Path.Combine(tempDir, "Assets");
        if (Directory.Exists(sourceAssets))
        {
            CopyDirectory(sourceAssets, targetAssets);
        }

        ProcessStartInfo startInfo = new()
        {
            FileName = tempExe,
            WorkingDirectory = tempDir,
            UseShellExecute = false
        };
        foreach (string arg in args)
        {
            startInfo.ArgumentList.Add(arg);
        }

        startInfo.ArgumentList.Add("--temp");
        startInfo.ArgumentList.Add($"--install-dir={installDir}");
        Process.Start(startInfo);
        return true;
    }

    private static void CopyDirectory(string source, string destination)
    {
        foreach (string directory in Directory.EnumerateDirectories(source, "*", SearchOption.AllDirectories))
        {
            string relative = Path.GetRelativePath(source, directory);
            Directory.CreateDirectory(Path.Combine(destination, relative));
        }

        foreach (string file in Directory.EnumerateFiles(source, "*", SearchOption.AllDirectories))
        {
            string relative = Path.GetRelativePath(source, file);
            string target = Path.Combine(destination, relative);
            Directory.CreateDirectory(Path.GetDirectoryName(target)!);
            File.Copy(file, target, overwrite: true);
        }
    }

}
