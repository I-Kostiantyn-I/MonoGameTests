using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using MiniSceneEditor.Camera;
using MiniSceneEditor.Commands;
using MiniSceneEditor.Core.Components.Impls;
using MiniSceneEditor.Core.Utils;
using System.Collections.Generic;
using System.Diagnostics;

namespace MiniSceneEditor.Gizmo;

public class ScaleGizmo : BaseGizmo
{
	private float _axisLength = 1.0f;
	private float _boxSize = 0.1f;
	private int _hoveredAxis = -1;

	private readonly EditorLogger _log;

	public ScaleGizmo(GraphicsDevice graphicsDevice, CommandManager commandManager, SnapSystem snapSystem)
	   : base(graphicsDevice, commandManager, snapSystem)
	{
		_log = new EditorLogger(nameof(ScaleGizmo), false);
	}

	public override void Draw(BasicEffect effect, TransformComponent transform)
	{
		_log.Log($"Drawing ScaleGizmo. HoveredAxis: {_hoveredAxis}, ActiveAxis: {ActiveAxis}");

		for (int i = 0; i < 3; i++)
		{
			Color axisColor = AxisColors[i];
			if (i == _hoveredAxis || i == ActiveAxis)
			{
				axisColor = Color.White;
				_log.Log($"Axis {i} is highlighted");
			}

			_log.Log($"Drawing axis {i} with color: {axisColor}");

			// Встановлюємо колір для BasicEffect
			effect.DiffuseColor = axisColor.ToVector3();
			effect.Alpha = axisColor.A / 255f;

			DrawScaleAxis(effect, transform.Position, i, axisColor);
		}
	}

	public override bool HandleInput(InputState input, CameraMatricesState camera, TransformComponent transform)
	{
		Ray ray = CreatePickingRay(input.MousePosition, camera);

		if (!IsDragging)
		{
			int previousHovered = _hoveredAxis;
			_hoveredAxis = -1;
			float nearestDistance = float.MaxValue;

			for (int i = 0; i < 3; i++)
			{
				Vector3 boxPosition = transform.Position + AxisDirections[i] * _axisLength;
				if (CheckBoxIntersection(ray, boxPosition, out float distance))
				{
					if (distance < nearestDistance)
					{
						nearestDistance = distance;
						_hoveredAxis = i;
					}
				}
			}

			if (_hoveredAxis != previousHovered)
			{
				_log.Log($"Hovered axis changed from {previousHovered} to {_hoveredAxis}");
			}
		}

		if (InputManager.Instance.IsMouseButtonPressed(ButtonState.Pressed) && _hoveredAxis != -1)
		{
			ActiveAxis = _hoveredAxis;
			BeginDrag(transform, TransformCommand.TransformationType.Scale);
			DragStart = input.MousePosition.ToVector3();
			return true;
		}

		Debug.WriteLine("Is Pressed left mouse " + InputManager.Instance.IsMouseButtonDown(ButtonState.Pressed) + " IsDragging " + IsDragging);

		if (IsDragging && InputManager.Instance.IsMouseButtonDown(ButtonState.Pressed))
		{
			Vector2 currentPos = input.MousePosition;
			Vector2 delta = currentPos - new Vector2(DragStart.X, DragStart.Y);

			// Проектуємо рух миші на екранну площину
			Vector3 axisStart = transform.Position;
			Vector3 axisEnd = transform.Position + AxisDirections[ActiveAxis];

			// Перетворюємо точки осі в екранні координати
			Vector2 screenStart = WorldToScreen(axisStart, camera);
			Vector2 screenEnd = WorldToScreen(axisEnd, camera);

			// Отримуємо напрямок осі на екрані
			Vector2 screenAxis = Vector2.Normalize(screenEnd - screenStart);

			// Проектуємо рух миші на напрямок осі
			float movement = Vector2.Dot(delta, screenAxis);

			// Застосовуємо масштабування
			float scaleAmount = 1.0f + movement * 0.01f;
			transform.Scale = transform.Scale.ScaleAxis(ActiveAxis, scaleAmount);

			DragStart = currentPos.ToVector3();
			return true;
		}

		if (InputManager.Instance.IsMouseButtonReleased(ButtonState.Released))
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

	protected override TransformCommand.TransformationType GetTransformationType()
	{
		return TransformCommand.TransformationType.Scale;
	}

	private void DrawScaleAxis(BasicEffect effect, Vector3 position, int axisIndex, Color color)
	{
		Vector3 direction = AxisDirections[axisIndex];
		Vector3 endPoint = position + direction * _axisLength;

		// Малюємо лінію
		var lineVertices = new[]
		{
			new VertexPositionColor(position, color),
			new VertexPositionColor(endPoint, color)
		};

		foreach (var pass in effect.CurrentTechnique.Passes)
		{
			pass.Apply();
			GraphicsDevice.DrawUserPrimitives(PrimitiveType.LineList, lineVertices, 0, 1);
		}

		// Малюємо куб на кінці
		DrawScaleBox(effect, endPoint, color);
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
}