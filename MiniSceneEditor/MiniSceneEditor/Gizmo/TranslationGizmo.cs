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
	private float _axisLength = 1.0f;
	private float _selectionThreshold = 0.1f;
	private float _movementSensitivity = 0.01f;
	private float _arrowSize = 0.1f;
	private float _normalThickness = 0.02f;
	private float _selectedThickness = 0.1f;
	private readonly Vector3[] _axisDirections;
	private readonly Color[] _axisColors;

	private readonly EditorLogger _log;

	private VertexBuffer _vertexBuffer;
	private IndexBuffer _indexBuffer;
	private int _hoveredAxis = -1;

	public TranslationGizmo(GraphicsDevice graphicsDevice, CommandManager commandManager, SnapSystem snapSystem)
		: base(graphicsDevice, commandManager, snapSystem)
	{
		_log = new EditorLogger(nameof(TranslationGizmo));

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
				Vector3 arrowPosition = transform.Position + _axisDirections[i] * _axisLength;
				if (CheckArrowIntersection(ray, arrowPosition, _axisDirections[i], out float distance))
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
			Vector3 moveAxis = _axisDirections[ActiveAxis];

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

	// Метод для налаштування параметрів гізмо
	public void SetGizmoParameters(float axisLength, float arrowSize, float normalThickness, float selectedThickness)
	{
		_axisLength = axisLength;
		_arrowSize = arrowSize;
		_normalThickness = normalThickness;
		_selectedThickness = selectedThickness;

		// Перестворюємо геометрію з новими параметрами
		_vertexBuffer?.Dispose();
		_indexBuffer?.Dispose();
		_vertexBuffer = null; // Це призведе до перестворення геометрії при наступному Draw

		_log.Log($"Updated gizmo parameters: AxisLength={axisLength}, ArrowSize={arrowSize}, " +
				 $"NormalThickness={normalThickness}, SelectedThickness={selectedThickness}");
	}

	private bool CheckAxisIntersection(Ray ray, Vector3 origin, Vector3 axisDirection)
	{
		// Відстань від променя до осі
		//float threshold = 0.1f; // Можна налаштувати чутливість вибору осі

		Vector3 lineEnd = origin + axisDirection * _axisLength;

		// Перевіряємо перетин променя з лінією осі
		Vector3 v = lineEnd - origin;
		Vector3 w = ray.Position - origin;

		float a = Vector3.Dot(v, v); // квадрат довжини осі
		float b = Vector3.Dot(v, ray.Direction);
		float c = Vector3.Dot(v, w);
		float d = Vector3.Dot(ray.Direction, ray.Direction); // завжди 1
		float e = Vector3.Dot(ray.Direction, w);
		float D = a * d - b * b;

		float sc, tc;

		if (D < float.Epsilon) // лінії паралельні
		{
			sc = 0;
			tc = (b > c ? c / b : c / a);
		}
		else
		{
			sc = (b * e - c * d) / D;
			tc = (a * e - b * c) / D;
		}

		// Знаходимо найближчі точки
		Vector3 pointOnRay = ray.Position + ray.Direction * sc;
		Vector3 pointOnAxis = origin + v * tc;

		// Перевіряємо відстань між точками
		float distance = Vector3.Distance(pointOnRay, pointOnAxis);

		// Перевіряємо, чи точка на осі знаходиться в межах відрізка
		bool isWithinAxis = tc >= 0 && tc <= 1;

		_log.Log($"Axis intersection check: Distance={distance}, IsWithinAxis={isWithinAxis}");

		return distance < _selectionThreshold && isWithinAxis;
	}

	private void DrawAxis(BasicEffect effect, Vector3 position, int axisIndex)
	{
		// Встановлюємо колір осі
		Color color = _axisColors[axisIndex];
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
			new VertexPositionColor(position + _axisDirections[axisIndex] * _axisLength, color)
		};

		foreach (var pass in effect.CurrentTechnique.Passes)
		{
			pass.Apply();
			GraphicsDevice.DrawUserPrimitives(PrimitiveType.LineList, lineVertices, 0, 1);
		}

		// Малюємо стрілку на кінці
		Vector3 arrowTip = position + _axisDirections[axisIndex] * _axisLength;
		DrawArrow(effect, arrowTip, _axisDirections[axisIndex], color);
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

	//public override bool HandleInput(InputState input, CameraMatricesState camera, TransformComponent transform)
	//{
	//	Vector2 mousePosition = input.MousePosition;
	//	Vector3 worldPosition = transform.Position; // Використовуємо позицію з переданого transform

	//	if (input.IsMouseButtonPressed(ButtonState.Pressed))
	//	{
	//		for (int i = 0; i < 3; i++)
	//		{
	//			if (IsAxisHovered(mousePosition, camera, worldPosition, _axisDirections[i], out float distance))
	//			{
	//				ActiveAxis = i;
	//				DragStart = mousePosition.ToVector3();
	//				BeginTransform(transform);
	//				return true;
	//			}
	//		}
	//	}
	//	else if (input.CurrentMouse.LeftButton == ButtonState.Released && ActiveAxis != -1)
	//	{
	//		EndTransform(transform);
	//		ActiveAxis = -1;
	//	}

	//	if (ActiveAxis != -1)
	//	{
	//		Vector2 currentPos = input.MousePosition;
	//		Vector3 delta = (currentPos.ToVector3() - DragStart);

	//		Vector3 moveAxis = _axisDirections[ActiveAxis];
	//		float moveAmount = delta.X * 0.01f;

	//		transform.Position += moveAxis * moveAmount;

	//		DragStart = currentPos.ToVector3();
	//		return true;
	//	}

	//	return false;
	//}
}