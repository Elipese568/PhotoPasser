using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using CommunityToolkit.WinUI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Imaging;
using Microsoft.Windows.Storage.Pickers;
using PhotoPasser.Converters;
using PhotoPasser.Helper;
using PhotoPasser.Service;
using PhotoPasser.Strings;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Windows.ApplicationModel.DataTransfer;
using Windows.Storage;
using Windows.Storage.Streams;
using Windows.System;
using PhotoPasser.Models;
using PhotoPasser.Primitive;

namespace PhotoPasser.ViewModels;


public partial class TaskWorkspaceViewModel : ObservableRecipient, IDisposable
{
    private readonly ITaskDetailPhysicalManagerService _taskDpmService;
    public ITaskDetailPhysicalManagerService TaskDpmService => _taskDpmService;
    private readonly IClipboardService _clipboardService;
    private readonly IDialogService _dialogService;
    private readonly IWorkspaceViewManager? _workspaceViewManager;

    private WorkspaceViewState? _workspaceStateCache;
    private Timer? _saveDebounceTimer;
    private readonly TimeSpan _saveDebounceDelay = TimeSpan.FromMilliseconds(500);

    [ObservableProperty]
    private PhotoInfo _selectedImage;

    public SortBy SortBy
    {
        get => (SortBy)(_workspaceStateCache?.SortBy ?? default);
        set
        {
            if ((SortBy)_workspaceStateCache.SortBy == value) return;

            _workspaceStateCache.SortBy = (PhotoPasser.Primitive.SortBy)value;
            DebouncedSaveWorkspaceState();
        }
        }

    private ObservableCollection<PhotoInfoViewModel> _photos;
    public ObservableCollection<PhotoInfoViewModel> Photos
    {
        get => _photos; 
        set
        {
            if (_photos == value) return;
            _photos = value;
            OnPropertyChanged(nameof(Photos));
        }
    }

    public SortOrder SortOrder
    {
        get => (SortOrder)(_workspaceStateCache?.SortOrder ?? default);
        set
        {
            if ((SortOrder)_workspaceStateCache.SortOrder == value) return;

            _workspaceStateCache.SortOrder = (PhotoPasser.Primitive.SortOrder)value;
            DebouncedSaveWorkspaceState();
        }
    }

    [ObservableProperty]
    private ObservableCollection<PhotoInfoViewModel> _searchResult = new EmptyPhotoCollection();

    [ObservableProperty]
    private FiltResult? _currentResult;

    private ObservableCollection<FiltResult> _favoriteResults;

    [ObservableProperty]
    private ObservableCollection<FiltResult> _displayedResults;
    [ObservableProperty]
    private int _displayedResultSelectedIndex;
    [ObservableProperty]
    private bool _isFavoriteResultsSelection;
    [ObservableProperty]
    private FiltResult _navigatingResult;

    public FiltTaskViewModel FiltTask { get; }
    public TaskDetailViewModel Detail { get; }

    public DisplayView CurrentView
    {
        get => (DisplayView)(_workspaceStateCache?.CurrentView ?? default);
        set
        {
            if ((DisplayView)_workspaceStateCache.CurrentView == value) return;

            _workspaceStateCache.CurrentView = (PhotoPasser.Primitive.DisplayView)value;
            DebouncedSaveWorkspaceState();
        }
    }

    public TaskWorkspaceViewModel(FiltTask task, ITaskDetailPhysicalManagerService service, TaskDetail detail, FiltResult? result = null)
    {
        FiltTask = new(task);
        _taskDpmService = service;
        Detail = new(detail);
        CurrentResult = result;
        _workspaceViewManager = service.WorkspaceViewManager;


        // try resolve clipboard service from root provider (scoped provider not available here)
        _clipboardService = App.GetService<IClipboardService>()!;
        _dialogService = App.GetService<IDialogService>()!;

        _favoriteResults = new ObservableCollection<FiltResult>(Detail.Results.Where(r => r.IsFavorite));
        _displayedResults = Detail.Results;
        Photos = new ObservableCollection<PhotoInfoViewModel>((Detail.Photos ?? new()).Select(p => new PhotoInfoViewModel(p)));

        WeakReferenceMessenger.Default.Register<FiltResult, string>(this, "AddToFavorite", AddToFavorite);

        WeakReferenceMessenger.Default.Register<FiltResult, string>(this, "RemoveFromFavorite", (r, m) =>
        {
            m.IsFavorite = false;
            _favoriteResults.Remove(m);
            if (!_favoriteResults.Any() && IsFavoriteResultsSelection)
            {
                IsFavoriteResultsSelection = false;
                DisplayedResultSelectedIndex = Detail.Results.IndexOf(m);
            }
        });

        WeakReferenceMessenger.Default.Register<FiltResult, string>(this, "DeleteResult", (r, m) =>
        {
            Detail.Results.Remove(m);
            _favoriteResults.Remove(m);
            if (_currentResult == m || _navigatingResult == m)
            {
                if (_displayedResults.FirstOrDefault() is FiltResult next)
                {
                    CurrentResult = next;
                    NavigatingResult = next;
                }
                else
                {
                    CurrentResult = null;
                    NavigatingResult = null;
                    WeakReferenceMessenger.Default.Send("NoResultAvaliable", "NoResultAvaliable");
                }
            }
        });

        WeakReferenceMessenger.Default.Register<FiltingFinishedMessage>(this, (r, m) =>
        {
            var resultObj = new FiltResult()
            {
                Date = DateTime.Now,
                Description = m.Description,
                Name = m.Name,
                Photos = m.FiltedPhotos ?? new List<PhotoInfo>(),
                PinnedPhotos = new List<PhotoInfo>(),
                IsFavorite = false,
                ResultId = Guid.NewGuid()
            };
            Detail.Results.Add(resultObj);
            CurrentResult = resultObj;
            DisplayedResultSelectedIndex = DisplayedResults.IndexOf(NavigatingResult);
            IsFavoriteResultsSelection = false;
            _taskDpmService.ProcessPhysicalResultAsync(resultObj).ConfigureAwait(false);
            WeakReferenceMessenger.Default.Send(resultObj, "NavigateToNewResult");
        });

        PropertyChanging += TaskWorkspaceViewModel_PropertyChanging;
    }

