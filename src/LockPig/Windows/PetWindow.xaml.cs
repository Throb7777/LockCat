using LockPig.Localization;
using LockPig.Models;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using Forms = System.Windows.Forms;
using WpfMouseEventArgs = System.Windows.Input.MouseEventArgs;
using WpfPoint = System.Windows.Point;

namespace LockPig.Windows;

public partial class PetWindow : Window
{
    private const int FrameMilliseconds = 125;
    private const double DragThreshold = 5;
    private const int PoseTransitionMilliseconds = 170;

    private readonly AppState _appState;
    private readonly Action? _settingsRequested;
    private readonly Action? _hidePetRequested;
    private readonly Action? _exitRequested;
    private readonly Func<bool>? _tripleClickLockRequested;
    private readonly DispatcherTimer _animationTimer = new();
    private readonly DispatcherTimer _singleClickTimer = new();
    private readonly Dictionary<PetPose, ImageSource> _sprites;
    private readonly Random _random = new();
    private readonly Dictionary<PetAction, DateTime> _cooldowns = [];
    private readonly Queue<DateTime> _keyboardActivity = [];

    private PetAction _currentAction = PetAction.Idle;
    private PetPriority _currentPriority = PetPriority.Idle;
    private PetPose _currentPose = PetPose.Idle;
    private DateTime _actionStartedAt = DateTime.MinValue;
    private DateTime _actionEndsAt = DateTime.MinValue;
    private DateTime _lastUserActivity = DateTime.UtcNow;
    private DateTime _nextRandomActionAt = DateTime.UtcNow.AddSeconds(5);
    private DateTime _effectEndsAt = DateTime.MinValue;
    private DateTime _bubbleEndsAt = DateTime.MinValue;
    private DateTime _nextHoverHeartAt = DateTime.MinValue;
    private DateTime _nextTypingReactionAt = DateTime.MinValue;
    private WpfPoint _dragStartInWindow;
    private WpfPoint _dragStartOnScreen;
    private int _frame;
    private bool _isPointerDown;
    private bool _isDragging;
    private bool _isAnimationPaused;
    private bool _lockAfterIntro;
    private bool _skipNextLockedIntro;
    private LocalizedStrings _strings;

    public PetWindow(
        AppState appState,
        AppSettings settings,
        Action? settingsRequested = null,
        Action? hidePetRequested = null,
        Action? exitRequested = null,
        Func<bool>? tripleClickLockRequested = null)
    {
        InitializeComponent();
        _appState = appState;
        _settingsRequested = settingsRequested;
        _hidePetRequested = hidePetRequested;
        _exitRequested = exitRequested;
        _tripleClickLockRequested = tripleClickLockRequested;
        _strings = Strings.For(settings.Language);
        _sprites = LoadSprites();

        ApplySettings(settings);
        ApplyLanguage(settings.Language);
        ApplyPose(PetPose.Idle);
        Loaded += OnLoaded;

        _singleClickTimer.Interval = TimeSpan.FromMilliseconds(220);
        _singleClickTimer.Tick += (_, _) =>
        {
            _singleClickTimer.Stop();
            PlayClickResponse();
        };

        _animationTimer.Interval = TimeSpan.FromMilliseconds(FrameMilliseconds);
        _animationTimer.Tick += (_, _) => Animate();
        _animationTimer.Start();
    }

    public void ApplySettings(AppSettings settings)
    {
        ApplyLanguage(settings.Language);
        Topmost = settings.PetAlwaysOnTop;
        Opacity = Math.Clamp(settings.PetOpacity, 0.2, 1.0);
        UpdateMenuText();
    }

    public void ApplyLanguage(string language)
    {
        _strings = Strings.For(language);
        UpdateMenuText();
    }

