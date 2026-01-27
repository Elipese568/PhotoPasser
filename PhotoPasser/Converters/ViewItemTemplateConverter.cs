using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Data;
using PhotoPasser.Primitive;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PhotoPasser.Converters;

public class ViewItemTemplateConverter : IValueConverter
{
    public DataTemplate TrumbullItemTemplate { get; set; }
    public DataTemplate DetailItemTemplate { get; set; }
    public DataTemplate TileItemTemplate { get; set; }
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        return (DisplayView)value switch
        {
            DisplayView.Trumbull => TrumbullItemTemplate,
            DisplayView.Details => DetailItemTemplate,
            DisplayView.Tiles => TileItemTemplate,
            _ => TrumbullItemTemplate
        };
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        throw new NotImplementedException();
    }
}
