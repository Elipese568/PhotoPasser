using System.Collections.ObjectModel;
using System.Linq;
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
        var sorted = _sorter.Sort(tasks, descriptor);
        if (tasks != null && sorted.SequenceEqual(tasks)) return tasks; // 避免不必要的集合替换，保持 UI 绑定稳定
        return sorted;
    }
}
