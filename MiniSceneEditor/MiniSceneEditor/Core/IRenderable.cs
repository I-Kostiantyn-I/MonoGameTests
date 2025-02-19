using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace MiniSceneEditor.Core
{
	public interface IRenderable
	{
		void SetGraphicsDevice(GraphicsDevice graphicsDevice);
		void Render(Matrix view, Matrix projection);
	}
}
