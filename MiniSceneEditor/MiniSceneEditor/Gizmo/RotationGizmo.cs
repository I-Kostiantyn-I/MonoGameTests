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

public class RotationGizmo : BaseGizmo
{
	private const int CIRCLE_SEGMENTS = 32;
	private readonly Color[] _axisColors = new[]
	{
		Color.Red,    // X axis
        Color.Green,  // Y axis
        Color.Blue    // Z axis
    };
	private readonly CommandManager commandManager;
	private readonly SnapSystem snapSystem;

	public RotationGizmo(GraphicsDevice graphicsDevice, CommandManager commandManager, SnapSystem snapSystem)
		: base(graphicsDevice, commandManager, snapSystem)
	{
		CreateGeometry();
		this.commandManager = commandManager;
		this.snapSystem = snapSystem;
	}

	private void CreateGeometry()
	{
		var vertices = new List<VertexPositionColor>();

		// Створюємо кола для кожної осі обертання
		CreateCircle(vertices, Vector3.Right, _axisColors[0]);  // X
		CreateCircle(vertices, Vector3.Up, _axisColors[1]);     // Y
		CreateCircle(vertices, Vector3.Forward, _axisColors[2]); // Z

		VertexBuffer = new VertexBuffer(
			GraphicsDevice,
			typeof(VertexPositionColor),
			vertices.Count,
			BufferUsage.WriteOnly
		);
		VertexBuffer.SetData(vertices.ToArray());
	}

	private void CreateCircle(List<VertexPositionColor> vertices, Vector3 normal, Color color)
	{
		Vector3 tangent = Vector3.Cross(normal, normal == Vector3.Up ? Vector3.Forward : Vector3.Up);
		Vector3 bitangent = Vector3.Cross(normal, tangent);

		for (int i = 0; i < CIRCLE_SEGMENTS; i++)
		{
			float angle1 = MathHelper.TwoPi * i / CIRCLE_SEGMENTS;
			float angle2 = MathHelper.TwoPi * (i + 1) / CIRCLE_SEGMENTS;

			Vector3 point1 = GIZMO_SIZE * (tangent * (float)Math.Cos(angle1) + bitangent * (float)Math.Sin(angle1));
			Vector3 point2 = GIZMO_SIZE * (tangent * (float)Math.Cos(angle2) + bitangent * (float)Math.Sin(angle2));

			vertices.Add(new VertexPositionColor(point1, color));
			vertices.Add(new VertexPositionColor(point2, color));
		}
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
			// Перевіряємо, чи клікнули на якусь вісь
			for (int i = 0; i < 3; i++)
			{
				if (IsAxisHovered(mousePosition, camera, worldPosition, GetAxisVector(i), out float distance))
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

			// Застосовуємо обертання
			float rotationAmount = delta.X * 0.01f; // Можна налаштувати чутливість
			Vector3 rotationAxis = GetAxisVector(ActiveAxis);

			// TODO: Застосувати обертання до трансформації

			DragStart = currentPos.ToVector3();
			return true;
		}

		return false;
	}

	private Vector3 GetAxisVector(int axis)
	{
		return axis switch
		{
			0 => Vector3.Right,
			1 => Vector3.Up,
			2 => Vector3.Forward,
			_ => Vector3.Zero
		};
	}

	protected override TransformCommand.TransformationType GetTransformationType()
	{
		return TransformCommand.TransformationType.Rotation;
	}
}