using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework;
using MiniSceneEditor.Core.Components.Impls;
using MiniSceneEditor.Commands;
using MiniSceneEditor.Camera;

namespace MiniSceneEditor.Gizmo;
public abstract class BaseGizmo : IGizmo
{
	protected GraphicsDevice GraphicsDevice;
	protected VertexBuffer VertexBuffer;
	protected Color BaseColor = Color.Gray;
	protected Color HoverColor = Color.Yellow;
	protected Color ActiveColor = Color.Green;
	protected int HoveredAxis = -1;
	protected int ActiveAxis = -1;
	protected Vector3 DragStart;
	protected Vector3 OriginalTransform;
	protected const float GIZMO_SIZE = 1.0f;
	protected const float HOVER_THRESHOLD = 0.1f;
	protected CommandManager CommandManager;
	protected Vector3 TransformStart;
	protected bool IsDragging;

	protected readonly Vector3[] AxisDirections = new[]
	{
		Vector3.Right,   // X axis
        Vector3.Up,      // Y axis
        Vector3.Forward  // Z axis
    };

	protected readonly Color[] AxisColors = new[]
	{
		Color.Red,    // X axis
        Color.Green,  // Y axis
        Color.Blue    // Z axis
    };

	protected readonly EditorLogger _log;

	protected Vector3 OriginalValue;

	protected readonly SnapSystem SnapSystem;

	protected BaseGizmo(GraphicsDevice graphicsDevice, CommandManager commandManager, SnapSystem snapSystem)
	{
		GraphicsDevice = graphicsDevice;
		CommandManager = commandManager;
		SnapSystem = snapSystem;

		_log = new EditorLogger(nameof(BaseGizmo), false);
	}

	public abstract void Draw(BasicEffect effect, TransformComponent transform);
	public abstract bool HandleInput(InputState input, CameraMatricesState camera, TransformComponent transform);

	public virtual void SetColor(Color baseColor, Color hoverColor, Color activeColor)
	{
		BaseColor = baseColor;
		HoverColor = hoverColor;
		ActiveColor = activeColor;
	}

	protected bool IsAxisHovered(Vector2 mousePosition, CameraMatricesState camera, Vector3 worldPosition, Vector3 axisDirection, out float distance)
	{
		Ray mouseRay = GetMouseRay(mousePosition, camera);
		Vector3 lineStart = worldPosition;
		Vector3 lineEnd = worldPosition + axisDirection * GIZMO_SIZE;

		return RayLineIntersection(mouseRay, lineStart, lineEnd, out distance);
	}

	protected void BeginDrag(TransformComponent transform, TransformCommand.TransformationType type)
	{
		IsDragging = true;
		OriginalValue = GetTransformValue(transform, type);
		_log.Log($"Started dragging: {type}, Original value: {OriginalValue}");
	}

	protected void EndDrag(TransformComponent transform, TransformCommand.TransformationType type)
	{
		if (IsDragging)
		{
			Vector3 newValue = GetTransformValue(transform, type);
			if (newValue != OriginalValue)
			{
				var command = new TransformCommand(transform, OriginalValue, newValue, type);
				CommandManager.ExecuteCommand(command);
				_log.Log($"Ended dragging: {type}, New value: {newValue}");
			}
		}
		IsDragging = false;
		ActiveAxis = -1;
	}

	protected Vector3 GetTransformValue(TransformComponent transform, TransformCommand.TransformationType type)
	{
		return type switch
		{
			TransformCommand.TransformationType.Position => transform.Position,
			TransformCommand.TransformationType.Rotation => transform.Rotation,
			TransformCommand.TransformationType.Scale => transform.Scale,
			_ => Vector3.Zero
		};
	}


	protected Ray GetMouseRay(Vector2 mousePosition, CameraMatricesState camera)
	{
		Vector3 nearPoint = GraphicsDevice.Viewport.Unproject(
			new Vector3(mousePosition, 0),
			camera.ProjectionMatrix,
			camera.ViewMatrix,
			Matrix.Identity);

		Vector3 farPoint = GraphicsDevice.Viewport.Unproject(
			new Vector3(mousePosition, 1),
			camera.ProjectionMatrix,
			camera.ViewMatrix,
			Matrix.Identity);

		Vector3 direction = Vector3.Normalize(farPoint - nearPoint);
		return new Ray(nearPoint, direction);
	}

	protected bool RayLineIntersection(Ray ray, Vector3 lineStart, Vector3 lineEnd, out float distance)
	{
		Vector3 lineDirection = Vector3.Normalize(lineEnd - lineStart);
		Vector3 cross = Vector3.Cross(ray.Direction, lineDirection);

		float denominator = cross.Length();
		if (denominator < float.Epsilon)
		{
			distance = float.MaxValue;
			return false;
		}

		Vector3 lineToRay = ray.Position - lineStart;
		float t = Vector3.Dot(Vector3.Cross(lineToRay, lineDirection), cross) / (denominator * denominator);

		if (t < 0)
		{
			distance = float.MaxValue;
			return false;
		}

		Vector3 closestPoint = ray.Position + ray.Direction * t;
		float lineT = Vector3.Dot(closestPoint - lineStart, lineDirection);

		if (lineT < 0 || lineT > (lineEnd - lineStart).Length())
		{
			distance = float.MaxValue;
			return false;
		}

		distance = Vector3.Distance(closestPoint, lineStart);
		return distance < HOVER_THRESHOLD;
	}

	protected void BeginTransform(TransformComponent transform)
	{
		IsDragging = true;
		switch (GetTransformationType())
		{
			case TransformCommand.TransformationType.Position:
				TransformStart = transform.Position;
				break;
			case TransformCommand.TransformationType.Rotation:
				TransformStart = transform.Rotation;
				break;
			case TransformCommand.TransformationType.Scale:
				TransformStart = transform.Scale;
				break;
		}
	}

	protected void EndTransform(TransformComponent transform)
	{
		if (IsDragging)
		{
			Vector3 endValue;
			switch (GetTransformationType())
			{
				case TransformCommand.TransformationType.Position:
					endValue = transform.Position;
					break;
				case TransformCommand.TransformationType.Rotation:
					endValue = transform.Rotation;
					break;
				case TransformCommand.TransformationType.Scale:
					endValue = transform.Scale;
					break;
				default:
					return;
			}

			var command = new TransformCommand(
				transform,
				TransformStart,
				endValue,
				GetTransformationType()
			);
			CommandManager.ExecuteCommand(command);
		}
		IsDragging = false;
	}

	protected Vector3 ApplySnapping(Vector3 value, TransformCommand.TransformationType type)
	{
		switch (type)
		{
			case TransformCommand.TransformationType.Position:
				return SnapSystem.SnapPosition(value, TransformStart);
			case TransformCommand.TransformationType.Rotation:
				return SnapSystem.SnapRotation(value);
			case TransformCommand.TransformationType.Scale:
				return SnapSystem.SnapScale(value);
			default:
				return value;
		}
	}

	protected abstract TransformCommand.TransformationType GetTransformationType();
}