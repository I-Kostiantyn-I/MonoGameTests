using Microsoft.Core;
using Microsoft.Xna.Framework;
using MiniSceneEditor.Core.Components.Abstractions;

namespace MiniSceneEditor.Core.Components.Impls;

public class TransformComponent : IComponent
{
	public SceneObject Owner { get; set; }

	private Vector3 _position;
	private Vector3 _rotation;
	private Vector3 _scale;

	public Vector3 Position
	{
		get => _position;
		set
		{
			if (_position != value)
			{
				_position = value;
				OnTransformChanged();
			}
		}
	}

	public Vector3 Rotation
	{
		get => _rotation;
		set
		{
			if (_rotation != value)
			{
				_rotation = value;
				OnTransformChanged();
			}
		}
	}

	public Vector3 Scale
	{
		get => _scale;
		set
		{
			if (_scale != value)
			{
				_scale = value;
				OnTransformChanged();
			}
		}
	}

	public TransformComponent()
	{
		_position = Vector3.Zero;
		_rotation = Vector3.Zero;
		_scale = Vector3.One;
	}

	public void Initialize()
	{
		// Ініціалізація, якщо потрібна
	}

	public void OnDestroy()
	{
		// Очищення ресурсів, якщо потрібно
	}

	public Matrix GetWorldMatrix()
	{
		return Matrix.CreateScale(_scale) *
			   Matrix.CreateRotationX(_rotation.X) *
			   Matrix.CreateRotationY(_rotation.Y) *
			   Matrix.CreateRotationZ(_rotation.Z) *
			   Matrix.CreateTranslation(_position);
	}

	private void OnTransformChanged()
	{
		// Тут можна додати логіку, яка має виконуватись при зміні трансформації
		// Наприклад, сповіщення інших компонентів про зміну
	}
}
