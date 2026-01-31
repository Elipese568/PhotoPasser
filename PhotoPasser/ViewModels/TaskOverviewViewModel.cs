using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.WinUI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using PhotoPasser.Dialog;
using PhotoPasser.Models;
using PhotoPasser.Primitive;
using PhotoPasser.Service;
using PhotoPasser.Strings;
using PhotoPasser.Helper;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

namespace PhotoPasser.ViewModels;

public sealed partial class TaskOverviewViewModel : ObservableObject
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
            case ChangeType.Added:
                Tasks.Add(e.Task);
                break;
            case ChangeType.Updated:
                int idx = Tasks.IndexOf(Tasks.First(x => x.Id == e.Task.Id));
                if (idx >= 0)
                {
                    Tasks[idx] = e.Task;
                }
                break;
            case ChangeType.Deleted:
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
            Title = "AddNewTaskTitle".GetLocalized(LC.TaskOverview),
            Content = page,
            PrimaryButtonText = "AddPrompt".GetLocalized(LC.General),
            CloseButtonText = "CancelPrompt".GetLocalized(LC.General),
            DefaultButton = ContentDialogButton.Primary,
            IsPrimaryButtonEnabled = false,
        }.With(x => x.ApplyApplicationOption());
        page.ValidationStateChanged += (s, e) =>
        {
            cd.IsPrimaryButtonEnabled = e.IsValid;
        };
        var result = await cd.ShowAsync();
        if (result == ContentDialogResult.None)
        {
            return;
        }

        var newTask = new FiltTask()
        {
            Name = page.ViewModel.TaskName,
            CreateAt = DateTime.Now,
            DestinationPath = page.ViewModel.DestinationPath,
            CopySource = page.ViewModel.CopySource,
            Description = page.ViewModel.Description,
            Id = Guid.NewGuid(),
            PresentPhoto = App.Current.Resources["EmptyTaskItemPresentPhotoPath"] as string,
            RecentlyVisitAt = DateTime.Now
        };

        _taskItemService.AddTask(newTask);
    }

    [RelayCommand]
    public async Task EditTask(FiltTask task)
    {
        var page = new EditTaskInformationDialogPage(task);
        ContentDialog cd = new ContentDialog()
        {
            Title = "EditTaskInformationTitle".GetLocalized(LC.TaskOverview),
            Content = page,
            PrimaryButtonText = "SavePrompt".GetLocalized(LC.General),
            CloseButtonText = "CancelPrompt".GetLocalized(LC.General),
            DefaultButton = ContentDialogButton.Primary
        }.With(x => x.ApplyApplicationOption());
        var result = await cd.ShowAsync();
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
            PresentPhoto = task.PresentPhoto,
            CreateAt = task.CreateAt,
            RecentlyVisitAt = task.RecentlyVisitAt
        };
        _taskItemService.UpdateTask(updatedTask);
    }

    [RelayCommand]
    public async Task DeleteTask(FiltTask task)
    {
        ContentDialog cd = new ContentDialog()
        {
            Title = "DeleteTaskTitle".GetLocalized(LC.TaskOverview),
            Content = "DeleteTip".GetLocalized(LC.General).Replace(ReplaceItem.DeleteItemPron, "RemoveTaskPron".GetLocalized(LC.TaskOverview)),
            PrimaryButtonText = "DeletePrompt".GetLocalized(LC.General),
            CloseButtonText = "CancelPrompt".GetLocalized(LC.General),
            DefaultButton = ContentDialogButton.Close,
        }.With(x => x.ApplyApplicationOption());

        if (await cd.ShowAsync() == ContentDialogResult.None)
        {
            return;
        }
        _taskItemService.DeleteTask(task.Id);
    }
}
