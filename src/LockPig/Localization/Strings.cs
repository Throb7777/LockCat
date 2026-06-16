namespace LockPig.Localization;

public sealed record LocalizedStrings
{
    public required string Language { get; init; }
    public required string AppName { get; init; }
    public required string SettingsTitle { get; init; }
    public required string HeroSubtitle { get; init; }
    public required string ShortcutSection { get; init; }
    public required string ShortcutDescription { get; init; }
    public required string LockHotkey { get; init; }
    public required string UnlockHotkey { get; init; }
    public required string LockBehaviorSection { get; init; }
    public required string TurnOffMonitor { get; init; }
    public required string TurnOffMonitorDescription { get; init; }
    public required string Advanced { get; init; }
    public required string CatAppearanceSection { get; init; }
    public required string ShowPet { get; init; }
    public required string ShowPetDescription { get; init; }
    public required string AlwaysOnTop { get; init; }
    public required string AlwaysOnTopDescription { get; init; }
    public required string StartupLanguageSection { get; init; }
    public required string StartWithWindows { get; init; }
    public required string InterfaceLanguage { get; init; }
    public required string Synced { get; init; }
    public required string UnsavedChanges { get; init; }
    public required string Cancel { get; init; }
    public required string SaveChanges { get; init; }
    public required string Saved { get; init; }
    public required string Chinese { get; init; }
    public required string English { get; init; }
    public required string Japanese { get; init; }
    public required string HardwareDdc { get; init; }
    public required string WindowsPowerMessage { get; init; }
    public required string RedetectMonitor { get; init; }
    public required string MonitorDetectTitle { get; init; }
    public required string MonitorAvailable { get; init; }
    public required string MonitorUnavailable { get; init; }
    public required string LockHotkeyInvalidTitle { get; init; }
    public required string UnlockHotkeyInvalidTitle { get; init; }
    public required string HotkeyConflictTitle { get; init; }
    public required string HotkeyConflictMessage { get; init; }
    public required string HotkeyEmpty { get; init; }
    public required string HotkeyTooManyKeys { get; init; }
    public required string HotkeyUnknownKey { get; init; }
    public required string HotkeyOnlyOneMainKey { get; init; }
    public required string HotkeyNeedsModifierAndMainKey { get; init; }
    public required string TrayLock { get; init; }
    public required string TrayUnlock { get; init; }
    public required string TraySettings { get; init; }
    public required string TrayShowPet { get; init; }
    public required string TrayHidePet { get; init; }
    public required string TrayExit { get; init; }
    public required string TrayLockedSuffix { get; init; }
    public required string PetMenuLockNow { get; init; }
    public required string PetMenuOpenSettings { get; init; }
    public required string PetMenuPauseAnimation { get; init; }
    public required string PetMenuResumeAnimation { get; init; }
    public required string PetMenuAlwaysOnTop { get; init; }
    public required string PetMenuAlwaysOnTopChecked { get; init; }
    public required string PetMenuHidePet { get; init; }
    public required string PetMenuExit { get; init; }
    public required string PetBubbleGuard { get; init; }
    public required string PetBubbleRecovered { get; init; }
    public required string PetBubbleDragging { get; init; }
    public required string PetBubbleSettings { get; init; }
    public required string PetBubbleSaved { get; init; }
    public required string PetBubbleTyping { get; init; }
    public required string PetBubbleMeow { get; init; }
    public required string PetBubbleCurious { get; init; }
    public required string PetBubbleHere { get; init; }
    public required string PetBubbleYawn { get; init; }
    public required string PetTripleClickPromptTitle { get; init; }
    public required string PetTripleClickPromptMessage { get; init; }
    public required string PetTripleClickPromptRemaining { get; init; }
    public required string PetTripleClickPromptLock { get; init; }
    public required string PetTripleClickPromptSkip { get; init; }
}

public static class Strings
{
    public const string AppName = "LockCat";

    public static LocalizedStrings For(string? language)
    {
        return NormalizeLanguage(language) switch
        {
            "en-US" => English,
            "ja-JP" => Japanese,
            _ => Chinese
        };
    }

