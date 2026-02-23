using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
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
using PhotoPasser; // ���Ӵ��������� TextBoxDialog
using PhotoPasser.Dialog;
using PhotoPasser.Helper;
using PhotoPasser.Primitive;
using PhotoPasser.Service;
using PhotoPasser.Strings;
using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Foundation.Collections;
using Windows.Storage;
using Windows.System;
using WinRT;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace PhotoPasser.Views;

/// <summary>
/// An empty page that can be used on its own or navigated to within a Frame.
/// </summary>
public sealed partial class TaskWorkspace : Page
{
    private static TaskWorkspaceViewModel _currentVm;

    public TaskWorkspaceViewModel ViewModel { get; set; }

    public TaskWorkspace()
    {
        InitializeComponent();
    }

    protected override async void OnNavigatedTo(NavigationEventArgs e)
    {
        if (e.Parameter is FiltTask task && e.NavigationMode == NavigationMode.New)
        {
            task.RecentlyVisitAt = DateTime.Now;
            var _scopedServiceProvider = App.CreateScope().ServiceProvider;
            var _taskDpmService = _scopedServiceProvider.GetRequiredService<ITaskDetailPhysicalManagerService>();
            var detail = await _taskDpmService.InitializeAsync(task);
            ViewModel = new TaskWorkspaceViewModel(task, _taskDpmService, detail, detail.Results?.FirstOrDefault() ?? null);
            await ViewModel.Initialize();
            this.DataContext = ViewModel;
            Bindings.Update();
            _currentVm = ViewModel;
            ResultViewFrame.Navigate(typeof(ResultView), new ResultViewNavigationParameter()
            {
                TaskDpmService = ViewModel.TaskDpmService,
                Result = ViewModel.CurrentResultViewModel
            }, new Microsoft.UI.Xaml.Media.Animation.EntranceNavigationTransitionInfo());
            WeakReferenceMessenger.Default.Register(ViewModel, "NoResultAvaliable", (TaskWorkspaceViewModel r, string m) =>
            {
                ResultViewFrame.Content = null;
            });
            WeakReferenceMessenger.Default.Register(ViewModel, "NavigateToNewResult", async (TaskWorkspaceViewModel r, FiltResultViewModel m) =>
            {
                await ResultViewFrame.DispatcherQueue.EnqueueAsync(() =>
                {
                    ResultViewFrame.Navigate(typeof(ResultView), new ResultViewNavigationParameter()
                    {
                        TaskDpmService = ViewModel.TaskDpmService,
                        Result = m
                    });
                });
            });
            App.Current.ExitProcess += OnExit;
        }
        else
        {
            ViewModel = _currentVm;
            this.DataContext = ViewModel;
            Bindings.Update();
            var selectedIndex = ViewModel.DisplayedResultSelectedIndex;
            if (selectedIndex >= 0 && selectedIndex < ViewModel.DisplayedResults.Count)
            {
                ResultViewFrame.Navigate(typeof(ResultView), new ResultViewNavigationParameter()
                {
                    TaskDpmService = ViewModel.TaskDpmService,
                    Result = ViewModel.DisplayedResults[selectedIndex]
                }, new Microsoft.UI.Xaml.Media.Animation.EntranceNavigationTransitionInfo());
            }

        }

        base.OnNavigatedTo(e);
    }

    private void OnExit(object? sender, EventArgs e)
    {
        ExitInternal();
    }

    private void ExitInternal()
    {
        if (ViewModel == null) return;

        ViewModel.Dispose();
        WeakReferenceMessenger.Default.Unregister<string, string>(ViewModel, "NoResultAvaliable");
        WeakReferenceMessenger.Default.Unregister<string, string>(ViewModel, "NavigateToNewResult");
    }

    private void NavigationViewer_ItemInvoked(NavigationView sender, NavigationViewItemInvokedEventArgs args)
    {
        if ((string)args.InvokedItemContainer.Tag == "Home")
            App.GetService<MainWindow>()!.Frame.GoBack();
        else if ((string)args.InvokedItemContainer.Tag == "StartFiltering")
            App.GetService<MainWindow>()!.Frame.Navigate(typeof(ProcessingPage), ViewModel.Detail.Photos);
    }

    

