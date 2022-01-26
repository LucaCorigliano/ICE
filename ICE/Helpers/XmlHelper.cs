using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Media.Media3D;
using System.Xml.Linq;

namespace Microsoft.Research.ICE.Helpers
{

	public static class XmlHelper
	{
		public static string GetStringAttribute(this XElement element, string attributeName, string defaultValue = null)
		{
			return ((string)element.Attribute(attributeName)) ?? defaultValue;
		}

		public static bool GetBooleanAttribute(this XElement element, string attributeName, bool defaultValue = false)
		{
			return ((bool?)element.Attribute(attributeName)).GetValueOrDefault(defaultValue);
		}

		public static int GetIntegerAttribute(this XElement element, string attributeName, int defaultValue = 0)
		{
			return ((int?)element.Attribute(attributeName)).GetValueOrDefault(defaultValue);
		}

		public static float GetFloatAttribute(this XElement element, string attributeName, float defaultValue = 0f)
		{
			return ((float?)element.Attribute(attributeName)).GetValueOrDefault(defaultValue);
		}

		public static double GetDoubleAttribute(this XElement element, string attributeName, double defaultValue = 0.0)
		{
			return ((double?)element.Attribute(attributeName)).GetValueOrDefault(defaultValue);
		}

		public static Version GetVersionAttribute(this XElement element, string attributeName)
		{
			Version result = null;
			string stringAttribute = element.GetStringAttribute(attributeName);
			if (stringAttribute != null)
			{
				result = Version.Parse(stringAttribute);
			}
			return result;
		}

		public static double GetAngleAttribute(this XElement element, string attributeName, AngleUnit angleUnit)
		{
			return element.GetDoubleAttribute(attributeName).ToDegrees(angleUnit);
		}

		public static Matrix3D GetMatrixAttribute(this XElement element, string attributeName)
		{
			Matrix3D result = Matrix3D.Identity;
			string stringAttribute = element.GetStringAttribute(attributeName);
			if (stringAttribute != null)
			{
				string[] array = stringAttribute.Split(',');
				double[] array2 = new double[9];
				for (int i = 0; i < array2.Length; i++)
				{
					array2[i] = double.Parse(array[i].Trim(), CultureInfo.InvariantCulture);
				}
				result = new Matrix3D(array2[0], array2[1], array2[2], 0.0, array2[3], array2[4], array2[5], 0.0, array2[6], array2[7], array2[8], 0.0, 0.0, 0.0, 0.0, 1.0);
			}
			return result;
		}

		public static void SetMatrixAttribute(this XElement element, string attributeName, Matrix3D value)
		{
			string value2 = string.Format(CultureInfo.InvariantCulture, "{0:R},{1:R},{2:R},{3:R},{4:R},{5:R},{6:R},{7:R},{8:R}", value.M11, value.M12, value.M13, value.M21, value.M22, value.M23, value.M31, value.M32, value.M33);
			element.Add(new XAttribute(attributeName, value2));
		}

		public static void SetAttribute<T>(this XElement element, string attributeName, T value, Dictionary<string, T> values, T defaultValue = default(T))
		{
			if (!Equals(value, defaultValue))
			{
				element.Add(new XAttribute(attributeName, values.First((KeyValuePair<string, T> item) => Equals(item.Value, value)).Key));
			}
		}

		public static T GetAttribute<T>(this XElement element, string attributeName) where T : struct
		{
			T result = default(T);
			string text = (string)element.Attribute(attributeName);
			if (text != null && !Enum.TryParse<T>(text, ignoreCase: true, out result))
			{
				result = default(T);
			}
			return result;
		}

		public static T GetAttribute<T>(this XElement element, string attributeName, Dictionary<string, T> values, T defaultValue = default(T))
		{
			T value = defaultValue;
			string text = (string)element.Attribute(attributeName);
			if (text != null && !values.TryGetValue(text, out value))
			{
				value = defaultValue;
			}
			return value;
		}

		public static float[] GetFloatArrayValue(this XElement element, int length)
		{
			return element.GetArrayValue(length, (string number) => float.Parse(number, CultureInfo.InvariantCulture));
		}

		public static double[] GetDoubleArrayValue(this XElement element, int length)
		{
			return element.GetArrayValue(length, (string number) => double.Parse(number, CultureInfo.InvariantCulture));
		}

		private static T[] GetArrayValue<T>(this XElement element, int length, Func<string, T> parseFunc)
		{
			T[] array = null;
			string value = element.Value;
			if (!string.IsNullOrWhiteSpace(value))
			{
				array = new T[length];
				string[] array2 = value.SplitWords();
				for (int i = 0; i < length; i++)
				{
					array[i] = parseFunc(array2[i]);
				}
			}
			return array;
		}

		public static Vector GetOldVectorAttribute(this XElement element, string attributeName)
		{
			Vector result = default(Vector);
			string stringAttribute = element.GetStringAttribute(attributeName);
			if (stringAttribute != null)
			{
				string[] array = stringAttribute.SplitWords();
				double x = double.Parse(array[0], CultureInfo.InvariantCulture);
				double y = double.Parse(array[1], CultureInfo.InvariantCulture);
				result = new Vector(x, y);
			}
			return result;
		}

		public static double GetOldAngleAttribute(this XElement element, string attributeName)
		{
			double result = 0.0;
			string stringAttribute = element.GetStringAttribute(attributeName);
			if (stringAttribute != null)
			{
				string[] array = stringAttribute.SplitWords();
				result = ParseOldAngle(array[0], array[1]);
			}
			return result;
		}

		public static Quaternion GetOldRotationAttribute(this XElement element, string attributeName)
		{
			Quaternion result = Quaternion.Identity;
			string stringAttribute = element.GetStringAttribute(attributeName);
			if (stringAttribute != null)
			{
				string[] array = stringAttribute.SplitWords();
				double z = ParseOldAngle(array[0], array[1]);
				double x = ParseOldAngle(array[2], array[3]);
				double y = ParseOldAngle(array[4], array[5]);
				Vector3D eulerAngles = new Vector3D(x, y, z);
				result = eulerAngles.ToQuaternion();
			}
			return result;
		}

		private static double ParseOldAngle(string number, string unit)
		{
			double num = double.Parse(number, CultureInfo.InvariantCulture);
			if (!string.Equals(unit, "d", StringComparison.OrdinalIgnoreCase) && !string.Equals(unit, "deg", StringComparison.OrdinalIgnoreCase) && !string.Equals(unit, "degrees", StringComparison.OrdinalIgnoreCase))
			{
				num = num.ToDegrees();
			}
			return num;
		}

		private static string[] SplitWords(this string value)
		{
			return value.Split(new char[7] { ' ', '\f', '\n', '\r', '\t', '\v', '\u0085' }, StringSplitOptions.RemoveEmptyEntries);
		}
	}

}