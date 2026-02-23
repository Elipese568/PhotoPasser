using PhotoPasser.Helper;
using PhotoPasser.Primitive;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace PhotoPasser.Service.Mock;

public class MockTaskItemProviderService : ITaskItemProviderService
{
    private static FiltTask[] _mockChoices = [
        new FiltTask
        {
            Id = Guid.NewGuid(),
            Name = "Task 1",
            CreateAt = DateTime.Now.AddDays(-2),
            RecentlyVisitAt = DateTime.Now,
            Description = "Description 1",
            PresentPhoto = "ms-appx:///Assets/StoreLogo.png",
            DestinationPath = Path.GetTempPath()
        },
        new FiltTask
        {
            Id = Guid.NewGuid(),
            Name = "Task 2",
            CreateAt = DateTime.Now.AddDays(-3),
            RecentlyVisitAt = DateTime.Now.AddDays(-1),
            Description = "Looooooooooooooo oooooooooooo oooooooooooo nggggggggg Description 2",
            PresentPhoto = "ms-appx:///Assets/StoreLogo.png",
            DestinationPath = Path.GetTempPath()
        },
        new FiltTask
        {
            Id = Guid.NewGuid(),
            Name = "Task 3",
            CreateAt = DateTime.Now.AddDays(-3),
            RecentlyVisitAt = DateTime.Now.AddDays(-2),
            Description = "",
            PresentPhoto = "ms-appx:///Assets/StoreLogo.png",
            DestinationPath = Path.GetTempPath()
        }
    ];
    private List<FiltTask> _tasks = Enumerable.Range(0, 100).Select(_ => Random.Shared.GetItems(_mockChoices, 1)[0].Let(x => new FiltTask()
    {
        Id = Guid.NewGuid(),
        Name = x.Name,
        CreateAt = x.CreateAt,
        RecentlyVisitAt = x.RecentlyVisitAt,
        Description = x.Description,
        PresentPhoto = Random.Shared.GetItems(MockTaskDetailPhysicalManagerService.MockPicturePath, 1)[0],
        DestinationPath = x.DestinationPath
    })).ToList();

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
        TasksChanged(this, new(ChangeType.Added, task));
    }
    public void UpdateTask(FiltTask task)
    {
        var existingTask = GetTaskById(task.Id);
        if (existingTask != null)
        {
            existingTask.Name = task.Name;
            existingTask.Description = task.Description;
            TasksChanged(this, new(ChangeType.Updated, task));
        }
    }
    public void DeleteTask(Guid id)
    {
        var task = GetTaskById(id);
        if (task != null)
        {
            _tasks.Remove(task);
            TasksChanged(this, new(ChangeType.Deleted, task));
        }
    }
}