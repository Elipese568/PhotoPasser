using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Imaging;
using Microsoft.Windows.Storage.Pickers;
using PhotoPasser.Converters;
using PhotoPasser.Helper;
using PhotoPasser.Service;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Windows.ApplicationModel.DataTransfer;
using Windows.Storage;
using Windows.Storage.Streams;
using Windows.System;

namespace PhotoPasser;

public enum DisplayView
{
    Trumbull,
    Details,
    Tiles
}

public enum SortOrder
{
    Ascending,
    Descending
}

public enum SortBy
{
    Name,
    Type,
    DateCreated,
    DateModified,
    TotalSize
}

public partial class TaskWorkspaceViewModel : ObservableObject, IDisposable
{
    private readonly ITaskDetailPhysicalManagerService _taskDpmService;

    [ObservableProperty]
    private PhotoInfo _selectedImage;

    [ObservableProperty]
    private SortBy _sortBy;

    [ObservableProperty]
    private SortOrder _sortOrder;

    [ObservableProperty]
    private Visibility _fileDetailHeaderVisibility = Visibility.Collapsed;

    [ObservableProperty]
    private bool _isMultiSelection;

    public FiltTask FiltTask { get; }
    public TaskDetail Detail { get; }
    public FiltResult CurrentResult { get; }
    public PhotoInfo RightTappedPhoto { get; set; }
    public BitmapImage PictureExtensionTrumbull { get; set; }

    public DisplayView CurrentView
    {
        get => field;
        set
        {
            if (field != value)
            {
                field = value;
                OnPropertyChanged(nameof(CurrentView));
                FileDetailHeaderVisibility = value == DisplayView.Details ? Visibility.Visible : Visibility.Collapsed;
            }
        }
    }

    public TaskWorkspaceViewModel(FiltTask task, ITaskDetailPhysicalManagerService service, TaskDetail detail, FiltResult result = null)
    {
        FiltTask = task;
        _taskDpmService = service;
        Detail = detail;
        CurrentResult = result;
    }

    [RelayCommand]
    public async Task Open(object param)
    {
        if (param is IEnumerable<PhotoInfo> photos)
        {
            foreach (var photo in photos)
            {
                var file = await StorageItemProvider.GetStorageFile(photo.Path);
                await Launcher.LaunchFileAsync(file);
            }
        }
        else if (param is PhotoInfo photo)
        {
            var file = await StorageItemProvider.GetStorageFile(photo.Path);
            await Launcher.LaunchFileAsync(file);
        }
    }

    [RelayCommand]
    public async Task OpenInExplorer(object param)
    {
        // 禁止多选
        if (param is PhotoInfo photo)
        {
            StorageFolder folder = await StorageItemProvider.GetStorageFolderFromFileParent(photo.Path);
            await Launcher.LaunchFolderAsync(folder);
        }
    }

    [RelayCommand]
    public async Task Copy(object param)
    {
        if (param is IEnumerable<PhotoInfo> photos)
        {
            var files = new List<StorageFile>();
            foreach (var photo in photos)
            {
                var file = await StorageItemProvider.GetStorageFile(photo.Path);
                if (file != null) files.Add(file);
            }
            var dataPackage = new Windows.ApplicationModel.DataTransfer.DataPackage();
            dataPackage.SetStorageItems(files);
            Windows.ApplicationModel.DataTransfer.Clipboard.SetContent(dataPackage);
        }
        else if (param is PhotoInfo photo)
        {
            var dataPackage = new Windows.ApplicationModel.DataTransfer.DataPackage();
            dataPackage.SetStorageItems([await StorageItemProvider.GetStorageFile(photo.Path)]);
            Windows.ApplicationModel.DataTransfer.Clipboard.SetContent(dataPackage);
        }
    }

    [RelayCommand]
    public void CopyAsPath(object param)
    {
        if (param is IEnumerable<PhotoInfo> photos)
        {
            var dataPackage = new Windows.ApplicationModel.DataTransfer.DataPackage();
            dataPackage.SetText(string.Join("\r\n", photos.Select(p => p.Path)));
            Windows.ApplicationModel.DataTransfer.Clipboard.SetContent(dataPackage);
        }
        else if (param is PhotoInfo photo)
        {
            var dataPackage = new Windows.ApplicationModel.DataTransfer.DataPackage();
            dataPackage.SetText(photo.Path);
            Windows.ApplicationModel.DataTransfer.Clipboard.SetContent(dataPackage);
        }
    }

    [RelayCommand]
    public async Task CopyAsBitmap(object param)
    {
        // 禁止多选
        if (param is PhotoInfo photo)
        {
            var dataPackage = new Windows.ApplicationModel.DataTransfer.DataPackage();
            dataPackage.SetBitmap(RandomAccessStreamReference.CreateFromFile(await StorageItemProvider.GetStorageFile(photo.Path)));
            Windows.ApplicationModel.DataTransfer.Clipboard.SetContent(dataPackage);
        }
    }

