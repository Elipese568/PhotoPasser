using System.Collections.ObjectModel;
using PhotoPasser.Models;
using PhotoPasser.Primitive;

namespace PhotoPasser.Sorting;

/// <summary>
/// 任务排序的便捷 API
/// </summary>
public sealed class TaskSorter
{
    private readonly ISorterService _sorter;

    public TaskSorter(ISorterService sorter)
    {
        _sorter = sorter;
    }

    public ObservableCollection<FiltTask> Sort(
        ObservableCollection<FiltTask> tasks,
        TaskSortBy sortBy,
        SortOrder order)
    {
        var fieldName = sortBy.ToString();
        var descriptor = new SortDescriptor(fieldName, order);
        return _sorter.Sort(tasks, descriptor);
    }
}
