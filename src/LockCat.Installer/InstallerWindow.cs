using LockCat.SetupCommon;
using Microsoft.Win32;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Rectangle = System.Windows.Shapes.Rectangle;

namespace LockCat.Installer;

public sealed class InstallerWindow : PixelSetupWindow
{
    private readonly string[] _args;
    private string _installDirectory;
    private bool _desktopShortcut = true;
    private bool _startWithWindows;
    private bool _addStartMenuShortcut;
    private bool _advancedExpanded;
    private bool _launchNow;
    private bool _viewGuide;
    private Rectangle? _progressFill;
    private TextBlock? _progressText;
    private TextBlock? _progressMessage;
    private CancellationTokenSource? _installCts;
    private Exception? _lastError;
    private string _installDetailsPath = Path.Combine(Path.GetTempPath(), "LockCatInstaller.details.log");

    public InstallerWindow(SetupTexts texts, string[] args)
        : base(texts, texts.InstallerTitle, "Assets/Pixel/cat-mini-24.png")
    {
        _args = args;
        _installDirectory = SetupOperations.NormalizeInstallDirectoryForInstall(
            SetupOperations.ReadOption(args, "--install-dir") ?? SetupOperations.DefaultInstallDirectory());
        _desktopShortcut = !SetupOperations.HasFlag(args, "--no-desktop-shortcut")
            && (SetupOperations.HasFlag(args, "--desktop-shortcut") || _desktopShortcut);
        _startWithWindows = SetupOperations.HasFlag(args, "--startup")
            || (!SetupOperations.HasFlag(args, "--no-startup") && SetupOperations.HasFlag(args, "--start-with-windows"));
        _addStartMenuShortcut = SetupOperations.HasFlag(args, "--start-menu");
        _launchNow = SetupOperations.HasFlag(args, "--launch-after-install");
        ShowWelcome();
    }

    public void RenderQa(string output)
    {
        ShowWelcome();
        RenderTo(Path.Combine(output, "installer-welcome.png"));
        ShowOptions();
        RenderTo(Path.Combine(output, "installer-options.png"));
        _advancedExpanded = true;
        ShowOptions();
        RenderTo(Path.Combine(output, "installer-options-advanced.png"));
        ShowInstalling(preview: true);
        UpdateProgress(new SetupProgress(62, Texts.CopyingFiles));
        RenderTo(Path.Combine(output, "installer-installing.png"));
        ShowComplete();
        RenderTo(Path.Combine(output, "installer-complete.png"));
        File.WriteAllLines(Path.Combine(output, "qa-report.txt"), [
            "InstallerWelcomeRendered=True",
            "InstallerOptionsRendered=True",
            "InstallerOptionsAdvancedRendered=True",
            "InstallerProgressRendered=True",
            "InstallerCompleteRendered=True"
        ]);
    }

    private void ShowWelcome()
    {
        ClearPage();
        StackPanel right = new() { VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(20, 0, 0, 0) };
        Image logo = PixelImage("Assets/Pixel/target-logo.png", 360, 95);
        logo.HorizontalAlignment = HorizontalAlignment.Left;
        right.Children.Add(logo);
        TextBlock subtitle = Text(Texts.WelcomeSubtitle, 20, FontWeights.SemiBold);
        subtitle.Margin = new Thickness(0, 26, 0, 0);
        right.Children.Add(subtitle);
        right.Children.Add(DottedLine());
        StackPanel note = new() { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 22, 0, 0) };
        note.Children.Add(Text("*", 24, FontWeights.Black, "#F4B62D"));
        TextBlock noteText = Text(Texts.WelcomeNote, 17, FontWeights.SemiBold);
        noteText.Margin = new Thickness(12, 0, 0, 0);
        note.Children.Add(noteText);
        right.Children.Add(note);
        TextBlock version = Text(Texts.Version, 17, FontWeights.SemiBold, "#A89480");
        version.Margin = new Thickness(0, 24, 0, 0);
        right.Children.Add(version);

