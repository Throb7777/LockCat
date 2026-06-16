using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using WpfBrush = System.Windows.Media.Brush;
using WpfBrushes = System.Windows.Media.Brushes;
using WpfColor = System.Windows.Media.Color;
using WpfPen = System.Windows.Media.Pen;
using WpfPoint = System.Windows.Point;

namespace LockPig.Controls;

public sealed class PixelFrame : ContentControl
{
    private static ControlTemplate? _template;

    public static readonly DependencyProperty CutProperty =
        DependencyProperty.Register(nameof(Cut), typeof(double), typeof(PixelFrame), new FrameworkPropertyMetadata(8.0, FrameworkPropertyMetadataOptions.AffectsRender));

    public static readonly DependencyProperty ShadowOffsetProperty =
        DependencyProperty.Register(nameof(ShadowOffset), typeof(double), typeof(PixelFrame), new FrameworkPropertyMetadata(4.0, FrameworkPropertyMetadataOptions.AffectsMeasure | FrameworkPropertyMetadataOptions.AffectsRender));

    public static readonly DependencyProperty SoftShadowProperty =
        DependencyProperty.Register(nameof(SoftShadow), typeof(bool), typeof(PixelFrame), new FrameworkPropertyMetadata(true, FrameworkPropertyMetadataOptions.AffectsRender));

    static PixelFrame()
    {
        BackgroundProperty.OverrideMetadata(typeof(PixelFrame), new FrameworkPropertyMetadata(WpfBrushes.White, FrameworkPropertyMetadataOptions.AffectsRender));
        BorderBrushProperty.OverrideMetadata(typeof(PixelFrame), new FrameworkPropertyMetadata(WpfBrushes.Black, FrameworkPropertyMetadataOptions.AffectsRender));
    }

    public PixelFrame()
    {
        SnapsToDevicePixels = true;
        Template = _template ??= CreateTemplate();
    }

    public double Cut
    {
        get => (double)GetValue(CutProperty);
        set => SetValue(CutProperty, value);
    }

    public double ShadowOffset
    {
        get => (double)GetValue(ShadowOffsetProperty);
        set => SetValue(ShadowOffsetProperty, value);
    }

    public bool SoftShadow
    {
        get => (bool)GetValue(SoftShadowProperty);
        set => SetValue(SoftShadowProperty, value);
    }

    protected override void OnRender(DrawingContext drawingContext)
    {
        base.OnRender(drawingContext);

        double shadowOffset = ShadowOffset;
        Rect frameRect = new(0.5, 0.5, Math.Max(0, ActualWidth - shadowOffset - 1), Math.Max(0, ActualHeight - shadowOffset - 1));
        if (frameRect.Width <= 0 || frameRect.Height <= 0)
        {
            return;
        }

        Geometry frame = CreatePixelGeometry(frameRect, Cut);
        Geometry hardShadow = CreatePixelGeometry(new Rect(frameRect.X + shadowOffset, frameRect.Y + shadowOffset, frameRect.Width, frameRect.Height), Cut);
        Rect innerRect = new(frameRect.X + 5, frameRect.Y + 5, Math.Max(0, frameRect.Width - 10), Math.Max(0, frameRect.Height - 10));
        Geometry innerFrame = CreatePixelGeometry(innerRect, Math.Max(3, Cut - 3));

        if (SoftShadow)
        {
            Geometry softShadow = CreatePixelGeometry(new Rect(frameRect.X + shadowOffset + 4, frameRect.Y + shadowOffset + 6, frameRect.Width, frameRect.Height), Cut);
            drawingContext.DrawGeometry(new SolidColorBrush(WpfColor.FromArgb(28, 178, 122, 50)), null, softShadow);
        }

        drawingContext.DrawGeometry(new SolidColorBrush(WpfColor.FromArgb(86, 124, 78, 36)), null, hardShadow);
        drawingContext.DrawGeometry(Background, new WpfPen(BorderBrush, 2), frame);

        WpfBrush innerLine = TryFindResource("LcAccentSoftBrush") as WpfBrush ?? WpfBrushes.White;
        WpfBrush corner = TryFindResource("LcAccentBrush") as WpfBrush ?? innerLine;
        drawingContext.DrawGeometry(null, new WpfPen(innerLine, 1), innerFrame);
        WpfBrush highlight = TryFindResource("LcAccentSoftBrush") as WpfBrush ?? WpfBrushes.White;
        drawingContext.DrawRectangle(highlight, null, new Rect(frameRect.Left + Cut + 2, frameRect.Top + 1, Math.Max(0, frameRect.Width - Cut * 2 - 4), 1));
        drawingContext.DrawRectangle(new SolidColorBrush(WpfColor.FromArgb(82, 155, 104, 46)), null, new Rect(frameRect.Left + Cut + 4, frameRect.Bottom - 3, Math.Max(0, frameRect.Width - Cut * 2 - 8), 1));
        drawingContext.DrawRectangle(new SolidColorBrush(WpfColor.FromArgb(62, 155, 104, 46)), null, new Rect(frameRect.Right - 3, frameRect.Top + Cut + 4, 1, Math.Max(0, frameRect.Height - Cut * 2 - 8)));
        DrawCornerPixels(drawingContext, frameRect, corner);
    }

