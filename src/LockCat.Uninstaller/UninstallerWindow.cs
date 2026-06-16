using LockCat.SetupCommon;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Rectangle = System.Windows.Shapes.Rectangle;

namespace LockCat.Uninstaller;

public sealed class UninstallerWindow : PixelSetupWindow
{
    private readonly string _installDirectory;
    private bool _keepSettings = true;
    private Rectangle? _progressFill;
    private TextBlock? _progressText;
    private TextBlock? _progressMessage;
    private CancellationTokenSource? _uninstallCts;
    private Exception? _lastError;

    public UninstallerWindow(SetupTexts texts, string[] args)
        : base(texts, texts.UninstallerTitle, "Assets/Pixel/cat-mini-24.png")
    {
        _installDirectory = SetupOperations.ResolveInstallDirectoryFromArgs(args);
        _keepSettings = !SetupOperations.HasFlag(args, "--remove-settings");
        ShowConfirm();
    }

    public void RenderQa(string output)
    {
        ShowConfirm();
        RenderTo(Path.Combine(output, "uninstaller-confirm.png"));
        ShowUninstalling(preview: true);
        UpdateProgress(new SetupProgress(58, Texts.RemovingFiles));
        RenderTo(Path.Combine(output, "uninstaller-uninstalling.png"));
        ShowComplete();
        RenderTo(Path.Combine(output, "uninstaller-complete.png"));
        File.WriteAllLines(Path.Combine(output, "qa-report.txt"), [
            "UninstallerConfirmRendered=True",
            "UninstallerProgressRendered=True",
            "UninstallerCompleteRendered=True"
        ]);
    }

    private void ShowConfirm()
    {
        ClearPage();
        StackPanel right = new() { VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(20, 0, 0, 0) };
        Image logo = PixelImage("Assets/Pixel/target-logo.png", 230, 60);
        logo.HorizontalAlignment = HorizontalAlignment.Left;
        logo.Margin = new Thickness(0, 0, 0, 18);
        right.Children.Add(logo);
        right.Children.Add(Text(Texts.UninstallConfirmTitle, 37, FontWeights.Black));
        TextBlock line = Text(Texts.UninstallConfirmLine, 20, FontWeights.SemiBold);
        line.Margin = new Thickness(0, 28, 0, 0);
        right.Children.Add(line);
        right.Children.Add(DottedLine());
        right.Children.Add(PixelCheck(Texts.KeepSettings, Texts.KeepSettingsHelp, _keepSettings, value => _keepSettings = value));
        Border card = PixelCard(TwoColumnCard("uninstall-confirm.png", right, 390));
        card.Margin = new Thickness(46, 48, 46, 10);
        PageContent.Children.Add(card);
        FooterButtons(
            PixelButton(Texts.Cancel, false, (_, _) => ExitSetup()),
            PixelButton(Texts.UninstallLockCat, true, async (_, _) => await BeginUninstallAsync()));
    }

    private async Task BeginUninstallAsync()
    {
        if (SetupOperations.NeedsElevation(_installDirectory))
        {
            string settingsArg = _keepSettings ? string.Empty : " --remove-settings";
            SetupOperations.RelaunchElevated($"--install-dir \"{_installDirectory}\"{settingsArg}");
            ExitSetup();
            return;
        }

        ShowUninstalling(preview: false);
        _uninstallCts = new CancellationTokenSource();
        Progress<SetupProgress> progress = new(UpdateProgress);
        try
        {
            await SetupOperations.UninstallAsync(new UninstallOptions
            {
                InstallDirectory = _installDirectory,
                KeepSettings = _keepSettings
            }, progress, _uninstallCts.Token);
            ShowComplete();
        }
        catch (Exception ex)
        {
            _lastError = ex;
            File.WriteAllText(Path.Combine(Path.GetTempPath(), "LockCatUninstaller.log"), ex.ToString());
            ShowFailure();
        }
    }

