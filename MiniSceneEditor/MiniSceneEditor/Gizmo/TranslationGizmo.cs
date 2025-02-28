using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using MiniSceneEditor.Camera;
using MiniSceneEditor.Commands;
using MiniSceneEditor.Core.Components.Impls;
using MiniSceneEditor.Core.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MiniSceneEditor.Gizmo;

public class TranslationGizmo : BaseGizmo
{
	private readonly Vector3[] _axisDirections;
	private readonly Color[] _axisColors;

	public TranslationGizmo(GraphicsDevice graphicsDevice, CommandManager commandManager, SnapSystem snapSystem)
		: base(graphicsDevice, commandManager, snapSystem)
	{
		_axisDirections = new[]
		{
			Vector3.Right,  // X axis
            Vector3.Up,     // Y axis
            Vector3.Forward // Z axis
        };

		_axisColors = new[]
		{
			Color.Red,    // X axis
            Color.Green,  // Y axis
            Color.Blue    // Z axis
        };

		CreateGeometry();
	}

	protected override TransformCommand.TransformationType GetTransformationType()
	{
		return TransformCommand.TransformationType.Position;
	}

	private void CreateGeometry()
	{
		var vertices = new List<VertexPositionColor>();

		// Створюємо стрілки для кожної осі
		foreach (var direction in _axisDirections)
		{
			int index = Array.IndexOf(_axisDirections, direction);
			CreateArrow(vertices, direction, _axisColors[index]);
		}

		VertexBuffer = new VertexBuffer(
			GraphicsDevice,
			typeof(VertexPositionColor),
			vertices.Count,
			BufferUsage.WriteOnly
		);
		VertexBuffer.SetData(vertices.ToArray());
	}

	private void CreateArrow(List<VertexPositionColor> vertices, Vector3 direction, Color color)
	{
		// Лінія осі
		vertices.Add(new VertexPositionColor(Vector3.Zero, color));
		vertices.Add(new VertexPositionColor(direction * GIZMO_SIZE, color));

		// Наконечник стрілки
		float arrowSize = 0.1f;
		Vector3 tip = direction * GIZMO_SIZE;
		Vector3 right = Vector3.Cross(direction, Vector3.Up);
		if (right.Length() < 0.001f)
			right = Vector3.Cross(direction, Vector3.Right);
		right.Normalize();
		Vector3 up = Vector3.Cross(direction, right);

		vertices.Add(new VertexPositionColor(tip, color));
		vertices.Add(new VertexPositionColor(tip - direction * arrowSize + right * arrowSize, color));
		vertices.Add(new VertexPositionColor(tip, color));
		vertices.Add(new VertexPositionColor(tip - direction * arrowSize - right * arrowSize, color));
		vertices.Add(new VertexPositionColor(tip, color));
		vertices.Add(new VertexPositionColor(tip - direction * arrowSize + up * arrowSize, color));
		vertices.Add(new VertexPositionColor(tip, color));
		vertices.Add(new VertexPositionColor(tip - direction * arrowSize - up * arrowSize, color));
	}

	public override void Draw(BasicEffect effect, TransformComponent transform)
	{
		effect.World = Matrix.CreateTranslation(transform.Position);
		effect.VertexColorEnabled = true;

		foreach (var pass in effect.CurrentTechnique.Passes)
		{
			pass.Apply();
			GraphicsDevice.SetVertexBuffer(VertexBuffer);
			GraphicsDevice.DrawPrimitives(
				PrimitiveType.LineList,
				0,
				VertexBuffer.VertexCount / 2
			);
		}
	}

	public override bool HandleInput(InputState input, CameraMatricesState camera, TransformComponent transform)
	{
		Vector2 mousePosition = input.MousePosition;
		Vector3 worldPosition = transform.Position; // Використовуємо позицію з переданого transform

		if (input.IsMouseButtonPressed(ButtonState.Pressed))
		{
			for (int i = 0; i < 3; i++)
			{
				if (IsAxisHovered(mousePosition, camera, worldPosition, _axisDirections[i], out float distance))
				{
					ActiveAxis = i;
					DragStart = mousePosition.ToVector3();
					BeginTransform(transform);
					return true;
				}
			}
		}
		else if (input.CurrentMouse.LeftButton == ButtonState.Released && ActiveAxis != -1)
		{
			EndTransform(transform);
			ActiveAxis = -1;
		}

		if (ActiveAxis != -1)
		{
			Vector2 currentPos = input.MousePosition;
			Vector3 delta = (currentPos.ToVector3() - DragStart);

			Vector3 moveAxis = _axisDirections[ActiveAxis];
			float moveAmount = delta.X * 0.01f;

			transform.Position += moveAxis * moveAmount;

			DragStart = currentPos.ToVector3();
			return true;
		}

		return false;
	}
}