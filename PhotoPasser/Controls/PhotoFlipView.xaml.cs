using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using CommunityToolkit.Mvvm.ComponentModel;
using System.Collections.ObjectModel;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace PhotoPasser.Controls;

[ObservableObject]
public sealed partial class PhotoFlipView : UserControl
{


    public ObservableCollection<PhotoInfo> Photos
    {
        get { return (ObservableCollection<PhotoInfo>)GetValue(PhotosProperty); }
        set { SetValue(PhotosProperty, value); OnPropertyChanged(); }
    }

    // Using a DependencyProperty as the backing store for Photos.  This enables animation, styling, binding, etc...
    public static readonly DependencyProperty PhotosProperty =
        DependencyProperty.Register(nameof(Photos), typeof(ObservableCollection<PhotoInfo>), typeof(PhotoFlipView), new PropertyMetadata(null));



    public PhotoInfo SelectedImage
    {
        get { return (PhotoInfo)GetValue(SelectedImageProperty); }
        set { SetValue(SelectedImageProperty, value); OnPropertyChanged(); }
    }

    // Using a DependencyProperty as the backing store for SelectedImage.  This enables animation, styling, binding, etc...
    public static readonly DependencyProperty SelectedImageProperty =
        DependencyProperty.Register(nameof(SelectedImage), typeof(PhotoInfo), typeof(PhotoFlipView), new PropertyMetadata(null));



    public int SelectedIndex
    {
        get { return (int)GetValue(SelectedIndexProperty); }
        set { SetValue(SelectedIndexProperty, value); OnPropertyChanged(); }
    }

    // Using a DependencyProperty as the backing store for SelectedIndex.  This enables animation, styling, binding, etc...
    public static readonly DependencyProperty SelectedIndexProperty =
        DependencyProperty.Register(nameof(SelectedIndex), typeof(int), typeof(PhotoFlipView), new PropertyMetadata(0));



    public DataTemplate HeaderTemplate
    {
        get { return (DataTemplate)GetValue(HeaderTemplateProperty); }
        set { SetValue(HeaderTemplateProperty, value); OnPropertyChanged(); }
    }

    // Using a DependencyProperty as the backing store for HeaderTemplate.  This enables animation, styling, binding, etc...
    public static readonly DependencyProperty HeaderTemplateProperty =
        DependencyProperty.Register(nameof(HeaderTemplate), typeof(DataTemplate), typeof(PhotoFlipView), new PropertyMetadata(null));



    public DataTemplate ItemTemplate
    {
        get { return (DataTemplate)GetValue(ItemTemplateProperty); }
        set { SetValue(ItemTemplateProperty, value); OnPropertyChanged(); }
    }

    // Using a DependencyProperty as the backing store for ItemTemplate.  This enables animation, styling, binding, etc...
    public static readonly DependencyProperty ItemTemplateProperty =
        DependencyProperty.Register(nameof(ItemTemplate), typeof(DataTemplate), typeof(PhotoFlipView), new PropertyMetadata(null));

    [ObservableProperty]
    private double _zoomScale = 1.0;

    public PhotoFlipView()
    {
        RegisterPropertyChangedCallback(SelectedIndexProperty, (obj, prop) =>
        {
            if(SelectedIndex < Photos.Count)
                View.SelectedIndex = SelectedIndex;
            else
                SelectedIndex = Photos.Count - 1;

        });
        InitializeComponent();
    }

    private void FlipView_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if(e.AddedItems.Count == 0) return;

        SelectedImage = e.AddedItems[0] as PhotoInfo;
        SelectedIndex = (sender as FlipView).SelectedIndex;
    }

    private void UserControl_PointerEntered(object sender, PointerRoutedEventArgs e)
    {
        VisualStateManager.GoToState(this, "ShowHeader", true);
    }

    private void UserControl_PointerExited(object sender, PointerRoutedEventArgs e)
    {
        VisualStateManager.GoToState(this, "HideHeader", true);
    }

    public double Decrease(int val) => val - 1;
    public string StrDecrease(int val) => (val - 1).ToString();
    public int FallbackToZero(int val) => val < 0 ? 0 : val;
    public string StrFallbackToZero(int val) => (val < 0 ? 0 : val).ToString();

    private void GoFirstButton_Click(object sender, RoutedEventArgs e)
    {
        GoFirst();
    }

    private void GoFirst()
    {
        SelectedIndex = 0;
    }

    private void PreviousButton_Click(object sender, RoutedEventArgs e)
    {
        GoPrevious();
    }

    private void GoPrevious()
    {
        SelectedIndex = FallbackToZero(SelectedIndex - 1);
    }

    private void NextButton_Click(object sender, RoutedEventArgs e)
    {
        GoNext();
    }

    private void GoNext()
    {
        SelectedIndex = SelectedIndex + 1 >= Photos.Count ? SelectedIndex : SelectedIndex + 1;
    }

    private void GoLastButton_Click(object sender, RoutedEventArgs e)
    {
        GoLast();
    }

    private void GoLast()
    {
        SelectedIndex = Photos.Count - 1;
    }

    private void Previous_KeyboardAccelerator_Invoked(KeyboardAccelerator sender, KeyboardAcceleratorInvokedEventArgs args)
    {
        GoPrevious();
    }
    private void Next_KeyboardAccelerator_Invoked(KeyboardAccelerator sender, KeyboardAcceleratorInvokedEventArgs args)
    {
        GoNext();
    }

    private void GoFirst_KeyboardAccelerator_Invoked(KeyboardAccelerator sender, KeyboardAcceleratorInvokedEventArgs args)
    {
        GoFirst();
    }
    private void GoLast_KeyboardAccelerator_Invoked(KeyboardAccelerator sender, KeyboardAcceleratorInvokedEventArgs args)
    {
        GoLast();
    }
    public DataTemplate GetImageView(bool advanced)
        => advanced ? AdvancedImageView : ImageView;
}
