using System;
using Microsoft.UI.Xaml.Data;
using PhotoPasser.Primitive;

namespace PhotoPasser.Converters;

/// <summary>
/// 将 TaskSortBy 转换为显示文本（中文）
/// </summary>
public class TaskSortByToDisplayConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        if (value is TaskSortBy sortBy)
        {
            return sortBy switch
            {
                TaskSortBy.Name => "名称",
                TaskSortBy.Description => "描述",
                TaskSortBy.CreateAt => "创建时间",
                TaskSortBy.RecentlyVisitAt => "最近访问",
                _ => sortBy.ToString()
            };
        }
        return value?.ToString() ?? "";
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        throw new NotImplementedException();
    }
}
