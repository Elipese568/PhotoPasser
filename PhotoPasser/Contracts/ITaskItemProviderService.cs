using PhotoPasser.Primitive;
using System;
using System.Collections.Generic;

namespace PhotoPasser.Service;

public interface ITaskItemProviderService
{
    void AddTask(FiltTask task);
    void DeleteTask(Guid id);
    List<FiltTask> GetAllTasks();
    FiltTask? GetTaskById(Guid id);
    void UpdateTask(FiltTask task);
    event EventHandler<TaskChangedEventArgs> TasksChanged;
}