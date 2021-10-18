using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace CodeBlockEndTag
{
    internal class TextColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            switch (value)
            {
                case Brush brush:
                    return brush;
                case Color color:
                    return new SolidColorBrush(color);
                case string str:
                    return new SolidColorBrush((Color)ColorConverter.ConvertFromString(str));
                default:
                    return Colors.Black;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
