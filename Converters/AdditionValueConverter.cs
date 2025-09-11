using System;
using System.Windows.Data;
using System.Globalization;

namespace CodeBlockEndTag.Converters;

internal class AdditionValueConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture) =>
        System.Convert.ToDouble(value) +
        System.Convert.ToDouble(parameter);

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
        System.Convert.ToDouble(value) -
        System.Convert.ToDouble(parameter);
}
