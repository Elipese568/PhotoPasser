using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
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
using PhotoPasser.Helper;
using PhotoPasser.Primitive;
using PhotoPasser.Service;
using PhotoPasser.Sorting;
using PhotoPasser.Strings;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Foundation.Collections;
using Windows.Storage;
using Windows.Storage.Streams;
using Windows.System;

namespace PhotoPasser.Controls;

public delegate void TypedEventHandler<TSender, TResult>(TSender sender, TResult args);
public delegate TProcessed TypedEventHandler<TSender, TResult, TProcessed>(TSender sender, TResult args);

public class AddPhotoResolveEventArgs : EventArgs
{
    public string PhotoPath { get; set; }
}

public class DeletePhotoResolvingEventArgs : EventArgs
{
    public bool Cancel { get; set; }
}

public class DeletePhotoRequestedEventArgs : EventArgs
{
    public PhotoInfo DeleteItem { get; set; }
    public object State { get; set; }
}

public class CopyPhotoResolveEventArgs : EventArgs
{
    public IList<PhotoInfo> CopyItems { get; set; }
}

public class RenameResolveEventArgs : EventArgs
{
    public string OldName { get; set; }
    public string NewName { get; set; }
    public IList<PhotoInfo> OriginalSource { get; set; }
    public PhotoInfo RenameItem { get; set; }
}

public class ItemOperationInvokedEventArgs : EventArgs
{
    public new static ItemOperationInvokedEventArgs Empty = new();
    public IList<PhotoInfo> OperationItems { get; set; }
}

public enum PhotoItemOperationMode
{
    Single,
    Multiply,
    Both
}

public interface IPhotoViewerOperation
{
    IconElement DescriptiveIcon { get; set; }
    string OperationName { get; set; }
    bool ShowAtCommandBar { get; set; }
    bool ShowAtRightTapMenu { get; set; }

    event TypedEventHandler<PhotoGalleryViewer, ItemOperationInvokedEventArgs> Invoked;

    void Invoke(PhotoGalleryViewer viewer, ItemOperationInvokedEventArgs args);
}

public class PhotoItemOperation : DependencyObject, IPhotoViewerOperation
{
    public string OperationName
    {
        get { return (string)GetValue(OperationNameProperty); }
        set { SetValue(OperationNameProperty, value); }
    }

    public static readonly DependencyProperty OperationNameProperty =
        DependencyProperty.Register(nameof(OperationName), typeof(string), typeof(PhotoItemOperation), new PropertyMetadata(string.Empty));


    public IconElement DescriptiveIcon
    {
        get { return (IconElement)GetValue(DescriptiveIconProperty); }
        set { SetValue(DescriptiveIconProperty, value); }
    }

    public static readonly DependencyProperty DescriptiveIconProperty =
        DependencyProperty.Register(nameof(DescriptiveIcon), typeof(IconElement), typeof(PhotoItemOperation), new PropertyMetadata(null));


    public event TypedEventHandler<PhotoGalleryViewer, ItemOperationInvokedEventArgs> Invoked;


    public bool ShowAtCommandBar
    {
        get { return (bool)GetValue(ShowAtCommandBarProperty); }
        set { SetValue(ShowAtCommandBarProperty, value); }
    }

    public static readonly DependencyProperty ShowAtCommandBarProperty =
        DependencyProperty.Register(nameof(ShowAtCommandBar), typeof(bool), typeof(PhotoItemOperation), new PropertyMetadata(false));


    public bool ShowAtRightTapMenu
    {
        get { return (bool)GetValue(ShowAtRightTapMenuProperty); }
        set { SetValue(ShowAtRightTapMenuProperty, value); }
    }

    public static readonly DependencyProperty ShowAtRightTapMenuProperty =
        DependencyProperty.Register(nameof(ShowAtRightTapMenu), typeof(bool), typeof(PhotoItemOperation), new PropertyMetadata(true));



    public PhotoItemOperationMode Mode
    {
        get { return (PhotoItemOperationMode)GetValue(ModeProperty); }
        set { SetValue(ModeProperty, value); }
    }

    public static readonly DependencyProperty ModeProperty =
        DependencyProperty.Register(nameof(Mode), typeof(PhotoItemOperationMode), typeof(PhotoItemOperation), new PropertyMetadata(PhotoItemOperationMode.Single));

