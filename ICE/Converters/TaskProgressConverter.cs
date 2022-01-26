using System;
using System.Globalization;
using System.Windows.Data;

namespace Microsoft.Research.ICE.Converters
{
    public sealed class TaskProgressConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return (double)value / 100.0;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}