using Microsoft.Xna.Framework;
using System;

namespace MiniSceneEditor.Core.Utils;

public static class VectorExtensions
{
	public static Vector3 ToXNA(this System.Numerics.Vector3 vector)
	{
		return new Vector3(vector.X, vector.Y, vector.Z);
	}

	public static Quaternion ToXNA(this System.Numerics.Quaternion quaternion)
	{
		return new Quaternion(quaternion.X, quaternion.Y, quaternion.Z, quaternion.W);
	}

	public static Vector3 Multiply(this Vector3 vector, float value)
	{
		return new Vector3(
			vector.X * value,
			vector.Y * value,
			vector.Z * value
		);
	}

	public static Vector3 Multiply(this float value, Vector3 vector)
	{
		return new Vector3(
			vector.X * value,
			vector.Y * value,
			vector.Z * value
		);
	}

	public static Vector3 Divide(this Vector3 vector, float value)
	{
		return new Vector3(
			vector.X / value,
			vector.Y / value,
			vector.Z / value
		);
	}

	public static Vector3 Round(this Vector3 vector, int decimals)
	{
		return new Vector3(
			MathF.Round(vector.X, decimals),
			MathF.Round(vector.Y, decimals),
			MathF.Round(vector.Z, decimals)
		);
	}

	public static float GetAxis(this Vector3 vector, int axisIndex)
	{
		return axisIndex switch
		{
			0 => vector.X,
			1 => vector.Y,
			2 => vector.Z,
			_ => throw new ArgumentOutOfRangeException(nameof(axisIndex))
		};
	}

	public static Vector3 WithAxis(this Vector3 vector, int axisIndex, float value)
	{
		return axisIndex switch
		{
			0 => new Vector3(value, vector.Y, vector.Z),
			1 => new Vector3(vector.X, value, vector.Z),
			2 => new Vector3(vector.X, vector.Y, value),
			_ => throw new ArgumentOutOfRangeException(nameof(axisIndex))
		};
	}

	public static Vector3 ScaleAxis(this Vector3 vector, int axisIndex, float scale)
	{
		return vector.WithAxis(axisIndex, vector.GetAxis(axisIndex) * scale);
	}
}