using System;
using System.Globalization;
using System.Windows.Data;

namespace Microsoft.Research.ICE.Converters
{
    public sealed class EnumToIntConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return System.Convert.ToInt32(value);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return Enum.ToObject(targetType, (int)value);
        }
    }
}