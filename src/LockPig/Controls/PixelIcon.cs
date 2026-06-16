using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace LockPig.Controls;

public sealed class PixelIcon : Canvas
{
    public static readonly DependencyProperty KindProperty =
        DependencyProperty.Register(nameof(Kind), typeof(string), typeof(PixelIcon), new PropertyMetadata("Keyboard", OnKindChanged));

    public PixelIcon()
    {
        Width = 26;
        Height = 26;
        SnapsToDevicePixels = true;
        RenderOptions.SetEdgeMode(this, EdgeMode.Aliased);
        Loaded += (_, _) => Draw();
    }

    public string Kind
    {
        get => (string)GetValue(KindProperty);
        set => SetValue(KindProperty, value);
    }

    private static void OnKindChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        ((PixelIcon)d).Draw();
    }

    private void Draw()
    {
        Children.Clear();

        if (DrawAsset())
        {
            return;
        }

        switch (Kind)
        {
            case "Lock":
                DrawLock();
                break;
            case "Cat":
                DrawCat();
                break;
            case "Gear":
                DrawGear();
                break;
            case "Paw":
                DrawPaw();
                break;
            case "Sparkle":
                DrawSparkle();
                break;
            case "WindowMinimize":
                DrawWindowMinimize();
                break;
            case "WindowMaximize":
                DrawWindowMaximize();
                break;
            case "WindowRestore":
                DrawWindowRestore();
                break;
            case "WindowClose":
                DrawWindowClose();
                break;
            default:
                DrawKeyboard();
                break;
        }
    }

    private bool DrawAsset()
    {
        string? fileName = Kind switch
        {
            "KeyboardTile" => "target-tile-keyboard.png",
            "LockTile" => "target-tile-lock.png",
            "CatTile" => "target-tile-cat.png",
            "GearTile" => "target-tile-gear.png",
            "PawSave" => "target-footer-paw.png",
            "PawFooterLeft" => "target-footer-paw-left.png",
            "PawFooterRight" => "target-footer-paw-right.png",
            "PawStatus" => "target-footer-paw-small.png",
            "PawButton" => "target-footer-paw-button.png",
            "Lock" => "icon-lock-24.png",
            "Cat" => "cat-mini-24.png",
            "Gear" => "icon-gear-24.png",
            "Paw" => "target-paw-bullet.png",
            "Sparkle" => "target-sparkle-small.png",
            "Keyboard" => "icon-keyboard-24.png",
            _ => null
        };

        if (fileName is null)
        {
            return false;
        }

        try
        {
            BitmapImage source = new();
            source.BeginInit();
            source.UriSource = new Uri($"pack://application:,,,/LockCat;component/Assets/Pixel/{fileName}", UriKind.Absolute);
            source.CacheOption = BitmapCacheOption.OnLoad;
            source.EndInit();
            source.Freeze();

            System.Windows.Controls.Image image = new()
            {
                Source = source,
                Width = Width,
                Height = Height,
                Stretch = Stretch.Uniform,
                SnapsToDevicePixels = true
            };
            RenderOptions.SetBitmapScalingMode(image, BitmapScalingMode.NearestNeighbor);
            Children.Add(image);
            return true;
        }
        catch
        {
            Children.Clear();
            return false;
        }
    }

    private void DrawKeyboard()
    {
        Rect(3, 4, 20, 17, "#2A2418");
        Rect(5, 6, 16, 13, "#FFF4C6");
        for (int y = 8; y <= 16; y += 4)
        {
            for (int x = 7; x <= 17; x += 5)
            {
                Rect(x, y, 3, 2, "#2A2418");
            }
        }

        Rect(8, 17, 10, 1, "#2A2418");
    }

    private void DrawLock()
    {
        Rect(7, 11, 13, 10, "#2A2418");
        Rect(9, 13, 9, 6, "#FFF4C6");
        Rect(10, 7, 7, 2, "#2A2418");
        Rect(8, 9, 3, 4, "#2A2418");
        Rect(16, 9, 3, 4, "#2A2418");
        Rect(12, 15, 3, 3, "#2A2418");
    }

    private void DrawCat()
    {
        Rect(7, 4, 4, 5, "#2A2418");
        Rect(16, 4, 4, 5, "#2A2418");
        Rect(8, 6, 10, 2, "#F5A623");
        Rect(6, 8, 16, 12, "#2A2418");
        Rect(8, 9, 12, 9, "#FFF4C6");
        Rect(8, 8, 4, 4, "#F5A623");
        Rect(16, 8, 4, 4, "#F5A623");
        Rect(10, 13, 2, 2, "#2A2418");
        Rect(16, 13, 2, 2, "#2A2418");
        Rect(13, 15, 2, 2, "#D66E4E");
        Rect(5, 14, 3, 1, "#2A2418");
        Rect(20, 14, 3, 1, "#2A2418");
    }

    private void DrawGear()
    {
        Rect(11, 3, 4, 4, "#2A2418");
        Rect(11, 19, 4, 4, "#2A2418");
        Rect(3, 11, 4, 4, "#2A2418");
        Rect(19, 11, 4, 4, "#2A2418");
        Rect(7, 7, 12, 12, "#2A2418");
        Rect(10, 10, 6, 6, "#FFF4C6");
        Rect(12, 12, 2, 2, "#2A2418");
    }

    private void DrawPaw()
    {
        Rect(11, 12, 6, 6, "#F2C94C");
        Rect(6, 8, 4, 4, "#F2C94C");
        Rect(11, 5, 4, 4, "#F2C94C");
        Rect(16, 8, 4, 4, "#F2C94C");
        Rect(8, 18, 11, 3, "#F2C94C");
    }

    private void DrawSparkle()
    {
        Rect(12, 4, 3, 18, "#F2C94C");
        Rect(5, 11, 17, 3, "#F2C94C");
        Rect(3, 3, 3, 3, "#F2C94C");
        Rect(20, 20, 3, 3, "#F2C94C");
    }

    private void DrawWindowMinimize()
    {
        Rect(6, 16, 20, 3, "#2A2418");
        Rect(6, 19, 20, 2, "#D39A3A");
    }

    private void DrawWindowMaximize()
    {
        Rect(7, 7, 19, 3, "#2A2418");
        Rect(7, 10, 3, 15, "#2A2418");
        Rect(23, 10, 3, 15, "#2A2418");
        Rect(10, 22, 13, 3, "#2A2418");
        Rect(11, 11, 11, 10, "#FFF4C6");
    }

    private void DrawWindowRestore()
    {
        Rect(11, 6, 15, 3, "#2A2418");
        Rect(23, 9, 3, 12, "#2A2418");
        Rect(16, 18, 7, 3, "#2A2418");
        Rect(6, 11, 15, 3, "#2A2418");
        Rect(6, 14, 3, 12, "#2A2418");
        Rect(18, 14, 3, 12, "#2A2418");
        Rect(9, 23, 9, 3, "#2A2418");
        Rect(10, 15, 7, 7, "#FFF4C6");
    }

    private void DrawWindowClose()
    {
        Rect(7, 6, 5, 4, "#2A2418");
        Rect(10, 9, 5, 4, "#2A2418");
        Rect(13, 12, 5, 5, "#2A2418");
        Rect(16, 15, 5, 4, "#2A2418");
        Rect(19, 18, 5, 4, "#2A2418");

        Rect(19, 6, 5, 4, "#2A2418");
        Rect(16, 9, 5, 4, "#2A2418");
        Rect(10, 15, 5, 4, "#2A2418");
        Rect(7, 18, 5, 4, "#2A2418");

        Rect(10, 7, 3, 2, "#D85B53");
        Rect(20, 7, 3, 2, "#D85B53");
        Rect(14, 13, 3, 3, "#D85B53");
        Rect(10, 19, 3, 2, "#D85B53");
        Rect(20, 19, 3, 2, "#D85B53");
    }

    private void Rect(double x, double y, double width, double height, string color)
    {
        System.Windows.Shapes.Rectangle rectangle = new()
        {
            Width = width,
            Height = height,
            Fill = (System.Windows.Media.Brush)new BrushConverter().ConvertFromString(color)!
        };

        SetLeft(rectangle, x);
        SetTop(rectangle, y);
        Children.Add(rectangle);
    }
}
