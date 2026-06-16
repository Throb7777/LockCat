using System.Windows;
using System.Windows.Media;
using WpfBrush = System.Windows.Media.Brush;
using WpfBrushes = System.Windows.Media.Brushes;

namespace LockPig.Controls;

public sealed class PixelWordmark : FrameworkElement
{
    public static readonly DependencyProperty TextProperty =
        DependencyProperty.Register(nameof(Text), typeof(string), typeof(PixelWordmark), new FrameworkPropertyMetadata("LockCat", FrameworkPropertyMetadataOptions.AffectsMeasure | FrameworkPropertyMetadataOptions.AffectsRender));

    public static readonly DependencyProperty PixelSizeProperty =
        DependencyProperty.Register(nameof(PixelSize), typeof(double), typeof(PixelWordmark), new FrameworkPropertyMetadata(8.0, FrameworkPropertyMetadataOptions.AffectsMeasure | FrameworkPropertyMetadataOptions.AffectsRender));

    public static readonly DependencyProperty FillProperty =
        DependencyProperty.Register(nameof(Fill), typeof(WpfBrush), typeof(PixelWordmark), new FrameworkPropertyMetadata(WpfBrushes.Black, FrameworkPropertyMetadataOptions.AffectsRender));

    private static readonly Dictionary<char, string[]> Glyphs = new()
    {
        ['L'] = ["10000", "10000", "10000", "10000", "10000", "10000", "11111"],
        ['o'] = ["00000", "01110", "10001", "10001", "10001", "10001", "01110"],
        ['c'] = ["00000", "01111", "10000", "10000", "10000", "10000", "01111"],
        ['k'] = ["10000", "10010", "10100", "11000", "10100", "10010", "10001"],
        ['C'] = ["01111", "10000", "10000", "10000", "10000", "10000", "01111"],
        ['a'] = ["00000", "01110", "00001", "01111", "10001", "10001", "01111"],
        ['t'] = ["01000", "11110", "01000", "01000", "01000", "01001", "00110"]
    };

    public PixelWordmark()
    {
        SnapsToDevicePixels = true;
        RenderOptions.SetEdgeMode(this, EdgeMode.Aliased);
    }

    public string Text
    {
        get => (string)GetValue(TextProperty);
        set => SetValue(TextProperty, value);
    }

    public double PixelSize
    {
        get => (double)GetValue(PixelSizeProperty);
        set => SetValue(PixelSizeProperty, value);
    }

    public WpfBrush Fill
    {
        get => (WpfBrush)GetValue(FillProperty);
        set => SetValue(FillProperty, value);
    }

    protected override System.Windows.Size MeasureOverride(System.Windows.Size availableSize)
    {
        return new System.Windows.Size(MeasureTextWidth(), PixelSize * 7);
    }

    protected override void OnRender(DrawingContext drawingContext)
    {
        base.OnRender(drawingContext);

        double x = 0;
        foreach (char character in Text)
        {
            if (!Glyphs.TryGetValue(character, out string[]? rows))
            {
                x += PixelSize * 4;
                continue;
            }

            for (int y = 0; y < rows.Length; y++)
            {
                for (int column = 0; column < rows[y].Length; column++)
                {
                    if (rows[y][column] == '1')
                    {
                        drawingContext.DrawRectangle(Fill, null, new Rect(x + column * PixelSize, y * PixelSize, PixelSize, PixelSize));
                    }
                }
            }

            x += (rows[0].Length + 1) * PixelSize;
        }
    }

    private double MeasureTextWidth()
    {
        double width = 0;
        foreach (char character in Text)
        {
            width += Glyphs.TryGetValue(character, out string[]? rows)
                ? (rows[0].Length + 1) * PixelSize
                : PixelSize * 4;
        }

        return Math.Max(0, width - PixelSize);
    }
}