    [RelayCommand]
    public async Task Delete(object param)
    {
        var textBlock = new TextBlock
        {
            Text = "Once it’s gone, it’s gone. Are you sure you want to delete the file(s)?"
        };

        // 创建 CheckBox
        var CheckToRemoveRealFile = new CheckBox
        {
            Content = "Remove physical file",
            IsChecked = false
        };
        var RemoveTips = new StackPanel
        {
            Spacing = 16
        }.With(x =>
        {
            x.Children.Add(textBlock);
            if (!FiltTask.CopySource)
            {
                x.Children.Add(CheckToRemoveRealFile);
            }
            else
            {
                CheckToRemoveRealFile.IsChecked = true;
            }
        });

        ContentDialog cd = new ContentDialog()
        {
            Title = "Delete File",
            Content =  RemoveTips,
            PrimaryButtonText = "Delete",
            CloseButtonText = "Cancel",
            DefaultButton = ContentDialogButton.Close,
            XamlRoot = App.Current.MainWindow.Content.XamlRoot,
        };

        if(await cd.ShowAsync() != ContentDialogResult.Primary)
        {
            return;
        }

        if (param is IEnumerable<PhotoInfo> photos)
        {
            foreach (var photo in photos.ToList())
            {
                var file = await StorageItemProvider.GetStorageFile(photo.Path, false);
                if (FiltTask.CopySource || (file != null && (CheckToRemoveRealFile.IsChecked??false)))
                    await file.DeleteAsync();
                Detail.Photos.Remove(photo);
            }
        }
        else if (param is PhotoInfo photo)
        {
            var file = await StorageItemProvider.GetStorageFile(photo.Path, false);
            if (FiltTask.CopySource || (file != null && (CheckToRemoveRealFile.IsChecked ?? false)))
                await file.DeleteAsync();
            Detail.Photos.Remove(photo);
        }
    }

    [RelayCommand]
    public async Task Rename((PhotoInfo photoInfo, string newName, bool inResourceView) param)
    {
        (PhotoInfo photoInfo, string newName, bool inResourceView) = param;
        if (inResourceView)
        {
            // 资源视图：更新 TaskDetail.Photos
            if (Detail?.Photos != null)
                Detail.Photos[Detail.Photos.IndexOf(x => x.Path == photoInfo.Path)].UserName = newName;
            OnPropertyChanged(nameof(Detail));
        }
        else
        {
            // 结果视图：更新 FiltResult.Photos
            if (CurrentResult?.Photos != null)
                CurrentResult.Photos[CurrentResult.Photos.IndexOf(x => x.Path == photoInfo.Path)].UserName = newName;
            OnPropertyChanged(nameof(CurrentResult));
        }
    }

    [RelayCommand]
    public async Task ShowProperties(object param)
    {
        if (param is IEnumerable<PhotoInfo> photos)
        {
            var paths = await photos.Select(p => StorageItemProvider.GetRawFilePath(p.Path)).EvalResults().ToListAsync();
            ShellInterop.ShowFileProperties(paths.ToArray());
        }
        else if (param is PhotoInfo photo)
        {
            ShellInterop.ShowFileProperties(await StorageItemProvider.GetRawFilePath(photo.Path), WinRT.Interop.WindowNative.GetWindowHandle(App.Current.MainWindow));
        }
    }

    public async Task Add()
    {
        FileOpenPicker picker = new FileOpenPicker(App.Current.MainWindow.AppWindow.Id);
        picker.FileTypeFilter.Add("*");
        var files = await picker.PickMultipleFilesAsync();
        if (files != null && files.Count > 0)
        {
            foreach (var file in files)
            {
                await AddPhoto(file.Path);
            }
        }
    }

    public async Task AddPhoto(string path)
    {
        Uri fileUri = new(path);
        try
        {
            System.Drawing.Image.FromFile(fileUri.LocalPath);
        }
        catch
        {
            return;
        }
        if(FiltTask.CopySource)
        {
            fileUri = await _taskDpmService.CopySourceAsync(fileUri);
        }

        Detail.Photos.Add(await PhotoInfo.Create(fileUri.LocalPath));
    }

    public ObservableCollection<PhotoInfo> GetSortedPhotos(ObservableCollection<PhotoInfo> origin, SortBy sortBy, SortOrder order)
    {
        if(origin == null)
        {
            origin = new ObservableCollection<PhotoInfo>();
            return origin;
        }
        return new (sortBy switch
        {
            SortBy.Name => order == SortOrder.Ascending
                ? origin.OrderBy(x => x.UserName)
                : origin.OrderByDescending(x => x.UserName),

            SortBy.Type => order == SortOrder.Ascending
                ? origin.OrderBy(x => x.UserName.Split(".")[^1])
                : origin.OrderByDescending(x => x.UserName.Split(".")[^1]),

            SortBy.DateCreated => order == SortOrder.Ascending
                ? origin.OrderBy(x => x.DateCreated)
                : origin.OrderByDescending(x => x.DateCreated),

            SortBy.DateModified => order == SortOrder.Ascending
                ? origin.OrderBy(x => x.DateModified)
                : origin.OrderByDescending(x => x.DateModified),

            SortBy.TotalSize => order == SortOrder.Ascending
                ? origin.OrderBy(x => x.Size)
                : origin.OrderByDescending(x => x.Size),

            _ => origin // 可选的默认情况
        });
    }

    public async Task PasteClipboardFiles()
    {
        var dataView = Clipboard.GetContent();
        if(dataView.Contains(Windows.ApplicationModel.DataTransfer.StandardDataFormats.StorageItems))
        {
            var items = await dataView.GetStorageItemsAsync();
            foreach (var item in items)
            {
                if (item is StorageFile file)
                {
                    await AddPhoto(file.Path);
                }
            }
        }
    }

    public void Dispose()
    {
        _taskDpmService.Dispose();
    }
}
