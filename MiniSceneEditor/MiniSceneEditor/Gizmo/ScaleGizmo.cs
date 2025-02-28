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

public class ScaleGizmo : BaseGizmo
{
	private readonly Vector3[] _axisDirections;
	private readonly Color[] _axisColors;
	private readonly CommandManager commandManager;
	private readonly SnapSystem snapSystem;

	public ScaleGizmo(GraphicsDevice graphicsDevice, CommandManager commandManager, SnapSystem snapSystem)
		: base(graphicsDevice, commandManager, snapSystem)
	{
		_axisDirections = new[]
		{
			Vector3.Right,
			Vector3.Up,
			Vector3.Forward
		};

		_axisColors = new[]
		{
			Color.Red,
			Color.Green,
			Color.Blue
		};

		CreateGeometry();
		this.commandManager = commandManager;
		this.snapSystem = snapSystem;
	}

	private void CreateGeometry()
	{
		var vertices = new List<VertexPositionColor>();

		// Створюємо лінії та куби для кожної осі
		foreach (var direction in _axisDirections)
		{
			int index = Array.IndexOf(_axisDirections, direction);
			CreateAxisGeometry(vertices, direction, _axisColors[index]);
		}

		VertexBuffer = new VertexBuffer(
			GraphicsDevice,
			typeof(VertexPositionColor),
			vertices.Count,
			BufferUsage.WriteOnly
		);
		VertexBuffer.SetData(vertices.ToArray());
	}

	private void CreateAxisGeometry(List<VertexPositionColor> vertices, Vector3 direction, Color color)
	{
		// Лінія осі
		vertices.Add(new VertexPositionColor(Vector3.Zero, color));
		vertices.Add(new VertexPositionColor(direction * GIZMO_SIZE, color));

		// Куб на кінці
		float cubeSize = 0.1f;
		Vector3 cubeCenter = direction * GIZMO_SIZE;
		CreateCube(vertices, cubeCenter, cubeSize, color);
	}

	private void CreateCube(List<VertexPositionColor> vertices, Vector3 center, float size, Color color)
	{
		Vector3 min = center - Vector3.One * size * 0.5f;
		Vector3 max = center + Vector3.One * size * 0.5f;

		// Front face
		vertices.Add(new VertexPositionColor(new Vector3(min.X, min.Y, max.Z), color));
		vertices.Add(new VertexPositionColor(new Vector3(max.X, min.Y, max.Z), color));
		vertices.Add(new VertexPositionColor(new Vector3(max.X, min.Y, max.Z), color));
		vertices.Add(new VertexPositionColor(new Vector3(max.X, max.Y, max.Z), color));
		vertices.Add(new VertexPositionColor(new Vector3(max.X, max.Y, max.Z), color));
		vertices.Add(new VertexPositionColor(new Vector3(min.X, max.Y, max.Z), color));
		vertices.Add(new VertexPositionColor(new Vector3(min.X, max.Y, max.Z), color));
		vertices.Add(new VertexPositionColor(new Vector3(min.X, min.Y, max.Z), color));

		// Back face
		vertices.Add(new VertexPositionColor(new Vector3(min.X, min.Y, min.Z), color));
		vertices.Add(new VertexPositionColor(new Vector3(max.X, min.Y, min.Z), color));
		vertices.Add(new VertexPositionColor(new Vector3(max.X, min.Y, min.Z), color));
		vertices.Add(new VertexPositionColor(new Vector3(max.X, max.Y, min.Z), color));
		vertices.Add(new VertexPositionColor(new Vector3(max.X, max.Y, min.Z), color));
		vertices.Add(new VertexPositionColor(new Vector3(min.X, max.Y, min.Z), color));
		vertices.Add(new VertexPositionColor(new Vector3(min.X, max.Y, min.Z), color));
		vertices.Add(new VertexPositionColor(new Vector3(min.X, min.Y, min.Z), color));

		// Connecting lines
		vertices.Add(new VertexPositionColor(new Vector3(min.X, min.Y, min.Z), color));
		vertices.Add(new VertexPositionColor(new Vector3(min.X, min.Y, max.Z), color));
		vertices.Add(new VertexPositionColor(new Vector3(max.X, min.Y, min.Z), color));
		vertices.Add(new VertexPositionColor(new Vector3(max.X, min.Y, max.Z), color));
		vertices.Add(new VertexPositionColor(new Vector3(max.X, max.Y, min.Z), color));
		vertices.Add(new VertexPositionColor(new Vector3(max.X, max.Y, max.Z), color));
		vertices.Add(new VertexPositionColor(new Vector3(min.X, max.Y, min.Z), color));
		vertices.Add(new VertexPositionColor(new Vector3(min.X, max.Y, max.Z), color));
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
		Vector2 mousePosition = new Vector2(input.CurrentMouse.X, input.CurrentMouse.Y);
		Vector3 worldPosition = Matrix.Invert(camera.ViewMatrix).Translation;

		if (input.IsMouseButtonPressed(ButtonState.Pressed))
		{
			for (int i = 0; i < 3; i++)
			{
				if (IsAxisHovered(mousePosition, camera, worldPosition, _axisDirections[i], out float distance))
				{
					ActiveAxis = i;
					DragStart = mousePosition.ToVector3();
					return true;
				}
			}
		}
		else if (input.CurrentMouse.LeftButton == ButtonState.Released && ActiveAxis != -1)
		{
			ActiveAxis = -1;
		}

		if (ActiveAxis != -1)
		{
			Vector2 currentPos = new Vector2(input.CurrentMouse.X, input.CurrentMouse.Y);
			Vector3 delta = (currentPos.ToVector3() - DragStart);

			// Застосовуємо масштабування
			float scaleAmount = 1.0f + delta.X * 0.01f;
			Vector3 scaleAxis = _axisDirections[ActiveAxis];

			// TODO: Застосувати масштабування до трансформації

			DragStart = currentPos.ToVector3();
			return true;
		}

		return false;
	}

	protected override TransformCommand.TransformationType GetTransformationType()
	{
		return TransformCommand.TransformationType.Scale;
	}
}