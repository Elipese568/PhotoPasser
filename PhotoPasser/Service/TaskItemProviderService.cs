using PhotoPasser.Helper;
using PhotoPasser.Service.Primitive;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Storage;

namespace PhotoPasser.Service;

[JsonStorageFile(FileName = "tasks.json")]
public class TaskItemProviderService : JsonSeriailizingServiceBase<List<FiltTask>>, ITaskItemProviderService
{
    public event EventHandler<TaskChangedEventArgs> TasksChanged;

    private StorageFolder _storageFolder;
    public TaskItemProviderService()
    {
        InitializeAsync();
    }
    public async Task InitializeAsync()
    {
        _storageFolder = await ApplicationData.Current.LocalFolder.CreateFolderAsync("Tasks", CreationCollisionOption.OpenIfExists);
        _data ??= new();
    }
    public List<FiltTask> GetAllTasks()
    {
        return _data;
    }
    public FiltTask? GetTaskById(Guid id)
    {
        return _data.FirstOrDefault(t => t.Id == id);
    }
    public void AddTask(FiltTask task)
    {
        task.Id = Guid.NewGuid();
        _data.Add(task);
        _storageFolder.CreateFolderAsync(task.Id.ToString()).Sync();

        TasksChanged(this, new(TaskChangedEventArgs.ChangeType.Added, task));
    }
    public void UpdateTask(FiltTask task)
    {
        var existingTask = GetTaskById(task.Id);
        if (existingTask != null)
        {
            existingTask.Name = task.Name;
            existingTask.Description = task.Description;
            TasksChanged(this, new(TaskChangedEventArgs.ChangeType.Updated, task));
        }
        
    }
    public void DeleteTask(Guid id)
    {
        var task = GetTaskById(id);
        if (task != null)
        {
            _data.Remove(task);
        }
        TasksChanged(this, new(TaskChangedEventArgs.ChangeType.Deleted, task));
    }
}
public class MockTaskItemProviderService : ITaskItemProviderService
{
    private List<FiltTask> _tasks = [
        new FiltTask { Id = Guid.NewGuid(), Name = "Task 1", Description = "Description 1", PresentPhoto = "ms-appx:///Assets/StoreLogo.png" },
        new FiltTask { Id = Guid.NewGuid(), Name = "Task 2", Description = "Looooooooooooooo oooooooooooo oooooooooooo nggggggggg Description 2", PresentPhoto = "ms-appx:///Assets/StoreLogo.png" },
        new FiltTask { Id = Guid.NewGuid(), Name = "Task 3", Description = "", PresentPhoto = "ms-appx:///Assets/StoreLogo.png" },

    ];

    public event EventHandler<TaskChangedEventArgs> TasksChanged;

    public List<FiltTask> GetAllTasks()
    {
        return _tasks;
    }
    public FiltTask? GetTaskById(Guid id)
    {
        return _tasks.FirstOrDefault(t => t.Id == id);
    }
    public void AddTask(FiltTask task)
    {
        task.Id = Guid.NewGuid();
        _tasks.Add(task);
        TasksChanged(this, new(TaskChangedEventArgs.ChangeType.Added, task));
    }
    public void UpdateTask(FiltTask task)
    {
        var existingTask = GetTaskById(task.Id);
        if (existingTask != null)
        {
            existingTask.Name = task.Name;
            existingTask.Description = task.Description;
            TasksChanged(this, new(TaskChangedEventArgs.ChangeType.Updated, task));
        }
    }
    public void DeleteTask(Guid id)
    {
        var task = GetTaskById(id);
        if (task != null)
        {
            _tasks.Remove(task);
        }
        TasksChanged(this, new(TaskChangedEventArgs.ChangeType.Deleted, task));
    }
}