using System;
using System.ComponentModel;
using System.Globalization;
using System.Linq;

namespace CodeBlockEndTag
{
    class BoolArrayConverter : TypeConverter
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
            string v = value as string;
            // Bit string to bool array
            return v == null ? 
                base.ConvertFrom(context, culture, value) : 
                v.ToCharArray().Select(chr => chr=='1').ToArray();
        }

        public override object ConvertTo(ITypeDescriptorContext context, CultureInfo culture, object value, Type destinationType)
        {
            bool[] v = value as bool[];
            if (destinationType != typeof(string) || v == null)
            {
                return base.ConvertTo(context, culture, value, destinationType);
            }
            // bool array to bit string
            return string.Join("", v.Select(b => b ? '1' : '0'));
        }

    }

}
