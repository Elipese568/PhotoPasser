using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PhotoPasser.Converters;

public class UriImageSourceConverter : IValueConverter
{
    //private static Dictionary<Uri, ImageSource> _cache = new();
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        Uri uri = value as Uri ?? new Uri(value as string);
        /*if (_cache.TryGetValue(uri, out var val))
            return val;*/
        var src = new BitmapImage();

        src.UriSource = uri;
        //_cache.Add(uri, src);
        return src;
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        throw new NotImplementedException();
    }
}