    private void ShowUninstalling(bool preview)
    {
        ClearPage();
        Grid page = new() { Margin = new Thickness(72, 58, 72, 8) };
        page.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(390) });
        page.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        page.Children.Add(Illustration("uninstall-progress.png", 370, 360));

        StackPanel right = new() { VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(24, 0, 0, 0) };
        Image logo = PixelImage("Assets/Pixel/target-logo.png", 220, 58);
        logo.HorizontalAlignment = HorizontalAlignment.Left;
        logo.Margin = new Thickness(0, 0, 0, 16);
        right.Children.Add(logo);
        right.Children.Add(Text(Texts.UninstallingTitle, 38, FontWeights.Black));
        TextBlock line = Text(Texts.UninstallingLine, 19, FontWeights.SemiBold);
        line.Margin = new Thickness(0, 26, 0, 26);
        right.Children.Add(line);
        right.Children.Add(DottedLine());
        _progressMessage = Text(Texts.RemovingFiles, 20, FontWeights.SemiBold);
        _progressMessage.Margin = new Thickness(0, 32, 0, 16);
        right.Children.Add(_progressMessage);
        StackPanel progressRow = new() { Orientation = Orientation.Horizontal };
        progressRow.Children.Add(ProgressBar(out _progressFill));
        _progressText = Text("0%", 22, FontWeights.Bold);
        _progressText.Margin = new Thickness(24, 0, 0, 0);
        progressRow.Children.Add(_progressText);
        right.Children.Add(progressRow);

        Grid.SetColumn(right, 1);
        page.Children.Add(right);
        PageContent.Children.Add(page);
        FooterButtons(PixelButton(Texts.CancelUninstall, false, (_, _) => _uninstallCts?.Cancel()));
        if (preview)
        {
            UpdateProgress(new SetupProgress(58, Texts.RemovingFiles));
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
    }

    private void ShowComplete()
    {
        ClearPage();
        StackPanel right = new() { VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(20, 0, 0, 0) };
        Image logo = PixelImage("Assets/Pixel/target-logo.png", 230, 60);
        logo.HorizontalAlignment = HorizontalAlignment.Left;
        logo.Margin = new Thickness(0, 0, 0, 18);
        right.Children.Add(logo);
        right.Children.Add(Text(Texts.UninstallCompleteTitle, 37, FontWeights.Black));
        TextBlock line1 = Text(Texts.UninstallCompleteLine1, 20, FontWeights.SemiBold);
        line1.Margin = new Thickness(0, 28, 0, 10);
        right.Children.Add(line1);
        right.Children.Add(Text(Texts.UninstallCompleteLine2, 19, FontWeights.SemiBold));
        right.Children.Add(DottedLine());
        Button feedback = PixelLinkButton(Texts.Feedback + "  >", (_, _) => SetupOperations.OpenExternal(SetupOperations.FeedbackUrl));
        feedback.Margin = new Thickness(0, 24, 0, 0);
        right.Children.Add(feedback);
        Border card = PixelCard(TwoColumnCard("uninstall-complete.png", right, 390));
        card.Margin = new Thickness(46, 48, 46, 10);
        PageContent.Children.Add(card);
        FooterButtons(PixelButton(Texts.Finish, true, (_, _) => ExitSetup()));
    }

    private void ShowFailure()
    {
        ClearPage();
        StackPanel panel = new() { Margin = new Thickness(120, 100, 120, 0) };
        panel.Children.Add(Text(Texts.InstallFailedTitle, 36, FontWeights.Black));
        TextBlock line = Text(_lastError?.Message ?? Texts.InstallFailedLine, 20, FontWeights.SemiBold);
        line.Margin = new Thickness(0, 26, 0, 20);
        panel.Children.Add(line);
        PageContent.Children.Add(PixelCard(panel));
        FooterButtons(
            PixelButton(Texts.Details, false, (_, _) => Process.Start("notepad.exe", Path.Combine(Path.GetTempPath(), "LockCatUninstaller.log"))),
            PixelButton(Texts.Finish, true, (_, _) => ExitSetup(1)));
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