    public void Invoke(PhotoGalleryViewer viewer, ItemOperationInvokedEventArgs args) => Invoked?.Invoke(viewer, args);
}

public class PhotoGeneralOperation : DependencyObject, IPhotoViewerOperation
{
    public string OperationName
    {
        get { return (string)GetValue(OperationNameProperty); }
        set { SetValue(OperationNameProperty, value); }
    }

    public static readonly DependencyProperty OperationNameProperty =
        DependencyProperty.Register(nameof(OperationName), typeof(string), typeof(PhotoItemOperation), new PropertyMetadata(string.Empty));


    public IconElement DescriptiveIcon
    {
        get { return (IconElement)GetValue(DescriptiveIconProperty); }
        set { SetValue(DescriptiveIconProperty, value); }
    }

    public static readonly DependencyProperty DescriptiveIconProperty =
        DependencyProperty.Register(nameof(DescriptiveIcon), typeof(IconElement), typeof(PhotoItemOperation), new PropertyMetadata(null));

    public bool ShowAtCommandBar
    {
        get { return (bool)GetValue(ShowAtCommandBarProperty); }
        set { SetValue(ShowAtCommandBarProperty, value); }
    }

    public static readonly DependencyProperty ShowAtCommandBarProperty =
        DependencyProperty.Register(nameof(ShowAtCommandBar), typeof(bool), typeof(PhotoItemOperation), new PropertyMetadata(false));

    public bool ShowAtRightTapMenu
    {
        get { return (bool)GetValue(ShowAtRightTapMenuProperty); }
        set { SetValue(ShowAtRightTapMenuProperty, value); }
    }

    public static readonly DependencyProperty ShowAtRightTapMenuProperty =
        DependencyProperty.Register(nameof(ShowAtRightTapMenu), typeof(bool), typeof(PhotoGeneralOperation), new PropertyMetadata(true));


    public event TypedEventHandler<PhotoGalleryViewer, ItemOperationInvokedEventArgs> Invoked;

    public void Invoke(PhotoGalleryViewer viewer, ItemOperationInvokedEventArgs args) => Invoked?.Invoke(viewer, args);
}

[ObservableObject]
public sealed partial class PhotoGalleryViewer : UserControl
{
    private readonly PhotoSorter _photoSorter;
    private readonly IClipboardService _clipboardService;
    private readonly IDialogService _dialogService;

    private List<AppBarButton> _singleFileOperationButtons = new();
    private List<AppBarButton> _multiFileOperationButtons = new();

    public PhotoGalleryViewer()
    {
        _photoSorter = App.GetService<PhotoSorter>();
        _clipboardService = App.GetService<IClipboardService>();
        _dialogService = App.GetService<IDialogService>();
        InitializeComponent();
        Loaded += PhotoGalleryViewer_Loaded;

        RegisterPropertyChangedCallback(CurrentViewProperty, (obj, p) =>
        {
            FileDetailHeaderVisibility = CurrentView == DisplayView.Details ? Visibility.Visible : Visibility.Collapsed;
        });
    }

    private bool _initialized = false;
    private void PhotoGalleryViewer_Loaded(object sender, RoutedEventArgs e)
    {
        if (!_initialized)
        {
            _initialized = true;
            InitializeAll();
        }
    }