    public static string NormalizeLanguage(string? language)
    {
        return language switch
        {
            "en-US" => "en-US",
            "ja-JP" => "ja-JP",
            _ => "zh-CN"
        };
    }

    private static readonly LocalizedStrings Chinese = new()
    {
        Language = "zh-CN",
        AppName = AppName,
        SettingsTitle = "LockCat 设置",
        HeroSubtitle = "让小猫帮你暂时锁住键盘和屏幕",
        ShortcutSection = "快捷键",
        ShortcutDescription = "点击键帽可重新设置组合键。",
        LockHotkey = "锁定快捷键",
        UnlockHotkey = "恢复快捷键",
        LockBehaviorSection = "锁定行为",
        TurnOffMonitor = "锁定时关闭屏幕",
        TurnOffMonitorDescription = "锁定后自动关闭显示器，不会让系统进入睡眠。",
        Advanced = "高级",
        CatAppearanceSection = "小猫外观",
        ShowPet = "显示桌面小猫",
        ShowPetDescription = "隐藏后可从托盘或设置页重新显示小猫。",
        AlwaysOnTop = "小猫保持置顶",
        AlwaysOnTopDescription = "让 LockCat 小猫一直显示在其他窗口上方。",
        StartupLanguageSection = "启动与语言",
        StartWithWindows = "开机自启动",
        InterfaceLanguage = "界面语言",
        Synced = "设置已同步",
        UnsavedChanges = "有未保存的更改",
        Cancel = "取消",
        SaveChanges = "保存更改",
        Saved = "已保存",
        Chinese = "中文",
        English = "English",
        Japanese = "日本語",
        HardwareDdc = "硬件 DDC/CI",
        WindowsPowerMessage = "Windows 兼容关屏",
        RedetectMonitor = "重新检测显示器",
        MonitorDetectTitle = "显示器检测",
        MonitorAvailable = "DDC/CI 可用：{0}，当前 D6={1}",
        MonitorUnavailable = "当前显示器未报告 DDC/CI 电源控制能力。",
        LockHotkeyInvalidTitle = "锁定快捷键无效",
        UnlockHotkeyInvalidTitle = "恢复快捷键无效",
        HotkeyConflictTitle = "快捷键冲突",
        HotkeyConflictMessage = "锁定快捷键和恢复快捷键不能相同。",
        HotkeyEmpty = "快捷键不能为空。",
        HotkeyTooManyKeys = "快捷键最多只能包含三个键。",
        HotkeyUnknownKey = "无法识别按键：{0}",
        HotkeyOnlyOneMainKey = "快捷键只能有一个主键。",
        HotkeyNeedsModifierAndMainKey = "快捷键需要至少一个修饰键和一个主键。",
        TrayLock = "锁定",
        TrayUnlock = "恢复",
        TraySettings = "设置",
        TrayShowPet = "显示小猫",
        TrayHidePet = "隐藏小猫",
        TrayExit = "退出",
        TrayLockedSuffix = "已锁定",
        PetMenuLockNow = "立即锁定",
        PetMenuOpenSettings = "打开设置",
        PetMenuPauseAnimation = "暂停动画",
        PetMenuResumeAnimation = "继续动画",
        PetMenuAlwaysOnTop = "小猫置顶",
        PetMenuAlwaysOnTopChecked = "小猫置顶 ✓",
        PetMenuHidePet = "隐藏小猫",
        PetMenuExit = "退出 LockCat",
        PetBubbleGuard = "值班",
        PetBubbleRecovered = "回来了",
        PetBubbleDragging = "拎起",
        PetBubbleSettings = "设置",
        PetBubbleSaved = "已保存",
        PetBubbleTyping = "敲键",
        PetBubbleMeow = "喵",
        PetBubbleCurious = "嗯？",
        PetBubbleHere = "在呢",
        PetBubbleYawn = "哈欠",
        PetTripleClickPromptTitle = "三连击锁定",
        PetTripleClickPromptMessage = "连续点击小猫 3 次，可以让 LockCat 进入锁定。键盘会暂时锁住，屏幕也会按你的设置关闭。",
        PetTripleClickPromptRemaining = "这类提示还会出现 {0} 次。",
        PetTripleClickPromptLock = "这次锁定",
        PetTripleClickPromptSkip = "这次不锁"
    };

