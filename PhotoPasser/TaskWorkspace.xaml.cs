using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.WinUI;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Markup;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using PhotoPasser; // 添加此行以引用 TextBoxDialog
using PhotoPasser.Helper;
using PhotoPasser.Service;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace PhotoPasser;

public class FileNameConverter : IValueConverter
{
    public TaskWorkspace Workspace { get; set; }
    public bool InResourceView { get; set; }

    public object Convert(object value, Type targetType, object parameter, string language)
    {
        return (value as PhotoInfo).UserName;
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        throw new NotImplementedException();
    }
}

public class AdaptiveWrapPanel : Panel
{
    public double HorizontalSpacing
    {
        get => (double)GetValue(HorizontalSpacingProperty);
        set => SetValue(HorizontalSpacingProperty, value);
    }

    public static readonly DependencyProperty HorizontalSpacingProperty =
        DependencyProperty.Register(nameof(HorizontalSpacing), typeof(double), typeof(AdaptiveWrapPanel),
            new PropertyMetadata(8.0, LayoutPropertyChanged));

    public double VerticalSpacing
    {
        get => (double)GetValue(VerticalSpacingProperty);
        set => SetValue(VerticalSpacingProperty, value);
    }

    public static readonly DependencyProperty VerticalSpacingProperty =
        DependencyProperty.Register(nameof(VerticalSpacing), typeof(double), typeof(AdaptiveWrapPanel),
            new PropertyMetadata(8.0, LayoutPropertyChanged));

    private static void LayoutPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is AdaptiveWrapPanel panel)
        {
            panel.InvalidateMeasure();
            panel.InvalidateArrange();
        }
    }

    protected override Size MeasureOverride(Size availableSize)
    {
        double x = 0;
        double y = 0;
        double lineHeight = 0;
        double totalHeight = 0;
        double maxWidth = availableSize.Width;

        foreach (var child in Children)
        {
            child.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
            var desired = child.DesiredSize;

            // 换行判断
            if (x + desired.Width > maxWidth && x > 0)
            {
                y += lineHeight + VerticalSpacing;
                totalHeight += lineHeight + VerticalSpacing;
                x = 0;
                lineHeight = 0;
            }

            lineHeight = Math.Max(lineHeight, desired.Height);
            x += desired.Width + HorizontalSpacing;
        }

        totalHeight += lineHeight;
        return new Size(maxWidth, totalHeight);
    }

    protected override Size ArrangeOverride(Size finalSize)
    {
        double maxWidth = finalSize.Width;
        double x = 0;
        double y = 0;
        double lineHeight = 0;

        var currentLine = new List<UIElement>();
        var lines = new List<(List<UIElement> children, double height)>();

        // --- 先按行分组 ---
        foreach (var child in Children)
        {
            var desired = child.DesiredSize;

            if (x + desired.Width > maxWidth && x > 0)
            {
                lines.Add((new List<UIElement>(currentLine), lineHeight));
                y += lineHeight + VerticalSpacing;

                currentLine.Clear();
                x = 0;
                lineHeight = 0;
            }

            currentLine.Add(child);
            x += desired.Width + HorizontalSpacing;
            lineHeight = Math.Max(lineHeight, desired.Height);
        }

        if (currentLine.Count > 0)
        {
            lines.Add((new List<UIElement>(currentLine), lineHeight));
        }

        // --- 再逐行排列 ---
        y = 0;
        foreach (var line in lines)
        {
            x = 0;
            foreach (var child in line.children)
            {
                var desired = child.DesiredSize;
                child.Arrange(new Rect(new Point(x, y), new Size(desired.Width, line.height)));
                x += desired.Width + HorizontalSpacing;
            }
            y += line.height + VerticalSpacing;
        }

        return finalSize;
    }
}

/// <summary>
/// An empty page that can be used on its own or navigated to within a Frame.
/// </summary>
public sealed partial class TaskWorkspace : Page
{
    private readonly IServiceProvider _scopedServiceProvider;
    private readonly ITaskDetailPhysicalManagerService _taskDpmService;
    public FiltTask FiltTask { get; set; }
    public TaskDetail Detail { get; set; }
    public TaskWorkspaceViewModel ViewModel { get; set; }

