namespace MiniSceneEditor.Core.Utils;

public static class VectorExtensions
{
	public static Microsoft.Xna.Framework.Vector3 ToXNA(this System.Numerics.Vector3 vector)
	{
		return new Microsoft.Xna.Framework.Vector3(vector.X, vector.Y, vector.Z);
	}
}