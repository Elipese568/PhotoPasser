using System;
using Microsoft.UI.Xaml.Data;
using PhotoPasser.Models;
using PhotoPasser.Primitive;

namespace PhotoPasser.Converters;

public class TaskDisplayTimeConverter : IValueConverter
{
    public TaskSortBy CurrentSortBy { get; set; }

    public object Convert(object value, Type targetType, object parameter, string language)
    {
        if (value is FiltTask task)
        {
            var displayTime = CurrentSortBy switch
            {
                TaskSortBy.CreateAt => task.CreateAt,
                TaskSortBy.RecentlyVisitAt => task.RecentlyVisitAt,
                _ => task.RecentlyVisitAt
            };
            return displayTime.ToString("yyyy-MM-dd HH:mm");
        }
        return string.Empty;
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        throw new NotImplementedException();
    }
}
