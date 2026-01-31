using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using CommunityToolkit.WinUI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Navigation;
using PhotoPasser.Helper;
using PhotoPasser.Models;
using PhotoPasser.Strings;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

namespace PhotoPasser.Views;

using PhotoPasser.Primitive;
using PhotoPasser.ViewModels;

public class FiltingItemTrumbullMaskBrushConverter : DependencyObject, IValueConverter
{
    public object UnsetFill
    {
        get { return (object)GetValue(UnsetFillProperty); }
        set { SetValue(UnsetFillProperty, value); }
    }

    public static readonly DependencyProperty UnsetFillProperty =
        DependencyProperty.Register(nameof(UnsetFill), typeof(object), typeof(FiltingItemTrumbullMaskBrushConverter), new PropertyMetadata(new object()));

    public object FilteredFill
    {
        get { return (object)GetValue(FilteredFillProperty); }
        set { SetValue(FilteredFillProperty, value); }
    }

    public static readonly DependencyProperty FilteredFillProperty =
        DependencyProperty.Register(nameof(FilteredFill), typeof(object), typeof(FiltingItemTrumbullMaskBrushConverter), new PropertyMetadata(new object()));

    public object UnfilteredFill
    {
        get { return (object)GetValue(UnfilteredFillProperty); }
        set { SetValue(UnfilteredFillProperty, value); }
    }

    public static readonly DependencyProperty UnfilteredFillProperty =
        DependencyProperty.Register(nameof(UnfilteredFill), typeof(object), typeof(FiltingItemTrumbullMaskBrushConverter), new PropertyMetadata(new object()));

    public object Convert(object value, Type targetType, object parameter, string language)
    {
        var status = (FiltingStatus)value;
        return status switch
        {
            FiltingStatus.Unset => UnsetFill,
            FiltingStatus.Filtered => FilteredFill,
            FiltingStatus.Unfiltered => UnfilteredFill,
            _ => UnsetFill
        };
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        throw new NotImplementedException();
    }
}

public sealed partial class ProcessingPage : Page
{
    public ProcessingPageViewModel ViewModel { get; } = new ProcessingPageViewModel();

    public ProcessingPage()
    {
        InitializeComponent();
    }

    protected override async void OnNavigatedTo(NavigationEventArgs e)
    {
        if (e.Parameter is IEnumerable<PhotoInfo> photos)
        {
            ViewModel.DisplayedPhotos = photos.Select(x => new PhotoInfoViewModel(x)).AsObservable();
            ViewModel.FiltingPhotos = photos.Select(x => FiltingPhotoInfoViewModel.Create(x)).AsObservable();
        }
        base.OnNavigatedTo(e);
    }

