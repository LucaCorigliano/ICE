using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace Microsoft.Research.ICE.Converters
{
    public sealed class ObjectToVisibilityConverter : IValueConverter
    {
        public bool IsNegated { get; set; }

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            bool flag = false;
            string text = parameter as string;
            flag = ((text == null || !(value is Enum)) ? Equals(value?.ToString(), text) : Equals(value, Enum.Parse(value.GetType(), text)));
            if (IsNegated)
            {
                flag = !flag;
            }
            return (!flag) ? Visibility.Collapsed : Visibility.Visible;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}