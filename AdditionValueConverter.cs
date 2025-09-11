using System;
using System.Windows.Data;
using System.Globalization;

namespace CodeBlockEndTag;

internal class AdditionValueConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return System.Convert.ToDouble(value) + System.Convert.ToDouble(parameter);
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return System.Convert.ToDouble(value) - System.Convert.ToDouble(parameter);
    }
}