    private void TextBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        StartButton.IsEnabled = !string.IsNullOrEmpty(ViewModel.Name);
    }

    private void ExitButton_Click(object sender, RoutedEventArgs e)
    {
        App.GetService<MainWindow>()!.Frame.GoBack();
    }

    private async void StartButton_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrEmpty(ViewModel.Name))
        {
            ContentDialog cd = new ContentDialog()
            {
                Content = "ProjectNameRequiredErrorContent".GetLocalized(LC.ProcessingPage),
                Title = "ErrorPrompt".GetLocalized(LC.General),
                CloseButtonText = "OkPrompt".GetLocalized(LC.General),
            }.With(x => x.ApplyApplicationOption());

            await cd.ShowAsync();
            return;
        }
        ViewModel.Step = "Filting";
    }

    private async void GridView_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (e.AddedItems.Count == 0)
            return;
        var obj = e.AddedItems[0];
        await (sender as GridView)!.SmoothScrollIntoViewWithItemAsync(obj, CommunityToolkit.WinUI.ScrollItemPlacement.Center, scrollIfVisible: true);
    }

    private async void Reject_KeyboardAccelerator_Invoked(KeyboardAccelerator sender, KeyboardAcceleratorInvokedEventArgs args)
    {
        await DispatcherQueue.EnqueueAsync(async () =>
        {
            VisualStateManager.GoToState(RejectButton, "PointerOver", true);
            await Task.Delay(50);
            VisualStateManager.GoToState(RejectButton, "Pressed", true);
            await Task.Delay(132);
            VisualStateManager.GoToState(RejectButton, "Normal", true);
        });
    }

    private async void Accept_KeyboardAccelerator_Invoked(KeyboardAccelerator sender, KeyboardAcceleratorInvokedEventArgs args)
    {
        await DispatcherQueue.EnqueueAsync(async () =>
        {
            VisualStateManager.GoToState(AcceptButton, "PointerOver", true);
            await Task.Delay(50);
            VisualStateManager.GoToState(AcceptButton, "Pressed", true);
            await Task.Delay(132);
            VisualStateManager.GoToState(AcceptButton, "Normal", true);
        });
    }

    private void RejectButton_Click(object sender, RoutedEventArgs e)
    {
        Reject();
    }

    private void Reject()
    {
        if (ViewModel.SelectedPhotoIndex >= ViewModel.FiltingPhotos.Count) return;

        var oriStatus = ViewModel.FiltingPhotos[ViewModel.SelectedPhotoIndex].Status;
        ViewModel.FiltingPhotos[ViewModel.SelectedPhotoIndex].Status = FiltingStatus.Unfiltered;
        ViewModel.SelectedPhotoIndex++;
        if (oriStatus == FiltingStatus.Filtered)
        {
            ViewModel.AcceptedCount--;
        }
        if (oriStatus != FiltingStatus.Unfiltered)
            ViewModel.RejectedCount++;
    }

    private void AcceptButton_Click(object sender, RoutedEventArgs e)
    {
        Accept();
    }

    private void Accept()
    {
        if (ViewModel.SelectedPhotoIndex >= ViewModel.FiltingPhotos.Count) return;

        var oriStatus = ViewModel.FiltingPhotos[ViewModel.SelectedPhotoIndex].Status;
        ViewModel.FiltingPhotos[ViewModel.SelectedPhotoIndex].Status = FiltingStatus.Filtered;
        ViewModel.SelectedPhotoIndex++;
        if (oriStatus == FiltingStatus.Unfiltered)
        {
            ViewModel.RejectedCount--;
        }
        if (oriStatus != FiltingStatus.Filtered)
            ViewModel.AcceptedCount++;
    }

    public string CountToPrecentStr(int count, int total)
    {
        if (total == 0)
            return "0.00%";
        double precent = (double)count / total * 100;
        return $"{precent:0.##}%";
    }

    private async void FinishButton_Click(object sender, RoutedEventArgs e)
    {
        if ((ViewModel.AcceptedCount + ViewModel.RejectedCount) != ViewModel.FiltingPhotos.Count)
        {
            ContentDialog cd = new ContentDialog()
            {
                Content = "UnvotedPhotosErrorContent".GetLocalized(LC.ProcessingPage),
                Title = "ErrorPrompt".GetLocalized(LC.General),
                CloseButtonText = "OkPrompt".GetLocalized(LC.General),
            }.With(x => x.ApplyApplicationOption());
            await cd.ShowAsync();
            return;
        }
        if (ViewModel.RejectedCount == ViewModel.FiltingPhotos.Count)
        {
            ContentDialog cd = new ContentDialog()
            {
                Content = "AllRejectedErrorContent".GetLocalized(LC.ProcessingPage),
                Title = "ErrorPrompt".GetLocalized(LC.General),
                CloseButtonText = "OkPrompt".GetLocalized(LC.General),
            }.With(x => x.ApplyApplicationOption());
            await cd.ShowAsync();
            return;
        }

        WeakReferenceMessenger.Default.Send(new FiltingFinishedMessage()
        {
            FiltedPhotos = ViewModel.FiltingPhotos.Where(x => x.Status == FiltingStatus.Filtered).Select(x => x.Model).ToList(),
            Name = ViewModel.Name,
            Description = ViewModel.Description
        });
        App.GetService<MainWindow>()!.Frame.GoBack();
    }
}

internal class FiltingFinishedMessage
{
    public List<PhotoInfo> FiltedPhotos { get; set; }
    public string Name { get; set; }
    public string Description { get; set; }
}