using System;
using System.ComponentModel;
using System.Globalization;
using System.Linq;

namespace CodeBlockEndTag;

internal class BoolArrayConverter : TypeConverter
{
    public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType)
    {
        return sourceType == typeof(string) || base.CanConvertFrom(context, sourceType);
    }

    public override bool CanConvertTo(ITypeDescriptorContext context, Type destinationType)
    {
        return destinationType == typeof(bool[]) || base.CanConvertTo(context, destinationType);
    }

    public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value)
    {
        // Bit string to bool array
        return value is not string v ?
            base.ConvertFrom(context, culture, value) :
            v.ToCharArray().Select(chr => chr == '1').ToArray();
    }

    public override object ConvertTo(ITypeDescriptorContext context, CultureInfo culture, object value, Type destinationType)
    {
        if (destinationType != typeof(string) || value is not bool[] v)
        {
            return base.ConvertTo(context, culture, value, destinationType);
        }
        // bool array to bit string
        return string.Join("", v.Select(b => b ? '1' : '0'));
    }
}
