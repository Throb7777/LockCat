using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace LockCat.SetupCommon;

public abstract class PixelSetupWindow : Window
{
    protected readonly SetupTexts Texts;
    private readonly Grid _root = new();
    private readonly Grid _pageContent = new();
    private readonly Grid _pageFooter = new();
    private readonly FontFamily _pixelFont;
    private bool _exitRequested;

    protected PixelSetupWindow(SetupTexts texts, string title, string iconPath)
    {
        Texts = texts;
        _pixelFont = new FontFamily(new Uri("pack://application:,,,/", UriKind.Absolute), "./Assets/Fonts/#Fusion Pixel 12px Prop zh_hans");

        Title = title;
        Width = 960;
        Height = 640;
        MinWidth = 960;
        MinHeight = 640;
        WindowStartupLocation = WindowStartupLocation.CenterScreen;
        ResizeMode = ResizeMode.NoResize;
        WindowStyle = WindowStyle.None;
        Background = Brush("#FFF9EF");
        Icon = Bitmap(iconPath);
        FontFamily = _pixelFont;
        TextOptions.SetTextFormattingMode(this, TextFormattingMode.Display);
        TextOptions.SetTextRenderingMode(this, TextRenderingMode.Grayscale);
        UseLayoutRounding = true;
        SnapsToDevicePixels = true;

        _root.Background = Brush("#FFF9EF");
        _root.RowDefinitions.Add(new RowDefinition { Height = new GridLength(64) });
        _root.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
        _root.RowDefinitions.Add(new RowDefinition { Height = new GridLength(106) });

        _root.Children.Add(TitleBar(title, iconPath));
        Grid.SetRow(_pageContent, 1);
        _root.Children.Add(_pageContent);
        Grid.SetRow(_pageFooter, 2);
        _root.Children.Add(_pageFooter);
        _root.Children.Add(OuterFrame());
        base.Content = _root;
    }

    protected Grid PageContent => _pageContent;
    protected Grid PageFooter => _pageFooter;

    protected void ClearPage()
    {
        PageContent.Children.Clear();
        PageFooter.Children.Clear();
    }

    protected TextBlock Text(string value, double size, FontWeight weight, string color = "#3B2114")
    {
        return new TextBlock
        {
            Text = value,
            FontFamily = _pixelFont,
            FontSize = size,
            FontWeight = weight,
            Foreground = Brush(color),
            TextWrapping = TextWrapping.Wrap,
            LineHeight = Math.Ceiling(size * 1.28),
            VerticalAlignment = VerticalAlignment.Center
        };
    }

    protected Button PixelButton(string text, bool primary, RoutedEventHandler click)
    {
        Button button = new()
        {
            Content = text,
            MinWidth = primary ? 216 : 164,
            Height = 54,
            Padding = new Thickness(18, 0, 18, 4),
            FontFamily = _pixelFont,
            FontSize = 20,
            FontWeight = FontWeights.Black,
            Foreground = Brush("#3B2114"),
            Background = primary ? Brush("#FFD15A") : Brush("#FFFFF8"),
            BorderBrush = Brush("#7A4C24"),
            BorderThickness = new Thickness(2),
            Cursor = System.Windows.Input.Cursors.Hand,
            Margin = new Thickness(14, 0, 0, 0)
        };
        button.Click += click;
        button.Effect = new System.Windows.Media.Effects.DropShadowEffect
        {
            BlurRadius = 0,
            ShadowDepth = 4,
            Direction = 315,
            Color = primary ? Color.FromRgb(171, 106, 19) : Color.FromRgb(122, 76, 36),
            Opacity = 0.8
        };
        return button;
    }

    protected Button PixelLinkButton(string text, RoutedEventHandler click)
    {
        Button button = new()
        {
            Content = text,
            HorizontalAlignment = HorizontalAlignment.Left,
            MinWidth = 0,
            Height = 30,
            Padding = new Thickness(0),
            FontFamily = _pixelFont,
            FontSize = 17,
            FontWeight = FontWeights.Bold,
            Foreground = Brush("#C17A15"),
            Background = Brushes.Transparent,
            BorderThickness = new Thickness(0),
            Cursor = System.Windows.Input.Cursors.Hand
        };
        button.Click += click;
        return button;
    }

