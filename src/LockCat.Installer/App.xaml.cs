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
                string payload = SetupOperations.ReadOption(e.Args, "--payload-dir") ?? Path.Combine(AppContext.BaseDirectory, "Payload");
                string uninstaller = SetupOperations.ReadOption(e.Args, "--uninstaller") ?? Path.Combine(AppContext.BaseDirectory, SetupOperations.UninstallerExecutable);
                await SetupOperations.InstallAsync(new InstallOptions
                {
                    InstallDirectory = installDir,
                    PayloadDirectory = payload,
                    UninstallerPath = uninstaller,
                    CreateDesktopShortcut = SetupOperations.HasFlag(e.Args, "--desktop-shortcut"),
                    StartWithWindows = !SetupOperations.HasFlag(e.Args, "--no-startup"),
                    StartAfterInstall = false,
                    AddStartMenuShortcut = !SetupOperations.HasFlag(e.Args, "--no-start-menu")
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