    private static readonly LocalizedStrings English = Chinese with
    {
        Language = "en-US",
        SettingsTitle = "LockCat Settings",
        HeroSubtitle = "Let your cat temporarily lock the keyboard and screen",
        ShortcutSection = "Shortcuts",
        ShortcutDescription = "Click a keycap to reset the key combo.",
        LockHotkey = "Lock shortcut",
        UnlockHotkey = "Recovery shortcut",
        LockBehaviorSection = "Lock Behavior",
        TurnOffMonitor = "Turn off screen when locked",
        TurnOffMonitorDescription = "Turns off the display after locking without putting Windows to sleep.",
        Advanced = "Advanced",
        CatAppearanceSection = "Cat Appearance",
        ShowPet = "Show desktop cat",
        ShowPetDescription = "When hidden, you can show the cat again from the tray or settings.",
        AlwaysOnTop = "Keep cat on top",
        AlwaysOnTopDescription = "Keep the LockCat companion above other windows.",
        StartupLanguageSection = "Startup & Language",
        StartWithWindows = "Start with Windows",
        InterfaceLanguage = "Interface language",
        Synced = "Settings synced",
        UnsavedChanges = "Unsaved changes",
        Cancel = "Cancel",
        SaveChanges = "Save Changes",
        Saved = "Saved",
        Chinese = "中文",
        Japanese = "日本語",
        HardwareDdc = "Hardware DDC/CI",
        WindowsPowerMessage = "Windows screen-off",
        RedetectMonitor = "Redetect monitor",
        MonitorDetectTitle = "Monitor Detection",
        MonitorAvailable = "DDC/CI available: {0}, current D6={1}",
        MonitorUnavailable = "The current monitor does not report DDC/CI power-control support.",
        LockHotkeyInvalidTitle = "Invalid lock shortcut",
        UnlockHotkeyInvalidTitle = "Invalid recovery shortcut",
        HotkeyConflictTitle = "Shortcut conflict",
        HotkeyConflictMessage = "The lock shortcut and recovery shortcut cannot be the same.",
        HotkeyEmpty = "Shortcut cannot be empty.",
        HotkeyTooManyKeys = "A shortcut can contain at most three keys.",
        HotkeyUnknownKey = "Unrecognized key: {0}",
        HotkeyOnlyOneMainKey = "A shortcut can only have one main key.",
        HotkeyNeedsModifierAndMainKey = "A shortcut needs at least one modifier and one main key.",
        TrayLock = "Lock",
        TrayUnlock = "Recover",
        TraySettings = "Settings",
        TrayShowPet = "Show Cat",
        TrayHidePet = "Hide Cat",
        TrayExit = "Exit",
        TrayLockedSuffix = "Locked",
        PetMenuLockNow = "Lock Now",
        PetMenuOpenSettings = "Open Settings",
        PetMenuPauseAnimation = "Pause Animation",
        PetMenuResumeAnimation = "Resume Animation",
        PetMenuAlwaysOnTop = "Cat On Top",
        PetMenuAlwaysOnTopChecked = "Cat On Top ✓",
        PetMenuHidePet = "Hide Cat",
        PetMenuExit = "Exit LockCat",
        PetBubbleGuard = "Guard",
        PetBubbleRecovered = "Back",
        PetBubbleDragging = "Lifted",
        PetBubbleSettings = "Settings",
        PetBubbleSaved = "Saved",
        PetBubbleTyping = "Typing",
        PetBubbleMeow = "Meow",
        PetBubbleCurious = "Hm?",
        PetBubbleHere = "Here",
        PetBubbleYawn = "Yawn",
        PetTripleClickPromptTitle = "Triple-click lock",
        PetTripleClickPromptMessage = "Click the cat 3 times in a row to ask LockCat to lock. The keyboard will pause, and the screen follows your lock setting.",
        PetTripleClickPromptRemaining = "This hint will appear {0} more time(s).",
        PetTripleClickPromptLock = "Lock this time",
        PetTripleClickPromptSkip = "Not this time"
    };

