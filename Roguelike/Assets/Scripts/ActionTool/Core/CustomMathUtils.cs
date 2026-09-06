using UnityEngine;

namespace ActionTool.Core
{
	public static class CustomMathUtils
	{
		public const float EPSILON = 0.0001f;
		public const float PRECISION_THRESHOLD = 0.001f;

		public static float ClampToFloat(double value)
		{
			if (double.IsPositiveInfinity(value))
			{
				return float.PositiveInfinity;
			}

			if (double.IsNegativeInfinity(value))
			{
				return float.NegativeInfinity;
			}

			if (value < -3.4028234663852886E+38)
			{
				return float.MinValue;
			}

			if (value > 3.4028234663852886E+38)
			{
				return float.MaxValue;
			}

			return (float)value;
		}

		public static int ClampToInt(long value)
		{
			if (value < int.MinValue)
			{
				return int.MinValue;
			}

			if (value > int.MaxValue)
			{
				return int.MaxValue;
			}

			return (int)value;
		}

		public static int GetNumberOfDecimalsForMinimumDifference(float minDifference)
		{
			return Mathf.Clamp(-Mathf.FloorToInt(Mathf.Log10(Mathf.Abs(minDifference))), 0, 15);
		}

		static int GetNumberOfDecimalsForMinimumDifference(double minDifference)
		{
			return (int)System.Math.Max(0.0, 0.0 - System.Math.Floor(System.Math.Log10(System.Math.Abs(minDifference))));
		}

		static double DiscardLeastSignificantDecimal(double v)
		{
			int digits = System.Math.Max(0, (int)(5.0 - System.Math.Log10(System.Math.Abs(v))));
			try
			{
				return System.Math.Round(v, digits);
			}
			catch (System.ArgumentOutOfRangeException)
			{
				return 0.0;
			}
		}

		public static double RoundBasedOnMinimumDifference(double valueToRound, double minDifference)
		{
			if (minDifference == 0.0)
			{
				return DiscardLeastSignificantDecimal(valueToRound);
			}

			return System.Math.Round(valueToRound, GetNumberOfDecimalsForMinimumDifference(minDifference), System.MidpointRounding.AwayFromZero);
		}

		public static double CalculateFloatDragSensitivity(double value)
		{
			if (double.IsInfinity(value) || double.IsNaN(value))
			{
				return 0.0;
			}

			return System.Math.Max(1.0, System.Math.Pow(System.Math.Abs(value), 0.5)) * 0.029999999329447746;
		}

		public static float ClosestPointOnRay(Ray ray, Ray other)
		{
			// based on: https://math.stackexchange.com/questions/1036959/midpoint-of-the-shortest-distance-between-2-rays-in-3d
			// note: directions of both rays must be normalized
			// ray.origin -> a
			// ray.direction -> b
			// other.origin -> c
			// other.direction -> d

			float bd = Vector3.Dot(ray.direction, other.direction);
			float cd = Vector3.Dot(other.origin, other.direction);
			float ad = Vector3.Dot(ray.origin, other.direction);
			float bc = Vector3.Dot(ray.direction, other.origin);
			float ab = Vector3.Dot(ray.origin, ray.direction);

			float bottom = bd * bd - 1f;
			if (Mathf.Abs(bottom) < PRECISION_THRESHOLD)
			{
				return 0;
			}

			float top = ab - bc + bd * (cd - ad);
			return top / bottom;
		}

		/// <summary>
		/// 등속 이동일 때
		/// </summary>
		/// <param name="speed"></param>
		/// <param name="deltaReduce"></param>
		/// <param name="time"></param>
		/// <returns></returns>
		public static float GetDistanceAtTime(float speed, float time)
		{
			return speed * time;
		}

		/// <summary>
		/// 등감속 이동일 때
		/// </summary>
		/// <param name="speed"></param>
		/// <param name="deltaReduce"></param>
		/// <param name="time"></param>
		/// <returns></returns>
		public static float GetDistanceAtTime(float speed, float deltaReduce, float time)
		{
			float fixedDeltaTime = Time.fixedDeltaTime; //0.02f;  // Assuming fixedDeltaTime as 0.02
			int nTerms = Mathf.Min((int)(speed / deltaReduce) + 1, (int)(time / fixedDeltaTime));

			// Calculate the distance using the arithmetic sum formula
			float distanceCovered = 0.5f * nTerms * (2 * speed - (nTerms - 1) * deltaReduce) * fixedDeltaTime;

			return distanceCovered;
		}

		public static float GetYAxisAngle(Vector3 direction)
		{
			float angle = Mathf.Atan2(direction.x, direction.z) * Mathf.Rad2Deg;
			if (angle < 0)
				angle += 360;

			return angle;
		}

		public static Vector3 AngleToVector3(float angleInDegrees)
		{
			float angleInRadians = angleInDegrees * Mathf.Deg2Rad;
			float x = Mathf.Sin(angleInRadians);
			float z = Mathf.Cos(angleInRadians);

			return new Vector3(x, 0, z);
		}

		public static Vector3[] GetCapsulePoints(Vector3 center, float height, float radius, Quaternion rotation)
		{
			Vector3[] points = new Vector3[2];

			// Y-Axis 캡슐
			Vector3 direction = rotation * Vector3.up;

			float halfHeightMinusRadius = (height * 0.5f) - radius;

			points[0] = center + direction * halfHeightMinusRadius;
			points[1] = center - direction * halfHeightMinusRadius;

			return points;
		}
	}
}