    private static void DrawCornerPixels(DrawingContext drawingContext, Rect rect, WpfBrush brush)
    {
        const double size = 3;
        drawingContext.DrawRectangle(brush, null, new Rect(rect.Left + 8, rect.Top + 8, size, size));
        drawingContext.DrawRectangle(brush, null, new Rect(rect.Right - 13, rect.Top + 8, size, size));
        drawingContext.DrawRectangle(brush, null, new Rect(rect.Left + 8, rect.Bottom - 13, size, size));
        drawingContext.DrawRectangle(brush, null, new Rect(rect.Right - 13, rect.Bottom - 13, size, size));
    }

    private static Geometry CreatePixelGeometry(Rect rect, double cut)
    {
        cut = Math.Max(0, Math.Min(cut, Math.Min(rect.Width, rect.Height) / 3));
        StreamGeometry geometry = new();

        using (StreamGeometryContext context = geometry.Open())
        {
            context.BeginFigure(new WpfPoint(rect.Left + cut, rect.Top), true, true);
            context.LineTo(new WpfPoint(rect.Right - cut, rect.Top), true, false);
            context.LineTo(new WpfPoint(rect.Right - cut, rect.Top + cut / 2), true, false);
            context.LineTo(new WpfPoint(rect.Right, rect.Top + cut / 2), true, false);
            context.LineTo(new WpfPoint(rect.Right, rect.Bottom - cut), true, false);
            context.LineTo(new WpfPoint(rect.Right - cut / 2, rect.Bottom - cut), true, false);
            context.LineTo(new WpfPoint(rect.Right - cut / 2, rect.Bottom), true, false);
            context.LineTo(new WpfPoint(rect.Left + cut, rect.Bottom), true, false);
            context.LineTo(new WpfPoint(rect.Left + cut, rect.Bottom - cut / 2), true, false);
            context.LineTo(new WpfPoint(rect.Left, rect.Bottom - cut / 2), true, false);
            context.LineTo(new WpfPoint(rect.Left, rect.Top + cut), true, false);
            context.LineTo(new WpfPoint(rect.Left + cut / 2, rect.Top + cut), true, false);
            context.LineTo(new WpfPoint(rect.Left + cut / 2, rect.Top), true, false);
        }

        geometry.Freeze();
        return geometry;
    }

    private static ControlTemplate CreateTemplate()
    {
        FrameworkElementFactory presenter = new(typeof(ContentPresenter));
        presenter.SetValue(ContentPresenter.ContentProperty, new TemplateBindingExtension(ContentProperty));
        presenter.SetValue(ContentPresenter.ContentTemplateProperty, new TemplateBindingExtension(ContentTemplateProperty));
        presenter.SetValue(ContentPresenter.ContentStringFormatProperty, new TemplateBindingExtension(ContentStringFormatProperty));
        presenter.SetValue(FrameworkElement.MarginProperty, new TemplateBindingExtension(PaddingProperty));
        presenter.SetValue(FrameworkElement.HorizontalAlignmentProperty, System.Windows.HorizontalAlignment.Stretch);
        presenter.SetValue(FrameworkElement.VerticalAlignmentProperty, System.Windows.VerticalAlignment.Stretch);

        return new ControlTemplate(typeof(PixelFrame))
        {
            VisualTree = presenter
        };
    }
}