        Border card = PixelCard(TwoColumnCard("install-welcome.png", right, 410));
        card.Margin = new Thickness(46, 48, 46, 10);
        PageContent.Children.Add(card);
        FooterButtons(
            PixelButton(Texts.Cancel, false, (_, _) => ExitSetup()),
            PixelButton(Texts.StartInstall, true, (_, _) => ShowOptions()));
    }

    private void ShowOptions()
    {
        ClearPage();
        Grid page = new() { Margin = new Thickness(52, _advancedExpanded ? 8 : 44, 52, 8) };
        page.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(285) });
        page.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        page.Children.Add(Illustration("install-options.png", 260, 360));

        StackPanel form = new() { VerticalAlignment = _advancedExpanded ? VerticalAlignment.Top : VerticalAlignment.Center };
        StackPanel header = new() { Orientation = Orientation.Horizontal };
        header.Children.Add(Text("#", 32, FontWeights.Black, "#B97718"));
        TextBlock headerText = Text(Texts.InstallSettings, 30, FontWeights.Black);
        headerText.Margin = new Thickness(16, 0, 0, 0);
        header.Children.Add(headerText);
        form.Children.Add(header);

        TextBlock location = Text(Texts.InstallLocation, 19, FontWeights.Bold);
        location.Margin = new Thickness(0, _advancedExpanded ? 10 : 28, 0, 8);
        form.Children.Add(location);
        form.Children.Add(LocationRow());
        form.Children.Add(PixelCheck(Texts.CreateDesktopShortcut, null, _desktopShortcut, value => _desktopShortcut = value));
        form.Children.Add(PixelCheck(Texts.StartAfterLogin, null, _startWithWindows, value => _startWithWindows = value));
        form.Children.Add(DottedLine());

        Button advanced = PixelLinkButton((_advancedExpanded ? "v " : "> ") + Texts.AdvancedOptions, (_, _) =>
        {
            _advancedExpanded = !_advancedExpanded;
            ShowOptions();
        });
        advanced.Margin = new Thickness(0, _advancedExpanded ? 6 : 18, 0, 0);
        form.Children.Add(advanced);
        if (_advancedExpanded)
        {
            form.Children.Add(PixelCheck(Texts.AddStartMenuShortcut, null, _addStartMenuShortcut, value => _addStartMenuShortcut = value));
            form.Children.Add(PixelCheck(Texts.LaunchAfterInstall, null, _launchNow, value => _launchNow = value));
            TextBlock uninstallInfo = Text(Texts.UninstallEntryInfo, 14, FontWeights.SemiBold, "#8A715B");
            uninstallInfo.Margin = new Thickness(42, 0, 0, 0);
            form.Children.Add(uninstallInfo);
        }

        UIElement formContent = _advancedExpanded ? PixelScrollViewer(form, 410) : form;
        Border card = PixelCard(formContent);
        card.Padding = _advancedExpanded ? new Thickness(30, 14, 30, 14) : new Thickness(38, 30, 38, 30);
        Grid.SetColumn(card, 1);
        page.Children.Add(card);
        PageContent.Children.Add(page);
        FooterButtons(
            PixelButton(Texts.Back, false, (_, _) => ShowWelcome()),
            PixelButton(Texts.InstallLockCat, true, async (_, _) => await BeginInstallAsync()));
    }

    private UIElement LocationRow()
    {
        Grid row = new() { Margin = new Thickness(0, 0, 0, 24) };
        row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        row.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        Border box = new()
        {
            BorderBrush = Brush("#E6C99B"),
            BorderThickness = new Thickness(2),
            Background = Brush("#FFFFF8"),
            Height = 44,
            Padding = new Thickness(16, 0, 12, 0)
        };
        Grid inside = new();
        inside.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        inside.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        inside.Children.Add(Text(_installDirectory, 16, FontWeights.SemiBold));
        TextBlock folder = Text("[ ]", 18, FontWeights.Black, "#B97718");
        Grid.SetColumn(folder, 1);
        inside.Children.Add(folder);
        box.Child = inside;
        row.Children.Add(box);
        Button change = PixelButton(Texts.Change, false, (_, _) =>
        {
            OpenFolderDialog dialog = new()
            {
                InitialDirectory = Directory.Exists(_installDirectory) ? _installDirectory : Path.GetPathRoot(_installDirectory),
                Title = Texts.InstallLocation
            };
            if (dialog.ShowDialog(this) == true)
            {
                _installDirectory = SetupOperations.NormalizeInstallDirectoryForInstall(dialog.FolderName);
                ShowOptions();
            }
        });
        change.MinWidth = 104;
        change.Margin = new Thickness(18, 0, 0, 0);
        Grid.SetColumn(change, 1);
        row.Children.Add(change);
        return row;
    }

    private async Task BeginInstallAsync()
    {
        if (SetupOperations.NeedsElevation(_installDirectory) && !SetupOperations.HasFlag(_args, "--elevated"))
        {
            string desktopArg = _desktopShortcut ? string.Empty : " --no-desktop-shortcut";
            string startupArg = _startWithWindows ? " --startup" : string.Empty;
            string startMenuArg = _addStartMenuShortcut ? " --start-menu" : string.Empty;
            string launchArg = _launchNow ? " --launch-after-install" : string.Empty;
            string args = $"--elevated --install-dir \"{_installDirectory}\"{desktopArg}{startupArg}{startMenuArg}{launchArg}";
            SetupOperations.RelaunchElevated(args);
            ExitSetup();
            return;
        }

        ShowInstalling(preview: false);
        _installDetailsPath = Path.Combine(Path.GetTempPath(), "LockCatInstaller.details.log");
        File.WriteAllText(_installDetailsPath, $"LockCat setup started: {DateTimeOffset.Now}\nInstall directory: {_installDirectory}\n");
        _installCts = new CancellationTokenSource();
        Progress<SetupProgress> progress = new(UpdateProgress);
        try
        {
            using BundledInstallerPackage package = BundledInstallerPackage.Create();
            await SetupOperations.InstallAsync(new InstallOptions
            {
                InstallDirectory = _installDirectory,
                PayloadDirectory = package.PayloadDirectory,
                UninstallerPath = package.UninstallerPath,
                CreateDesktopShortcut = _desktopShortcut,
                StartWithWindows = _startWithWindows,
                StartAfterInstall = _launchNow,
                AddStartMenuShortcut = _addStartMenuShortcut
            }, progress, _installCts.Token);
            ShowComplete();
        }
        catch (Exception ex)
        {
            _lastError = ex;
            File.WriteAllText(Path.Combine(Path.GetTempPath(), "LockCatInstaller.log"), ex.ToString());
            File.AppendAllText(_installDetailsPath, "\nERROR\n" + ex + "\n");
            ShowFailure();
        }
    }

    private void ShowInstalling(bool preview)
    {
        ClearPage();
        Grid page = new() { Margin = new Thickness(72, 58, 72, 8) };
        page.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(390) });
        page.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        page.Children.Add(Illustration("install-progress.png", 360, 360));

        StackPanel right = new() { VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(24, 0, 0, 0) };
        Image logo = PixelImage("Assets/Pixel/target-logo.png", 220, 58);
        logo.HorizontalAlignment = HorizontalAlignment.Left;
        logo.Margin = new Thickness(0, 0, 0, 16);
        right.Children.Add(logo);
        right.Children.Add(Text(Texts.InstallingTitle, 38, FontWeights.Black));
        _progressMessage = Text(Texts.CopyingFiles, 21, FontWeights.SemiBold);
        _progressMessage.Margin = new Thickness(0, 32, 0, 18);
        right.Children.Add(_progressMessage);
        StackPanel progressRow = new() { Orientation = Orientation.Horizontal };
        progressRow.Children.Add(ProgressBar(out _progressFill));
        _progressText = Text("0%", 22, FontWeights.Bold);
        _progressText.Margin = new Thickness(24, 0, 0, 0);
        progressRow.Children.Add(_progressText);
        right.Children.Add(progressRow);
        TextBlock note = Text("* " + Texts.InstallingNote, 18, FontWeights.SemiBold);
        note.Margin = new Thickness(0, 26, 0, 0);
        right.Children.Add(note);
        Button details = PixelLinkButton(Texts.ViewInstallDetails + "  >", (_, _) => OpenInstallDetails());
        details.Margin = new Thickness(0, 24, 0, 0);
        right.Children.Add(details);

        Grid.SetColumn(right, 1);
        page.Children.Add(right);
        PageContent.Children.Add(page);
        FooterButtons(PixelButton(Texts.CancelInstall, false, (_, _) => _installCts?.Cancel()));
        if (preview)
        {
            UpdateProgress(new SetupProgress(62, Texts.CopyingFiles));
        }
    }

    private void UpdateProgress(SetupProgress progress)
    {
        if (_progressFill is not null)
        {
            _progressFill.Width = 386 * Math.Clamp(progress.Percent, 0, 100) / 100.0;
        }

        if (_progressText is not null)
        {
            _progressText.Text = $"{progress.Percent}%";
        }

        if (_progressMessage is not null)
        {
            _progressMessage.Text = progress.Message;
        }

        try
        {
            File.AppendAllText(_installDetailsPath, $"[{DateTimeOffset.Now:HH:mm:ss}] {progress.Percent}% {progress.Message}\n");
        }
        catch
        {
        }
    }

    private void ShowComplete()
    {
        ClearPage();
        StackPanel right = new() { VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(20, 0, 0, 0) };
        Image logo = PixelImage("Assets/Pixel/target-logo.png", 230, 60);
        logo.HorizontalAlignment = HorizontalAlignment.Left;
        logo.Margin = new Thickness(0, 0, 0, 18);
        right.Children.Add(logo);
        right.Children.Add(Text(Texts.InstallCompleteTitle, 37, FontWeights.Black));
        TextBlock line1 = Text(Texts.InstallCompleteLine1, 24, FontWeights.SemiBold);
        line1.Margin = new Thickness(0, 28, 0, 10);
        right.Children.Add(line1);
        right.Children.Add(Text(Texts.InstallCompleteLine2, 19, FontWeights.SemiBold));
        right.Children.Add(DottedLine());
        right.Children.Add(PixelCheck(Texts.LaunchNow, null, _launchNow, value => _launchNow = value));
        right.Children.Add(PixelCheck(Texts.ViewGuide, null, _viewGuide, value => _viewGuide = value));
        Border card = PixelCard(TwoColumnCard("install-complete.png", right, 380));
        card.Margin = new Thickness(46, 48, 46, 10);
        PageContent.Children.Add(card);
        FooterButtons(PixelButton(Texts.Finish, true, (_, _) =>
        {
            if (_launchNow)
            {
                SetupOperations.LaunchInstalledApp(_installDirectory);
            }

            if (_viewGuide)
            {
                OpenGuide();
            }

            ExitSetup();
        }));
    }

    private void ShowFailure()
    {
        ClearPage();
        StackPanel panel = new() { Margin = new Thickness(120, 100, 120, 0) };
        panel.Children.Add(Text(Texts.InstallFailedTitle, 36, FontWeights.Black));
        TextBlock line = Text(Texts.InstallFailedLine, 20, FontWeights.SemiBold);
        line.Margin = new Thickness(0, 26, 0, 20);
        panel.Children.Add(line);
        if (_lastError is not null)
        {
            panel.Children.Add(Text(_lastError.Message, 15, FontWeights.SemiBold, "#8A715B"));
        }

        PageContent.Children.Add(PixelCard(panel));
        FooterButtons(
            PixelButton(Texts.Details, false, (_, _) => OpenInstallDetails()),
            PixelButton(Texts.Retry, false, async (_, _) => await BeginInstallAsync()),
            PixelButton(Texts.ExitInstall, true, (_, _) => ExitSetup(1)));
    }

    private void OpenInstallDetails()
    {
        if (!File.Exists(_installDetailsPath))
        {
            File.WriteAllText(_installDetailsPath, "LockCat setup details are not available yet.\n");
        }

        Process.Start("notepad.exe", _installDetailsPath);
    }

    private void OpenGuide()
    {
        SetupOperations.OpenExternal(SetupOperations.GuideUrl(Texts.Language));
    }

    private UIElement DottedLine()
    {
        return new Rectangle
        {
            Height = 2,
            Stroke = Brush("#F3D189"),
            StrokeThickness = 2,
            StrokeDashArray = new DoubleCollection { 3, 3 },
            Margin = new Thickness(0, 26, 0, 0)
        };
    }
}
