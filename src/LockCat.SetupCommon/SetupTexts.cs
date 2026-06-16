using System.Globalization;

namespace LockCat.SetupCommon;

public sealed record SetupTexts
{
    public required string Language { get; init; }
    public required string InstallerTitle { get; init; }
    public required string UninstallerTitle { get; init; }
    public required string WelcomeTitle { get; init; }
    public required string WelcomeSubtitle { get; init; }
    public required string WelcomeNote { get; init; }
    public required string Version { get; init; }
    public required string Cancel { get; init; }
    public required string Back { get; init; }
    public required string StartInstall { get; init; }
    public required string InstallLockCat { get; init; }
    public required string InstallSettings { get; init; }
    public required string InstallLocation { get; init; }
    public required string Change { get; init; }
    public required string CreateDesktopShortcut { get; init; }
    public required string StartAfterLogin { get; init; }
    public required string AdvancedOptions { get; init; }
    public required string AddStartMenuShortcut { get; init; }
    public required string LaunchAfterInstall { get; init; }
    public required string UninstallEntryInfo { get; init; }
    public required string InstallingTitle { get; init; }
    public required string CopyingFiles { get; init; }
    public required string InstallingNote { get; init; }
    public required string ViewInstallDetails { get; init; }
    public required string CancelInstall { get; init; }
    public required string InstallCompleteTitle { get; init; }
    public required string InstallCompleteLine1 { get; init; }
    public required string InstallCompleteLine2 { get; init; }
    public required string LaunchNow { get; init; }
    public required string ViewGuide { get; init; }
    public required string Finish { get; init; }
    public required string InstallFailedTitle { get; init; }
    public required string InstallFailedLine { get; init; }
    public required string Retry { get; init; }
    public required string ExitInstall { get; init; }
    public required string Details { get; init; }
    public required string UninstallConfirmTitle { get; init; }
    public required string UninstallConfirmLine { get; init; }
    public required string KeepSettings { get; init; }
    public required string KeepSettingsHelp { get; init; }
    public required string UninstallLockCat { get; init; }
    public required string UninstallingTitle { get; init; }
    public required string UninstallingLine { get; init; }
    public required string RemovingFiles { get; init; }
    public required string CancelUninstall { get; init; }
    public required string UninstallCompleteTitle { get; init; }
    public required string UninstallCompleteLine1 { get; init; }
    public required string UninstallCompleteLine2 { get; init; }
    public required string Feedback { get; init; }
    public required string RunningTitle { get; init; }
    public required string RunningLine { get; init; }
    public required string CloseAndContinue { get; init; }
    public required string DiskSpaceTitle { get; init; }
    public required string DiskSpaceLine { get; init; }
    public required string ExistingVersionTitle { get; init; }
    public required string ExistingVersionLine { get; init; }
    public required string UpdateKeepSettings { get; init; }
    public required string Reinstall { get; init; }
}

public static class SetupText
{
    public const string AppName = "LockCat";
    public const string Version = "1.0.0";

    public static SetupTexts Current()
    {
        string lang = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName;
        return lang switch
        {
            "ja" => Japanese,
            "en" => English,
            _ => Chinese
        };
    }

