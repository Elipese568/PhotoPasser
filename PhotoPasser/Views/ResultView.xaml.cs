using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using CommunityToolkit.WinUI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using Microsoft.Windows.Storage.Pickers;
using PhotoPasser.Converters;
using PhotoPasser.Dialog;
using PhotoPasser.Helper;
using PhotoPasser.Strings;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Storage.Pickers.Provider;
using Windows.System;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace PhotoPasser.Views;

using PhotoPasser.Primitive;
using PhotoPasser.ViewModels;

/// <summary>
/// An empty page that can be used on its own or navigated to within a Frame.
/// </summary>

public sealed partial class ResultView : Page
{
    public TaskWorkspaceViewModel ParentViewModel;
    public ResultViewModel ViewModel { get; set; }

    public ResultView()
    {
        InitializeComponent();
    }

    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        var param = e.Parameter as ResultViewNavigationParameter;
        ViewModel = new ResultViewModel()
        {
            TaskDpmService = param.TaskDpmService,
            CurrentResult = new PhotoPasser.ViewModels.FiltResultViewModel(param.NavigatingResult)
        };

        base.OnNavigatedTo(e);
    }

    private async void PhotoGalleryViewer_CopyResolve(Controls.PhotoGalleryViewer sender, Controls.CopyPhotoResolveEventArgs args)
    {
        await PhotoItemOperationUtils.Copy(args.CopyItems);
    }

    private async void PhotoGalleryViewer_CopyAsBitmapResolve(Controls.PhotoGalleryViewer sender, Controls.CopyPhotoResolveEventArgs args)
    {
        // checked in control for single item
        await PhotoItemOperationUtils.CopyAsBitmap(args.CopyItems[0]);
    }

    private async void PhotoGalleryViewer_CopyAsPathResolve(Controls.PhotoGalleryViewer sender, Controls.CopyPhotoResolveEventArgs args)
    {
        await PhotoItemOperationUtils.CopyAsPath(args.CopyItems);
    }
    private object PhotoGalleryViewer_DeleteResolving(Controls.PhotoGalleryViewer sender, Controls.DeletePhotoResolvingEventArgs args)
    {
        (bool cancel, bool removePhysicalFile) = ParentViewModel.DeleteRequesting().AsAsyncOperation().Get();
        args.Cancel = cancel;
        return removePhysicalFile;
    }

    private async void PhotoGalleryViewer_RenameResolve(Controls.PhotoGalleryViewer sender, Controls.RenameResolveEventArgs args)
    {
        await PhotoItemOperationUtils.Rename(args.RenameItem, args.NewName);
    }

    private void GridView_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (e.AddedItems.Count == 0)
            return;
        var obj = e.AddedItems[0];
        (sender as GridView)!.SmoothScrollIntoViewWithItemAsync(obj, ScrollItemPlacement.Center, scrollIfVisible: true);
    }

    private async void ViewInFileExplorer_PhotoGeneralOperation_Invoked(Controls.PhotoGalleryViewer sender, Controls.ItemOperationInvokedEventArgs args)
    {
        await ViewModel.GotoResultFolder();
    }

    private FriendlySizeTextFormatConverter _innerSizeTextConverter = new();
    public string GetSelectedItemTotalSize(ObservableCollection<PhotoInfoViewModel> photos)
    {
        return (_innerSizeTextConverter.Convert(photos.Sum(p => p.Size), null, null, null) as string)!;
    }

    private void FavoriteButton_Click(object sender, RoutedEventArgs e)
    {
        string action;
        if(ViewModel.CurrentResult.IsFavorite)
        {
            action = "RemoveFromFavorite";
        }
        else
        {
            action = "AddToFavorite";
        }
        WeakReferenceMessenger.Default.Send(ViewModel.CurrentResult, action);
    }

    private async void EditButton_Click(object sender, RoutedEventArgs e)
    {
        var page = new EditResultInformationDialogPage(ViewModel.CurrentResult.Model);
        ContentDialog cd = new()
        {
            Title = "EditResultInformationTitle".GetLocalized(LC.ResultView),
            Content = page,
            PrimaryButtonText = "SavePrompt".GetLocalized(LC.General),
            CloseButtonText = "CancelPrompt".GetLocalized(LC.General),
            DefaultButton = ContentDialogButton.Primary,
            XamlRoot = this.XamlRoot
        };

        if (await cd.ShowAsync() != ContentDialogResult.Primary)
            return;

        ViewModel.CurrentResult.Name = page.ViewModel.Name;
        ViewModel.CurrentResult.Description = page.ViewModel.Description;
    }

    private void DeleteButton_Click(object sender, RoutedEventArgs e)
    {
        WeakReferenceMessenger.Default.Send(ViewModel.CurrentResult, "DeleteResult");
    }

    private async void GotoFileExplorerButton_Click(object sender, RoutedEventArgs e)
    {
        await ViewModel.GotoResultFolder();
    }

    private async void ExportButton_Click(object sender, RoutedEventArgs e)
    {
        FileSavePicker savePicker = new FileSavePicker(App.GetService<MainWindow>()!.AppWindow.Id);
        savePicker.FileTypeChoices.Add("ZipDescriptor".GetLocalized(LC.General), [".zip"]);
        var file = await savePicker.PickSaveFileAsync();
        if (file == null)
            return;
        using var exportFileStream = File.OpenWrite(file.Path);
        ZipFile.CreateFromDirectory(await ViewModel.GetResultFolder(), exportFileStream);
    }
    public Visibility GetCollectionVisibility(ObservableCollection<PhotoInfoViewModel> collections)
    {
        return collections == null || collections.Count == 0? Visibility.Collapsed : Visibility.Visible;
    }

    private void PinPhoto_Invoked(Controls.PhotoGalleryViewer sender, Controls.ItemOperationInvokedEventArgs args)
    {
        foreach(var item in args.OperationItems)
        {
            // args.OperationItems are PhotoInfo (models)
            if(ViewModel.CurrentResult.PinnedPhotos.Any(p => p.Path == item.Path))
            {
                continue;
            }
            // add model to result's model collection and also update VM
            ViewModel.CurrentResult.Model.PinnedPhotos.Add(item);
            ViewModel.CurrentResult.UpdateFromModel();
        }
    }

    private void UnPinButton_Click(object sender, RoutedEventArgs e)
    {
        var photo = (sender as Button).DataContext as PhotoInfoViewModel;
        var model = photo?.Model;
        if (model != null)
        {
            ViewModel.CurrentResult.Model.PinnedPhotos.Remove(model);
            ViewModel.CurrentResult.UpdateFromModel();
        }
    }

    private void ContinueToFilter_PhotoGeneralOperation_Invoked(Controls.PhotoGalleryViewer sender, Controls.ItemOperationInvokedEventArgs args)
    {
        // pass models list
        App.GetService<MainWindow>()!.Frame.Navigate(typeof(ProcessingPage), ViewModel.CurrentResult.Photos);
    }

    private void ContinueToFilterBasedOnSelected_Invoked(Controls.PhotoGalleryViewer sender, Controls.ItemOperationInvokedEventArgs args)
    {
        App.GetService<MainWindow>()!.Frame.Navigate(typeof(ProcessingPage), EnumerableExtensions.AsObservable(args.OperationItems.ToList()));
    }
}
