using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Animation;
using System.Windows.Media;

namespace LockPig.Controls;

public partial class ToggleSwitch : System.Windows.Controls.UserControl
{
    private const double CheckedThumbX = 52;

    public static readonly DependencyProperty IsCheckedProperty =
        DependencyProperty.Register(
            nameof(IsChecked),
            typeof(bool),
            typeof(ToggleSwitch),
            new FrameworkPropertyMetadata(false, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnIsCheckedChanged));

    public event EventHandler? CheckedChanged;

    public ToggleSwitch()
    {
        InitializeComponent();
        Loaded += (_, _) => UpdateVisual(animated: false);
    }

    public bool IsChecked
    {
        get => (bool)GetValue(IsCheckedProperty);
        set => SetValue(IsCheckedProperty, value);
    }

    private static void OnIsCheckedChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        ToggleSwitch toggle = (ToggleSwitch)d;
        toggle.UpdateVisual(animated: true);
        toggle.CheckedChanged?.Invoke(toggle, EventArgs.Empty);
    }

    private void Toggle()
    {
        IsChecked = !IsChecked;
    }

    private void UpdateVisual(bool animated)
    {
        if (OnTrackImage is null || OffTrackImage is null || ThumbTransform is null)
        {
            return;
        }

        double duration = animated ? 210 : 0;
        Ease(OnTrackImage, OpacityProperty, IsChecked ? 1 : 0, duration);
        Ease(OffTrackImage, OpacityProperty, IsChecked ? 0 : 1, duration);
        Ease(ThumbTransform, System.Windows.Media.TranslateTransform.XProperty, IsChecked ? CheckedThumbX : 0, duration);
    }

    private static void Ease(DependencyObject target, DependencyProperty property, double value, double milliseconds)
    {
        if (milliseconds <= 0)
        {
            target.SetValue(property, value);
            return;
        }

        DoubleAnimation animation = new(value, TimeSpan.FromMilliseconds(milliseconds))
        {
            EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
        };

        switch (target)
        {
            case UIElement element:
                element.BeginAnimation(property, animation);
                break;
            case Animatable animatable:
                animatable.BeginAnimation(property, animation);
                break;
            default:
                target.SetValue(property, value);
                break;
        }
    }

    private void ToggleSwitch_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        Focus();
        CaptureMouse();
        Ease(PressTransform, System.Windows.Media.TranslateTransform.YProperty, 2, 55);
        e.Handled = true;
    }

    private void ToggleSwitch_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        ReleaseMouseCapture();
        Ease(PressTransform, System.Windows.Media.TranslateTransform.YProperty, 0, 90);
        Toggle();
        e.Handled = true;
    }

    private void ToggleSwitch_MouseLeave(object sender, System.Windows.Input.MouseEventArgs e)
    {
        if (!IsMouseCaptured)
        {
            Ease(PressTransform, System.Windows.Media.TranslateTransform.YProperty, 0, 90);
        }
    }

    private void ToggleSwitch_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
    {
        if (e.Key is Key.Space or Key.Enter)
        {
            Ease(PressTransform, System.Windows.Media.TranslateTransform.YProperty, 2, 45);
            Toggle();
            Ease(PressTransform, System.Windows.Media.TranslateTransform.YProperty, 0, 110);
            e.Handled = true;
        }
    }
}
