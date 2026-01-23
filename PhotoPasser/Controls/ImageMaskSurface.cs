using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Shapes;
using PhotoPasser.Helper;
using PhotoPasser.Primitive;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.UI;

namespace PhotoPasser.Controls;

public sealed class MaskStop
{
    public double Offset { get; set; }
    public int Alpha { get; set; }
}

public sealed class ImageMaskSurface : ContentControl
{
    private Rectangle? _maskRect;

    public ImageMaskSurface()
    {
        DefaultStyleKey = typeof(ImageMaskSurface);

        MaskStops = new ObservableCollection<MaskStop>();
        MaskStops.CollectionChanged += (_, __) => UpdateMaskBrush();

        SettingProvider.Instance.ThemeChanged += async (_, e) =>
        {
            await RequestMaskUpdate();
        };
    }


    #region Dependency Properties

    public ImageSource? BackgroundImage
    {
        get => (ImageSource?)GetValue(BackgroundImageProperty);
        set => SetValue(BackgroundImageProperty, value);
    }

    public static readonly DependencyProperty BackgroundImageProperty =
        DependencyProperty.Register(
            nameof(BackgroundImage),
            typeof(ImageSource),
            typeof(ImageMaskSurface),
            new PropertyMetadata(null, OnImageChanged));

    public Color MaskColor
    {
        get => (Color)GetValue(MaskColorProperty);
        set => SetValue(MaskColorProperty, value);
    }

    public static readonly DependencyProperty MaskColorProperty =
        DependencyProperty.Register(
            nameof(MaskColor),
            typeof(Color),
            typeof(ImageMaskSurface),
            new PropertyMetadata(Color.FromArgb(200, 0, 0, 0), OnMaskPropertyChanged));

    public Point MaskStartPoint
    {
        get => (Point)GetValue(MaskStartPointProperty);
        set => SetValue(MaskStartPointProperty, value);
    }

    public static readonly DependencyProperty MaskStartPointProperty =
        DependencyProperty.Register(
            nameof(MaskStartPoint),
            typeof(Point),
            typeof(ImageMaskSurface),
            new PropertyMetadata(new Point(0, 0), OnMaskPropertyChanged));

    public Point MaskEndPoint
    {
        get => (Point)GetValue(MaskEndPointProperty);
        set => SetValue(MaskEndPointProperty, value);
    }

    public static readonly DependencyProperty MaskEndPointProperty =
        DependencyProperty.Register(
            nameof(MaskEndPoint),
            typeof(Point),
            typeof(ImageMaskSurface),
            new PropertyMetadata(new Point(0, 1), OnMaskPropertyChanged));

    public ObservableCollection<MaskStop> MaskStops
    {
        get => (ObservableCollection<MaskStop>)GetValue(MaskStopsProperty);
        set => SetValue(MaskStopsProperty, value);
    }

    public static readonly DependencyProperty MaskStopsProperty =
        DependencyProperty.Register(
            nameof(MaskStops),
            typeof(ObservableCollection<MaskStop>),
            typeof(ImageMaskSurface),
            new PropertyMetadata(null, OnMaskStopsChanged));

    public bool AutoMaskColor
    {
        get => (bool)GetValue(AutoMaskColorProperty);
        set => SetValue(AutoMaskColorProperty, value);
    }

    public static readonly DependencyProperty AutoMaskColorProperty =
        DependencyProperty.Register(
            nameof(AutoMaskColor), 
            typeof(bool), 
            typeof(ImageMaskSurface),
            new PropertyMetadata(true, OnAutoMaskFromImageChanged));



    public Stretch ImageStretch
    {
        get { return (Stretch)GetValue(ImageStretchProperty); }
        set { SetValue(ImageStretchProperty, value); }
    }

    // Using a DependencyProperty as the backing store for ImageStretch.  This enables animation, styling, binding, etc...
    public static readonly DependencyProperty ImageStretchProperty =
        DependencyProperty.Register(
            nameof(ImageStretch), 
            typeof(Stretch), 
            typeof(ImageMaskSurface), 
            new PropertyMetadata(Stretch.UniformToFill));


    #endregion

    private static void OnImageChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is ImageMaskSurface control)
        {
            _ = control.RequestMaskUpdate();
        }
    }

    private static void OnAutoMaskFromImageChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is ImageMaskSurface control && (bool)e.NewValue)
        {
            _ = control.RequestMaskUpdate();
        }
    }

    private async Task RequestMaskUpdate()
    {
        if (!AutoMaskColor || BackgroundImage == null) return;

        var avg = await ColorHelper.GetAverageColor(BackgroundImage, 255);
        var adjusted = ColorHelper.AdjustToBackground(avg);

        MaskColor = adjusted;
    }

    private static void OnMaskStopsChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is ImageMaskSurface control)
        {
            if (e.OldValue is ObservableCollection<MaskStop> oldCol)
                oldCol.CollectionChanged -= (_, __) => control.UpdateMaskBrush();

            if (e.NewValue is ObservableCollection<MaskStop> newCol)
                newCol.CollectionChanged += (_, __) => control.UpdateMaskBrush();

            control.UpdateMaskBrush();
        }
    }

    protected override void OnApplyTemplate()
    {
        base.OnApplyTemplate();

        _maskRect = GetTemplateChild("PART_MaskRect") as Rectangle;

        UpdateMaskBrush();
    }

    private static void OnMaskPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is ImageMaskSurface control)
        {
            control.UpdateMaskBrush();
        }
    }

    private void UpdateMaskBrush()
    {
        if (_maskRect == null || MaskStops == null || MaskStops.Count == 0)
            return;

        var brush = new LinearGradientBrush
        {
            StartPoint = MaskStartPoint,
            EndPoint = MaskEndPoint
        };

        foreach (var stop in MaskStops.OrderBy(s => s.Offset))
        {
            brush.GradientStops.Add(new GradientStop
            {
                Offset = stop.Offset,
                Color = Color.FromArgb(
                    byte.CreateSaturating(stop.Alpha),
                    MaskColor.R,
                    MaskColor.G,
                    MaskColor.B)
            });
        }

        _maskRect.Fill = brush;
    }
}
