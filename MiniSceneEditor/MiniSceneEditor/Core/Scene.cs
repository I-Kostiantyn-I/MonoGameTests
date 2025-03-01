using Microsoft.Xna.Framework;
using System.Collections.Generic;
using System;
using System.Linq;
using Microsoft.Xna.Framework.Graphics;
using MiniSceneEditor.Core.Components.Abstractions;
using MiniSceneEditor.Core.Components.Impls;

namespace Microsoft.Core;

public class Scene : IScene
{
	// Зберігання всіх об'єктів сцени
	private Dictionary<uint, SceneObject> _objects;
	private readonly GraphicsDevice _graphicsDevice;
	private readonly EditorLogger _log;

	// Системні об'єкти
	public SceneObject MainCamera { get; private set; }
	public SceneObject DirectionalLight { get; private set; }

	public GraphicsDevice GraphicsDevice => _graphicsDevice;

	public Scene(GraphicsDevice graphicsDevice)
	{
		_objects = new Dictionary<uint, SceneObject>();
		_graphicsDevice = graphicsDevice;

		// Створення обов'язкових системних об'єктів
		InitializeSystemObjects();
		_log = new EditorLogger(nameof(Scene));
	}

	private void InitializeSystemObjects()
	{
		// Створення і налаштування камери
		MainCamera = new SceneObject("Main Camera");
		MainCamera.Transform.Position = new Vector3(0, 5, -10);
		MainCamera.Transform.Rotation = new Vector3(
			MathHelper.ToRadians(15),
			0,
			0);

		// Додаємо компонент камери
		var cameraComponent = MainCamera.AddComponent<CameraComponent>();
		if (cameraComponent is IRenderable renderable)
		{
			renderable.SetGraphicsDevice(GraphicsDevice);
		}
		else
		{
			//System.Diagnostics.Debug.WriteLine("Camera component is not IRenderable!");
		}

		RegisterObject(MainCamera);

		// Створення і налаштування світла
		DirectionalLight = new SceneObject("Directional Light");
		DirectionalLight.Transform.Position = new Vector3(0, 10, 0);
		DirectionalLight.Transform.Rotation = new Vector3(
			MathHelper.ToRadians(50),
			MathHelper.ToRadians(-30),
			0);

		// Додаємо компонент світла
		var lightComponent = DirectionalLight.AddComponent<LightComponent>();
		lightComponent.Color = new Vector3(1.0f, 0.95f, 0.8f); // Тепле біле світло
		lightComponent.Intensity = 1.0f;
		lightComponent.Ambient = new Vector3(0.1f, 0.1f, 0.15f); // Злегка синюватий ambient

		RegisterObject(DirectionalLight);
	}

	public uint RegisterObject(SceneObject obj)
	{
		ArgumentNullException.ThrowIfNull(obj);

		_objects[obj.Id] = obj;
		return obj.Id;
	}

	public bool UnregisterObject(uint id)
	{
		if (_objects.TryGetValue(id, out var obj))
		{
			// Перевіряємо, чи це не системний об'єкт
			if (obj == MainCamera || obj == DirectionalLight)
				throw new InvalidOperationException("Cannot remove system objects");

			// Видаляємо об'єкт та всіх його нащадків
			foreach (var child in obj.Children.ToList())
			{
				UnregisterObject(child.Id);
			}

			return _objects.Remove(id);
		}
		return false;
	}

	public SceneObject GetObject(uint id)
	{
		return _objects.TryGetValue(id, out var obj) ? obj : null;
	}

	public IEnumerable<SceneObject> GetRootObjects()
	{
		return _objects.Values.Where(obj => obj.Parent == null);
	}

	public void Initialize()
	{
		foreach (var obj in _objects.Values)
		{
			// Ініціалізація об'єктів, якщо потрібно
		}
	}

	public void Update(GameTime gameTime)
	{
		foreach (var obj in _objects.Values)
		{
			obj.Update(gameTime);
		}
	}

	public void Draw(Matrix view, Matrix projection, GameTime gameTime = null)
	{
		// Отримуємо матриці з основної камери
		var cameraComponent = MainCamera.GetComponent<CameraComponent>();
		if (cameraComponent == null)
		{
			return;
		}

		// Рендеримо всі об'єкти
		foreach (var obj in _objects.Values)
		{
			_log.Log($"Drawing object: {obj.Name}");
			obj.Render(view, projection);


			// видалити цей цикл
			foreach (var component in obj.Components)
			{
				if (component is IRenderable renderable)
				{
					_log.Log($"Rendering component: {component.GetType().Name}");
					//renderable.Render(view, projection);
				}
			}
		}
	}

	public void SaveToFile(string path)
	{
		// TODO: Реалізація серіалізації
	}

	public void LoadFromFile(string path)
	{
		// TODO: Реалізація десеріалізації
	}
}