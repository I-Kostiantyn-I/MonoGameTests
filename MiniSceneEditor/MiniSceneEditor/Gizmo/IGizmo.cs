using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MiniSceneEditor.Camera;
using MiniSceneEditor.Core.Components.Impls;

namespace MiniSceneEditor.Gizmo;

public interface IGizmo
{
	void Draw(BasicEffect effect, TransformComponent transform);
	bool HandleInput(InputState input, CameraMatricesState camera, TransformComponent transform);
	void SetColor(Color baseColor, Color hoverColor, Color activeColor);
}