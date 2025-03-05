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
	private readonly Vector3[] _axisDirections = new[]
	{
		Vector3.Right,   // X axis
        Vector3.Up,      // Y axis
        Vector3.Forward  // Z axis
    };

	private readonly Color[] _axisColors = new[]
	{
		Color.Red,    // X axis
        Color.Green,  // Y axis
        Color.Blue    // Z axis
    };

	private float _axisLength = 1.0f;
	private float _boxSize = 0.1f;
	private int _hoveredAxis = -1;

	private readonly EditorLogger _log;

	public ScaleGizmo(GraphicsDevice graphicsDevice, CommandManager commandManager, SnapSystem snapSystem)
	   : base(graphicsDevice, commandManager, snapSystem)
	{
		_log = new EditorLogger(nameof(ScaleGizmo));
	}

	public override void Draw(BasicEffect effect, TransformComponent transform)
	{
		for (int i = 0; i < 3; i++)
		{
			DrawScaleAxis(effect, transform.Position, i);
		}
	}

	private void DrawScaleAxis(BasicEffect effect, Vector3 position, int axisIndex)
	{
		Color color = _axisColors[axisIndex];
		if (axisIndex == _hoveredAxis || axisIndex == ActiveAxis)
		{
			color = Color.White;
		}

		Vector3 direction = _axisDirections[axisIndex];
		Vector3 endPoint = position + direction * _axisLength;

		// Малюємо лінію
		var lineVertices = new[]
		{
			new VertexPositionColor(position, color),
			new VertexPositionColor(endPoint, color)
		};

		// Малюємо куб на кінці
		DrawScaleBox(effect, endPoint, color);

		foreach (var pass in effect.CurrentTechnique.Passes)
		{
			pass.Apply();
			GraphicsDevice.DrawUserPrimitives(PrimitiveType.LineList, lineVertices, 0, 1);
		}
	}

	private void DrawScaleBox(BasicEffect effect, Vector3 position, Color color)
	{
		// Створюємо куб для маніпулятора масштабування
		var vertices = new List<VertexPositionColor>();
		float size = _boxSize / 2;

		// Передня грань
		vertices.Add(new VertexPositionColor(position + new Vector3(-size, -size, size), color));
		vertices.Add(new VertexPositionColor(position + new Vector3(size, -size, size), color));
		vertices.Add(new VertexPositionColor(position + new Vector3(size, size, size), color));
		vertices.Add(new VertexPositionColor(position + new Vector3(-size, size, size), color));
		vertices.Add(new VertexPositionColor(position + new Vector3(-size, -size, size), color));

		foreach (var pass in effect.CurrentTechnique.Passes)
		{
			pass.Apply();
			GraphicsDevice.DrawUserPrimitives(PrimitiveType.LineStrip, vertices.ToArray(), 0, vertices.Count - 1);
		}
	}

	public override bool HandleInput(InputState input, CameraMatricesState camera, TransformComponent transform)
	{
		Ray ray = CreatePickingRay(input.MousePosition, camera);

		if (!IsDragging)
		{
			_hoveredAxis = -1;
			float nearestDistance = float.MaxValue;

			for (int i = 0; i < 3; i++)
			{
				Vector3 boxPosition = transform.Position + _axisDirections[i] * _axisLength;
				if (CheckBoxIntersection(ray, boxPosition, out float distance))
				{
					if (distance < nearestDistance)
					{
						nearestDistance = distance;
						_hoveredAxis = i;
					}
				}
			}
		}

		if (input.IsMouseButtonPressed(ButtonState.Pressed) && _hoveredAxis != -1)
		{
			ActiveAxis = _hoveredAxis;
			BeginDrag(transform, TransformCommand.TransformationType.Scale);
			DragStart = input.MousePosition.ToVector3();
			return true;
		}

		if (IsDragging && input.IsMouseButtonDown(ButtonState.Pressed))
		{
			Vector2 currentPos = input.MousePosition;
			Vector2 delta = currentPos - new Vector2(DragStart.X, DragStart.Y);

			// Конвертуємо рух миші в зміну масштабу
			float scaleAmount = 1.0f + delta.X * 0.01f;
			Vector3 currentScale = transform.Scale;

			// Застосовуємо новий масштаб
			transform.Scale = currentScale.ScaleAxis(ActiveAxis, scaleAmount); ;

			DragStart = currentPos.ToVector3();
			return true;
		}

		if (input.IsMouseButtonReleased(ButtonState.Released))
		{
			if (IsDragging)
			{
				EndDrag(transform, TransformCommand.TransformationType.Scale);
				ActiveAxis = -1;
			}
			IsDragging = false;
		}

		return false;
	}

	private Ray CreatePickingRay(Vector2 mousePosition, CameraMatricesState camera)
	{
		var viewport = GraphicsDevice.Viewport;
		Vector3 nearPoint = viewport.Unproject(
			new Vector3(mousePosition, 0),
			camera.ProjectionMatrix,
			camera.ViewMatrix,
			Matrix.Identity);

		Vector3 farPoint = viewport.Unproject(
			new Vector3(mousePosition, 1),
			camera.ProjectionMatrix,
			camera.ViewMatrix,
			Matrix.Identity);

		Vector3 direction = Vector3.Normalize(farPoint - nearPoint);
		return new Ray(nearPoint, direction);
	}

	private bool CheckBoxIntersection(Ray ray, Vector3 boxCenter, out float distance)
	{
		BoundingBox box = new BoundingBox(
			boxCenter - new Vector3(_boxSize),
			boxCenter + new Vector3(_boxSize));

		float? intersection = ray.Intersects(box);
		if (intersection.HasValue)
		{
			distance = intersection.Value;
			return true;
		}

		distance = float.MaxValue;
		return false;
	}

	protected override TransformCommand.TransformationType GetTransformationType()
	{
		return TransformCommand.TransformationType.Scale;
	}
}


/*
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
*/