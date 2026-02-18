using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.WinUI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using PhotoPasser.Dialog;
using PhotoPasser.Models;
using PhotoPasser.Primitive;
using PhotoPasser.Service;
using PhotoPasser.Sorting;
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
    private readonly ISorterService _sorter;
    private readonly TaskSorter _taskSorter;

    private ObservableCollection<FiltTask> _originalTasks;

    [ObservableProperty]
    private ObservableCollection<FiltTask> _tasks;

    [ObservableProperty]
    private TaskSortBy _currentSortBy;

    [ObservableProperty]
    private SortOrder _currentSortOrder;

    /// <summary>
    /// 可用的排序字段选项
    /// </summary>
    public TaskSortBy[] SortByOptions => Enum.GetValues<TaskSortBy>();

    public TaskOverviewViewModel()
    {
        _taskItemService = App.GetService<ITaskItemProviderService>();
        _sorter = App.GetService<ISorterService>();
        _taskSorter = new TaskSorter(_sorter);

        // 从 SettingProvider 加载保存的排序配置
        LoadSortSettings();

        _taskItemService.TasksChanged += TasksChanged;

        _originalTasks = new(_taskItemService.GetAllTasks() ?? []);
        Tasks = ApplySorting(_originalTasks);
    }

    /// <summary>
    /// 从持久化存储加载排序设置
    /// </summary>
    private void LoadSortSettings()
    {
        CurrentSortBy = SettingProvider.Instance.TaskSortBy;
        CurrentSortOrder = SettingProvider.Instance.TaskSortOrder;
    }

    private void TasksChanged(object? sender, TaskChangedEventArgs e)
    {
        switch (e.TypeOfChange)
        {
            case ChangeType.Added:
                _originalTasks.Add(e.Task);
                break;
            case ChangeType.Updated:
                int idx = _originalTasks.IndexOf(_originalTasks.First(x => x.Id == e.Task.Id));
                if (idx >= 0)
                {
                    _originalTasks[idx] = e.Task;
                }
                break;
            case ChangeType.Deleted:
                var taskToRemove = _originalTasks.FirstOrDefault(x => x.Id == e.Task.Id);
                if (taskToRemove != null)
                {
                    _originalTasks.Remove(taskToRemove);
                }
                break;
        }

        Tasks = ApplySorting(_originalTasks);
    }

    private ObservableCollection<FiltTask> ApplySorting(ObservableCollection<FiltTask> source)
    {
        return _taskSorter.Sort(source, CurrentSortBy, CurrentSortOrder);
    }

    /// <summary>
    /// 排序字段变更时：重新排序并保存设置
    /// </summary>
    partial void OnCurrentSortByChanged(TaskSortBy value)
    {
        Tasks = ApplySorting(_originalTasks);
        SettingProvider.Instance.TaskSortBy = value;
    }

    /// <summary>
    /// 排序方向变更时：重新排序并保存设置
    /// </summary>
    partial void OnCurrentSortOrderChanged(SortOrder value)
    {
        Tasks = ApplySorting(_originalTasks);
        SettingProvider.Instance.TaskSortOrder = value;
    }

    /// <summary>
    /// 切换排序方向（便捷方法）
    /// </summary>
    [RelayCommand]
    public void ToggleSortDirection()
    {
        CurrentSortOrder = CurrentSortOrder == SortOrder.Ascending
            ? SortOrder.Descending
            : SortOrder.Ascending;
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
