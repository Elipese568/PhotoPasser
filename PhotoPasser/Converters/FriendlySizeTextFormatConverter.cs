using Microsoft.UI.Xaml.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PhotoPasser.Converters;

public class FriendlySizeTextFormatConverter : IValueConverter
{
    private const double B = 1;
    private const double KiB = B * 1024;
    private const double MiB = KiB * 1024;
    private const double GiB = MiB * 1024;
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        long longSize = (long)value;

        return longSize switch
        {
            >= (long)GiB => $"{longSize / GiB:0.##} GiB",
            >= (long)MiB => $"{longSize / MiB:0.##} MiB",
            >= (long)KiB => $"{longSize / KiB:0.##} KiB",
            _ => $"{longSize} B",
        };
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        throw new NotImplementedException();
    }
}
