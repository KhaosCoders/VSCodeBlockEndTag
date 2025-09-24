using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;

namespace CodeBlockEndTag;

internal static class MaxAdornmentWidthExtension
{
    public static bool GetEnable(DependencyObject obj) => (bool)obj.GetValue(EnableProperty);
    public static void SetEnable(DependencyObject obj, bool value) => obj.SetValue(EnableProperty, value);
    public static readonly DependencyProperty EnableProperty =
        DependencyProperty.RegisterAttached("Enable", typeof(bool), typeof(MaxAdornmentWidthExtension), new PropertyMetadata(false,
            new PropertyChangedCallback(OnEnableChanged)));

    private static void OnEnableChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (e.NewValue is not bool enabled || !enabled) return;
        if (d is not FrameworkElement element) return;

        if (!BindMaxWidth(element))
        {
            element.Loaded += (s, ev) => BindMaxWidth(element);
        }
    }

    private static bool BindMaxWidth(FrameworkElement element)
    {
        var adornmentWrapper = FindParent(element, "AdornmentWrapper");
        var viewStack = FindParent(adornmentWrapper, "ViewStack");
        if (viewStack != null && adornmentWrapper != null)
        {
            var multiBinding = new MultiBinding
            {
                Converter = new MaxAdornmentWidthConverter()
            };
            multiBinding.Bindings.Add(new Binding("ActualWidth")
            {
                Source = viewStack
            });
            multiBinding.Bindings.Add(new Binding("(Canvas.Left)")
            {
                Source = adornmentWrapper
            });
            BindingOperations.SetBinding(element, FrameworkElement.MaxWidthProperty, multiBinding);
            return true;
        }
        return false;
    }

    private static FrameworkElement FindParent(FrameworkElement child, string parentType)
    {
        FrameworkElement parent = VisualTreeHelper.GetParent(child) as FrameworkElement;
        while (parent != null)
        {
            if (parent.GetType().Name == parentType)
            {
                return parent;
            }
            parent = VisualTreeHelper.GetParent(parent) as FrameworkElement;
        }
        return null;
    }
}

internal class MaxAdornmentWidthConverter : IMultiValueConverter
{
    public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
    {
        if (values.Length == 2 &&
            values[0] is double actualWidth &&
            values[1] is double canvasLeft)
        {
            return Math.Max(0, actualWidth - canvasLeft - 20); // 20 is margin
        }
        return double.MaxValue;

    }

    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture) =>
        throw new NotImplementedException();
}
