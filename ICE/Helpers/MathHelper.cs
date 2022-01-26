using System;
using System.Windows.Media.Media3D;

namespace Microsoft.Research.ICE.Helpers
{

	public static class MathHelper
	{
		public static bool IsNearlyZero(this double value)
		{
			return Math.Abs(value) < 1E-06;
		}

		public static double Clamp(this double value, double min, double max)
		{
			return Math.Max(min, Math.Min(value, max));
		}

		public static double ToDegrees(this double value, AngleUnit angleUnit = AngleUnit.Radians)
		{
			if (angleUnit == AngleUnit.Radians)
			{
				return value * 180.0 / Math.PI;
			}
			return value;
		}

		public static double ToRadians(this double value, AngleUnit angleUnit = AngleUnit.Degrees)
		{
			if (angleUnit == AngleUnit.Degrees)
			{
				return value * Math.PI / 180.0;
			}
			return value;
		}

		public static Vector3D ToEulerAngles(this Quaternion q)
		{
			Matrix3D identity = Matrix3D.Identity;
			identity.Rotate(q);
			Vector3D result = default(Vector3D);
			double num = identity.M32.Clamp(-1.0, 1.0);
			result.X = Math.Asin(0.0 - num).ToDegrees();
			double m = identity.M31;
			double m2 = identity.M33;
			double m3 = identity.M12;
			double m4 = identity.M22;
			if ((!m.IsNearlyZero() || !m2.IsNearlyZero()) && (!m3.IsNearlyZero() || !m4.IsNearlyZero()))
			{
				result.Y = Math.Atan2(m, m2).ToDegrees();
				result.Z = Math.Atan2(m3, m4).ToDegrees();
			}
			else
			{
				result.Z = 0.0;
				double y = 0.0 - identity.M13;
				double m5 = identity.M11;
				result.Y = Math.Atan2(y, m5).ToDegrees();
			}
			return result;
		}

		public static Quaternion ToQuaternion(this Vector3D eulerAngles)
		{
			Quaternion quaternion = new Quaternion(new Vector3D(1.0, 0.0, 0.0), eulerAngles.X);
			Quaternion quaternion2 = new Quaternion(new Vector3D(0.0, 1.0, 0.0), eulerAngles.Y);
			Quaternion quaternion3 = new Quaternion(new Vector3D(0.0, 0.0, 1.0), eulerAngles.Z);
			return quaternion2 * (quaternion * quaternion3);
		}
	}

}