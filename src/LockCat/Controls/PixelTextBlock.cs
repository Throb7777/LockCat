using System.Globalization;
using System.Windows;
using System.Windows.Media;
using WpfBrush = System.Windows.Media.Brush;
using WpfBrushes = System.Windows.Media.Brushes;
using WpfFontFamily = System.Windows.Media.FontFamily;
using WpfPoint = System.Windows.Point;
using WpfSize = System.Windows.Size;

namespace LockPig.Controls;

public sealed class PixelTextBlock : FrameworkElement
{
    private static readonly WpfFontFamily DefaultPixelFontFamily =
        new(new Uri("pack://application:,,,/LockCat;component/", UriKind.Absolute), "./Assets/Fonts/#Fusion Pixel 12px Prop zh_hans");

    public static readonly DependencyProperty TextProperty =
        DependencyProperty.Register(nameof(Text), typeof(object), typeof(PixelTextBlock), new FrameworkPropertyMetadata(string.Empty, FrameworkPropertyMetadataOptions.AffectsMeasure | FrameworkPropertyMetadataOptions.AffectsRender));

    public static readonly DependencyProperty FontSizeProperty =
        DependencyProperty.Register(nameof(FontSize), typeof(double), typeof(PixelTextBlock), new FrameworkPropertyMetadata(16.0, FrameworkPropertyMetadataOptions.AffectsMeasure | FrameworkPropertyMetadataOptions.AffectsRender));

    public static readonly DependencyProperty FontWeightProperty =
        DependencyProperty.Register(nameof(FontWeight), typeof(FontWeight), typeof(PixelTextBlock), new FrameworkPropertyMetadata(FontWeights.SemiBold, FrameworkPropertyMetadataOptions.AffectsMeasure | FrameworkPropertyMetadataOptions.AffectsRender));

    public static readonly DependencyProperty FontFamilyProperty =
        DependencyProperty.Register(nameof(FontFamily), typeof(WpfFontFamily), typeof(PixelTextBlock), new FrameworkPropertyMetadata(DefaultPixelFontFamily, FrameworkPropertyMetadataOptions.AffectsMeasure | FrameworkPropertyMetadataOptions.AffectsRender));

    public static readonly DependencyProperty ForegroundProperty =
        DependencyProperty.Register(nameof(Foreground), typeof(WpfBrush), typeof(PixelTextBlock), new FrameworkPropertyMetadata(WpfBrushes.Black, FrameworkPropertyMetadataOptions.AffectsRender));

    public static readonly DependencyProperty PixelScaleProperty =
        DependencyProperty.Register(nameof(PixelScale), typeof(double), typeof(PixelTextBlock), new FrameworkPropertyMetadata(1.0, FrameworkPropertyMetadataOptions.AffectsMeasure | FrameworkPropertyMetadataOptions.AffectsRender));

    public object? Text
    {
        get => GetValue(TextProperty);
        set => SetValue(TextProperty, value);
    }

    public double FontSize
    {
        get => (double)GetValue(FontSizeProperty);
        set => SetValue(FontSizeProperty, value);
    }

    public FontWeight FontWeight
    {
        get => (FontWeight)GetValue(FontWeightProperty);
        set => SetValue(FontWeightProperty, value);
    }

    public WpfFontFamily FontFamily
    {
        get => (WpfFontFamily)GetValue(FontFamilyProperty);
        set => SetValue(FontFamilyProperty, value);
    }

    public WpfBrush Foreground
    {
        get => (WpfBrush)GetValue(ForegroundProperty);
        set => SetValue(ForegroundProperty, value);
    }

    public double PixelScale
    {
        get => (double)GetValue(PixelScaleProperty);
        set => SetValue(PixelScaleProperty, value);
    }

    protected override WpfSize MeasureOverride(WpfSize availableSize)
    {
        FormattedText text = CreateFormattedText();
        return new WpfSize(Math.Ceiling(text.WidthIncludingTrailingWhitespace), Math.Ceiling(text.Height));
    }

    protected override void OnRender(DrawingContext drawingContext)
    {
        base.OnRender(drawingContext);

        string value = Convert.ToString(Text, CultureInfo.CurrentCulture) ?? string.Empty;
        if (string.IsNullOrEmpty(value) || ActualWidth <= 0 || ActualHeight <= 0)
        {
            return;
        }

        drawingContext.PushGuidelineSet(new GuidelineSet([0, ActualWidth], [0, ActualHeight]));
        drawingContext.DrawText(CreateFormattedText(), new WpfPoint(0, 0));
        drawingContext.Pop();
    }

    private FormattedText CreateFormattedText()
    {
        string value = Convert.ToString(Text, CultureInfo.CurrentCulture) ?? string.Empty;
        double pixelsPerDip = VisualTreeHelper.GetDpi(this).PixelsPerDip;
        return new FormattedText(
            value,
            CultureInfo.CurrentUICulture,
            System.Windows.FlowDirection.LeftToRight,
            new Typeface(FontFamily, FontStyles.Normal, FontWeight, FontStretches.Normal),
            Math.Max(1, FontSize),
            Foreground,
            pixelsPerDip);
    }
}