    private static readonly SetupTexts Chinese = new()
    {
        Language = "zh-CN",
        InstallerTitle = "LockCat 安装向导",
        UninstallerTitle = "LockCat 卸载向导",
        WelcomeTitle = "欢迎使用 LockCat",
        WelcomeSubtitle = "让小猫帮你暂时锁住键盘和屏幕。",
        WelcomeNote = "安装过程只需不到一分钟。",
        Version = $"版本 {Version}",
        Cancel = "取消",
        Back = "< 返回",
        StartInstall = "开始安装 >",
        InstallLockCat = "安装 LockCat",
        InstallSettings = "安装设置",
        InstallLocation = "安装位置",
        Change = "更改",
        CreateDesktopShortcut = "创建桌面快捷方式",
        StartAfterLogin = "登录系统后自动运行 LockCat",
        AdvancedOptions = "高级选项",
        AddStartMenuShortcut = "添加到开始菜单",
        LaunchAfterInstall = "安装完成后立即启动",
        UninstallEntryInfo = "卸载入口会自动创建，方便以后安全移除。",
        InstallingTitle = "正在安装 LockCat",
        CopyingFiles = "正在复制程序文件...",
        InstallingNote = "请稍候，小猫正在整理它的新住处。",
        ViewInstallDetails = "查看安装详情",
        CancelInstall = "取消安装",
        InstallCompleteTitle = "LockCat 已安装完成",
        InstallCompleteLine1 = "小猫已经准备好了。",
        InstallCompleteLine2 = "你可以通过快捷键或系统托盘随时使用 LockCat。",
        LaunchNow = "立即启动 LockCat",
        ViewGuide = "查看使用说明",
        Finish = "完成",
        InstallFailedTitle = "安装未能完成",
        InstallFailedLine = "LockCat 未能正确安装。你可以重试，或查看安装详情。",
        Retry = "重试",
        ExitInstall = "退出安装",
        Details = "查看详情",
        UninstallConfirmTitle = "要卸载 LockCat 吗？",
        UninstallConfirmLine = "小猫会从这台电脑上离开，但你可以选择保留设置。",
        KeepSettings = "保留我的设置和快捷键",
        KeepSettingsHelp = "下次重新安装时可以继续使用这些设置。",
        UninstallLockCat = "卸载 LockCat",
        UninstallingTitle = "正在卸载 LockCat",
        UninstallingLine = "请稍候，小猫正在收拾它的行李。",
        RemovingFiles = "正在移除程序文件...",
        CancelUninstall = "取消卸载",
        UninstallCompleteTitle = "LockCat 已卸载完成",
        UninstallCompleteLine1 = "感谢你曾经让小猫住在这里。",
        UninstallCompleteLine2 = "你可以随时重新安装 LockCat。",
        Feedback = "提交反馈",
        RunningTitle = "LockCat 当前正在运行",
        RunningLine = "继续操作前，需要暂时关闭 LockCat。",
        CloseAndContinue = "自动关闭并继续",
        DiskSpaceTitle = "磁盘空间不足",
        DiskSpaceLine = "还需要至少 {0} MB 可用空间。请选择其他安装位置后重试。",
        ExistingVersionTitle = "检测到已有版本",
        ExistingVersionLine = "可以更新现有的 LockCat，并保留当前设置。",
        UpdateKeepSettings = "更新并保留设置",
        Reinstall = "重新安装"
    };

    private static readonly SetupTexts English = Chinese with
    {
        Language = "en-US",
        InstallerTitle = "LockCat Setup",
        UninstallerTitle = "LockCat Uninstall",
        WelcomeTitle = "Welcome to LockCat",
        WelcomeSubtitle = "Let the cat temporarily lock your keyboard and screen.",
        WelcomeNote = "Setup takes less than a minute.",
        Version = $"Version {Version}",
        Cancel = "Cancel",
        Back = "< Back",
        StartInstall = "Start Install >",
        InstallLockCat = "Install LockCat",
        InstallSettings = "Install Settings",
        InstallLocation = "Install location",
        Change = "Change",
        CreateDesktopShortcut = "Create desktop shortcut",
        StartAfterLogin = "Run LockCat after signing in",
        AdvancedOptions = "Advanced Options",
        AddStartMenuShortcut = "Add to Start menu",
        LaunchAfterInstall = "Launch after setup",
        UninstallEntryInfo = "A safe uninstall entry will always be created.",
        InstallingTitle = "Installing LockCat",
        CopyingFiles = "Copying program files...",
        InstallingNote = "Please wait while the cat arranges its new home.",
        ViewInstallDetails = "View install details",
        CancelInstall = "Cancel Install",
        InstallCompleteTitle = "LockCat is installed",
        InstallCompleteLine1 = "The cat is ready.",
        InstallCompleteLine2 = "Use LockCat anytime from shortcuts or the system tray.",
        LaunchNow = "Launch LockCat now",
        ViewGuide = "View guide",
        Finish = "Finish",
        InstallFailedTitle = "Setup could not complete",
        InstallFailedLine = "LockCat was not installed correctly. You can retry or view details.",
        Retry = "Retry",
        ExitInstall = "Exit Setup",
        Details = "View Details",
        UninstallConfirmTitle = "Uninstall LockCat?",
        UninstallConfirmLine = "The cat will leave this computer, but you can keep your settings.",
        KeepSettings = "Keep my settings and shortcuts",
        KeepSettingsHelp = "You can reuse these settings next time.",
        UninstallLockCat = "Uninstall LockCat",
        UninstallingTitle = "Uninstalling LockCat",
        UninstallingLine = "Please wait while the cat packs its things.",
        RemovingFiles = "Removing program files...",
        CancelUninstall = "Cancel Uninstall",
        UninstallCompleteTitle = "LockCat has been uninstalled",
        UninstallCompleteLine1 = "Thanks for letting the cat stay here.",
        UninstallCompleteLine2 = "You can reinstall LockCat anytime.",
        Feedback = "Send feedback",
        RunningTitle = "LockCat is running",
        RunningLine = "LockCat needs to close temporarily before continuing.",
        CloseAndContinue = "Close and Continue",
        DiskSpaceTitle = "Not enough disk space",
        DiskSpaceLine = "At least {0} MB more free space is required. Choose another location and try again.",
        ExistingVersionTitle = "Existing version detected",
        ExistingVersionLine = "You can update the existing LockCat and keep current settings.",
        UpdateKeepSettings = "Update and keep settings",
        Reinstall = "Reinstall"
    };

