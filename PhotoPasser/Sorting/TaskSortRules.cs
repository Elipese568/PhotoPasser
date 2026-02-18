using System;
using PhotoPasser.Models;
using PhotoPasser.Primitive;

namespace PhotoPasser.Sorting;

/// <summary>
/// FiltTask 项目的排序规则
/// </summary>
public static class TaskSortRules
{
    public class NameRule : ISortRule<FiltTask>
    {
        public string FieldName => nameof(TaskSortBy.Name);
        public string DisplayName => "TaskSortName";

        public IComparable GetSortKey(FiltTask item)
            => item.Name ?? string.Empty;
    }

    public class DescriptionRule : ISortRule<FiltTask>
    {
        public string FieldName => nameof(TaskSortBy.Description);
        public string DisplayName => "TaskSortDescription";

        public IComparable GetSortKey(FiltTask item)
            => item.Description ?? string.Empty;
    }

    public class CreateAtRule : ISortRule<FiltTask>
    {
        public string FieldName => nameof(TaskSortBy.CreateAt);
        public string DisplayName => "TaskSortCreateAt";

        public IComparable GetSortKey(FiltTask item)
            => item.CreateAt;
    }

    public class RecentlyVisitAtRule : ISortRule<FiltTask>
    {
        public string FieldName => nameof(TaskSortBy.RecentlyVisitAt);
        public string DisplayName => "TaskSortRecentlyVisitAt";

        public IComparable GetSortKey(FiltTask item)
            => item.RecentlyVisitAt;
    }
}
