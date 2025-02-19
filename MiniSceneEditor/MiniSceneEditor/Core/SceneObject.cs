using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework;
using System.Collections.Generic;
using System;
using System.ComponentModel;
using System.Linq;
using MiniSceneEditor.Core;

public class SceneObject : IDisposable
{
	public uint Id { get; }
	public string Name { get; set; }
	public Transform Transform;
	public SceneObject Parent { get; private set; }
	public List<SceneObject> Children { get; } = new();
	public bool IsVisible { get; set; } = true;
	public bool IsSelected { get; set; }

	private Dictionary<Type, IComponent> _components = new();
	private static uint _nextId = 1;

	public SceneObject(string name)
	{
		Id = _nextId++;
		Name = name;
		Transform = Transform.Identity;
		Children = new List<SceneObject>();
	}

	// Робота з компонентами
	public T AddComponent<T>() where T : class, IComponent, new()
	{
		if (_components.ContainsKey(typeof(T)))
			throw new Exception($"Component {typeof(T)} already exists");

		var component = new T { Owner = this };
		_components[typeof(T)] = component;
		component.Initialize();
		return component;
	}

	public T GetComponent<T>() where T : class, IComponent
	{
		return _components.TryGetValue(typeof(T), out var component)
			? (T)component
			: null;
	}

	public bool RemoveComponent<T>() where T : class, IComponent
	{
		if (_components.TryGetValue(typeof(T), out var component))
		{
			component.OnDestroy();
			return _components.Remove(typeof(T));
		}
		return false;
	}

	public bool HasComponent<T>() where T : class, IComponent
	{
		return _components.ContainsKey(typeof(T));
	}

	// Робота з ієрархією
	public void AddChild(SceneObject child)
	{
		if (child.Parent != null)
		{
			child.Parent.Children.Remove(child);
		}
		child.Parent = this;
		Children.Add(child);
	}

	public void RemoveChild(SceneObject child)
	{
		if (Children.Contains(child))
		{
			child.Parent = null;
			Children.Remove(child);
		}
	}

	public void SetParent(SceneObject parent)
	{
		if (parent != null)
		{
			parent.AddChild(this);
		}
		else if (Parent != null)
		{
			Parent.RemoveChild(this);
		}
	}

	// Методи оновлення та рендерингу
	public virtual void Update(GameTime gameTime)
	{
		foreach (var component in _components.Values)
		{
			if (component is ISceneUpdateable updateable)
			{
				updateable.Update(gameTime);
			}
		}

		foreach (var child in Children)
		{
			child.Update(gameTime);
		}
	}

	public virtual void Render(Matrix view, Matrix projection)
	{
		if (!IsVisible) return;

		foreach (var component in _components.Values)
		{
			if (component is IRenderable drawable)
			{
				drawable.Render(view, projection);
			}
		}

		foreach (var child in Children)
		{
			child.Render(view, projection);
		}
	}

	// Утиліти
	public Matrix GetWorldMatrix()
	{
		Matrix worldMatrix = Transform.GetWorldMatrix();
		if (Parent != null)
		{
			worldMatrix *= Parent.GetWorldMatrix();
		}
		return worldMatrix;
	}

	public Vector3 GetWorldPosition()
	{
		return Vector3.Transform(Transform.Position, GetWorldMatrix());
	}

	// Очищення ресурсів
	public void Dispose()
	{
		foreach (var component in _components.Values)
		{
			if (component is IDisposable disposable)
			{
				disposable.Dispose();
			}
		}

		foreach (var child in Children.ToList())
		{
			child.Dispose();
		}

		Children.Clear();
		_components.Clear();
	}

	// Клонування об'єкта
	public SceneObject Clone()
	{
		var clone = new SceneObject(Name)
		{
			Transform = Transform,
			IsVisible = IsVisible
		};

		// Клонування компонентів
		foreach (var component in _components.Values)
		{
			if (component is ICloneable cloneable)
			{
				var componentClone = cloneable.Clone() as IComponent;
				if (componentClone != null)
				{
					clone._components[component.GetType()] = componentClone;
					componentClone.Owner = clone;
					componentClone.Initialize();
				}
			}
		}

		// Клонування дочірніх об'єктів
		foreach (var child in Children)
		{
			var childClone = child.Clone();
			clone.AddChild(childClone);
		}

		return clone;
	}
}


/*public class SceneObject : IDisposable
{
	public string Name { get; set; }
	public Transform Transform;
	public List<SceneObject> Children { get; private set; }
	public SceneObject Parent { get; private set; }
	public bool IsSelected { get; set; }
	public bool IsVisible { get; set; }

	private BasicEffect _effect;
	private GraphicsDevice _graphicsDevice;
	private VertexBuffer _axisVertexBuffer;
	private float _axisLength = 1.0f;

	public SceneObject(GraphicsDevice graphicsDevice, string name)
	{
		_graphicsDevice = graphicsDevice;
		Name = name;
		Transform = Transform.Identity;
		Children = new List<SceneObject>();
		IsVisible = true;

		_effect = new BasicEffect(graphicsDevice)
		{
			VertexColorEnabled = true,
			LightingEnabled = false
		};

		CreateAxisVisual();
	}

	private void CreateAxisVisual()
	{
		// Створюємо візуалізацію осей координат
		var vertices = new VertexPositionColor[6];

		// X axis - червоний
		vertices[0] = new VertexPositionColor(Vector3.Zero, Color.Red);
		vertices[1] = new VertexPositionColor(Vector3.Right * _axisLength, Color.Red);

		// Y axis - зелений
		vertices[2] = new VertexPositionColor(Vector3.Zero, Color.Green);
		vertices[3] = new VertexPositionColor(Vector3.Up * _axisLength, Color.Green);

		// Z axis - синій
		vertices[4] = new VertexPositionColor(Vector3.Zero, Color.Blue);
		vertices[5] = new VertexPositionColor(Vector3.Forward * _axisLength, Color.Blue);

		_axisVertexBuffer = new VertexBuffer(
			_graphicsDevice,
			typeof(VertexPositionColor),
			6,
			BufferUsage.WriteOnly);

		_axisVertexBuffer.SetData(vertices);
	}

	public void AddChild(SceneObject child)
	{
		if (child.Parent != null)
		{
			child.Parent.Children.Remove(child);
		}
		child.Parent = this;
		Children.Add(child);
	}

	public void RemoveChild(SceneObject child)
	{
		if (Children.Contains(child))
		{
			child.Parent = null;
			Children.Remove(child);
		}
	}

	public void Draw(Matrix view, Matrix projection)
	{
		if (!IsVisible)
			return;

		_effect.World = Transform.GetWorldMatrix();
		_effect.View = view;
		_effect.Projection = projection;

		// Малюємо осі координат
		_graphicsDevice.SetVertexBuffer(_axisVertexBuffer);

		foreach (EffectPass pass in _effect.CurrentTechnique.Passes)
		{
			pass.Apply();

			// Малюємо три лінії (осі)
			_graphicsDevice.DrawPrimitives(
				PrimitiveType.LineList,
				0,
				3); // 3 лінії
		}

		// Якщо об'єкт виділений, можна додати додаткову візуалізацію
		if (IsSelected)
		{
			// Тут можна додати додаткову візуалізацію для виділеного об'єкта
		}

		// Рекурсивно малюємо дочірні об'єкти
		foreach (var child in Children)
		{
			child.Draw(view, projection);
		}
	}

	public void Dispose()
	{
		_effect?.Dispose();
		_axisVertexBuffer?.Dispose();

		foreach (var child in Children)
		{
			child.Dispose();
		}
	}
}*/