    protected override void OnNavigatedFrom(NavigationEventArgs e)
    {
        if(e.SourcePageType != typeof(ProcessingPage))
        {
            ExitInternal();
            App.Current.ExitProcess -= OnExit;
        }

        base.OnNavigatedFrom(e);
    }

    public string GetQueryPlaceHolderText(string taskName) => "SearchPlaceholderFormat".GetLocalized(LC.TaskWorkspace).Replace("$NAME$", taskName);

    
    public double Decrease(int val) => val - 1;
    public string StrDecrease(int val) => (val - 1).ToString();
    public int FallbackToZero(int val) => val < 0 ? 0 : val;
    public string StrFallbackToZero(int val) => (val < 0 ? 0 : val).ToString();


    private async void PhotoGalleryViewer_AddResolve(Controls.PhotoGalleryViewer sender, Controls.AddPhotoResolveEventArgs args)
    {
        await ViewModel.AddPhoto(args.PhotoPath);
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

    private async void PhotoGalleryViewer_DeleteRequested(Controls.PhotoGalleryViewer sender, Controls.DeletePhotoRequestedEventArgs args)
    {
        await ViewModel.Delete(args.DeleteItem, (bool)args.State);
        // remove corresponding item viewmodel
        var vm = ViewModel.Photos.FirstOrDefault(p => p.Model == args.DeleteItem || p.Path == args.DeleteItem.Path);
        if (vm != null)
            ViewModel.Photos.Remove(vm);
    }

    private async Task<object> PhotoGalleryViewer_DeleteResolving(Controls.PhotoGalleryViewer sender, Controls.DeletePhotoResolvingEventArgs args)
    {
        (bool cancel, bool removePhysicalFile) = await ViewModel.DeleteRequesting();
        args.Cancel = cancel;
        return removePhysicalFile;
    }

    private async void PhotoGalleryViewer_RenameResolve(Controls.PhotoGalleryViewer sender, Controls.RenameResolveEventArgs args)
    {
        await PhotoItemOperationUtils.Rename(args.RenameItem, args.NewName);
        // update VM so UI reflects the change
        var vm = ViewModel.Photos.FirstOrDefault(p => p.Model == args.RenameItem || p.Path == args.RenameItem.Path);
        if (vm != null)
            vm.UserName = args.NewName;
    }

    private void ListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (e.AddedItems.Any() && !(ViewModel?.CurrentResultViewModel?.Equals(e.AddedItems[0])).GetValueOrDefault(false)) // unless .Equals, this is reference comperation
        {
            ResultViewFrame.Navigate(typeof(ResultView), new ResultViewNavigationParameter()
            {
                TaskDpmService = ViewModel.TaskDpmService,
                Result = e.AddedItems[0] as FiltResultViewModel
            }, new Microsoft.UI.Xaml.Media.Animation.EntranceNavigationTransitionInfo());
            ViewModel.CurrentResultViewModel = e.AddedItems[0] as FiltResultViewModel;
        }
    }
    public Visibility GetEmptyTipVisibility(ObservableCollection<FiltResultViewModel> results) => results.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
    public bool Not(bool b) => !b;
    private void FilterBasedOnSelected_Invoked(Controls.PhotoGalleryViewer sender, Controls.ItemOperationInvokedEventArgs args)
    {
        App.GetService<MainWindow>()!.Frame.Navigate(typeof(ProcessingPage), EnumerableExtensions.AsObservable(args.OperationItems.ToList()));
    }

    private void SelectToPresent_Invoked(Controls.PhotoGalleryViewer sender, Controls.ItemOperationInvokedEventArgs args)
    {
        ViewModel.FiltTask.PresentPhoto = args.OperationItems[0].Path;
    }

    private void NavigationViewer_SelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
    {
    }
}
