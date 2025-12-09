using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using PhotoPasser.Dialog;
using PhotoPasser.Service;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Collections;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace PhotoPasser;

public partial class TaskOverviewViewModel : ObservableRecipient
{
    private readonly ITaskItemProviderService _taskItemService;

    public ObservableCollection<FiltTask> Tasks;

    public TaskOverviewViewModel()
    {
        _taskItemService = App.GetService<ITaskItemProviderService>();
        _taskItemService.TasksChanged += TasksChanged;

        Tasks = new(_taskItemService.GetAllTasks() ?? []);
    }

    private void TasksChanged(object? sender, TaskChangedEventArgs e)
    {
        switch (e.TypeOfChange)
        {
            case TaskChangedEventArgs.ChangeType.Added:
                Tasks.Add(e.Task);
                break;
            case TaskChangedEventArgs.ChangeType.Updated:
                int idx = Tasks.IndexOf(Tasks.First(x => x.Id == e.Task.Id));
                if (idx >= 0)
                {
                    Tasks[idx] = e.Task;
                }
                break;
            case TaskChangedEventArgs.ChangeType.Deleted:
                var taskToRemove = Tasks.FirstOrDefault(x => x.Id == e.Task.Id);
                if (taskToRemove != null)
                {
                    Tasks.Remove(taskToRemove);
                }
                break;
        }
    }

    [RelayCommand]
    public async Task AddTask()
    {
        var page = new EditTaskInformationDialogPage();
        ContentDialog cd = new ContentDialog()
        {
            Title = "Add New Task",
            Content = page,
            PrimaryButtonText = "Add",
            CloseButtonText = "Cancel",
            DefaultButton = ContentDialogButton.Primary,
            XamlRoot = App.Current.MainWindow.Content.XamlRoot,
        };

        var result = await cd.ShowAsync();
        if (result == ContentDialogResult.None)
        {
            return;
        }

        var newTask = new FiltTask()
        {
            Name = page.ViewModel.TaskName,
            DestinationPath = page.ViewModel.DestinationPath,
            CopySource = page.ViewModel.CopySource,
            Description = page.ViewModel.Description,
            Id = Guid.NewGuid(),
            PresentPhoto = App.Current.Resources["EmptyTaskItemPresentPhotoPath"] as string
        };

        _taskItemService.AddTask(newTask);
    }

    [RelayCommand]
    public async Task EditTask(FiltTask task)
    {
        var page = new EditTaskInformationDialogPage(task);
        ContentDialog cd = new ContentDialog()
        {
            Title = "Edit Task Information",
            Content = page,
            PrimaryButtonText = "Save",
            CloseButtonText = "Cancel",
            DefaultButton = ContentDialogButton.Primary,
            XamlRoot = App.Current.MainWindow.Content.XamlRoot,
        };
        var result = await cd.ShowAsync(ContentDialogPlacement.InPlace);
        if (result == ContentDialogResult.Secondary)
        {
            return;
        }
        var updatedTask = new FiltTask()
        {
            Name = page.ViewModel.TaskName,
            DestinationPath = page.ViewModel.DestinationPath,
            CopySource = page.ViewModel.CopySource,
            Description = page.ViewModel.Description,
            Id = task.Id,
            PresentPhoto = task.PresentPhoto
        };
        _taskItemService.UpdateTask(updatedTask);
    }

    [RelayCommand]
    public async Task DeleteTask(FiltTask task)
    {
        ContentDialog cd = new ContentDialog()
        {
            Title = "Delete Task",
            Content = "Once it¡¯s gone, it¡¯s gone. Are you sure you want to delete it?",
            PrimaryButtonText = "Delete",
            CloseButtonText = "Cancel",
            DefaultButton = ContentDialogButton.Close,
            XamlRoot = App.Current.MainWindow.Content.XamlRoot,
        };

        if (await cd.ShowAsync() == ContentDialogResult.None)
        {
            return;
        }
        _taskItemService.DeleteTask(task.Id);
    }
}

/// <summary>
/// An empty page that can be used on its own or navigated to within a Frame.
/// </summary>
public sealed partial class TaskOverview : Page
{
    public TaskOverviewViewModel ViewModel { get; set; }

    public TaskOverview()
    {
        InitializeComponent();

        ViewModel = App.GetService<TaskOverviewViewModel>();
    }

    private void LibraryItem_PointerEntered(object sender, PointerRoutedEventArgs e)
    {
        VisualStateManager.GoToState(sender as Control, "OptionSettingButtonVisible", true);
    }

    private void LibraryItem_PointerExited(object sender, PointerRoutedEventArgs e)
    {
        VisualStateManager.GoToState(sender as Control, "OptionSettingButtonDefault", true);
    }

    private void TaskItemPresenter_DoubleTapped(object sender, DoubleTappedRoutedEventArgs e)
    {
        App.Current.MainWindow.Frame.Navigate(typeof(TaskWorkspace), (sender as FrameworkElement).DataContext, new Microsoft.UI.Xaml.Media.Animation.DrillInNavigationTransitionInfo());
    }

    private void Border_Loaded(object sender, RoutedEventArgs e)
    {

    }

    private void ShadowRect_Loaded(object sender, RoutedEventArgs e)
    {
        shadow.Receivers.Add(ShadowCastGrid);
    }
}
