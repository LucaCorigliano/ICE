using System;
using System.Globalization;
using System.Windows.Data;

namespace Microsoft.Research.ICE.Converters
{
    public sealed class LogarithmicConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return Math.Log((double)value, 2.0);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return Math.Pow(2.0, (double)value);
        }
    }
}