using System;

namespace PhotoPasser.Primitive;

public class TaskChangedEventArgs : EventArgs
{
    public ChangeType TypeOfChange { get; }
    public FiltTask Task { get; }
    public TaskChangedEventArgs(ChangeType changeType, FiltTask task)
    {
        TypeOfChange = changeType;
        Task = task;
    }
}
