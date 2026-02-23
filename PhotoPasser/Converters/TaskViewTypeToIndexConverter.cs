using System;
using Microsoft.UI.Xaml.Data;
using PhotoPasser.Primitive;

namespace PhotoPasser.Converters;

public class TaskViewTypeToIndexConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        if (value is TaskViewType viewType)
        {
            return (int)viewType;
        }
        return 0;
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        if (value is int index)
        {
            return (TaskViewType)index;
        }
        return TaskViewType.Grid;
    }
}
