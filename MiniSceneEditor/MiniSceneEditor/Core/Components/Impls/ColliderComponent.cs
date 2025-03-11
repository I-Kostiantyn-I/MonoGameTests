using Microsoft.Core;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MiniSceneEditor.Core.Components.Abstractions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MiniSceneEditor.Core.Components.Impls;

public class ColliderComponent : IComponent
{
	public SceneObject Owner { get; set; }
	private ICollider _collider;
	private bool _showDebug = true;
	public bool ShowDebug
	{
		get => _showDebug;
		set => _showDebug = value;
	}

	// Параметри колайдера
	public ColliderType Type { get; private set; }
	public Vector3 Size { get; set; } = Vector3.One; // для Box
	public float Radius { get; set; } = 0.5f; // для Sphere

	public ColliderComponent()
	{
		Type = ColliderType.Box; // За замовчуванням створюємо box
	}

	public void SetType(ColliderType type)
	{
		Type = type;
		UpdateCollider();
	}

	public void Initialize()
	{
		UpdateCollider();
	}

	public void UpdateCollider()
	{
		_collider = Type switch
		{
			ColliderType.Box => new BepuBoxCollider(Size),
			ColliderType.Sphere => new BepuSphereCollider(Radius),
			_ => throw new ArgumentException("Unknown collider type")
		};
	}

	public ICollider GetCollider() => _collider;

	public void OnDestroy()
	{
		// Очищення ресурсів, якщо потрібно
	}

	public void DrawDebug(BasicEffect effect)
	{
		if (ShowDebug && _collider != null)
		{
			Color debugColor = Color.Yellow;// Owner.IsSelected ? Color.Yellow : Color.White;
			_collider.DrawDebug(effect, debugColor);
		}
	}

	public void SetDebugVisible(bool visible)
	{
		_showDebug = visible;
	}
}

public enum ColliderType
{
	Box,
	Sphere
	// Можна додати інші типи пізніше
}