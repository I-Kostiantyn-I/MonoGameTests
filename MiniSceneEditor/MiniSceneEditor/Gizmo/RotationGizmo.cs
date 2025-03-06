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

public class RotationGizmo : BaseGizmo
{
	private float _radius = 1.0f;
	private float _sphereRadius = 0.05f; // Розмір кульки-індикатора
	private int _hoveredAxis = -1;
	private const int CIRCLE_SEGMENTS = 32;

	private readonly EditorLogger _log;

	public RotationGizmo(GraphicsDevice graphicsDevice, CommandManager commandManager, SnapSystem snapSystem)
		: base(graphicsDevice, commandManager, snapSystem)
	{
		_log = new EditorLogger(nameof(RotationGizmo), false);
	}

	public override void Draw(BasicEffect effect, TransformComponent transform)
	{
		_log.Log($"Drawing RotationGizmo. HoveredAxis: {_hoveredAxis}, ActiveAxis: {ActiveAxis}");

		for (int i = 0; i < 3; i++)
		{
			Color ringColor = AxisColors[i];
			if (i == _hoveredAxis || i == ActiveAxis)
			{
				ringColor = Color.White;
				_log.Log($"Axis {i} is highlighted");
			}

			_log.Log($"Drawing axis {i} with color: {ringColor}");

			// Встановлюємо колір для BasicEffect
			effect.DiffuseColor = ringColor.ToVector3();
			effect.Alpha = ringColor.A / 255f;

			DrawRotationRing(effect, transform.Position, i, ringColor);
			DrawAngleIndicator(effect, transform.Position, i, transform.Rotation.GetAxis(i), ringColor);
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
				if (CheckRingIntersection(ray, transform.Position, AxisDirections[i], out float distance))
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
			Vector3 rotationAxis = AxisDirections[ActiveAxis];

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

	protected override TransformCommand.TransformationType GetTransformationType()
	{
		return TransformCommand.TransformationType.Rotation;
	}

	private void DrawRotationRing(BasicEffect effect, Vector3 position, int axisIndex, Color color)
	{
		var vertices = new List<VertexPositionColor>();
		Vector3 axis = AxisDirections[axisIndex];

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
		Vector3 axis = AxisDirections[axisIndex];
		Vector3 indicatorPos = GetCirclePoint(center, axis, angle);

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
}