    private void UpdateMenuText()
    {
        if (PetMenuLockText is null)
        {
            return;
        }

        PetMenuLockText.Text = _strings.PetMenuLockNow;
        PetMenuSettingsText.Text = _strings.PetMenuOpenSettings;
        PauseAnimationText.Text = _isAnimationPaused ? _strings.PetMenuResumeAnimation : _strings.PetMenuPauseAnimation;
        TopmostText.Text = Topmost ? _strings.PetMenuAlwaysOnTopChecked : _strings.PetMenuAlwaysOnTop;
        PetMenuHideText.Text = _strings.PetMenuHidePet;
        PetMenuExitText.Text = _strings.PetMenuExit;
    }

    public void SetMood(PetMood mood)
    {
        switch (mood)
        {
            case PetMood.Locked:
                if (_skipNextLockedIntro)
                {
                    _skipNextLockedIntro = false;
                    StartAction(PetAction.Guard, PetPriority.System, TimeSpan.FromDays(1), _strings.PetBubbleGuard, "LOCK");
                }
                else
                {
                    StartAction(PetAction.LockIntro, PetPriority.System, TimeSpan.FromMilliseconds(900), _strings.PetBubbleGuard, "LOCK");
                }

                break;
            case PetMood.Recovered:
                StartAction(PetAction.UnlockWake, PetPriority.System, TimeSpan.FromMilliseconds(1500), _strings.PetBubbleRecovered, "OPEN");
                break;
            case PetMood.Dragging:
                StartAction(PetAction.Dragging, PetPriority.System, TimeSpan.FromMinutes(10), _strings.PetBubbleDragging, string.Empty);
                break;
            case PetMood.Clicked:
                PlayClickResponse();
                break;
            default:
                ReturnToIdle();
                break;
        }
    }

    public void NotifySettingsOpened()
    {
        MarkUserActivity();
        StartAction(PetAction.Wave, PetPriority.Context, TimeSpan.FromMilliseconds(1300), _strings.PetBubbleSettings, "!");
    }

    public void NotifySettingsSaved()
    {
        MarkUserActivity();
        StartAction(PetAction.SaveSuccess, PetPriority.Context, TimeSpan.FromMilliseconds(1300), _strings.PetBubbleSaved, "✓");
    }

    public void NotifySettingsChanged()
    {
        if (_currentPriority <= PetPriority.Idle)
        {
            StartAction(PetAction.EarWiggle, PetPriority.Context, TimeSpan.FromMilliseconds(650), string.Empty, string.Empty);
        }
    }

    public void NotifyKeyboardActivity()
    {
        if (_appState.CurrentState == LockState.Locked)
        {
            return;
        }

        DateTime now = DateTime.UtcNow;
        _lastUserActivity = now;
        _keyboardActivity.Enqueue(now);

        while (_keyboardActivity.Count > 0 && now - _keyboardActivity.Peek() > TimeSpan.FromSeconds(12))
        {
            _keyboardActivity.Dequeue();
        }

        if (_keyboardActivity.Count >= 30
            && now - _keyboardActivity.Peek() >= TimeSpan.FromSeconds(8)
            && now >= _nextTypingReactionAt
            && _currentPriority <= PetPriority.Random)
        {
            _nextTypingReactionAt = now.AddSeconds(90);
            _keyboardActivity.Clear();
            StartAction(PetAction.Typing, PetPriority.Context, TimeSpan.FromMilliseconds(1800), _strings.PetBubbleTyping, "*");
        }
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        Left = SystemParameters.WorkArea.Right - Width - 28;
        Top = SystemParameters.WorkArea.Bottom - Height - 28;
    }

    private void Animate()
    {
        _frame++;
        UpdateTimedChrome();

        if (_isAnimationPaused)
        {
            return;
        }

        if (_currentAction == PetAction.Guard && _appState.CurrentState == LockState.Normal)
        {
            ReturnToIdle();
        }

        if (_currentAction == PetAction.Idle)
        {
            UpdateMouseProximity();
            MaybeStartRandomAction();
        }

        UpdateActionVisual();

        if (_currentAction != PetAction.Idle && DateTime.UtcNow >= _actionEndsAt)
        {
            CompleteAction();
        }
    }