    private void AddToFavorite(object r, FiltResult m)
    {
        m.IsFavorite = true;
        _favoriteResults.Add(m);
    }

    private void TaskWorkspaceViewModel_PropertyChanging(object? sender, System.ComponentModel.PropertyChangingEventArgs e)
    {
        if (e.PropertyName == nameof(IsFavoriteResultsSelection))
        {
            if (CurrentResult != null)
                NavigatingResult = CurrentResult;
            DisplayedResults = !IsFavoriteResultsSelection ? _favoriteResults : Detail.Results;
            DisplayedResultSelectedIndex = DisplayedResults.IndexOf(NavigatingResult);
        }
    }

    public async Task Initialize()
    {
        if (_workspaceViewManager != null)
        {
            _workspaceViewManager.StateChanged += WorkspaceViewManager_StateChanged;
            await LoadWorkspaceStateAsync();
        }
    }

    private void WorkspaceViewManager_StateChanged(object? sender, WorkspaceViewState e)
    {
        // external change -> apply
        ApplyWorkspaceState(e);
    }

    public void ApplyWorkspaceState(WorkspaceViewState? state)
    {
        if (state == null) return;

        _workspaceStateCache = state;
        CurrentView = (DisplayView)state.CurrentView;
        SortBy = (SortBy)state.SortBy;
        SortOrder = (SortOrder)state.SortOrder;
        SearchResult = new EmptyPhotoCollection();
        // selection restoration should be handled in view (page) because it requires UI elements
    }

    private async Task LoadWorkspaceStateAsync()
    {
        var st = await _workspaceViewManager!.LoadAsync();
        if (st != null)
        {
            ApplyWorkspaceState(st);
        }
    }

    private void DebouncedSaveWorkspaceState()
    {
        _saveDebounceTimer?.Change(Timeout.Infinite, Timeout.Infinite);
        _saveDebounceTimer ??= new Timer(async _ => await SaveWorkspaceStateAsync(), null, Timeout.Infinite, Timeout.Infinite);
        _saveDebounceTimer.Change(_saveDebounceDelay, Timeout.InfiniteTimeSpan);
    }

    private async Task SaveWorkspaceStateAsync()
    {
        try
        {
            var state = new WorkspaceViewState
            {
                CurrentView = (PhotoPasser.Primitive.DisplayView)this.CurrentView,
                SortBy = (PhotoPasser.Primitive.SortBy)this.SortBy,
                SortOrder = (PhotoPasser.Primitive.SortOrder)this.SortOrder,
                SearchText = null // could be wired if you expose SearchText property
            };
            await _workspaceViewManager!.SaveAsync(state);
        }
        catch
        {
        }
    }

    public async Task AddPhoto(string path)
    {
        // Delegate validation/copying to the physical manager service
        var prepared = await _taskDpmService.PrepareSourceAsync(path);
        if (prepared == null)
            return;

        var photoInfo = await _taskDpmService.CreatePhotoInfoAsync(prepared);

        Detail.Photos.Add(photoInfo);
        Photos.Add(new PhotoInfoViewModel(photoInfo));

        if (Detail.Photos.Count == 1)
        {
            FiltTask.PresentPhoto = photoInfo.Path;
        }
    }
    
    public void Dispose()
    {
        _taskDpmService.Dispose();
    }

    public async Task<(bool Cancel, bool RemovePhysical)> DeleteRequesting()
    {
        var textBlock = new TextBlock
        {
            Text = "DeleteTip".GetLocalized(LC.General).Replace(ReplaceItem.DeleteItemPron, "DeleteFilePron".GetLocalized(LC.TaskWorkspace))
        };

        // ´´½¨ CheckBox
        var CheckToRemoveRealFile = new CheckBox
        {
            Content = "RemovePhysicalFileTip".GetLocalized(LC.TaskWorkspace),
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

        if (!await _dialogService.ShowConfirmAsync(
                        title: "DeleteFileTitle".GetLocalized(LC.TaskWorkspace)!,
                        message: RemoveTips,
                        primaryButtonText: "DeletePrompt".GetLocalized(LC.General)!,
                        closeButtonText: "CancelPrompt".GetLocalized(LC.General)!))
        {
            return (true, false);
        }

        var shouldRemovePhysical = FiltTask.CopySource || (CheckToRemoveRealFile.IsChecked ?? false);

        return (false, shouldRemovePhysical);
    }

    public async Task Delete(PhotoInfo photo, bool shouldRemovePhysical)
    {
        if (shouldRemovePhysical)
        {
            await _taskDpmService.DeletePhotoFileIfExistsAsync(photo.Path);
        }
        Detail.Photos.Remove(photo);

        if (!Detail.Photos.Any(x => x.Path == FiltTask.PresentPhoto))
        {
            FiltTask.PresentPhoto = Detail.Photos.Count > 0 ? Detail.Photos[0].Path : App.Current.Resources["EmptyTaskItemPresentPhotoPath"] as string;
        }
    }
}
