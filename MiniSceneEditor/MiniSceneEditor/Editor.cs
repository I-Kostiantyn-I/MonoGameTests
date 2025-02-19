using ImGuiNET;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;
using System.Linq;

namespace MiniSceneEditor;

public class Editor : Game
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

	private Camera _camera;
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
		_camera = new Camera(GraphicsDevice);
		//_camera.Position = new Vector3(0, 10, -20);
		_camera.LookAt(Vector3.Zero);
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

		_imGuiRenderer.BeginLayout(gameTime);

		// Вікно налаштувань камери
		ImGui.Begin("Camera Settings");

		var position = new System.Numerics.Vector3(
			_camera.Position.X,
			_camera.Position.Y,
			_camera.Position.Z);
		if (ImGui.DragFloat3("Position", ref position, 0.1f))
		{
			_camera.Position = new Vector3(
				position.X,
				position.Y,
				position.Z);
		}

		var rotation = new System.Numerics.Vector3(
			MathHelper.ToDegrees(_camera.Rotation.X),
			MathHelper.ToDegrees(_camera.Rotation.Y),
			MathHelper.ToDegrees(_camera.Rotation.Z));
		if (ImGui.DragFloat3("Rotation (degrees)", ref rotation, 0.1f))
		{
			_camera.Rotation = new Vector3(
				MathHelper.ToRadians(rotation.X),
				MathHelper.ToRadians(rotation.Y),
				MathHelper.ToRadians(rotation.Z));
		}

		var fov = MathHelper.ToDegrees(_camera.FieldOfView);
		if (ImGui.SliderFloat("Field of View", ref fov, 1f, 120f))
		{
			_camera.FieldOfView = MathHelper.ToRadians(fov);
		}

		ImGui.End();



		// Головне вікно редактора
		ImGui.Begin("Scene Editor");

		// Ваш існуючий код редактора

		ImGui.End();

		// Вікно зі списком об'єктів сцени
		ImGui.SetNextWindowPos(new System.Numerics.Vector2(
			GraphicsDevice.Viewport.Width - 300, 0),
			ImGuiCond.FirstUseEver);
		ImGui.SetNextWindowSize(new System.Numerics.Vector2(
			300, GraphicsDevice.Viewport.Height),
			ImGuiCond.FirstUseEver);

		ImGui.Begin("Scene Hierarchy",
			ImGuiWindowFlags.NoCollapse |
			ImGuiWindowFlags.NoMove |
			ImGuiWindowFlags.NoResize);

		


		//if (ImGui.BeginPopupContextWindow())
		//{
		//	if (ImGui.MenuItem("Add New Object"))
		//	{
		//		_sceneObjects.Add(new SceneObject($"New Object {_sceneObjects.Count}"));
		//	}
		//	ImGui.EndPopup();
		//}

		ImGui.End();


		DrawSceneHierarchy();

		// Відображення властивостей вибраного об'єкта
		if (_selectedObject != null)
		{
			//DrawObjectProperties();
		}



		_imGuiRenderer.EndLayout();

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

	private void DrawSceneHierarchy()
	{
		ImGui.Begin($"Scene Hierarchy - {_sceneName}");

		// Отримуємо всі кореневі об'єкти
		foreach (var obj in _currentScene.GetRootObjects())
		{
			DrawSceneObject(obj);
		}

		// Контекстне меню для сцени (правий клік на пустому місці)
		if (ImGui.BeginPopupContextWindow())
		{
			if (ImGui.MenuItem("Add Empty Object"))
			{
				var newObject = new SceneObject($"New Object {_currentScene.GetRootObjects().Count()}");
				_currentScene.RegisterObject(newObject);
			}
			ImGui.EndPopup();
		}

		ImGui.End();
	}

	private unsafe void DrawSceneObject(SceneObject obj)
	{
		// Налаштування прапорців для вузла дерева
		ImGuiTreeNodeFlags flags = ImGuiTreeNodeFlags.OpenOnArrow |
								 ImGuiTreeNodeFlags.OpenOnDoubleClick |
								 ImGuiTreeNodeFlags.SpanAvailWidth;

		// Додаємо прапорець Selected, якщо об'єкт вибраний
		if (obj == _selectedObject)
			flags |= ImGuiTreeNodeFlags.Selected;

		// Якщо об'єкт не має дочірніх елементів, робимо його листком
		if (obj.Children.Count == 0)
			flags |= ImGuiTreeNodeFlags.Leaf;

		// Системні об'єкти (камера, світло) виділяємо іншим кольором
		if (obj == _currentScene.MainCamera || obj == _currentScene.DirectionalLight)
		{
			ImGui.PushStyleColor(ImGuiCol.Text, new System.Numerics.Vector4(0.5f, 0.8f, 1.0f, 1.0f));
		}

		// Відображаємо вузол дерева
		bool isOpen = ImGui.TreeNodeEx($"{obj.Name}###{obj.Id}", flags);

		// Повертаємо колір тексту до стандартного
		if (obj == _currentScene.MainCamera || obj == _currentScene.DirectionalLight)
		{
			ImGui.PopStyleColor();
		}

		// Обробка кліку на об'єкті
		if (ImGui.IsItemClicked())
		{
			_selectedObject = obj;
		}

		// Контекстне меню для об'єкта (правий клік)
		if (ImGui.BeginPopupContextItem())
		{
			if (ImGui.MenuItem("Add Child"))
			{
				var newChild = new SceneObject($"New Child {obj.Children.Count}");
				_currentScene.RegisterObject(newChild);
				obj.AddChild(newChild);
			}

			if (ImGui.MenuItem("Delete") &&
				obj != _currentScene.MainCamera &&
				obj != _currentScene.DirectionalLight)
			{
				if (_selectedObject == obj)
					_selectedObject = null;

				_currentScene.UnregisterObject(obj.Id);
				ImGui.EndPopup();
				if (isOpen)
					ImGui.TreePop();
				return;
			}

			if (ImGui.MenuItem("Rename"))
			{
				// TODO: Додати логіку перейменування
			}

			ImGui.EndPopup();
		}

		// Підтримка Drag & Drop
		if (ImGui.BeginDragDropSource())
		{
			ImGui.SetDragDropPayload("SCENE_OBJECT", IntPtr.Zero, 0);
			ImGui.Text($"Moving {obj.Name}");
			_draggedObject = obj;
			ImGui.EndDragDropSource();
		}

		if (ImGui.BeginDragDropTarget())
		{
			var payload = ImGui.AcceptDragDropPayload("SCENE_OBJECT");
			if (payload.NativePtr != null && _draggedObject != null)
			{
				if (_draggedObject != obj && !IsChildOf(_draggedObject, obj))
				{
					_draggedObject.SetParent(obj);
				}
			}
			ImGui.EndDragDropTarget();
		}

		// Якщо вузол відкритий, рекурсивно відображаємо дочірні елементи
		if (isOpen)
		{
			foreach (var child in obj.Children)
			{
				DrawSceneObject(child);
			}
			ImGui.TreePop();
		}
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