    private void UpdateActionVisual()
    {
        CatScale.ScaleX = 1;
        CatScale.ScaleY = 1;
        GroundShadow.Opacity = 0.14;

        switch (_currentAction)
        {
            case PetAction.Idle:
                ApplyPose(PetPose.Idle);
                CatTranslate.Y = _frame % 16 < 8 ? 0 : -1;
                CatScale.ScaleY = _frame % 24 < 12 ? 1 : 0.99;
                break;
            case PetAction.LookLeft:
                ApplyPoseWithTransition(PetPose.Idle);
                CatTranslate.X = -2;
                CatTranslate.Y = -1;
                break;
            case PetAction.LookRight:
                ApplyPoseWithTransition(PetPose.Idle);
                CatTranslate.X = 2;
                CatTranslate.Y = -1;
                break;
            case PetAction.EarWiggle:
                ApplyPoseWithTransition(PetPose.Idle);
                CatTranslate.X = _frame % 2 == 0 ? -1 : 1;
                CatTranslate.Y = -1;
                break;
            case PetAction.Wave:
            case PetAction.SaveSuccess:
                ApplyPoseWithTransition(PetPose.Wave);
                CatTranslate.X = _frame % 4 < 2 ? 0 : 1;
                CatTranslate.Y = -2;
                break;
            case PetAction.Lick:
            case PetAction.Sit:
            case PetAction.Yawn:
                ApplyPoseWithTransition(PetPose.Sit);
                CatTranslate.X = 0;
                CatTranslate.Y = _frame % 10 < 5 ? 0 : -1;
                break;
            case PetAction.Typing:
                ApplyPoseWithTransition(_frame % 4 < 2 ? PetPose.Sit : PetPose.Wave);
                CatTranslate.X = _frame % 2 == 0 ? -1 : 1;
                CatTranslate.Y = -1;
                ShowBubble(_strings.PetBubbleTyping, 2);
                break;
            case PetAction.Stretch:
                ApplyPoseWithTransition(PetPose.Stretch);
                CatTranslate.X = 0;
                CatTranslate.Y = _frame % 6 < 3 ? 1 : 0;
                GroundShadow.Opacity = 0.1;
                break;
            case PetAction.Sleep:
                ApplyPoseWithTransition(PetPose.Sit);
                CatTranslate.X = 0;
                CatTranslate.Y = 1;
                CatScale.ScaleY = 0.97;
                ShowBubble("Z", 2);
                break;
            case PetAction.LockIntro:
                ApplyPoseWithTransition(_frame % 4 < 2 ? PetPose.Guard : PetPose.Wave);
                CatTranslate.X = 0;
                CatTranslate.Y = -3;
                ShowSparkles(2);
                break;
            case PetAction.Guard:
                ApplyPoseWithTransition(PetPose.Guard);
                CatTranslate.X = _frame % 32 < 16 ? -1 : 1;
                CatTranslate.Y = 0;
                ShowBubble(_strings.PetBubbleGuard, 2);
                break;
            case PetAction.UnlockWake:
                ApplyPoseWithTransition(_frame % 6 < 3 ? PetPose.Sit : PetPose.Stretch);
                CatTranslate.X = 0;
                CatTranslate.Y = -2;
                ShowSparkles(2);
                break;
            case PetAction.Dragging:
                ApplyPoseWithTransition(PetPose.Guard);
                CatTranslate.X = _frame % 2 == 0 ? -1 : 1;
                CatTranslate.Y = -8;
                CatScale.ScaleY = 1.02;
                GroundShadow.Opacity = 0.06;
                break;
            case PetAction.Land:
                ApplyPoseWithTransition(PetPose.Idle);
                CatTranslate.X = 0;
                CatTranslate.Y = _frame % 4 < 2 ? 3 : -1;
                CatScale.ScaleY = _frame % 4 < 2 ? 0.95 : 1.02;
                break;
        }
    }

