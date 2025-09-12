using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace CodeBlockEndTag.Converters;

internal class TextColorConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture) =>
        value switch
        {
            Brush brush => brush,
            Color color => new SolidColorBrush(color),
            string str => new SolidColorBrush((Color)ColorConverter.ConvertFromString(str)),
            _ => Colors.Black,
        };

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
        Binding.DoNothing;
}
