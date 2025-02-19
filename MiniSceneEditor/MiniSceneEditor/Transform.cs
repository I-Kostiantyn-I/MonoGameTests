using Microsoft.Xna.Framework;

public struct Transform
{
	public Vector3 Position;
	public Vector3 Rotation; // в радіанах
	public Vector3 Scale;

	public Transform(Vector3 position, Vector3 rotation, Vector3 scale)
	{
		Position = position;
		Rotation = rotation;
		Scale = scale;
	}

	public static Transform Identity => new Transform(
		Vector3.Zero,
		Vector3.Zero,
		Vector3.One);

	public Matrix GetWorldMatrix()
	{
		return Matrix.CreateScale(Scale) *
			   Matrix.CreateRotationX(Rotation.X) *
			   Matrix.CreateRotationY(Rotation.Y) *
			   Matrix.CreateRotationZ(Rotation.Z) *
			   Matrix.CreateTranslation(Position);
	}

	public static Transform Lerp(Transform start, Transform end, float amount)
	{
		return new Transform(
			Vector3.Lerp(start.Position, end.Position, amount),
			Vector3.Lerp(start.Rotation, end.Rotation, amount),
			Vector3.Lerp(start.Scale, end.Scale, amount));
	}
}