    private void CompleteAction()
    {
        if (_currentAction == PetAction.LockIntro)
        {
            if (_lockAfterIntro)
            {
                _lockAfterIntro = false;
                _skipNextLockedIntro = true;
                _appState.Lock();
            }

            if (_appState.CurrentState == LockState.Locked)
            {
                StartAction(PetAction.Guard, PetPriority.System, TimeSpan.FromDays(1), _strings.PetBubbleGuard, "LOCK");
                return;
            }
        }

        if (_currentAction == PetAction.UnlockWake || _currentAction == PetAction.Land)
        {
            ShowSparkles(6);
        }

        ReturnToIdle();
    }

    private void ReturnToIdle()
    {
        _currentAction = PetAction.Idle;
        _currentPriority = PetPriority.Idle;
        _actionEndsAt = DateTime.MinValue;
        ApplyPose(PetPose.Idle);
        CatTranslate.X = 0;
        CatTranslate.Y = 0;
        CatScale.ScaleX = 1;
        CatScale.ScaleY = 1;
        GroundShadow.Opacity = 0.14;
    }

    private void StartAction(
        PetAction action,
        PetPriority priority,
        TimeSpan duration,
        string bubble,
        string effect,
        bool force = false)
    {
        if (!force && _currentPriority > priority && _currentAction != PetAction.Idle)
        {
            return;
        }

        _currentAction = action;
        _currentPriority = priority;
        _actionStartedAt = DateTime.UtcNow;
        _actionEndsAt = DateTime.UtcNow + duration;
        if (!string.IsNullOrWhiteSpace(bubble))
        {
            ShowBubble(bubble, Math.Max(3, (int)(duration.TotalMilliseconds / FrameMilliseconds)));
        }

        if (!string.IsNullOrWhiteSpace(effect))
        {
            ShowEffect(effect, Math.Max(3, (int)(duration.TotalMilliseconds / FrameMilliseconds)));
        }
    }

    private void ForceAction(
        PetAction action,
        PetPriority priority,
        TimeSpan duration,
        string bubble,
        string effect)
    {
        StartAction(action, priority, duration, bubble, effect, force: true);
    }

    private void RequestTripleClickLock()
    {
        bool shouldLock = _tripleClickLockRequested?.Invoke() ?? true;
        if (shouldLock)
        {
            StartLockIntro();
            return;
        }

        StartAction(PetAction.EarWiggle, PetPriority.User, TimeSpan.FromMilliseconds(700), _strings.PetBubbleCurious, "?");
    }

    private void StartLockIntro()
    {
        _lockAfterIntro = true;
        StartAction(PetAction.LockIntro, PetPriority.System, TimeSpan.FromMilliseconds(850), _strings.PetBubbleGuard, "LOCK");
    }

    private void PlayClickResponse()
    {
        MarkUserActivity();
        int choice = _random.Next(4);
        if (choice == 0)
        {
            StartAction(PetAction.Wave, PetPriority.User, TimeSpan.FromMilliseconds(1000), _strings.PetBubbleMeow, "!");
        }
        else if (choice == 1)
        {
            StartAction(PetAction.EarWiggle, PetPriority.User, TimeSpan.FromMilliseconds(700), _strings.PetBubbleCurious, "?");
        }
        else if (choice == 2)
        {
            StartAction(PetAction.Sit, PetPriority.User, TimeSpan.FromMilliseconds(900), string.Empty, "❤");
        }
        else
        {
            StartAction(PetAction.LookRight, PetPriority.User, TimeSpan.FromMilliseconds(800), _strings.PetBubbleHere, "*");
        }
    }

