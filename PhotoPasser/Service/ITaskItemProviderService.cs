using System;
using System.Collections.Generic;

namespace PhotoPasser.Service;

public class TaskChangedEventArgs : EventArgs
{
    public enum ChangeType
    {
        Added,
        Updated,
        Deleted
    }
    public ChangeType TypeOfChange { get; }
    public FiltTask Task { get; }
    public TaskChangedEventArgs(ChangeType changeType, FiltTask task)
    {
        TypeOfChange = changeType;
        Task = task;
    }
}

public interface ITaskItemProviderService
{
    void AddTask(FiltTask task);
    void DeleteTask(Guid id);
    List<FiltTask> GetAllTasks();
    FiltTask? GetTaskById(Guid id);
    void UpdateTask(FiltTask task);
    event EventHandler<TaskChangedEventArgs> TasksChanged;
}