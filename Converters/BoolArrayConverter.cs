using System;
using System.ComponentModel;
using System.Globalization;
using System.Linq;

namespace CodeBlockEndTag.Converters;

internal class BoolArrayConverter : TypeConverter
{
    public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType) =>
        sourceType == typeof(string) ||
        base.CanConvertFrom(context, sourceType);

    public override bool CanConvertTo(ITypeDescriptorContext context, Type destinationType) =>
        destinationType == typeof(bool[]) ||
        base.CanConvertTo(context, destinationType);

    // Bit string to bool array
    public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value) =>
        value is string v ?
            v.ToCharArray().Select(chr => chr == '1').ToArray() :
            base.ConvertFrom(context, culture, value);

    public override object ConvertTo(ITypeDescriptorContext context, CultureInfo culture, object value, Type destinationType)
    {
        if (destinationType != typeof(string) || value is not bool[] bools)
        {
            return base.ConvertTo(context, culture, value, destinationType);
        }
        // bool array to bit string
        return string.Join("", bools.Select(b => b ? '1' : '0'));
    }
}