    private void MaybeStartRandomAction()
    {
        DateTime now = DateTime.UtcNow;
        if (now < _nextRandomActionAt)
        {
            return;
        }

        TimeSpan idleFor = now - _lastUserActivity;
        PetAction action;
        TimeSpan duration;
        string bubble = string.Empty;
        string effect = string.Empty;

        if (idleFor.TotalMinutes >= 8 && IsOffCooldown(PetAction.Sleep, TimeSpan.FromMinutes(4)))
        {
            action = PetAction.Sleep;
            duration = TimeSpan.FromSeconds(10);
            bubble = "Z";
        }
        else if (idleFor.TotalMinutes >= 3 && Roll(40) && IsOffCooldown(PetAction.Yawn, TimeSpan.FromSeconds(70)))
        {
            action = PetAction.Yawn;
            duration = TimeSpan.FromSeconds(3);
            bubble = _strings.PetBubbleYawn;
        }
        else if (idleFor.TotalMinutes >= 1 && Roll(35) && IsOffCooldown(PetAction.Stretch, TimeSpan.FromSeconds(80)))
        {
            action = PetAction.Stretch;
            duration = TimeSpan.FromSeconds(4);
            effect = "*";
        }
        else
        {
            int weighted = _random.Next(100);
            if (weighted < 30 && IsOffCooldown(PetAction.EarWiggle, TimeSpan.FromSeconds(8)))
            {
                action = PetAction.EarWiggle;
                duration = TimeSpan.FromMilliseconds(650);
            }
            else if (weighted < 50 && IsOffCooldown(PetAction.LookLeft, TimeSpan.FromSeconds(5)))
            {
                action = Roll(50) ? PetAction.LookLeft : PetAction.LookRight;
                duration = TimeSpan.FromMilliseconds(900);
            }
            else if (weighted < 65 && IsOffCooldown(PetAction.Lick, TimeSpan.FromSeconds(25)))
            {
                action = PetAction.Lick;
                duration = TimeSpan.FromSeconds(2);
            }
            else if (weighted < 85 && IsOffCooldown(PetAction.Wave, TimeSpan.FromSeconds(45)))
            {
                action = PetAction.Wave;
                duration = TimeSpan.FromMilliseconds(1100);
                effect = "*";
            }
            else
            {
                action = PetAction.Sit;
                duration = TimeSpan.FromSeconds(2);
            }
        }

        SetCooldown(action);
        StartAction(action, PetPriority.Random, duration, bubble, effect);
        _nextRandomActionAt = now.AddSeconds(_random.Next(5, idleFor.TotalSeconds > 60 ? 18 : 11));
    }

    private void UpdateMouseProximity()
    {
        WpfPoint mouse = new(Forms.Control.MousePosition.X, Forms.Control.MousePosition.Y);
        WpfPoint center = new(Left + Width / 2, Top + Height / 2);
        double dx = mouse.X - center.X;
        double dy = mouse.Y - center.Y;
        double distance = Math.Sqrt(dx * dx + dy * dy);

        if (distance > 150)
        {
            return;
        }

        CatTranslate.X = dx switch
        {
            < -80 => -2,
            > 80 => 2,
            _ => 0
        };

        if (distance < 80)
        {
            CatTranslate.Y = -2;
        }

        if (distance < 24 && DateTime.UtcNow >= _nextHoverHeartAt)
        {
            _nextHoverHeartAt = DateTime.UtcNow.AddSeconds(4);
            ShowEffect("❤", 6);
        }
    }

    private bool IsOffCooldown(PetAction action, TimeSpan cooldown)
    {
        return !_cooldowns.TryGetValue(action, out DateTime lastTime)
            || DateTime.UtcNow - lastTime >= cooldown;
    }

    private void SetCooldown(PetAction action)
    {
        _cooldowns[action] = DateTime.UtcNow;
    }

    private bool Roll(int percent)
    {
        return _random.Next(100) < percent;
    }

    private void ShowBubble(string text, int frames)
    {
        MoodText.Text = text;
        SpeechBubble.Visibility = Visibility.Visible;
        SpeechBubble.Opacity = 0.96;
        _bubbleEndsAt = DateTime.UtcNow.AddMilliseconds(frames * FrameMilliseconds);
    }

    private void ShowEffect(string text, int frames)
    {
        EffectText.Text = text;
        EffectText.Opacity = 1;
        _effectEndsAt = DateTime.UtcNow.AddMilliseconds(frames * FrameMilliseconds);
        ShowSparkles(frames);
    }

