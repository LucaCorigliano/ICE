using System;
using System.Globalization;
using System.Windows.Data;

namespace Microsoft.Research.ICE.Converters
{

	public abstract class BooleanConverter<T> : IValueConverter
	{
		public T TrueValue { get; set; }

		public T FalseValue { get; set; }

		protected BooleanConverter(T trueValue, T falseValue)
		{
			TrueValue = trueValue;
			FalseValue = falseValue;
		}

		public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
			return Equals(value, true) ? TrueValue : FalseValue;
		}

		public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
		{
			return Equals(value, TrueValue) ? true : false;
		}
	}
}