    private static readonly SetupTexts Japanese = Chinese with
    {
        Language = "ja-JP",
        InstallerTitle = "LockCat セットアップ",
        UninstallerTitle = "LockCat アンインストール",
        WelcomeTitle = "LockCat へようこそ",
        WelcomeSubtitle = "小さな猫がキーボードと画面を一時的にロックします。",
        WelcomeNote = "インストールは 1 分以内で完了します。",
        Version = $"バージョン {Version}",
        Cancel = "キャンセル",
        Back = "< 戻る",
        StartInstall = "インストール開始 >",
        InstallLockCat = "LockCat をインストール",
        InstallSettings = "インストール設定",
        InstallLocation = "インストール先",
        Change = "変更",
        CreateDesktopShortcut = "デスクトップショートカットを作成",
        StartAfterLogin = "サインイン後に LockCat を起動",
        AdvancedOptions = "詳細オプション",
        AddStartMenuShortcut = "スタートメニューに追加",
        LaunchAfterInstall = "完了後に起動",
        UninstallEntryInfo = "安全なアンインストール入口は自動で作成されます。",
        InstallingTitle = "LockCat をインストール中",
        CopyingFiles = "プログラムファイルをコピーしています...",
        InstallingNote = "少しお待ちください。猫が新しい住みかを整えています。",
        ViewInstallDetails = "インストール詳細を見る",
        CancelInstall = "インストールをキャンセル",
        InstallCompleteTitle = "LockCat のインストールが完了しました",
        InstallCompleteLine1 = "猫の準備ができました。",
        InstallCompleteLine2 = "ショートカットやシステムトレイからいつでも使えます。",
        LaunchNow = "今すぐ LockCat を起動",
        ViewGuide = "使い方を見る",
        Finish = "完了",
        InstallFailedTitle = "インストールできませんでした",
        InstallFailedLine = "LockCat を正しくインストールできませんでした。再試行するか、詳細を確認してください。",
        Retry = "再試行",
        ExitInstall = "終了",
        Details = "詳細を見る",
        UninstallConfirmTitle = "LockCat をアンインストールしますか？",
        UninstallConfirmLine = "猫はこのコンピューターから離れますが、設定は残せます。",
        KeepSettings = "設定とショートカットを保持する",
        KeepSettingsHelp = "次回の再インストール時に同じ設定を使えます。",
        UninstallLockCat = "LockCat をアンインストール",
        UninstallingTitle = "LockCat をアンインストール中",
        UninstallingLine = "少しお待ちください。猫が荷物を片付けています。",
        RemovingFiles = "プログラムファイルを削除しています...",
        CancelUninstall = "アンインストールをキャンセル",
        UninstallCompleteTitle = "LockCat のアンインストールが完了しました",
        UninstallCompleteLine1 = "猫をここに住まわせてくれてありがとうございました。",
        UninstallCompleteLine2 = "LockCat はいつでも再インストールできます。",
        Feedback = "フィードバックを送信",
        RunningTitle = "LockCat は実行中です",
        RunningLine = "続行する前に LockCat を一時的に閉じる必要があります。",
        CloseAndContinue = "閉じて続行",
        DiskSpaceTitle = "ディスク容量が足りません",
        DiskSpaceLine = "少なくとも {0} MB の空き容量が必要です。別の場所を選んで再試行してください。",
        ExistingVersionTitle = "既存のバージョンを検出しました",
        ExistingVersionLine = "現在の設定を保持したまま LockCat を更新できます。",
        UpdateKeepSettings = "設定を保持して更新",
        Reinstall = "再インストール"
    };
}
