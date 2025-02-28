using Microsoft.Xna.Framework;
using MiniSceneEditor.Core.Components.Impls;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MiniSceneEditor.Commands;

public class TransformCommand : ICommand
{
	protected readonly TransformComponent Transform;
	protected readonly Vector3 OldValue;
	protected readonly Vector3 NewValue;
	protected readonly TransformationType Type;

	public enum TransformationType
	{
		Position,
		Rotation,
		Scale
	}

	public TransformCommand(TransformComponent transform, Vector3 oldValue, Vector3 newValue, TransformationType type)
	{
		Transform = transform;
		OldValue = oldValue;
		NewValue = newValue;
		Type = type;
	}

	public virtual void Execute()
	{
		ApplyValue(NewValue);
	}

	public virtual void Undo()
	{
		ApplyValue(OldValue);
	}

	protected void ApplyValue(Vector3 value)
	{
		switch (Type)
		{
			case TransformationType.Position:
				Transform.Position = value;
				break;
			case TransformationType.Rotation:
				Transform.Rotation = value;
				break;
			case TransformationType.Scale:
				Transform.Scale = value;
				break;
		}
	}
}