    protected Image Illustration(string fileName, double width, double height)
    {
        return PixelImage($"Assets/Illustrations/{fileName}", width, height);
    }

    protected Image PixelImage(string path, double width, double height)
    {
        Image image = new()
        {
            Width = width,
            Height = height,
            Stretch = Stretch.Uniform,
            Source = Bitmap(path),
            SnapsToDevicePixels = true
        };
        RenderOptions.SetBitmapScalingMode(image, BitmapScalingMode.NearestNeighbor);
        return image;
    }

    protected Border PixelCard(UIElement child)
    {
        return new Border
        {
            BorderBrush = Brush("#E6C99B"),
            BorderThickness = new Thickness(2),
            Background = Brush("#FFFCF4"),
            Padding = new Thickness(36),
            Child = child,
            Effect = new System.Windows.Media.Effects.DropShadowEffect
            {
                BlurRadius = 10,
                ShadowDepth = 4,
                Direction = 270,
                Color = Color.FromRgb(122, 76, 36),
                Opacity = 0.12
            }
        };
    }

    protected ScrollViewer PixelScrollViewer(UIElement child, double maxHeight)
    {
        ScrollViewer viewer = new()
        {
            Content = child,
            MaxHeight = maxHeight,
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
            PanningMode = PanningMode.VerticalOnly,
            Padding = new Thickness(0, 0, 6, 0)
        };
        viewer.Resources.Add(typeof(ScrollBar), PixelScrollBarStyle());
        return viewer;
    }

    private static Style PixelScrollBarStyle()
    {
        const string templateXaml = """
<ControlTemplate xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
                 xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
                 xmlns:p="clr-namespace:System.Windows.Controls.Primitives;assembly=PresentationFramework"
                 TargetType="{x:Type ScrollBar}">
    <Grid Width="10" SnapsToDevicePixels="True">
        <Border Margin="3,0,2,0"
                Background="#FFF6DE"
                BorderBrush="#E6C99B"
                BorderThickness="1"/>
        <p:Track x:Name="PART_Track"
                 Margin="3,2,2,2"
                 IsDirectionReversed="True">
            <p:Track.DecreaseRepeatButton>
                <RepeatButton Command="ScrollBar.PageUpCommand"
                              Opacity="0"
                              Focusable="False"/>
            </p:Track.DecreaseRepeatButton>
            <p:Track.Thumb>
                <Thumb MinHeight="28">
                    <Thumb.Template>
                        <ControlTemplate TargetType="{x:Type Thumb}">
                            <Border Background="#FFD15A"
                                    BorderBrush="#7A4C24"
                                    BorderThickness="1">
                                <Border Margin="2"
                                        Background="#FFE8A5"/>
                            </Border>
                        </ControlTemplate>
                    </Thumb.Template>
                </Thumb>
            </p:Track.Thumb>
            <p:Track.IncreaseRepeatButton>
                <RepeatButton Command="ScrollBar.PageDownCommand"
                              Opacity="0"
                              Focusable="False"/>
            </p:Track.IncreaseRepeatButton>
        </p:Track>
    </Grid>
</ControlTemplate>
""";
        Style style = new(typeof(ScrollBar));
        style.Setters.Add(new Setter(WidthProperty, 10.0));
        style.Setters.Add(new Setter(Control.BackgroundProperty, Brush("#FFF1CF")));
        style.Setters.Add(new Setter(Control.ForegroundProperty, Brush("#FFD15A")));
        style.Setters.Add(new Setter(BorderBrushProperty, Brush("#E6C99B")));
        style.Setters.Add(new Setter(TemplateProperty, (ControlTemplate)XamlReader.Parse(templateXaml)));
        return style;
    }

    protected Grid TwoColumnCard(string image, UIElement right, double imageWidth = 350)
    {
        Grid grid = new();
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(imageWidth) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        Image cat = Illustration(image, imageWidth, 370);
        cat.HorizontalAlignment = HorizontalAlignment.Center;
        cat.VerticalAlignment = VerticalAlignment.Center;
        grid.Children.Add(cat);
        Grid.SetColumn(right, 1);
        grid.Children.Add(right);
        return grid;
    }

