using CommunityToolkit.Common;
using Microsoft.Graphics.Canvas;
using Microsoft.Graphics.Canvas.Effects;
using Microsoft.Graphics.Canvas.UI.Xaml;
using Microsoft.UI.Input;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Documents;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using PhotoPasser.Helper;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Storage;
using Windows.UI;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace PhotoPasser.Controls;

public sealed partial class ZoomableImageView : Control
{
    private static Dictionary<Uri, CanvasBitmap> _bitmapsStore = new Dictionary<Uri, CanvasBitmap>();
    private struct PtrPointCapture
    {
        public (double X, double Y) OriginalPhotoDrawPos { get; set; }
        public (double X, double Y) PressPointerPos { get; set; }
    }

    private CanvasControl _canvas;
    private Slider _scaleSlider;
    private Button _zoomInButton;
    private Button _zoomOutButton;
    private Button _uniformButton;
    private Button _counterclockwiseFlipButton;
    private Button _clockwiseFlipButton;
    public Uri ImageUri
    {
        get { return (Uri)GetValue(ImageUriProperty); }
        set { SetValue(ImageUriProperty, value); }
    }

    // Using a DependencyProperty as the backing store for ImageUri.  This enables animation, styling, binding, etc...
    public static readonly DependencyProperty ImageUriProperty =
        DependencyProperty.Register(nameof(ImageUri), typeof(Uri), typeof(ZoomableImageView), new PropertyMetadata(new()));

    public double ImageScale
    {
        get { return (double)GetValue(ImageScaleProperty); }
        set { SetValue(ImageScaleProperty, value); }
    }

    // Using a DependencyProperty as the backing store for ImageScale.  This enables animation, styling, binding, etc...
    public static readonly DependencyProperty ImageScaleProperty =
        DependencyProperty.Register(nameof(ImageScale), typeof(double), typeof(ZoomableImageView), new PropertyMetadata(1.0));

    private double _frameAdaptScale;

    public ZoomableImageView()
    {
        DefaultStyleKey = typeof(ZoomableImageView);
        //Loaded += ZoomableImageView_Loaded;
        SizeChanged += OnSizeChanged;
        RegisterPropertyChangedCallback(ImageUriProperty, async (o, p) =>
        {
            if (_canvas == null || _drawingBitmapData == null) return;
            await Load(_canvas);
            ResetAll();
        });
        RegisterPropertyChangedCallback(ImageScaleProperty, (o, p) =>
        {
            if (_canvas == null || _drawingBitmapData == null) return;
            _canvas.Invalidate();
        });
    }
    protected override void OnPointerEntered(PointerRoutedEventArgs e)
    {
        base.OnPointerEntered(e);
        VisualStateManager.GoToState(this, "ShowPanel", true);
    }
    protected override void OnPointerExited(PointerRoutedEventArgs e)
    {
        base.OnPointerExited(e);
        VisualStateManager.GoToState(this, "HidePanel", true);
    }
    private void OnSizeChanged(object sender, SizeChangedEventArgs e)
    {
        if (_canvas == null || _drawingBitmapData == null) return;
        ResetZoom(false);
    }

    private void ResetZoom(bool frontChange)
    {
        if(frontChange)
            _scaleSlider.Value = 1.0;
        _frameAdaptScale = Math.Min((double)_canvas.ActualWidth / _drawingBitmapData.Size.Width, (double)_canvas.ActualHeight / _drawingBitmapData.Size.Height);

        _canvas.Invalidate();
    }

    protected override void OnPointerWheelChanged(PointerRoutedEventArgs e)
    {
        base.OnPointerWheelChanged(e);

        if (e.KeyModifiers != Windows.System.VirtualKeyModifiers.Control)
            return;

        double originalWidth = _drawingBitmapData.Size.Width * (ImageScale * _frameAdaptScale);
        double originalHeight = _drawingBitmapData.Size.Height * (ImageScale * _frameAdaptScale);

        var ptr = e.GetCurrentPoint(_canvas);
        var delta = ptr.Properties.MouseWheelDelta * 0.001;
        ScaleDelta(delta);
        double width = _drawingBitmapData.Size.Width * (ImageScale * _frameAdaptScale);
        double height = _drawingBitmapData.Size.Height * (ImageScale * _frameAdaptScale);
        double xRadio = (ptr.Position.X - X) / originalWidth;
        double yRadio = (ptr.Position.Y - Y) / originalHeight;

        X += originalWidth * xRadio - xRadio * width;
        Y += originalHeight * yRadio - yRadio * height;

        _canvas.Invalidate();
    }

    private void ScaleDelta(double deltaVal)
    {
        _scaleSlider.Value += deltaVal;
        if (_scaleSlider.Value <= 0.1)
            _scaleSlider.Value = 0.1;
    }

    public double X { get; set; }
    public double Y { get; set; }


    //private void ZoomableImageView_Loaded(object sender, RoutedEventArgs e)
    //{
    //    ApplyTemplate();

    //    // ¼ì²é _canvas ÊÇ·ñÎª null
    //}

    private object _sync = new();