    private void InitializeAll()
    {
        ResourceItemsView.SelectionChanged += ResourceItemsView_SelectionChanged;
        bool isBegin = true;
        foreach (var operItem in ExtendedOperations)
        {
            if (operItem is PhotoItemOperation piOper)
            {
                if (operItem.ShowAtRightTapMenu)
                {
                    MenuFlyoutItem operMenuItem = new MenuFlyoutItem()
                    {
                        Text = operItem.OperationName,
                        Icon = operItem.DescriptiveIcon
                    }.With(x => x.Click += (s, e) => operItem.Invoke(this, new ItemOperationInvokedEventArgs()
                    {
                        OperationItems = IsMultiSelection && piOper.Mode is PhotoItemOperationMode.Multiply or PhotoItemOperationMode.Both ? ResourceItemsView.SelectedItems.OfType<PhotoInfoViewModel>().Select(vm => vm.Model).ToList() : new List<PhotoInfo> { DecidePath(s, RightTappedPhoto, SelectedImage).Model }
                    }));
                    if (piOper.Mode is PhotoItemOperationMode.Single or PhotoItemOperationMode.Both)
                    {
                        if (isBegin) FileRightTapOperationsFlyout.Items.Add(new MenuFlyoutSeparator());
                        FileRightTapOperationsFlyout.Items.Add(operMenuItem);
                    }
                    if (piOper.Mode is PhotoItemOperationMode.Multiply or PhotoItemOperationMode.Both)
                    {
                        if (isBegin) FileRightTapOperationsFlyoutMulti.Items.Add(new MenuFlyoutSeparator());
                        FileRightTapOperationsFlyoutMulti.Items.Add(operMenuItem);
                    }
                }


                if (operItem.ShowAtCommandBar)
                {
                    var appButton = new AppBarButton()
                    {
                        Label = operItem.OperationName,
                        LabelPosition = CommandBarLabelPosition.Default,
                        Icon = operItem.DescriptiveIcon,
                        IsEnabled = false,
                        Tag = operItem
                    }.With(x => x.Click += (s, e) => operItem.Invoke(this, new ItemOperationInvokedEventArgs()
                    {
                        OperationItems = IsMultiSelection && piOper.Mode is PhotoItemOperationMode.Multiply or PhotoItemOperationMode.Both ? ResourceItemsView.SelectedItems.OfType<PhotoInfoViewModel>().Select(vm => vm.Model).ToList() : new List<PhotoInfo> { DecidePath(s, RightTappedPhoto, SelectedImage).Model }
                    }));

                    if (isBegin) ResourceOperationBar.PrimaryCommands.Add(new AppBarSeparator());
                    ResourceOperationBar.PrimaryCommands.Add(appButton);

                    if (piOper.Mode is PhotoItemOperationMode.Single or PhotoItemOperationMode.Both) _singleFileOperationButtons.Add(appButton);
                    if (piOper.Mode is PhotoItemOperationMode.Multiply or PhotoItemOperationMode.Both) _multiFileOperationButtons.Add(appButton);
                }
            }
            else if (operItem is PhotoGeneralOperation pgOper)
            {
                if (operItem.ShowAtRightTapMenu)
                {
                    MenuFlyoutItem operMenuItem = new MenuFlyoutItem()
                    {
                        Text = operItem.OperationName,
                        Icon = operItem.DescriptiveIcon
                    }.With(x => x.Click += (s, e) => operItem.Invoke(this, ItemOperationInvokedEventArgs.Empty));
                    (ResourceItemsView.ContextFlyout as MenuFlyout)!.Items.Add(operMenuItem);
                }
                if (operItem.ShowAtCommandBar)
                {
                    var appButton = new AppBarButton()
                    {
                        Label = operItem.OperationName,
                        LabelPosition = CommandBarLabelPosition.Default,
                        Icon = operItem.DescriptiveIcon,
                        IsEnabled = true,
                        Tag = operItem
                    }.With(x => x.Click += (s, e) => operItem.Invoke(this, ItemOperationInvokedEventArgs.Empty));

                    if (isBegin) ResourceOperationBar.PrimaryCommands.Add(new AppBarSeparator());
                    ResourceOperationBar.PrimaryCommands.Add(appButton);
                }
            }

            isBegin = false;
        }
        UpdateStatusBarStrings();
    }

    public SortBy SortBy
    {
        get { return (SortBy)GetValue(SortByProperty); }
        set { if (value != SortBy) SetValue(SortByProperty, value); }
    }

    public static readonly DependencyProperty SortByProperty =
        DependencyProperty.Register(nameof(SortBy), typeof(SortBy), typeof(PhotoGalleryViewer), new PropertyMetadata(0));

    public SortOrder SortOrder
    {
        get { return (SortOrder)GetValue(SortOrderProperty); }
        set { if (value != SortOrder) SetValue(SortOrderProperty, value); }
    }

    public static readonly DependencyProperty SortOrderProperty =
        DependencyProperty.Register(nameof(SortOrder), typeof(SortOrder), typeof(PhotoGalleryViewer), new PropertyMetadata(0));

    public DisplayView CurrentView
    {
        get { return (DisplayView)GetValue(CurrentViewProperty); }
        set { if (value != CurrentView) SetValue(CurrentViewProperty, value); }
    }

    public static readonly DependencyProperty CurrentViewProperty =
        DependencyProperty.Register(nameof(CurrentView), typeof(DisplayView), typeof(PhotoGalleryViewer), new PropertyMetadata(0));

