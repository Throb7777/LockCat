namespace LockPig.Controls;

public partial class PixelCatAvatar : System.Windows.Controls.UserControl
{
    public static readonly System.Windows.DependencyProperty VariantProperty =
        System.Windows.DependencyProperty.Register(
            nameof(Variant),
            typeof(string),
            typeof(PixelCatAvatar),
            new System.Windows.PropertyMetadata("Avatar", OnVariantChanged));

    public PixelCatAvatar()
    {
        InitializeComponent();
        UpdateSource();
    }

    public string Variant
    {
        get => (string)GetValue(VariantProperty);
        set => SetValue(VariantProperty, value);
    }

    private static void OnVariantChanged(System.Windows.DependencyObject d, System.Windows.DependencyPropertyChangedEventArgs e)
    {
        ((PixelCatAvatar)d).UpdateSource();
    }

    private void UpdateSource()
    {
        string fileName = Variant switch
        {
            "Title" => "ref-title-cat.png",
            "Mini" => "target-tile-cat.png",
            "Peek" => "target-peeking-cat.png",
            _ => "target-peeking-cat.png"
        };

        System.Windows.Media.Imaging.BitmapImage source = new();
        source.BeginInit();
        source.UriSource = new System.Uri($"pack://application:,,,/LockCat;component/Assets/Pixel/{fileName}", System.UriKind.Absolute);
        source.CacheOption = System.Windows.Media.Imaging.BitmapCacheOption.OnLoad;
        source.EndInit();
        source.Freeze();
        AvatarImage.Source = source;
    }
}