    protected StackPanel FooterButtons(params Button[] buttons)
    {
        StackPanel panel = new()
        {
            Orientation = Orientation.Horizontal,
            HorizontalAlignment = HorizontalAlignment.Right,
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(0, 0, 58, 8)
        };
        foreach (Button button in buttons)
        {
            panel.Children.Add(button);
        }

        PageFooter.Children.Add(Separator());
        PageFooter.Children.Add(panel);
        return panel;
    }

    protected void ExitSetup(int exitCode = 0)
    {
        _exitRequested = true;
        Application.Current.Shutdown(exitCode);
    }

    protected override void OnClosed(EventArgs e)
    {
        base.OnClosed(e);
        if (!_exitRequested && Application.Current is not null)
        {
            _exitRequested = true;
            Application.Current.Shutdown();
        }
    }

    protected Border ProgressBar(out Rectangle fill)
    {
        Grid track = new()
        {
            Width = 390,
            Height = 28,
            HorizontalAlignment = HorizontalAlignment.Left,
            ClipToBounds = true
        };
        Border outer = new()
        {
            Width = 390,
            Height = 28,
            Background = Brush("#FFFFF8"),
            BorderBrush = Brush("#7A4C24"),
            BorderThickness = new Thickness(2),
            Child = track
        };
        fill = new Rectangle
        {
            Width = 0,
            Height = 24,
            Fill = Brush("#FFD15A"),
            HorizontalAlignment = HorizontalAlignment.Left,
            VerticalAlignment = VerticalAlignment.Center
        };
        track.Children.Add(fill);
        return outer;
    }

