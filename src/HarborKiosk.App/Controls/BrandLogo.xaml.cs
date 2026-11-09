using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace HarborKiosk.App.Controls;

public partial class BrandLogo : UserControl
{
    public static readonly DependencyProperty CheckBrushProperty = DependencyProperty.Register(
        nameof(CheckBrush), typeof(Brush), typeof(BrandLogo), new PropertyMetadata(Brushes.Transparent));

    public BrandLogo()
    {
        InitializeComponent();
    }

    public Brush CheckBrush
    {
        get => (Brush)GetValue(CheckBrushProperty);
        set => SetValue(CheckBrushProperty, value);
    }
}