    private void ShowSparkles(int frames)
    {
        double opacity = frames > 0 ? 0.9 : 0;
        SparkA.Opacity = opacity;
        SparkB.Opacity = opacity * 0.72;
        SparkC.Opacity = opacity * 0.62;
        _effectEndsAt = DateTime.UtcNow.AddMilliseconds(Math.Max(frames, 1) * FrameMilliseconds);
    }

    private void UpdateTimedChrome()
    {
        DateTime now = DateTime.UtcNow;
        if (SpeechBubble.Visibility == Visibility.Visible && now >= _bubbleEndsAt)
        {
            SpeechBubble.Opacity = 0;
            SpeechBubble.Visibility = Visibility.Collapsed;
        }

        if (EffectText.Opacity > 0 && now >= _effectEndsAt)
        {
            EffectText.Opacity = 0;
            SparkA.Opacity = 0;
            SparkB.Opacity = 0;
            SparkC.Opacity = 0;
        }
    }

    private void ApplyPose(PetPose pose)
    {
        if (_currentPose == pose && CatSprite.Source is not null)
        {
            return;
        }

        _currentPose = pose;
        CatSprite.Source = _sprites[pose];
    }

    private void ApplyPoseWithTransition(PetPose pose)
    {
        if (_currentPose != pose
            && DateTime.UtcNow - _actionStartedAt < TimeSpan.FromMilliseconds(PoseTransitionMilliseconds))
        {
            CatScale.ScaleX = 1.03;
            CatScale.ScaleY = 0.94;
            CatTranslate.Y += 3;
            return;
        }

        ApplyPose(pose);
    }

    private static Dictionary<PetPose, ImageSource> LoadSprites()
    {
        return new Dictionary<PetPose, ImageSource>
        {
            [PetPose.Idle] = LoadSprite("cat-idle.png"),
            [PetPose.Sit] = LoadSprite("cat-sit.png"),
            [PetPose.Wave] = LoadSprite("cat-wave.png"),
            [PetPose.Stretch] = LoadSprite("cat-stretch.png"),
            [PetPose.Guard] = LoadSprite("cat-guard.png")
        };
    }

    private static ImageSource LoadSprite(string fileName)
    {
        BitmapImage image = new();
        image.BeginInit();
        image.CacheOption = BitmapCacheOption.OnLoad;
        image.UriSource = new Uri($"pack://application:,,,/LockCat;component/Assets/Cat/Sprites/{fileName}", UriKind.Absolute);
        image.EndInit();
        image.Freeze();
        return image;
    }

    private void RootLayer_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        MarkUserActivity();
        if (_appState.CurrentState == LockState.Locked)
        {
            _appState.Unlock();
            e.Handled = true;
            return;
        }

        if (e.ClickCount >= 3)
        {
            _singleClickTimer.Stop();
            RequestTripleClickLock();
            e.Handled = true;
            return;
        }

        if (e.ClickCount == 2)
        {
            _singleClickTimer.Stop();
            e.Handled = true;
            return;
        }

