using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Media.Imaging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PhotoPasser.Helper;

namespace PhotoPasser.Converters;

public class FileIconConverter : IValueConverter
{
    private static Dictionary<string, BitmapImage> _cache = new();
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        string rawValExt = Path.GetExtension(value as string);
        if (_cache.TryGetValue(rawValExt, out var result))
            return result;
        BitmapImage icon = new();
        icon.SetSource(FileIconStreamHelper.GetFileTypeIconStream(rawValExt, false));
        _cache.Add(rawValExt, icon);
        return icon;
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        throw new NotImplementedException();
    }
}
