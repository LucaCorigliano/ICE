using System;
using System.Globalization;
using System.Windows.Data;

namespace Microsoft.Research.ICE.Converters
{
    public sealed class ObjectToBooleanConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            string text = parameter as string;
            if (text != null && value is Enum)
            {
                return Equals(value, Enum.Parse(value.GetType(), text));
            }
            return Equals(value?.ToString(), text);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if ((bool)value)
            {
                return Enum.Parse(targetType, parameter.ToString());
            }
            return Binding.DoNothing;
        }
    }
}