    protected override async void OnApplyTemplate()
    {
        base.OnApplyTemplate();
        
        _canvas = GetTemplateChild("PART_Canvas") as CanvasControl;
        if (_canvas != null)
        {
            _canvas.Draw += OnDraw;
            _canvas.CreateResources += async (s, e) =>
            {
                await Load(s);
            };
        }

        _scaleSlider = GetTemplateChild("PART_ScaleSlider") as Slider;
        if(_scaleSlider != null)
        {
            _scaleSlider.ValueChanged += ScaleSliderValueChanged;
        }

        _zoomOutButton = GetTemplateChild("PART_ZoomOutButton") as Button;
        if (_zoomOutButton != null)
        {
            _zoomOutButton.Click += (s, e) =>
            {
                ScaleDelta(-0.1);
            };
        }

        _zoomInButton = GetTemplateChild("PART_ZoomInButton") as Button;
        if (_zoomInButton != null)
        {
            _zoomInButton.Click += (s, e) =>
            {
                ScaleDelta(0.1);
            };
        }

        _uniformButton = GetTemplateChild("PART_UniformButton") as Button;
        if (_uniformButton != null)
        {
            _uniformButton.Click += (s, e) =>
            {
                ResetAll();
            };
        }

        _counterclockwiseFlipButton = GetTemplateChild("PART_CounterclockwiseFlipButton") as Button;
        if(_counterclockwiseFlipButton != null)
        {
            _counterclockwiseFlipButton.Click += (s, e) =>
            {
                _canvas.CenterPoint = new((float)_canvas.ActualWidth * 0.5F, (float)_canvas.ActualHeight * 0.5F, 0);
                _canvas.Rotation -= 90;
            };
        }
        _clockwiseFlipButton = GetTemplateChild("PART_ClockwiseFlipButton") as Button;
        if (_clockwiseFlipButton != null)
        {
            _clockwiseFlipButton.Click += (s, e) =>
            {
                _canvas.CenterPoint = new((float)_canvas.ActualWidth * 0.5F, (float)_canvas.ActualHeight * 0.5F, 0);
                _canvas.Rotation += 90;
            };
        }
    }

    private async Task Load(CanvasControl s)
    {
        var bitmapData = await LoadBitmapAsync();

        if (bitmapData != null)
        {
            _drawingBitmapData = bitmapData;
            _frameAdaptScale = Math.Min((double)s.ActualWidth / _drawingBitmapData.Size.Width, (double)s.ActualHeight / _drawingBitmapData.Size.Height);
        }
        RecalcPos();
        _canvas.Invalidate();
    }

    private bool _isDraging = false;
    private PtrPointCapture _capture;

    protected override void OnPointerPressed(PointerRoutedEventArgs e)
    {
        base.OnPointerPressed(e);
        var pos = e.GetCurrentPoint(_canvas).Position;
        _capture = new PtrPointCapture()
        {
            OriginalPhotoDrawPos = (X, Y),
            PressPointerPos = (pos.X, pos.Y)
        };
        _isDraging = true;
    }

    protected override void OnPointerMoved(PointerRoutedEventArgs e)
    {
        base.OnPointerMoved(e);
        var ptrPointPos = e.GetCurrentPoint(_canvas).Position;
        if (_isDraging)
        {
            (X, Y) = ((ptrPointPos.X - _capture.PressPointerPos.X) + _capture.OriginalPhotoDrawPos.X,
                      (ptrPointPos.Y - _capture.PressPointerPos.Y) + _capture.OriginalPhotoDrawPos.Y);
        }
        _canvas.Invalidate();
    }

    protected override void OnPointerReleased(PointerRoutedEventArgs e)
    {
        base.OnPointerReleased(e);
        _isDraging = false;
    }

    private void ScaleSliderValueChanged(object sender, Microsoft.UI.Xaml.Controls.Primitives.RangeBaseValueChangedEventArgs e)
    {
        ImageScale = Math.Round(e.NewValue,1);
    }

    CanvasBitmap _drawingBitmapData;
    private void ResetAll()
    {
        ResetZoom(true);
        RecalcPos();
        _canvas.CenterPoint = new(0);
        _canvas.Rotation = 0;
    }
    private void RecalcPos()
    {
        double width = _drawingBitmapData.Size.Width * (ImageScale * _frameAdaptScale);
        double height = _drawingBitmapData.Size.Height * (ImageScale * _frameAdaptScale);
        X = ((double)_canvas.ActualWidth - width) / 2;
        Y = ((double)_canvas.ActualHeight - height) / 2;
    }
    private async Task<CanvasBitmap> LoadBitmapAsync()
    {
        // Check if the bitmap is already in the cache
        if (_bitmapsStore.TryGetValue(ImageUri, out var cachedBitmap))
        {
            return cachedBitmap;
        }

        // Load the bitmap asynchronously
        var filePath = await StorageItemProvider.GetRawFilePath(ImageUri);
        var bitmap = await CanvasBitmap.LoadAsync(_canvas, filePath);
        _bitmapsStore[ImageUri] = bitmap;

        return bitmap;
    }

    private void OnDraw(CanvasControl sender, CanvasDrawEventArgs args)
    {
        if(_drawingBitmapData == null)
            return;
        var session = args.DrawingSession;
        session.Clear(new Vector4(0, 0, 0, 0));
        double width = _drawingBitmapData.Size.Width * (ImageScale * _frameAdaptScale);
        double height = _drawingBitmapData.Size.Height * (ImageScale * _frameAdaptScale);

        session.DrawImage(_drawingBitmapData, new Rect(X, Y, width, height));
        
    }
}
