using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;
using System;
using System.Linq;
using MiniSceneEditor.Core;
using MiniSceneEditor.Core.Components.Abstractions;
using MiniSceneEditor.Core.Components.Impls;

namespace Microsoft.Core;

public class SceneObject : IDisposable //DrawableGameComponent
{
	public uint Id { get; }
	public string Name { get; set; }
	public TransformComponent Transform;
	public SceneObject Parent { get; private set; }
	public List<SceneObject> Children { get; } = new();
	public bool IsVisible { get; set; } = true;
	public Vector2 Position { get; set; }
	public Texture2D Texture { get; set; }

	public bool IsMouseOver(Vector2 mousePosition)
	{
		return mousePosition.X >= Position.X && mousePosition.X <= Position.X + Texture.Width &&
			 mousePosition.Y >= Position.Y && mousePosition.Y <= Position.Y + Texture.Height;
	}

	public IReadOnlySet<IComponent> Components => _components.Values.ToHashSet();

	private Dictionary<Type, IComponent> _components = new();
	private static uint _nextId = 1;

	public SceneObject(string name)
	{
		Id = _nextId++;
		Name = name;
		Transform = new TransformComponent();
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

	/*public override void Draw(GameTime gameTime)
    {
        SpriteBatch spriteBatch = (SpriteBatch)Game.Services.GetService(typeof(SpriteBatch));
        spriteBatch.Begin();
        spriteBatch.Draw(Texture, Position, Color.White);
        
        // Додати візуальне позначення об'єкта якщо він вибран
        if (IsSelected)
        {
            Rectangle bounds = new Rectangle((int)Position.X, (int)Position.Y, Texture.Width, Texture.Height);
            spriteBatch.Draw(Game.Content.Load<Texture2D>("SelectionBox"), bounds, Color.Red * 0.5f);
        }

        spriteBatch.End();

        base.Draw(gameTime);
    }*/

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

	public override bool Equals(object obj)
	{
		if (obj == null || obj is not SceneObject sceneObject) return false;

		return Id == sceneObject.Id;
	}

	public override int GetHashCode()
	{
		return Id.GetHashCode();
	}
}
