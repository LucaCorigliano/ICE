using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media.Imaging;

namespace Microsoft.Research.ICE.Converters
{
    public sealed class BitmapImageConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            Uri result = value as Uri;
            if (result == null)
            {
                string uriString = value as string;
                Uri.TryCreate(uriString, UriKind.RelativeOrAbsolute, out result);
            }
            BitmapImage result2 = null;
            if (result != null)
            {
                try
                {
                    result2 = new BitmapImage();
                    result2.BeginInit();
                    result2.CacheOption = BitmapCacheOption.OnLoad;
                    result2.UriSource = result;
                    result2.EndInit();
                    return result2;
                }
                catch
                {
                    return null;
                }
            }
            return result2;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}