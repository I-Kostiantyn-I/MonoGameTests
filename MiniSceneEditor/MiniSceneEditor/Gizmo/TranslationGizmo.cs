using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using MiniSceneEditor.Camera;
using MiniSceneEditor.Commands;
using MiniSceneEditor.Core.Components.Impls;
using MiniSceneEditor.Core.Utils;
using System;
using System.Collections.Generic;

namespace MiniSceneEditor.Gizmo;

public class TranslationGizmo : BaseGizmo
{
	private float _axisLength = 1.0f;
	private float _arrowSize = 0.1f;
	private int _hoveredAxis = -1;

	private readonly EditorLogger _log;

	public TranslationGizmo(GraphicsDevice graphicsDevice, CommandManager commandManager, SnapSystem snapSystem)
		: base(graphicsDevice, commandManager, snapSystem)
	{
		_log = new EditorLogger(nameof(TranslationGizmo), false);

		CreateGeometry();
	}

	public override void Draw(BasicEffect effect, TransformComponent transform)
	{
		for (int i = 0; i < 3; i++)
		{
			DrawAxis(effect, transform.Position, i);
		}
	}

	public override bool HandleInput(InputState input, CameraMatricesState camera, TransformComponent transform)
	{
		Ray ray = CreatePickingRay(input.MousePosition, camera);

		// Оновлюємо наведення, якщо не перетягуємо
		if (!IsDragging)
		{
			_hoveredAxis = -1;
			float nearestDistance = float.MaxValue;

			for (int i = 0; i < 3; i++)
			{
				Vector3 arrowPosition = transform.Position + AxisDirections[i] * _axisLength;
				if (CheckArrowIntersection(ray, arrowPosition, AxisDirections[i], out float distance))
				{
					if (distance < nearestDistance)
					{
						nearestDistance = distance;
						_hoveredAxis = i;
					}
				}
			}
		}

		// Початок перетягування
		if (input.IsMouseButtonPressed(ButtonState.Pressed) && _hoveredAxis != -1)
		{
			ActiveAxis = _hoveredAxis;
			BeginDrag(transform, TransformCommand.TransformationType.Position);
			DragStart = input.MousePosition.ToVector3();
			_log.Log("Started dragging");
			return true;
		}

		// Перетягування
		if (IsDragging && input.IsMouseButtonDown(ButtonState.Pressed))
		{
			Vector2 currentPos = input.MousePosition;
			Vector2 delta = currentPos - new Vector2(DragStart.X, DragStart.Y);

			// Отримуємо напрямок переміщення в світових координатах
			Vector3 moveAxis = AxisDirections[ActiveAxis];

			// Проектуємо вектор руху на площину екрану
			Vector3 axisStart = transform.Position;
			Vector3 axisEnd = transform.Position + moveAxis;

			// Перетворюємо точки осі в екранні координати
			Vector2 screenStart = WorldToScreen(axisStart, camera);
			Vector2 screenEnd = WorldToScreen(axisEnd, camera);

			// Отримуємо напрямок осі на екрані
			Vector2 screenAxis = Vector2.Normalize(screenEnd - screenStart);

			// Проектуємо рух миші на напрямок осі
			float movement = Vector2.Dot(delta, screenAxis);

			// Застосовуємо переміщення
			transform.Position += moveAxis * movement * 0.01f;

			DragStart = currentPos.ToVector3();
			_log.Log($"Dragging: screen delta={delta}, movement={movement}, new position={transform.Position}");
			return true;
		}

		// Кінець перетягування
		if (input.IsMouseButtonReleased(ButtonState.Released))
		{
			if (IsDragging)
			{
				EndDrag(transform, TransformCommand.TransformationType.Position);
				ActiveAxis = -1;
				_log.Log("Ended dragging");
			}
			IsDragging = false;
		}

		return false;
	}

	protected override TransformCommand.TransformationType GetTransformationType()
	{
		return TransformCommand.TransformationType.Position;
	}

	private void CreateGeometry()
	{
		var vertices = new List<VertexPositionColor>();

		// Створюємо стрілки для кожної осі
		foreach (var direction in AxisDirections)
		{
			int index = Array.IndexOf(AxisDirections, direction);
			CreateArrow(vertices, direction, AxisColors[index]);
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

	private Vector2 WorldToScreen(Vector3 worldPosition, CameraMatricesState camera)
	{
		var viewport = GraphicsDevice.Viewport;
		Vector3 screenPos = viewport.Project(
			worldPosition,
			camera.ProjectionMatrix,
			camera.ViewMatrix,
			Matrix.Identity);
		return new Vector2(screenPos.X, screenPos.Y);
	}

	private bool CheckArrowIntersection(Ray ray, Vector3 arrowPosition, Vector3 direction, out float distance)
	{
		// Збільшимо радіус перевірки для легшого вибору
		float radius = _arrowSize;
		BoundingSphere arrowSphere = new BoundingSphere(arrowPosition, radius);

		float? intersection = ray.Intersects(arrowSphere);
		if (intersection.HasValue)
		{
			distance = intersection.Value;
			_log.Log($"Arrow intersection at position {arrowPosition}, distance {distance}");
			return true;
		}

		distance = float.MaxValue;
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
		_log.Log($"Created picking ray: origin={nearPoint}, direction={direction}");
		return new Ray(nearPoint, direction);
	}

	private void DrawAxis(BasicEffect effect, Vector3 position, int axisIndex)
	{
		// Встановлюємо колір осі
		Color color = AxisColors[axisIndex];
		if (axisIndex == _hoveredAxis || axisIndex == ActiveAxis)
		{
			// Робимо вісь яскравішою при наведенні або активації
			color = Color.Lerp(color, Color.White, 0.5f);
		}

		effect.DiffuseColor = color.ToVector3();

		// Малюємо лінію осі
		var lineVertices = new[]
		{
			new VertexPositionColor(position, color),
			new VertexPositionColor(position + AxisDirections[axisIndex] * _axisLength, color)
		};

		foreach (var pass in effect.CurrentTechnique.Passes)
		{
			pass.Apply();
			GraphicsDevice.DrawUserPrimitives(PrimitiveType.LineList, lineVertices, 0, 1);
		}

		// Малюємо стрілку на кінці
		Vector3 arrowTip = position + AxisDirections[axisIndex] * _axisLength;
		DrawArrow(effect, arrowTip, AxisDirections[axisIndex], color);
	}

	private void DrawArrow(BasicEffect effect, Vector3 position, Vector3 direction, Color color)
	{
		// Створюємо трикутник для стрілки
		Vector3 right = Vector3.Cross(direction, Vector3.Up);
		if (right.Length() < 0.001f)
		{
			right = Vector3.Cross(direction, Vector3.Right);
		}
		right.Normalize();
		Vector3 up = Vector3.Cross(right, direction);

		Vector3 tip = position + direction * _arrowSize;
		Vector3 baseRight = position + right * _arrowSize * 0.5f;
		Vector3 baseLeft = position - right * _arrowSize * 0.5f;

		var arrowVertices = new[]
		{
			new VertexPositionColor(tip, color),
			new VertexPositionColor(baseRight, color),
			new VertexPositionColor(baseLeft, color)
		};

		foreach (var pass in effect.CurrentTechnique.Passes)
		{
			pass.Apply();
			GraphicsDevice.DrawUserPrimitives(PrimitiveType.TriangleList, arrowVertices, 0, 1);
		}
	}
}