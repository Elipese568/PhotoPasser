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
using System.Drawing.Printing;

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

    private ObservableCollection<FiltResultViewModel> _favoriteResults;

    [ObservableProperty]
    private ObservableCollection<FiltResultViewModel> _displayedResults;
    [ObservableProperty]
    private int _displayedResultSelectedIndex;
    [ObservableProperty]
    private bool _isFavoriteResultsSelection;

    public FiltResultViewModel CurrentResultViewModel { get; set; }

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
        _workspaceViewManager = service.WorkspaceViewManager;

        // try resolve clipboard service from root provider (scoped provider not available here)
        _clipboardService = App.GetService<IClipboardService>()!;
        _dialogService = App.GetService<IDialogService>()!;

        // 创建 FiltResultViewModel 集合
        _favoriteResults = new ObservableCollection<FiltResultViewModel>(Detail.Results.Where(r => r.IsFavorite).Select(r => new FiltResultViewModel(r)));

        // Initialize displayed collection by explicitly requesting data source for default mode (show all)
        _displayedResults = ProvideAllResults();

        Photos = new ObservableCollection<PhotoInfoViewModel>((Detail.Photos ?? Enumerable.Empty<PhotoInfo>()).Select(p => new PhotoInfoViewModel(p)));

        WeakReferenceMessenger.Default.Register<FiltResultViewModel, string>(this, "AddToFavorite", AddToFavorite);
        WeakReferenceMessenger.Default.Register<FiltResultViewModel, string>(this, "RemoveFromFavorite", HandleRemoveFromFavorite);
        WeakReferenceMessenger.Default.Register<FiltResultViewModel, string>(this, "DeleteResult", HandleDeleteResult);
        WeakReferenceMessenger.Default.Register<FiltingFinishedMessage>(this, HandleFiltingFinished);

        PropertyChanging += TaskWorkspaceViewModel_PropertyChanging;

        CurrentResultViewModel = ProvideAllResults()[0];
    }

    private void AddToFavorite(object r, FiltResultViewModel m)
    {
        m.IsFavorite = true;
        _favoriteResults.Add(m);
    }

    #region Message Handlers

    private void HandleRemoveFromFavorite(object recipient, FiltResultViewModel result)
    {
        result.IsFavorite = false;
        _favoriteResults.Remove(result);

        // If currently showing favorites and no favorites remain, switch to all results
        if (!_favoriteResults.Any() && IsFavoriteResultsSelection)
        {
            IsFavoriteResultsSelection = false;  // This triggers PropertyChanging -> SwitchDisplayMode
            int index = _displayedResults.IndexOf(result);
            DisplayedResultSelectedIndex = index >= 0 ? index : 0;
        }
    }

    private void HandleDeleteResult(object recipient, FiltResultViewModel result)
    {
        // Remove from model
        Detail.Results.Remove(result.Model);

        // Remove from ViewModel collections
        _favoriteResults.Remove(result);
        _displayedResults.Remove(result);

        // Update selection if current result was deleted
        int deletedIndex = _displayedResults.IndexOf(result);
        if (deletedIndex == DisplayedResultSelectedIndex)
        {
            UpdateResultSelectionAfterDeletion();
        }
        else if (deletedIndex < DisplayedResultSelectedIndex)
        {
            // Adjust index if deleted item was before selected item
            DisplayedResultSelectedIndex--;
        }
    }

    private void UpdateResultSelectionAfterDeletion()
    {
        if (_displayedResults.Any())
        {
            DisplayedResultSelectedIndex = 0;
        }
        else
        {
            DisplayedResultSelectedIndex = -1;
            WeakReferenceMessenger.Default.Send("NoResultAvaliable", "NoResultAvaliable");
        }
    }

    private void HandleFiltingFinished(object recipient, FiltingFinishedMessage message)
    {
        // Create new result model
        var resultModel = CreateResultModelFromMessage(message);

        // Add to model and ViewModel collections
        Detail.Results.Add(resultModel);
        var resultViewModel = new FiltResultViewModel(resultModel);
        _displayedResults.Add(resultViewModel);

        // Select new result and switch to all results view
        DisplayedResultSelectedIndex = _displayedResults.Count - 1;
        IsFavoriteResultsSelection = false;  // Triggers PropertyChanging -> SwitchDisplayMode

        // Process physically
        _taskDpmService.ProcessPhysicalResultAsync(resultModel).ConfigureAwait(false);
        WeakReferenceMessenger.Default.Send(resultViewModel, "NavigateToNewResult");
    }

    private FiltResult CreateResultModelFromMessage(FiltingFinishedMessage message)
    {
        return new FiltResult()
        {
            Date = DateTime.Now,
            Description = message.Description,
            Name = message.Name,
            Photos = message.FiltedPhotos ?? new List<PhotoInfo>(),
            PinnedPhotos = new List<PhotoInfo>(),
            IsFavorite = false,
            ResultId = Guid.NewGuid()
        };
    }

    #endregion

    #region Data Source Provision

    /// <summary>
    /// Provides all results as ViewModel collection
    /// Data source: Detail.Results (Model) -> ViewModel collection
    /// </summary>
    private ObservableCollection<FiltResultViewModel> ProvideAllResults()
    {
        return new ObservableCollection<FiltResultViewModel>(
            Detail.Results.Select(r => new FiltResultViewModel(r))
        );
    }

    /// <summary>
    /// Provides favorite results as ViewModel collection
    /// Data source: _favoriteResults (already cached ViewModel collection)
    /// </summary>
    private ObservableCollection<FiltResultViewModel> ProvideFavoriteResults()
    {
        return _favoriteResults;
    }

    #endregion

    /// <summary>
    /// Handles property changing events to switch display mode.
    /// Uses PropertyChanging to intercept the change before it happens,
    /// allowing us to determine the target mode from the current value.
    ///
    /// Logic: !IsFavoriteResultsSelection gives the target mode (new value)
    /// because PropertyChanging fires before the value changes.
    ///
    /// Selection Logic:
    ///   If current index is valid -> try to find same result in new collection
    ///   If not found -> set index to -1 (no selection)
    /// </summary>
    private void TaskWorkspaceViewModel_PropertyChanging(object? sender, System.ComponentModel.PropertyChangingEventArgs e)
    {
        if (e.PropertyName == nameof(IsFavoriteResultsSelection))
        {
            // Preserve current selection in local variable
            var preservedResult = /*_displayedResults.Count > DisplayedResultSelectedIndex && DisplayedResultSelectedIndex >= 0
                ? _displayedResults[DisplayedResultSelectedIndex]
                : null;*/ CurrentResultViewModel;

            // Determine target mode and switch collection
            // !IsFavoriteResultsSelection = target (new) value because PropertyChanging fires before change
            DisplayedResults = !IsFavoriteResultsSelection ? ProvideFavoriteResults() : ProvideAllResults();

            // Restore selection:
            // - If new collection has same result -> select its index
            // - If not found -> set index to -1 (no selection)
            DisplayedResultSelectedIndex = preservedResult != null ? DisplayedResults.IndexOf(preservedResult) : -1;
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

        // ���� CheckBox
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
