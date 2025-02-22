using ImGuiNET;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;
using System.Linq;

namespace MiniSceneEditor;

public partial class Editor : Game
{
	private GraphicsDeviceManager _graphics;
	private SpriteBatch _spriteBatch;
	private ImGuiRenderer _imGuiRenderer;

	private List<SceneObject> _sceneObjects = new List<SceneObject>();
	private SceneObject _draggedObject = null;

	// Для сітки
	private BasicEffect _gridEffect;
	private VertexBuffer _gridVertexBuffer;
	private float _gridSize = 20f;
	private float _gridSpacing = 1f;

	// Для джерела світла
	private BasicEffect _lightEffect;
	private VertexBuffer _lightVertexBuffer;
	private Vector3 _lightPosition = new Vector3(0, 10, 0);
	private float _lightSize = 0.3f; // Розмір сфери світла

	private EditorCamera _camera;
	private SceneObject _selectedObject;

	private Scene _currentScene;
	private string _sceneName = "noname";

	public Editor()
	{
		_graphics = new GraphicsDeviceManager(this);
		Content.RootDirectory = "Content";
		IsMouseVisible = true;

		// Встановлюємо початковий розмір вікна
		_graphics.PreferredBackBufferWidth = 1280;
		_graphics.PreferredBackBufferHeight = 720;
		_graphics.ApplyChanges();

		// Дозволяємо змінювати розмір вікна
		Window.AllowUserResizing = true;
		Window.ClientSizeChanged += Window_ClientSizeChanged;

		ComponentRegister();
	}

	private void Window_ClientSizeChanged(object sender, EventArgs e)
	{
		// При зміні розміру вікна, розмір буде оновлено в BeginLayout
		_graphics.PreferredBackBufferWidth = Window.ClientBounds.Width;
		_graphics.PreferredBackBufferHeight = Window.ClientBounds.Height;
		_graphics.ApplyChanges();
	}


	protected override void Initialize()
	{

		_imGuiRenderer = new ImGuiRenderer(this);
		_camera = new EditorCamera(GraphicsDevice);
		//_camera.Position = new Vector3(0, 10, -20);
		//_camera.LookAt(Vector3.Zero);
		_currentScene = new Scene(GraphicsDevice);

		// Додаємо тестові об'єкти (потім це буде завантаження з файлу)
		var testObject = new SceneObject("Test Object");
		_currentScene.RegisterObject(testObject);

		var childObject = new SceneObject("Child Object");
		testObject.AddChild(childObject);

		var subChildObject = new SceneObject("Sub Child");
		childObject.AddChild(subChildObject);

		CreateGrid();
		CreateLightSource();

		base.Initialize();
	}

	protected override void LoadContent()
	{
		_spriteBatch = new SpriteBatch(GraphicsDevice);
	}

	protected override void Update(GameTime gameTime)
	{
		//if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed ||
		//	Keyboard.GetState().IsKeyDown(Keys.Escape))
		//	Exit();

		_camera.Update(gameTime);

		base.Update(gameTime);
	}

	protected override void Draw(GameTime gameTime)
	{
		GraphicsDevice.Clear(Color.CornflowerBlue);

		// Налаштування стану графічного пристрою
		GraphicsDevice.DepthStencilState = DepthStencilState.Default;
		GraphicsDevice.BlendState = BlendState.Opaque;

		// Налаштування камери
		Matrix view = _camera.GetViewMatrix();
		Matrix projection = _camera.GetProjectionMatrix();

		DrawGrid(view, projection);

		DrawLightObject(view, projection);
		// Рендеримо сцену використовуючи камеру редактора
		_currentScene.Draw(view, projection);
		//foreach (var obj in _sceneObjects)
		//{
		//	obj.Render(view, projection);
		//}
		DrawGUI(gameTime);

		base.Draw(gameTime);
	}

	protected override void UnloadContent()
	{
		foreach (var obj in _sceneObjects)
		{
			obj.Dispose();
		}
		_imGuiRenderer.Dispose();
		base.UnloadContent();
	}

	

	private bool IsChildOf(SceneObject parent, SceneObject potentialChild)
	{
		var current = potentialChild;
		while (current.Parent != null)
		{
			if (current.Parent == parent)
				return true;
			current = current.Parent;
		}
		return false;
	}


	// Допоміжні методи
	private void ClearSelection(List<SceneObject> objects)
	{
		foreach (var obj in objects)
		{
			obj.IsSelected = false;
			ClearSelection(obj.Children);
		}
	}

	private bool IsParentOf(SceneObject parent, SceneObject child)
	{
		if (child.Children.Contains(parent))
			return true;

		foreach (var childObj in child.Children)
		{
			if (IsParentOf(parent, childObj))
				return true;
		}

		return false;
	}

	private void RemoveObjectFromParent(SceneObject obj)
	{
		// Видалення з кореневого рівня
		if (_sceneObjects.Remove(obj))
			return;

		// Пошук і видалення з дочірніх елементів
		foreach (var sceneObj in _sceneObjects)
		{
			if (RemoveObjectFromChildren(sceneObj, obj))
				return;
		}
	}