    protected UIElement PixelCheck(string text, string? help, bool initial, Action<bool> changed)
    {
        bool isChecked = initial;
        Grid row = new()
        {
            Cursor = System.Windows.Input.Cursors.Hand,
            Margin = new Thickness(0, 10, 0, 10)
        };
        row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(42) });
        row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        Border square = new()
        {
            Width = 24,
            Height = 24,
            BorderBrush = Brush("#C69B66"),
            BorderThickness = new Thickness(2),
            Background = initial ? Brush("#FFD15A") : Brush("#FFFFF8"),
            VerticalAlignment = VerticalAlignment.Top
        };
        TextBlock mark = Text("OK", 11, FontWeights.Black);
        mark.HorizontalAlignment = HorizontalAlignment.Center;
        mark.VerticalAlignment = VerticalAlignment.Center;
        mark.Visibility = initial ? Visibility.Visible : Visibility.Collapsed;
        square.Child = mark;
        row.Children.Add(square);

        StackPanel labels = new();
        labels.Children.Add(Text(text, 19, FontWeights.Bold));
        if (!string.IsNullOrWhiteSpace(help))
        {
            TextBlock helpText = Text(help, 14, FontWeights.SemiBold, "#8A715B");
            helpText.Margin = new Thickness(0, 8, 0, 0);
            labels.Children.Add(helpText);
        }

        Grid.SetColumn(labels, 1);
        row.Children.Add(labels);
        row.MouseLeftButtonUp += (_, _) =>
        {
            isChecked = !isChecked;
            square.Background = isChecked ? Brush("#FFD15A") : Brush("#FFFFF8");
            mark.Visibility = isChecked ? Visibility.Visible : Visibility.Collapsed;
            changed(isChecked);
        };
        return row;
    }

    protected void RenderTo(string path)
    {
        Size size = new(Width, Height);
        _root.Measure(size);
        _root.Arrange(new Rect(0, 0, Width, Height));
        _root.UpdateLayout();

        RenderTargetBitmap bitmap = new((int)Width, (int)Height, 96, 96, PixelFormats.Pbgra32);
        bitmap.Render(_root);
        PngBitmapEncoder encoder = new();
        encoder.Frames.Add(BitmapFrame.Create(bitmap));
        using FileStream stream = File.Create(path);
        encoder.Save(stream);
    }

    protected static BitmapImage Bitmap(string path)
    {
        try
        {
            return BitmapFromUri($"pack://application:,,,/{path}");
        }
        catch (IOException)
        {
            return BitmapFromUri($"pack://siteoforigin:,,,/{path}");
        }
        catch (Exception)
        {
            return BitmapFromUri($"pack://siteoforigin:,,,/{path}");
        }
    }

    private static BitmapImage BitmapFromUri(string uri)
    {
        BitmapImage image = new();
        image.BeginInit();
        image.CacheOption = BitmapCacheOption.OnLoad;
        image.UriSource = new Uri(uri, UriKind.Absolute);
        image.EndInit();
        image.Freeze();
        return image;
    }

    protected static SolidColorBrush Brush(string color)
    {
        return (SolidColorBrush)new BrushConverter().ConvertFromString(color)!;
    }

    private UIElement TitleBar(string title, string iconPath)
    {
        Border border = new()
        {
            BorderBrush = Brush("#E6C99B"),
            BorderThickness = new Thickness(0, 0, 0, 1),
            Background = Brush("#FFFFF8")
        };
        Grid grid = new();
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

        StackPanel titlePanel = new()
        {
            Orientation = Orientation.Horizontal,
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(28, 0, 0, 0)
        };
        Image icon = PixelImage(iconPath, 34, 34);
        icon.Margin = new Thickness(0, 0, 14, 0);
        titlePanel.Children.Add(icon);
        titlePanel.Children.Add(Text(title, 21, FontWeights.Black));
        grid.Children.Add(titlePanel);

        StackPanel controls = new()
        {
            Orientation = Orientation.Horizontal,
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(0, 0, 24, 0)
        };
        controls.Children.Add(ChromeButton("minimize", (_, _) => WindowState = WindowState.Minimized));
        controls.Children.Add(ChromeButton("maximize", (_, _) => { }));
        controls.Children.Add(ChromeButton("close", (_, _) => ExitSetup()));
        Grid.SetColumn(controls, 1);
        grid.Children.Add(controls);
        border.Child = grid;
        border.MouseLeftButtonDown += (_, e) =>
        {
            if (e.ClickCount == 1)
            {
                DragMove();
            }
        };
        return border;
    }

    private Button ChromeButton(string kind, RoutedEventHandler click)
    {
        Button button = new()
        {
            Content = ChromeGlyph(kind),
            Width = 48,
            Height = 48,
            Background = Brushes.Transparent,
            BorderThickness = new Thickness(0),
            Cursor = System.Windows.Input.Cursors.Hand
        };
        button.Click += click;
        return button;
    }

    private UIElement ChromeGlyph(string kind)
    {
        Canvas canvas = new()
        {
            Width = 22,
            Height = 22
        };

        if (kind == "minimize")
        {
            canvas.Children.Add(PixelRect(3, 12, 16, 3));
        }
        else if (kind == "maximize")
        {
            canvas.Children.Add(PixelRect(4, 4, 14, 3));
            canvas.Children.Add(PixelRect(4, 15, 14, 3));
            canvas.Children.Add(PixelRect(4, 4, 3, 14));
            canvas.Children.Add(PixelRect(15, 4, 3, 14));
        }
        else
        {
            canvas.Children.Add(PixelRect(5, 5, 3, 3));
            canvas.Children.Add(PixelRect(8, 8, 3, 3));
            canvas.Children.Add(PixelRect(11, 11, 3, 3));
            canvas.Children.Add(PixelRect(14, 14, 3, 3));
            canvas.Children.Add(PixelRect(14, 5, 3, 3));
            canvas.Children.Add(PixelRect(11, 8, 3, 3));
            canvas.Children.Add(PixelRect(8, 11, 3, 3));
            canvas.Children.Add(PixelRect(5, 14, 3, 3));
        }

        return canvas;
    }

    private Rectangle PixelRect(double x, double y, double width, double height)
    {
        Rectangle rect = new()
        {
            Width = width,
            Height = height,
            Fill = Brush("#2A2418"),
            SnapsToDevicePixels = true
        };
        Canvas.SetLeft(rect, x);
        Canvas.SetTop(rect, y);
        return rect;
    }

    private UIElement Separator()
    {
        return new Rectangle
        {
            Height = 1,
            Fill = Brush("#E6C99B"),
            VerticalAlignment = VerticalAlignment.Top,
            Margin = new Thickness(32, 0, 32, 0)
        };
    }

    private UIElement OuterFrame()
    {
        Border border = new()
        {
            BorderBrush = Brush("#E6C99B"),
            BorderThickness = new Thickness(2),
            IsHitTestVisible = false
        };
        Grid.SetRowSpan(border, 3);
        return border;
    }
}
