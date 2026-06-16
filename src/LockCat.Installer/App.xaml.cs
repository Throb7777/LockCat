using LockCat.SetupCommon;
using System.IO;
using System.Windows;

namespace LockCat.Installer;

public partial class App : Application
{
    protected override async void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        SetupTexts texts = SetupText.Current();
        bool silentInstall = SetupOperations.HasFlag(e.Args, "--silent-install");
        bool qaRender = SetupOperations.HasFlag(e.Args, "--qa-render");

        try
        {
            if (silentInstall)
            {
                string installDir = SetupOperations.ReadOption(e.Args, "--install-dir") ?? SetupOperations.DefaultInstallDirectory();
                using BundledInstallerPackage package = BundledInstallerPackage.Create();
                string payload = SetupOperations.ReadOption(e.Args, "--payload-dir") ?? package.PayloadDirectory;
                string uninstaller = SetupOperations.ReadOption(e.Args, "--uninstaller") ?? package.UninstallerPath;
                await SetupOperations.InstallAsync(new InstallOptions
                {
                    InstallDirectory = installDir,
                    PayloadDirectory = payload,
                    UninstallerPath = uninstaller,
                    CreateDesktopShortcut = !SetupOperations.HasFlag(e.Args, "--no-desktop-shortcut"),
                    StartWithWindows = SetupOperations.HasFlag(e.Args, "--startup")
                        || SetupOperations.HasFlag(e.Args, "--start-with-windows"),
                    StartAfterInstall = false,
                    AddStartMenuShortcut = SetupOperations.HasFlag(e.Args, "--start-menu")
                });
                Shutdown(0);
                return;
            }

            InstallerWindow window = new(texts, e.Args);
            if (qaRender)
            {
                string output = SetupOperations.ReadOption(e.Args, "--output") ?? "qa-installer-render";
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
            File.WriteAllText(Path.Combine(Path.GetTempPath(), "LockCatInstaller.log"), ex.ToString());
            if (!silentInstall && !qaRender)
            {
                MessageBox.Show(ex.Message, texts.InstallFailedTitle, MessageBoxButton.OK, MessageBoxImage.Error);
            }
            Shutdown(1);
        }
    }
}