	private bool RemoveObjectFromChildren(SceneObject parent, SceneObject obj)
	{
		if (parent.Children.Remove(obj))
			return true;

		foreach (var child in parent.Children)
		{
			if (RemoveObjectFromChildren(child, obj))
				return true;
		}

		return false;
	}

	private bool DeleteObject(SceneObject objectToDelete)
	{
		// Видалення з кореневого рівня
		if (_sceneObjects.Remove(objectToDelete))
			return true;

		// Рекурсивний пошук і видалення з дочірніх елементів
		foreach (var obj in _sceneObjects)
		{
			if (DeleteObjectFromChildren(obj, objectToDelete))
				return true;
		}

		return false;
	}

	private bool DeleteObjectFromChildren(SceneObject parent, SceneObject objectToDelete)
	{
		if (parent.Children.Remove(objectToDelete))
			return true;

		foreach (var child in parent.Children)
		{
			if (DeleteObjectFromChildren(child, objectToDelete))
				return true;
		}

		return false;
	}


	private void CreateGrid()
	{
		_gridEffect = new BasicEffect(GraphicsDevice);
		_gridEffect.VertexColorEnabled = true;
		_gridEffect.LightingEnabled = false;

		var vertices = new List<VertexPositionColor>();

		// Горизонтальні лінії
		for (float x = -_gridSize; x <= _gridSize; x += _gridSpacing)
		{
			vertices.Add(new VertexPositionColor(
				new Vector3(x, 0, -_gridSize),
				x == 0 ? Color.Blue : Color.Gray));
			vertices.Add(new VertexPositionColor(
				new Vector3(x, 0, _gridSize),
				x == 0 ? Color.Blue : Color.Gray));
		}

		// Вертикальні лінії
		for (float z = -_gridSize; z <= _gridSize; z += _gridSpacing)
		{
			vertices.Add(new VertexPositionColor(
				new Vector3(-_gridSize, 0, z),
				z == 0 ? Color.Red : Color.Gray));
			vertices.Add(new VertexPositionColor(
				new Vector3(_gridSize, 0, z),
				z == 0 ? Color.Red : Color.Gray));
		}

		_gridVertexBuffer = new VertexBuffer(
			GraphicsDevice,
			typeof(VertexPositionColor),
			vertices.Count,
			BufferUsage.WriteOnly);

		_gridVertexBuffer.SetData(vertices.ToArray());
	}

	private void CreateLightSource()
	{
		_lightEffect = new BasicEffect(GraphicsDevice);
		_lightEffect.VertexColorEnabled = true;
		_lightEffect.LightingEnabled = false;

		// Створюємо просту візуалізацію джерела світла у вигляді хреста
		var vertices = new VertexPositionColor[]
		{
            // Вертикальна лінія
            new VertexPositionColor(new Vector3(0, -_lightSize, 0), Color.Yellow),
			new VertexPositionColor(new Vector3(0, _lightSize, 0), Color.Yellow),
            
            // Горизонтальна лінія X
            new VertexPositionColor(new Vector3(-_lightSize, 0, 0), Color.Yellow),
			new VertexPositionColor(new Vector3(_lightSize, 0, 0), Color.Yellow),
            
            // Горизонтальна лінія Z
            new VertexPositionColor(new Vector3(0, 0, -_lightSize), Color.Yellow),
			new VertexPositionColor(new Vector3(0, 0, _lightSize), Color.Yellow),
		};

		_lightVertexBuffer = new VertexBuffer(
			GraphicsDevice,
			typeof(VertexPositionColor),
			vertices.Length,
			BufferUsage.WriteOnly);

		_lightVertexBuffer.SetData(vertices);
	}

	private void DrawGrid(Matrix view, Matrix projection)
	{
		// Малювання сітки
		_gridEffect.View = view;
		_gridEffect.Projection = projection;
		_gridEffect.World = Matrix.Identity;

		foreach (EffectPass pass in _gridEffect.CurrentTechnique.Passes)
		{
			pass.Apply();
			GraphicsDevice.SetVertexBuffer(_gridVertexBuffer);
			GraphicsDevice.DrawPrimitives(
				PrimitiveType.LineList,
				0,
				_gridVertexBuffer.VertexCount / 2);
		}
	}

	private void DrawLightObject(Matrix view, Matrix projection)
	{
		// Малювання джерела світла
		_lightEffect.View = view;
		_lightEffect.Projection = projection;
		_lightEffect.World = Matrix.CreateTranslation(_lightPosition);

		foreach (EffectPass pass in _lightEffect.CurrentTechnique.Passes)
		{
			pass.Apply();
			GraphicsDevice.SetVertexBuffer(_lightVertexBuffer);
			GraphicsDevice.DrawPrimitives(
				PrimitiveType.LineList,
				0,
				3); // 3 лінії для хреста
		}
	}
}