    private static readonly LocalizedStrings Japanese = Chinese with
    {
        Language = "ja-JP",
        SettingsTitle = "LockCat 設定",
        HeroSubtitle = "猫がキーボードと画面を一時的にロックします",
        ShortcutSection = "ショートカット",
        ShortcutDescription = "キーキャップをクリックして組み合わせを変更できます。",
        LockHotkey = "ロック用ショートカット",
        UnlockHotkey = "復帰用ショートカット",
        LockBehaviorSection = "ロック動作",
        TurnOffMonitor = "ロック時に画面をオフ",
        TurnOffMonitorDescription = "ロック後にディスプレイをオフにします。Windows はスリープしません。",
        Advanced = "詳細",
        CatAppearanceSection = "猫の表示",
        ShowPet = "デスクトップの猫を表示",
        ShowPetDescription = "非表示にしても、トレイや設定から再表示できます。",
        AlwaysOnTop = "猫を最前面に表示",
        AlwaysOnTopDescription = "LockCat の猫をほかのウィンドウより前に表示します。",
        StartupLanguageSection = "起動と言語",
        StartWithWindows = "Windows 起動時に開始",
        InterfaceLanguage = "表示言語",
        Synced = "設定は同期済み",
        UnsavedChanges = "未保存の変更があります",
        Cancel = "キャンセル",
        SaveChanges = "変更を保存",
        Saved = "保存しました",
        HardwareDdc = "ハードウェア DDC/CI",
        WindowsPowerMessage = "Windows 互換の画面オフ",
        RedetectMonitor = "モニターを再検出",
        MonitorDetectTitle = "モニター検出",
        MonitorAvailable = "DDC/CI 利用可：{0}、現在の D6={1}",
        MonitorUnavailable = "現在のモニターは DDC/CI 電源制御に対応していません。",
        LockHotkeyInvalidTitle = "ロック用ショートカットが無効です",
        UnlockHotkeyInvalidTitle = "復帰用ショートカットが無効です",
        HotkeyConflictTitle = "ショートカットの競合",
        HotkeyConflictMessage = "ロック用と復帰用のショートカットは同じにできません。",
        HotkeyEmpty = "ショートカットは空にできません。",
        HotkeyTooManyKeys = "ショートカットは最大 3 キーまでです。",
        HotkeyUnknownKey = "認識できないキー：{0}",
        HotkeyOnlyOneMainKey = "メインキーは 1 つだけ指定できます。",
        HotkeyNeedsModifierAndMainKey = "少なくとも 1 つの修飾キーと 1 つのメインキーが必要です。",
        TrayLock = "ロック",
        TrayUnlock = "復帰",
        TraySettings = "設定",
        TrayShowPet = "猫を表示",
        TrayHidePet = "猫を隠す",
        TrayExit = "終了",
        TrayLockedSuffix = "ロック中",
        PetMenuLockNow = "今すぐロック",
        PetMenuOpenSettings = "設定を開く",
        PetMenuPauseAnimation = "アニメ停止",
        PetMenuResumeAnimation = "アニメ再開",
        PetMenuAlwaysOnTop = "猫を最前面",
        PetMenuAlwaysOnTopChecked = "猫を最前面 ✓",
        PetMenuHidePet = "猫を隠す",
        PetMenuExit = "LockCat を終了",
        PetBubbleGuard = "見張り中",
        PetBubbleRecovered = "おかえり",
        PetBubbleDragging = "持ち上げ",
        PetBubbleSettings = "設定",
        PetBubbleSaved = "保存",
        PetBubbleTyping = "入力中",
        PetBubbleMeow = "にゃ",
        PetBubbleCurious = "え？",
        PetBubbleHere = "いるよ",
        PetBubbleYawn = "あくび",
        PetTripleClickPromptTitle = "3 回クリックでロック",
        PetTripleClickPromptMessage = "小猫を続けて 3 回クリックすると、LockCat がロックします。キーボードは一時的に止まり、画面は設定に従ってオフになります。",
        PetTripleClickPromptRemaining = "この案内はあと {0} 回表示されます。",
        PetTripleClickPromptLock = "今回はロック",
        PetTripleClickPromptSkip = "今回はしない"
    };
}