    public bool IsMultiSelection => ResourceItemsView.SelectedItems.Count >1;

    public TaskWorkspace()
    {
        _scopedServiceProvider = App.CreateScope().ServiceProvider;
        _taskDpmService = _scopedServiceProvider.GetRequiredService<ITaskDetailPhysicalManagerService>();
        InitializeComponent();
    }

    protected override async void OnNavigatedTo(NavigationEventArgs e)
    {
        if (e.Parameter is FiltTask task)
        {
            FiltTask = task;
            Detail = await _taskDpmService.InitializeAsync(task);
            // 默认选第一个 result
            ViewModel = new TaskWorkspaceViewModel(task, _taskDpmService, Detail, Detail.Results?.FirstOrDefault() ?? null);
            this.DataContext = ViewModel;
            Bindings.Update();
        }
        ResourceItemsView.SelectionChanged += ResourceItemsView_SelectionChanged;
        base.OnNavigatedTo(e);
    }

    private void ResourceItemsView_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        ViewModel.IsMultiSelection = IsMultiSelection;
        UpdateResourceOperationBarButtons();
    }

    private void UpdateResourceOperationBarButtons()
    {
        //这里假设你有 x:Name="RenameButton" 等控件名
        RenameButton.IsEnabled = !IsMultiSelection && ViewModel.SelectedImage != null;
        CopyAsBitmapButton.IsEnabled = !IsMultiSelection && ViewModel.SelectedImage != null;
        OpenInExplorerButton.IsEnabled = !IsMultiSelection && ViewModel.SelectedImage != null;
    }

    private void FileItemPresenter_RightTapped(object sender, RightTappedRoutedEventArgs e)
    {
        if (IsMultiSelection)
            FileRightTapOperationsFlyoutMulti.ShowAt((FrameworkElement)sender, new FlyoutShowOptions { Position = e.GetPosition((FrameworkElement)sender) });
        else
        {
            FileRightTapOperationsFlyout.ShowAt((FrameworkElement)sender, new FlyoutShowOptions { Position = e.GetPosition((FrameworkElement)sender) });
            ViewModel.RightTappedPhoto = (sender as Grid)!.DataContext as PhotoInfo;
        }
    }

    private PhotoInfo DecidePath(object sender, PhotoInfo rightTappedValue, PhotoInfo selectedValue)
    {
        return sender is MenuFlyoutItem? rightTappedValue : selectedValue;
    }

    // 菜单项事件绑定
    private async void RenameMenu_Click(object sender, RoutedEventArgs e)
    {
        var file = DecidePath(sender, ViewModel.RightTappedPhoto, ViewModel.SelectedImage);
        var dialog = new TextBoxDialog("Rename File", "Enter new name:", file.UserName, false)
        {
            XamlRoot = App.Current.MainWindow.Content.XamlRoot
        };
        if (await dialog.ShowAsync() == ContentDialogResult.Primary)
        {
            var newName = dialog.Text;
            await ViewModel.RenameCommand.ExecuteAsync((file, newName, true));
        }
    }
    private async void OpenMenu_Click(object sender, RoutedEventArgs e)
    {
        await ViewModel.OpenCommand.ExecuteAsync(DecidePath(sender, ViewModel.RightTappedPhoto, ViewModel.SelectedImage));
    }
    private async void OpenInExplorerMenu_Click(object sender, RoutedEventArgs e)
    {
        await ViewModel.OpenInExplorerCommand.ExecuteAsync(DecidePath(sender, ViewModel.RightTappedPhoto, ViewModel.SelectedImage));
    }
    private void CopyMenu_Click(object sender, RoutedEventArgs e)
    {
        ViewModel.CopyCommand.Execute(ViewModel.IsMultiSelection? ResourceItemsView.SelectedItems.Cast<PhotoInfo>().ToList() : DecidePath(sender, ViewModel.RightTappedPhoto, ViewModel.SelectedImage));
    }
    private void CopyAsPathMenu_Click(object sender, RoutedEventArgs e)
    {
        ViewModel.CopyAsPathCommand.Execute(DecidePath(sender, ViewModel.RightTappedPhoto, ViewModel.SelectedImage));
    }
    private void CopyAsBitmapMenu_Click(object sender, RoutedEventArgs e)
    {
        ViewModel.CopyAsBitmapCommand.Execute(DecidePath(sender, ViewModel.RightTappedPhoto, ViewModel.SelectedImage));
    }
    private async void DeleteMenu_Click(object sender, RoutedEventArgs e)
    {
        await ViewModel.DeleteCommand.ExecuteAsync(ViewModel.IsMultiSelection ? ResourceItemsView.SelectedItems.Cast<PhotoInfo>().ToList() : DecidePath(sender, ViewModel.RightTappedPhoto, ViewModel.SelectedImage));
    }
    private async void PropertiesMenu_Click(object sender, RoutedEventArgs e)
    {
        if (IsMultiSelection)
        {
            var selected = ResourceItemsView.SelectedItems.Cast<PhotoInfo>().ToList();
            await ViewModel.ShowPropertiesCommand.ExecuteAsync(selected);
        }
        else
        {
            await ViewModel.ShowPropertiesCommand.ExecuteAsync(DecidePath(sender, ViewModel.RightTappedPhoto, ViewModel.SelectedImage));
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
        if (e.OriginalSource != ResourceItemsView)
            e.AcceptedOperation = Windows.ApplicationModel.DataTransfer.DataPackageOperation.Copy;
        else
            e.AcceptedOperation = Windows.ApplicationModel.DataTransfer.DataPackageOperation.None;
    }

    private async void GridView_Drop(object sender, DragEventArgs e)
    {
        var items = await e.DataView.GetStorageItemsAsync();
        foreach (var item in items)
            await ViewModel.AddPhoto(item.Path);
    }

    public ItemsPanelTemplate GetPanel(DisplayView view)
    {
        return view switch
        {
            DisplayView.Trumbull => TrumbullViewPanel,
            DisplayView.Details => DetailsViewPanel,
            DisplayView.Tiles => TilesViewPanel
        };
    }

    private void GridView_PointerPressed(object sender, PointerRoutedEventArgs e)
    {
        if(!e.GetCurrentPoint(sender as UIElement).Properties.IsRightButtonPressed)
            (sender as GridView).SelectedIndex = -1;
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
        var selectedItems = ResourceItemsView.SelectedItems.Cast<PhotoInfo>().ToList();
        foreach(var items in ViewModel.Detail.Photos)
        {
            if(selectedItems.Contains(items))
            {
                ResourceItemsView.SelectedItems.Remove(items);
            }
            else
            {
                ResourceItemsView.SelectedItems.Add(items);
            }
        }
    }

    private async void AppBarButton_Click(object sender, RoutedEventArgs e)
    {
        await ViewModel.Add();
    }
    private async void ResourceItem_DoubleTapped(object sender, DoubleTappedRoutedEventArgs e)
    {
        await ViewModel.OpenCommand.ExecuteAsync(DecidePath(sender, ViewModel.RightTappedPhoto, ViewModel.SelectedImage));
    }

    private void NavigationViewer_ItemInvoked(NavigationView sender, NavigationViewItemInvokedEventArgs args)
    {
        if((string)args.InvokedItemContainer.Tag == "Home")
            App.Current.MainWindow.Frame.GoBack();
    }

    private void DeleteAccelerator_Invoked(KeyboardAccelerator sender, KeyboardAcceleratorInvokedEventArgs args)
    {
        DeleteMenu_Click(null, null);
    }

    private void CopyAccelerator_Invoked(KeyboardAccelerator sender, KeyboardAcceleratorInvokedEventArgs args)
    {
        CopyMenu_Click(null, null);
    }

    private async void PasteAccelerator_Invoked(KeyboardAccelerator sender, KeyboardAcceleratorInvokedEventArgs args)
    {
        await ViewModel.PasteClipboardFiles();
    }

    protected override void OnNavigatedFrom(NavigationEventArgs e)
    {
        ViewModel.Dispose();
        base.OnNavigatedFrom(e);
    }
}
