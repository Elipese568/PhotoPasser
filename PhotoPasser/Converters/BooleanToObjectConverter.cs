using Microsoft.UI.Xaml.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PhotoPasser.Converters;

public class BooleanToObjectConverter : IValueConverter
{
    public object TrueObject { get; set; }
    public object FalseObject { get; set; }
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        return (bool)value ? TrueObject : FalseObject;
    }
    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        throw new NotImplementedException();
    }
}
