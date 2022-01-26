using System;
using System.Globalization;
using System.Windows.Data;

namespace Microsoft.Research.ICE.Converters;

public sealed class FirstOrSecondButNotThirdBooleanConverter : IMultiValueConverter
{
	public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
	{
		if (values.Length == 3 && values[0] is bool && values[1] is bool && values[2] is bool)
		{
			return ((bool)values[0] || (bool)values[1]) && !(bool)values[2];
		}
		return Binding.DoNothing;
	}

	public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
	{
		throw new NotSupportedException();
	}
}
