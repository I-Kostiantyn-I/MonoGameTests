using Microsoft.Xna.Framework;
using MiniSceneEditor.Core.Components.Impls;

namespace MiniSceneEditor.Commands;

public class TransformCommand : ICommand
{
	private readonly TransformComponent _transform;
	private readonly Vector3 _oldValue;
	private readonly Vector3 _newValue;
	private readonly TransformationType _type;

	public enum TransformationType
	{
		Position,
		Rotation,
		Scale
	}

	public TransformCommand(TransformComponent transform, Vector3 oldValue, Vector3 newValue, TransformationType type)
	{
		_transform = transform;
		_oldValue = oldValue;
		_newValue = newValue;
		_type = type;
	}

	public void Execute()
	{
		ApplyTransform(_newValue);
	}

	public void Undo()
	{
		ApplyTransform(_oldValue);
	}

	private void ApplyTransform(Vector3 value)
	{
		switch (_type)
		{
			case TransformationType.Position:
				_transform.Position = value;
				break;
			case TransformationType.Rotation:
				_transform.Rotation = value;
				break;
			case TransformationType.Scale:
				_transform.Scale = value;
				break;
		}
	}
}