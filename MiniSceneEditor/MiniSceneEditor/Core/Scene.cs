using Microsoft.Xna.Framework;
using System.Collections.Generic;
using System;
using System.Linq;
using Microsoft.Xna.Framework.Graphics;
using MiniSceneEditor.Core;

public class Scene : IScene
{
	// Зберігання всіх об'єктів сцени
	private Dictionary<uint, SceneObject> _objects;
	private readonly GraphicsDevice _graphicsDevice;

	// Системні об'єкти
	public SceneObject MainCamera { get; private set; }
	public SceneObject DirectionalLight { get; private set; }

	public Scene(GraphicsDevice graphicsDevice)
	{
		_objects = new Dictionary<uint, SceneObject>();
		_graphicsDevice = graphicsDevice;

		// Створення обов'язкових системних об'єктів
		InitializeSystemObjects();
		
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
			//System.Diagnostics.Debug.WriteLine("Setting GraphicsDevice for camera component");
			renderable.SetGraphicsDevice(_graphicsDevice);
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
		if (obj == null)
			throw new ArgumentNullException(nameof(obj));

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
			//System.Diagnostics.Debug.WriteLine("Main camera component not found!");
			return;
		}

		// Рендеримо всі об'єкти
		foreach (var obj in _objects.Values)
		{
			obj.Render(view, projection);
			//System.Diagnostics.Debug.WriteLine($"Drawing object: {obj.Name}");
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