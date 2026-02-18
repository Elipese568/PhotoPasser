using System;
using Microsoft.UI.Xaml.Data;
using PhotoPasser.Primitive;

namespace PhotoPasser.Converters;

/// <summary>
/// 将 SortOrder 转换为图标字符
/// </summary>
public class SortOrderToDescriptionConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        if (value is SortOrder order)
        {
            return order == SortOrder.Ascending ? "升序" : "降序";  // 升序/降序箭头
        }
        return "降序";  // 默认降序
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        throw new NotImplementedException();
    }
}