        _isPointerDown = true;
        _isDragging = false;
        _dragStartInWindow = e.GetPosition(this);
        _dragStartOnScreen = PointToScreen(_dragStartInWindow);
        RootLayer.CaptureMouse();
        e.Handled = true;
    }

    private void RootLayer_MouseMove(object sender, WpfMouseEventArgs e)
    {
        if (!_isPointerDown)
        {
            return;
        }

        WpfPoint currentInWindow = e.GetPosition(this);
        WpfPoint currentOnScreen = PointToScreen(currentInWindow);
        Vector delta = currentOnScreen - _dragStartOnScreen;

        if (!_isDragging && Math.Sqrt(delta.X * delta.X + delta.Y * delta.Y) >= DragThreshold)
        {
            _singleClickTimer.Stop();
            _isDragging = true;
            SetMood(PetMood.Dragging);
        }

        if (_isDragging)
        {
            Left += currentInWindow.X - _dragStartInWindow.X;
            Top += currentInWindow.Y - _dragStartInWindow.Y;
            KeepNearWorkArea();
        }
    }

    private void RootLayer_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        if (!_isPointerDown)
        {
            return;
        }

        _isPointerDown = false;
        RootLayer.ReleaseMouseCapture();

        if (_isDragging)
        {
            FinishDragging();
            e.Handled = true;
            return;
        }

        if (!_singleClickTimer.IsEnabled)
        {
            _singleClickTimer.Start();
        }

        e.Handled = true;
    }

    private void RootLayer_MouseRightButtonUp(object sender, MouseButtonEventArgs e)
    {
        MarkUserActivity();
        PetMenuPopup.IsOpen = true;
        StartAction(PetAction.Wave, PetPriority.User, TimeSpan.FromMilliseconds(600), string.Empty, string.Empty);
        e.Handled = true;
    }

    private void RootLayer_LostMouseCapture(object sender, WpfMouseEventArgs e)
    {
        if (_isPointerDown || _isDragging)
        {
            _isPointerDown = false;
            FinishDragging();
        }
    }

    private void FinishDragging()
    {
        if (!_isDragging)
        {
            return;
        }

        _isDragging = false;
        SnapToEdges();
        ForceAction(PetAction.Land, PetPriority.User, TimeSpan.FromMilliseconds(760), string.Empty, "*");
    }

    private void LockMenuItem_Click(object sender, RoutedEventArgs e)
    {
        PetMenuPopup.IsOpen = false;
        StartLockIntro();
    }

    private void SettingsMenuItem_Click(object sender, RoutedEventArgs e)
    {
        PetMenuPopup.IsOpen = false;
        _settingsRequested?.Invoke();
        NotifySettingsOpened();
    }

    private void PauseAnimationMenuItem_Click(object sender, RoutedEventArgs e)
    {
        PetMenuPopup.IsOpen = false;
        _isAnimationPaused = !_isAnimationPaused;
        UpdateMenuText();
        if (!_isAnimationPaused)
        {
            ReturnToIdle();
        }
    }

    private void TopmostMenuItem_Click(object sender, RoutedEventArgs e)
    {
        PetMenuPopup.IsOpen = false;
        Topmost = !Topmost;
        UpdateMenuText();
    }

    private void HideMenuItem_Click(object sender, RoutedEventArgs e)
    {
        PetMenuPopup.IsOpen = false;
        if (_hidePetRequested is not null)
        {
            _hidePetRequested();
        }
        else
        {
            Hide();
        }
    }

    private void ExitMenuItem_Click(object sender, RoutedEventArgs e)
    {
        PetMenuPopup.IsOpen = false;
        _exitRequested?.Invoke();
    }

    private void MarkUserActivity()
    {
        _lastUserActivity = DateTime.UtcNow;
        _nextRandomActionAt = DateTime.UtcNow.AddSeconds(_random.Next(4, 9));
    }

    private void KeepNearWorkArea()
    {
        Rect area = SystemParameters.WorkArea;
        Left = Math.Clamp(Left, area.Left - Width * 0.35, area.Right - Width * 0.65);
        Top = Math.Clamp(Top, area.Top, area.Bottom - Height * 0.45);
    }

    private void SnapToEdges()
    {
        Rect area = SystemParameters.WorkArea;
        const double snap = 26;
        if (Math.Abs(Left - area.Left) < snap)
        {
            Left = area.Left + 4;
        }
        else if (Math.Abs(area.Right - (Left + Width)) < snap)
        {
            Left = area.Right - Width - 4;
        }

        if (Top + Height > area.Bottom - 6)
        {
            Top = area.Bottom - Height - 6;
        }
    }

    private enum PetPose
    {
        Idle,
        Sit,
        Wave,
        Stretch,
        Guard
    }

    private enum PetAction
    {
        Idle,
        LookLeft,
        LookRight,
        EarWiggle,
        Lick,
        Yawn,
        Stretch,
        Sit,
        Sleep,
        Wave,
        Typing,
        SaveSuccess,
        LockIntro,
        Guard,
        UnlockWake,
        Dragging,
        Land
    }

    private enum PetPriority
    {
        Idle = 0,
        Random = 1,
        Context = 2,
        User = 3,
        System = 4
    }
}
