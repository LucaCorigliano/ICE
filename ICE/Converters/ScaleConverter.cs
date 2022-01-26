using System;
using System.Globalization;
using System.Windows.Data;

namespace Microsoft.Research.ICE.Converters
{
    public sealed class ScaleConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is double)
            {
                return ((double)value * 100.0).ToString(CultureInfo.CurrentCulture);
            }
            return Binding.DoNothing;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string s && double.TryParse(s, NumberStyles.Number, CultureInfo.CurrentCulture, out var result))
            {
                return result / 100.0;
            }
            return Binding.DoNothing;
        }
    }
}