using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Data;
using PhotoPasser.Primitive;
using System;

namespace PhotoPasser.Converters;

public class TaskViewItemTemplateConverter : IValueConverter
{
    public DataTemplate GridItemTemplate { get; set; }
    public DataTemplate ListItemTemplate { get; set; }
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        return (TaskViewType)value switch
        {
            TaskViewType.Grid => GridItemTemplate,
            TaskViewType.List => ListItemTemplate,
            _ => GridItemTemplate
        };
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        throw new NotImplementedException();
    }
}
