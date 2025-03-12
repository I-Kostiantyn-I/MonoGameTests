using Microsoft.Xna.Framework;

namespace MiniSceneEditor.Camera;

public readonly struct CameraMatricesState
{
	public Matrix ViewMatrix { get; }
	public Matrix ProjectionMatrix { get; }

	public CameraMatricesState(Matrix view, Matrix projection)
	{
		ViewMatrix = view;
		ProjectionMatrix = projection;
	}
}
