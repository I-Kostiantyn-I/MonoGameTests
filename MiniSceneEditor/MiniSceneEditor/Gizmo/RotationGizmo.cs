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
	private readonly Vector3[] _axisDirections = new[]
	{
		Vector3.Right,   // X axis - Червоний
        Vector3.Up,      // Y axis - Зелений
        Vector3.Forward  // Z axis - Синій
    };

	private readonly Color[] _axisColors = new[]
	{
		Color.Red,    // X axis
        Color.Green,  // Y axis
        Color.Blue    // Z axis
    };

	private float _radius = 1.0f;
	private float _sphereRadius = 0.05f; // Розмір кульки-індикатора
	private int _hoveredAxis = -1;
	private const int CIRCLE_SEGMENTS = 32;
	private const int SPHERE_SEGMENTS = 8;


	private readonly EditorLogger _log;

	public RotationGizmo(GraphicsDevice graphicsDevice, CommandManager commandManager, SnapSystem snapSystem)
		: base(graphicsDevice, commandManager, snapSystem)
	{
		_log = new EditorLogger(nameof(RotationGizmo));
	}

	public override void Draw(BasicEffect effect, TransformComponent transform)
	{
		for (int i = 0; i < 3; i++)
		{
			Color color = _axisColors[i];
			if (i == _hoveredAxis || i == ActiveAxis)
			{
				color = Color.White; // Підсвічування при наведенні/активації
			}

			DrawRotationRing(effect, transform.Position, i, color);

			// Малюємо кульку-індикатор на поточному куті обертання
			float angle = transform.Rotation.GetAxis(i);
			DrawAngleIndicator(effect, transform.Position, i, angle, color);
		}
	}

	private void DrawRotationRing(BasicEffect effect, Vector3 position, int axisIndex, Color color)
	{
		var vertices = new List<VertexPositionColor>();
		Vector3 axis = _axisDirections[axisIndex];

		for (int i = 0; i <= CIRCLE_SEGMENTS; i++)
		{
			float angle = i * MathHelper.TwoPi / CIRCLE_SEGMENTS;
			Vector3 point = GetCirclePoint(position, axis, angle);
			vertices.Add(new VertexPositionColor(point, color));
		}

		foreach (var pass in effect.CurrentTechnique.Passes)
		{
			pass.Apply();
			GraphicsDevice.DrawUserPrimitives(
				PrimitiveType.LineStrip,
				vertices.ToArray(),
				0,
				vertices.Count - 1);
		}
	}

	private void DrawAngleIndicator(BasicEffect effect, Vector3 center, int axisIndex, float angle, Color color)
	{
		Vector3 axis = _axisDirections[axisIndex];
		Vector3 indicatorPos = GetCirclePoint(center, axis, angle);

		// Створюємо вершини для кульки
		var vertices = new List<VertexPositionColor>();

		// Спрощена версія кульки (октаедр)
		Vector3[] directions = new[]
		{
			Vector3.Up, Vector3.Down,
			Vector3.Left, Vector3.Right,
			Vector3.Forward, Vector3.Backward
		};

		foreach (var dir in directions)
		{
			vertices.Add(new VertexPositionColor(
				indicatorPos + dir * _sphereRadius,
				color));
		}

		// Індекси для октаедра
		int[] indices = new[]
		{
			0,2,3, 0,3,4, 0,4,2,
			1,2,3, 1,3,4, 1,4,2
		};

		foreach (var pass in effect.CurrentTechnique.Passes)
		{
			pass.Apply();
			for (int i = 0; i < indices.Length; i += 3)
			{
				GraphicsDevice.DrawUserPrimitives(
					PrimitiveType.TriangleList,
					new[]
					{
						vertices[indices[i]],
						vertices[indices[i + 1]],
						vertices[indices[i + 2]]
					},
					0,
					1);
			}
		}
	}

	private Vector3 GetCirclePoint(Vector3 center, Vector3 axis, float angle)
	{
		Vector3 u = Vector3.Cross(axis, Vector3.Up);
		if (u.Length() < 0.001f)
			u = Vector3.Cross(axis, Vector3.Right);
		u.Normalize();
		Vector3 v = Vector3.Cross(axis, u);

		return center + _radius * (u * (float)Math.Cos(angle) + v * (float)Math.Sin(angle));
	}


	//private void DrawRotationRing(BasicEffect effect, Vector3 position, int axisIndex)
	//{
	//	Color color = _axisColors[axisIndex];
	//	if (axisIndex == _hoveredAxis || axisIndex == ActiveAxis)
	//	{
	//		color = Color.White;
	//	}

	//	var vertices = new List<VertexPositionColor>();
	//	Vector3 axis = _axisDirections[axisIndex];

	//	// Створюємо кільце
	//	for (int i = 0; i <= CIRCLE_SEGMENTS; i++)
	//	{
	//		float angle = i * MathHelper.TwoPi / CIRCLE_SEGMENTS;
	//		Vector3 point = GetCirclePoint(position, axis, angle);
	//		vertices.Add(new VertexPositionColor(point, color));
	//	}

	//	foreach (var pass in effect.CurrentTechnique.Passes)
	//	{
	//		pass.Apply();
	//		GraphicsDevice.DrawUserPrimitives(
	//			PrimitiveType.LineStrip,
	//			vertices.ToArray(),
	//			0,
	//			vertices.Count - 1);
	//	}
	//}

	//private Vector3 GetCirclePoint(Vector3 center, Vector3 axis, float angle)
	//{
	//	Vector3 u = Vector3.Cross(axis, Vector3.Up);
	//	if (u.Length() < 0.001f)
	//		u = Vector3.Cross(axis, Vector3.Right);
	//	u.Normalize();
	//	Vector3 v = Vector3.Cross(axis, u);

	//	return center + _radius * (u * (float)Math.Cos(angle) + v * (float)Math.Sin(angle));
	//}

	public override bool HandleInput(InputState input, CameraMatricesState camera, TransformComponent transform)
	{
		Ray ray = CreatePickingRay(input.MousePosition, camera);

		if (!IsDragging)
		{
			_hoveredAxis = -1;
			float nearestDistance = float.MaxValue;

			for (int i = 0; i < 3; i++)
			{
				if (CheckRingIntersection(ray, transform.Position, _axisDirections[i], out float distance))
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
			BeginDrag(transform, TransformCommand.TransformationType.Rotation);
			DragStart = input.MousePosition.ToVector3();
			return true;
		}

		if (IsDragging && input.IsMouseButtonDown(ButtonState.Pressed))
		{
			Vector2 currentPos = input.MousePosition;
			Vector2 delta = currentPos - new Vector2(DragStart.X, DragStart.Y);

			// Конвертуємо рух миші в кут обертання
			float rotationAmount = delta.X * 0.01f;
			Vector3 rotationAxis = _axisDirections[ActiveAxis];

			// Оновлюємо обертання
			transform.Rotation += rotationAxis * rotationAmount;

			DragStart = currentPos.ToVector3();
			return true;
		}

		if (input.IsMouseButtonReleased(ButtonState.Released))
		{
			if (IsDragging)
			{
				EndDrag(transform, TransformCommand.TransformationType.Rotation);
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

	private bool CheckRingIntersection(Ray ray, Vector3 center, Vector3 axis, out float distance)
	{
		// Спрощена перевірка перетину з кільцем
		float thickness = 0.1f;
		BoundingSphere ring = new BoundingSphere(center, _radius + thickness);

		float? intersection = ray.Intersects(ring);
		if (intersection.HasValue)
		{
			Vector3 hitPoint = ray.Position + ray.Direction * intersection.Value;
			Vector3 toHit = hitPoint - center;

			// Перевіряємо, чи точка перетину близька до кільця
			float distanceFromAxis = Vector3.Dot(toHit, axis);
			if (Math.Abs(distanceFromAxis) < thickness)
			{
				distance = intersection.Value;
				return true;
			}
		}

		distance = float.MaxValue;
		return false;
	}

	protected override TransformCommand.TransformationType GetTransformationType()
	{
		return TransformCommand.TransformationType.Rotation;
	}
}

/*
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
}*/