    public PhotoInfoViewModel SelectedImage
    {
        get { return (PhotoInfoViewModel)GetValue(SelectedImageProperty); }
        set { SetValue(SelectedImageProperty, value); }
    }

    public static readonly DependencyProperty SelectedImageProperty =
        DependencyProperty.Register(nameof(SelectedImage), typeof(PhotoInfoViewModel), typeof(PhotoGalleryViewer), new PropertyMetadata(null));



    public IList<PhotoInfoViewModel> SelectedPhotos
    {
        get { return (IList<PhotoInfoViewModel>)GetValue(SelectedPhotosProperty); }
        set { SetValue(SelectedPhotosProperty, value); }
    }

    public static readonly DependencyProperty SelectedPhotosProperty =
        DependencyProperty.Register(nameof(SelectedPhotos), typeof(IList<PhotoInfoViewModel>), typeof(PhotoGalleryViewer), new PropertyMetadata(null));



    public ObservableCollection<PhotoInfoViewModel> Photos
    {
        get { return (ObservableCollection<PhotoInfoViewModel>)GetValue(PhotosProperty); }
        set { SetValue(PhotosProperty, value); }
    }

    public static readonly DependencyProperty PhotosProperty =
        DependencyProperty.Register(nameof(Photos), typeof(ObservableCollection<PhotoInfoViewModel>), typeof(PhotoGalleryViewer), new PropertyMetadata(null));



    public Brush ItemPanelBackground
    {
        get { return (Brush)GetValue(ItemPanelBackgroundProperty); }
        set { SetValue(ItemPanelBackgroundProperty, value); }
    }

    public static readonly DependencyProperty ItemPanelBackgroundProperty =
        DependencyProperty.Register(nameof(ItemPanelBackground), typeof(Brush), typeof(PhotoGalleryViewer), new PropertyMetadata(null));

    public ItemPanelBackgroundThemeResource ItemPanelBackgroundTheme
    {
        get { return (ItemPanelBackgroundThemeResource)GetValue(ItemPanelBackgroundThemeProperty); }
        set { if (value != ItemPanelBackgroundTheme) SetValue(ItemPanelBackgroundThemeProperty, value); }
    }

    public static readonly DependencyProperty ItemPanelBackgroundThemeProperty =
        DependencyProperty.Register(
            nameof(ItemPanelBackgroundTheme),
            typeof(ItemPanelBackgroundThemeResource),
            typeof(PhotoGalleryViewer),
            new PropertyMetadata(ItemPanelBackgroundThemeResource.LayerOnMicaBaseAltFillColorDefaultState, OnItemPanelBackgroundThemeChanged)
        );

    private static void OnItemPanelBackgroundThemeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var viewer = (PhotoGalleryViewer)d;
        var stateName = e.NewValue.ToString();
        VisualStateManager.GoToState(viewer, stateName, true);
    }

    public bool IsAddEnabled
    {
        get { return (bool)GetValue(IsAddEnabledProperty); }
        set { SetValue(IsAddEnabledProperty, value); }
    }

    public static readonly DependencyProperty IsAddEnabledProperty =
        DependencyProperty.Register(nameof(IsAddEnabled), typeof(bool), typeof(PhotoGalleryViewer), new PropertyMetadata(true));



    public bool IsCopyEnabled
    {
        get { return (bool)GetValue(IsCopyEnabledProperty); }
        set { SetValue(IsCopyEnabledProperty, value); }
    }

    public static readonly DependencyProperty IsCopyEnabledProperty =
        DependencyProperty.Register(nameof(IsCopyEnabled), typeof(bool), typeof(PhotoGalleryViewer), new PropertyMetadata(true));



    public bool IsDeleteEnabled
    {
        get { return (bool)GetValue(IsDeleteEnabledProperty); }
        set { SetValue(IsDeleteEnabledProperty, value); }
    }

    public static readonly DependencyProperty IsDeleteEnabledProperty =
        DependencyProperty.Register(nameof(IsDeleteEnabled), typeof(bool), typeof(PhotoGalleryViewer), new PropertyMetadata(true));

    public bool IsRenameEnabled
    {
        get { return (bool)GetValue(IsRenameEnabledProperty); }
        set { SetValue(IsRenameEnabledProperty, value); }
    }

    public static readonly DependencyProperty IsRenameEnabledProperty =
        DependencyProperty.Register(nameof(IsRenameEnabled), typeof(bool), typeof(PhotoGalleryViewer), new PropertyMetadata(true));



    public ListViewSelectionMode SelectionMode
    {
        get { return (ListViewSelectionMode)GetValue(SelectionModeProperty); }
        set { SetValue(SelectionModeProperty, value); }
    }

    public static readonly DependencyProperty SelectionModeProperty =
        DependencyProperty.Register(nameof(SelectionMode), typeof(ListViewSelectionMode), typeof(PhotoGalleryViewer), new PropertyMetadata(ListViewSelectionMode.Extended));



    public ICollection<IPhotoViewerOperation> ExtendedOperations { get; set; } = new List<IPhotoViewerOperation>();

    public string Title
    {
        get { return (string)GetValue(TitleProperty); }
        set { SetValue(TitleProperty, value); }
    }

    public static readonly DependencyProperty TitleProperty =
        DependencyProperty.Register(nameof(Title), typeof(string), typeof(PhotoGalleryViewer), new PropertyMetadata(string.Empty));

    public event TypedEventHandler<PhotoGalleryViewer, AddPhotoResolveEventArgs> AddResolve;
    public event TypedEventHandler<PhotoGalleryViewer, DeletePhotoResolvingEventArgs, Task<object>> DeleteResolving;
    public event TypedEventHandler<PhotoGalleryViewer, DeletePhotoRequestedEventArgs> DeleteRequested;
    public event TypedEventHandler<PhotoGalleryViewer, CopyPhotoResolveEventArgs> CopyResolve;
    public event TypedEventHandler<PhotoGalleryViewer, CopyPhotoResolveEventArgs> CopyAsPathResolve;
    public event TypedEventHandler<PhotoGalleryViewer, CopyPhotoResolveEventArgs> CopyAsBitmapResolve;
    public event TypedEventHandler<PhotoGalleryViewer, RenameResolveEventArgs> RenameResolve;

    public ItemsPanelTemplate GetPanel(DisplayView view)
    {
        return view switch
        {
            DisplayView.Trumbull => TrumbullViewPanel,
            DisplayView.Details => DetailsViewPanel,
            DisplayView.Tiles => TilesViewPanel,
            _ => TrumbullViewPanel
        };
    }
    [ObservableProperty]
    private Visibility _fileDetailHeaderVisibility = Visibility.Collapsed;

    [ObservableProperty]
    private bool _isMultiSelection = false;

    [ObservableProperty]
    private ObservableCollection<PhotoInfoViewModel> _searchResult = new ObservableCollection<PhotoInfoViewModel>();

    [ObservableProperty]
    private int _selectedItemsCount;

    public PhotoInfoViewModel RightTappedPhoto { get; set; }

    public ObservableCollection<PhotoInfoViewModel>? GetSortedPhotos(ObservableCollection<PhotoInfoViewModel> origin, ObservableCollection<PhotoInfoViewModel> searchResult, SortBy sortBy, SortOrder order)
    {
        if (!origin?.Any() ?? true)
            return origin;

        var result = new ObservableCollection<PhotoInfoViewModel>();

        if (searchResult != null && searchResult.Any())
            result = searchResult;
        else
            result = origin;

        if (result == null || !result.Any())
            return result;

        return _photoSorter.Sort(result, sortBy, order);
    }

    public string GetQueryPlaceHolderText(string taskName)
    {
        return "SearchPlaceholderText"
            .GetLocalized(LC.PhotoGalleryViewer)
            !.Replace(ReplaceItem.SearchingPlace, taskName);
    }

    private void ResourceItemsView_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        IsMultiSelection = ResourceItemsView.SelectedItems.Count > 1;
        SelectedItemsCount = ResourceItemsView.SelectedItems.Count;

        SelectedPhotos = ResourceItemsView.SelectedItems.OfType<PhotoInfoViewModel>().ToList();

        // force bindings update for items which may have changed properties
        foreach (var item in ResourceItemsView.SelectedItems)
        {
            if (item is DependencyObject dobj)
            {
                // refresh container bindings
                var container = ResourceItemsView.ContainerFromItem(item) as FrameworkElement;
                container?.InvalidateMeasure();
                container?.InvalidateArrange();
            }
        }

        SizeStatusBarTextBlock.Text = GetSelectedItemTotalSize(ResourceItemsView.SelectedItems);
        SizeStatusBarTextBlock.Visibility = ResourceItemsView.SelectedItems.Count > 0 ? Visibility.Visible : Visibility.Collapsed;
        UpdateResourceOperationBarButtons();
        UpdateStatusBarStrings();
    }

    private void UpdateStatusBarStrings()
    {
        var totalCount = GetCurrentItemCount(Photos, SearchResult);
        ItemCountStatusTextBlock.Text = "ItemCountText"
            .GetLocalized(LC.PhotoGalleryViewer)!
            .Replace(ReplaceItem.CountNumber, totalCount);

        SelectedCountStatusTextBlock.Text = "SelectedCountText"
            .GetLocalized(LC.PhotoGalleryViewer)!
            .Replace(ReplaceItem.CountNumber, SelectedItemsCount.ToString());
    }

    private void UpdateResourceOperationBarButtons()
    {
        RenameButton.IsEnabled = !IsMultiSelection && SelectedImage != null;
        CopyAsBitmapButton.IsEnabled = !IsMultiSelection && SelectedImage != null;
        OpenInExplorerButton.IsEnabled = !IsMultiSelection && SelectedImage != null;
        _singleFileOperationButtons.ForEach(x => x.IsEnabled = (x.Tag as PhotoItemOperation)!.Mode is PhotoItemOperationMode mode && ((mode == PhotoItemOperationMode.Both && SelectedImage != null) || (mode == PhotoItemOperationMode.Single && !IsMultiSelection && SelectedImage != null)));
        _multiFileOperationButtons.ForEach(x => x.IsEnabled = (x.Tag as PhotoItemOperation)!.Mode is PhotoItemOperationMode mode && ((mode == PhotoItemOperationMode.Both && SelectedImage != null) || (mode == PhotoItemOperationMode.Multiply && IsMultiSelection && SelectedImage != null)));
    }

    private void FileItemPresenter_RightTapped(object sender, RightTappedRoutedEventArgs e)
    {
        if (!IsMultiSelection)
        {
            RightTappedPhoto = (sender as Grid)!.DataContext as PhotoInfoViewModel;
        }
    }

    private PhotoInfoViewModel DecidePath(object sender, PhotoInfoViewModel rightTappedValue, PhotoInfoViewModel selectedValue)
    {
        return sender is MenuFlyoutItem ? rightTappedValue : selectedValue;
    }

    private async void RenameMenu_Click(object sender, RoutedEventArgs e)
    {
        var file = DecidePath(sender, RightTappedPhoto, SelectedImage);

        string title = "RenameDialogTitle".GetLocalized(LC.PhotoGalleryViewer)!;
        string content = "RenameDialogContent".GetLocalized(LC.PhotoGalleryViewer)!;

        var dialog = new TextBoxDialog(title, content, file.UserName, false).With(x => x.ApplyApplicationOption());
        if (await dialog.ShowAsync() == ContentDialogResult.Primary)
        {
            var newName = dialog.Text;
            RenameResolve?.Invoke(this, new RenameResolveEventArgs()
            {
                OldName = file.UserName,
                NewName = newName,
                OriginalSource = Photos.Select(vm => vm.Model).ToList(),
                RenameItem = file.Model
            });
        }
    }
    private async void OpenMenu_Click(object sender, RoutedEventArgs e)
    {
        await Open(DecidePath(sender, RightTappedPhoto, SelectedImage));
    }
    private async void OpenInExplorerMenu_Click(object sender, RoutedEventArgs e)
    {
        await OpenInExplorer(DecidePath(sender, RightTappedPhoto, SelectedImage));
    }
    private void CopyMenu_Click(object sender, RoutedEventArgs e)
    {
        CopyResolve?.Invoke(this, new CopyPhotoResolveEventArgs()
        {
            CopyItems = IsMultiSelection ? ResourceItemsView.SelectedItems.OfType<PhotoInfoViewModel>().Select(vm => vm.Model).ToList() : new List<PhotoInfo> { DecidePath(sender, RightTappedPhoto, SelectedImage).Model }
        });
    }
    private void CopyAsPathMenu_Click(object sender, RoutedEventArgs e)
    {
        CopyAsPathResolve?.Invoke(this, new CopyPhotoResolveEventArgs()
        {
            CopyItems = new List<PhotoInfo> { DecidePath(sender, RightTappedPhoto, SelectedImage).Model }
        });
    }
    private void CopyAsBitmapMenu_Click(object sender, RoutedEventArgs e)
    {
        CopyAsBitmapResolve?.Invoke(this, new CopyPhotoResolveEventArgs()
        {
            CopyItems = new List<PhotoInfo> { DecidePath(sender, RightTappedPhoto, SelectedImage).Model }
        });
    }
    private async void DeleteMenu_Click(object sender, RoutedEventArgs e)
    {
        if (DeleteResolving == null)
            return;

        var args = new DeletePhotoResolvingEventArgs();
        var state = await DeleteResolving(this, args);

        if (args.Cancel)
            return;

        var removeItems = IsMultiSelection ? ResourceItemsView.SelectedItems.OfType<PhotoInfoViewModel>().Select(vm => vm.Model).ToList() : new List<PhotoInfo> { DecidePath(sender, RightTappedPhoto, SelectedImage).Model };
        foreach (var item in removeItems)
        {
            DeleteRequested?.Invoke(this, new DeletePhotoRequestedEventArgs()
            {
                State = state,
                DeleteItem = item
            });
        }
    }
    private async void PropertiesMenu_Click(object sender, RoutedEventArgs e)
    {
        if (IsMultiSelection)
        {
            var selected = ResourceItemsView.SelectedItems.OfType<PhotoInfoViewModel>().Select(vm => vm.Model).ToList();
            await ShowProperties(selected);
        }
        else
        {
            await ShowProperties(DecidePath(sender, RightTappedPhoto, SelectedImage).Model);
        }
    }

    private async void GridView_DragItemsStarting(object sender, DragItemsStartingEventArgs e)
    {
        var files = e.Items.OfType<PhotoInfo>()
               .Select(async x => await StorageItemProvider.GetStorageFile(x.Path))
               .EvalResults()
               .ToListAsync();

        e.Data.SetStorageItems(await files);
        e.Data.RequestedOperation = Windows.ApplicationModel.DataTransfer.DataPackageOperation.Copy;
    }

    private void GridView_DragOver(object sender, DragEventArgs e)
    {
        if (e.OriginalSource != ResourceItemsView && IsAddEnabled)
            e.AcceptedOperation = Windows.ApplicationModel.DataTransfer.DataPackageOperation.Copy;
        else
            e.AcceptedOperation = Windows.ApplicationModel.DataTransfer.DataPackageOperation.None;
    }

    private async void GridView_Drop(object sender, DragEventArgs e)
    {
        var items = await e.DataView.GetStorageItemsAsync();
        foreach (var item in items)
            AddResolve?.Invoke(this, new AddPhotoResolveEventArgs()
            {
                PhotoPath = item.Path
            });
    }

    private void GridView_PointerPressed(object sender, PointerRoutedEventArgs e)
    {
        if (!e.GetCurrentPoint(sender as UIElement).Properties.IsRightButtonPressed)
            (sender as GridView)!.SelectedIndex = -1;
    }

    private void SelectAllMenu_Click(object sender, RoutedEventArgs e)
    {
        ResourceItemsView.SelectAllSafe();
    }

    private void SelectNoneMenu_Click(object sender, RoutedEventArgs e)
    {
        ResourceItemsView.SelectedItems.Clear();
    }

    private void InvertSelectionMenu_Click(object sender, RoutedEventArgs e)
    {
        var selectedItems = ResourceItemsView.SelectedItems.OfType<PhotoInfoViewModel>().ToList();
        foreach (var itemVm in Photos)
        {
            if (selectedItems.Contains(itemVm))
            {
                ResourceItemsView.SelectedItems.Remove(itemVm);
            }
            else
            {
                ResourceItemsView.SelectedItems.Add(itemVm);
            }
        }
    }

    private async void AppBarButton_Click(object sender, RoutedEventArgs e)
    {
        await Add();
    }

    private async void ResourceItem_DoubleTapped(object sender, DoubleTappedRoutedEventArgs e)
    {
        await Open(DecidePath(sender, RightTappedPhoto, SelectedImage));
    }
    private void DeleteAccelerator_Invoked(KeyboardAccelerator sender, KeyboardAcceleratorInvokedEventArgs args)
    {
        if (ResourceItemsView.SelectedItems.Count > 0)
            DeleteMenu_Click(null, null);
    }

    private void CopyAccelerator_Invoked(KeyboardAccelerator sender, KeyboardAcceleratorInvokedEventArgs args)
    {
        if (ResourceItemsView.SelectedItems.Count > 0)
            CopyMenu_Click(null, null);
    }

    private async void PasteAccelerator_Invoked(KeyboardAccelerator sender, KeyboardAcceleratorInvokedEventArgs args)
    {
        await PasteClipboardFiles();
    }
    private void AutoSuggestBox_QuerySubmitted(AutoSuggestBox sender, AutoSuggestBoxQuerySubmittedEventArgs args)
    {
        if (args.QueryText == "")
            return;
        SearchResult = Photos.Where(p => p.UserName.Contains(args.QueryText, StringComparison.OrdinalIgnoreCase))
                             .AsObservable();
        ExitSearchingButton.Visibility = Visibility.Visible;
        UpdateStatusBarStrings();
    }

    private void ExitSearchingButton_Click(object sender, RoutedEventArgs e)
    {
        ExitSearchingButton.Visibility = Visibility.Collapsed;
        SearchResult = new EmptyPhotoCollection();
        UpdateStatusBarStrings();
    }
    public async Task Open(object param)
    {
        if (param is IEnumerable<PhotoInfoViewModel> photos)
        {
            foreach (var photo in photos)
            {
                var file = await StorageItemProvider.GetStorageFile(photo.Path);
                await Launcher.LaunchFileAsync(file);
            }
        }
        else if (param is PhotoInfoViewModel photo)
        {
            var file = await StorageItemProvider.GetStorageFile(photo.Path);
            await Launcher.LaunchFileAsync(file);
        }
    }

    public async Task OpenInExplorer(object param)
    {
        if (param is PhotoInfoViewModel photo)
        {
            StorageFolder folder = await StorageItemProvider.GetStorageFolderFromFileParent(photo.Path);
            await Launcher.LaunchFolderAsync(folder);
        }
    }

    public async Task ShowProperties(object param)
    {
        if (param is IEnumerable<PhotoInfo> photos)
        {
            var paths = await photos.Select(p => StorageItemProvider.GetRawFilePath(p.Path)).EvalResults().ToListAsync();
            ShellInterop.ShowFileProperties(paths.ToArray());
        }
        else if (param is PhotoInfo photo)
        {
            ShellInterop.ShowFileProperties(await StorageItemProvider.GetRawFilePath(photo.Path), WinRT.Interop.WindowNative.GetWindowHandle(App.GetService<MainWindow>()!));
        }
    }

    public async Task Add()
    {
        FileOpenPicker picker = new FileOpenPicker(App.GetService<MainWindow>()!.AppWindow.Id);
        picker.FileTypeFilter.Add("*");
        var files = await picker.PickMultipleFilesAsync();
        if (files != null && files.Count > 0)
        {
            foreach (var file in files)
            {
                AddResolve?.Invoke(this, new AddPhotoResolveEventArgs()
                {
                    PhotoPath = file.Path
                });
            }
        }
    }

    public async Task PasteClipboardFiles()
    {
        var files = await _clipboardService.GetStorageFilesAsync();
        foreach (var file in files)
        {
            AddResolve?.Invoke(this, new AddPhotoResolveEventArgs()
            {
                PhotoPath = file.Path
            });
        }
    }

    public Visibility GetVisibility(bool boolean) => boolean ? Visibility.Visible : Visibility.Collapsed;
    public Visibility GetVisibility2(bool boolean1, bool boolean2) => boolean1 | boolean2 ? Visibility.Visible : Visibility.Collapsed;
    public Visibility GetVisibility3(bool boolean1, bool boolean2, bool boolean3) => boolean1 | boolean2 | boolean3 ? Visibility.Visible : Visibility.Collapsed;

    private FriendlySizeTextFormatConverter _innerSizeTextConverter = new();
    public string GetSelectedItemTotalSize(IList<object> photos)
    {
        // support both PhotoInfo (models) and PhotoInfoViewModel in selection
        long total = 0;
        if (photos != null)
        {
            total += photos.OfType<PhotoInfo>().Sum(p => p.Size);
            total += photos.OfType<PhotoInfoViewModel>().Sum(vm => vm.Size);
        }
        return _innerSizeTextConverter.Convert(total, null, null, null) as string;
    }
    public string GetCurrentItemCount(ObservableCollection<PhotoInfoViewModel> photos, ObservableCollection<PhotoInfoViewModel> searchResults)
    {
        if (searchResults is not EmptyPhotoCollection)
            return searchResults.Count.ToString();
        return photos?.Count.ToString() ?? "0";
    }
}