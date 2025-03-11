using BepuPhysics;
using ImGuiNET;
using Microsoft.Core;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using MiniSceneEditor.Commands;
using MiniSceneEditor.Core.Components;
using MiniSceneEditor.Core.Components.Impls;
using MiniSceneEditor.Gizmo;
using System;
using System.Linq;

namespace MiniSceneEditor;

public partial class Editor
{
	private bool _showInspector = true;
	private bool _showHierarchy = true;

	// Додаємо змінні для параметрів редагованого боксу
	private Vector3 _editableBoxSize = new Vector3(2, 2, 2);
	private int _editableBoxSegmentsX = 1;
	private int _editableBoxSegmentsY = 1;
	private int _editableBoxSegmentsZ = 1;
	private bool _showEditableBoxDialog = false;

	private static void ComponentRegister()
	{
		ComponentEditorFactory.RegisterEditor<TransformComponent, TransformComponentEditor>();
		ComponentEditorFactory.RegisterEditor<MeshComponent, MeshComponentEditor>();
		ComponentEditorFactory.RegisterEditor<EditableMeshComponent, EditableMeshComponentEditor>();
		ComponentEditorFactory.RegisterEditor<ColliderComponent, ColliderComponentEditor>();
		ComponentEditorFactory.RegisterEditor<RigidbodyComponent, RigidbodyComponentEditor>();
		//ComponentEditorFactory.RegisterEditor<CameraComponent, CameraComponentEditor>();
	}

	private void DrawWindowWithCloseButton(string title, Action drawContent, ref bool isOpen)
	{
		// Додаємо флаг для кнопки закриття
		ImGuiWindowFlags flags = ImGuiWindowFlags.None;

		if (ImGui.Begin(title, ref isOpen, flags))
		{
			drawContent();
		}
		ImGui.End();
	}

	private void DrawGUI(GameTime gameTime)
	{

		_imGuiRenderer.BeginLayout(gameTime);

		DrawMainMenu();
		if (_showInspector)
		{
			DrawWindowWithCloseButton("Inspector", () =>
			{
				DrawInspector();
			}, ref _showInspector);
		}

		//DrawMeshEditTools();

		// Вікно ієрархії
		if (_showHierarchy)
		{
			DrawWindowWithCloseButton("Scene Hierarchy", () =>
			{
				DrawSceneHierarchy();
				//foreach (var obj in _currentScene.GetRootObjects())
				//{
				//	DrawHierarchyNode(obj);
				//}
			}, ref _showHierarchy);
		}

		DrawEditorCameraSettings();

		_snapSystem.DrawGUI();

		if (Keyboard.GetState().IsKeyDown(Keys.F3)) // або інша клавіша для debug режиму
		{
			_selectManager.DrawDebugInfo();
			_gizmoSystem.DrawDebugInfo();
		}

		_imGuiRenderer.EndLayout();
	}

	private void DrawMainMenu()
	{
		// Головне меню
		if (ImGui.BeginMainMenuBar())
		{
			if (ImGui.BeginMenu("File"))
			{
				if (ImGui.MenuItem("New Scene"))
				{
					_currentScene = new Scene(GraphicsDevice);
					_sceneName = "noname";
				}

				if (ImGui.MenuItem("Save Scene"))
				{
					// Код збереження сцени
				}

				if (ImGui.MenuItem("Load Scene"))
				{
					// Код завантаження сцени
				}

				if (ImGui.MenuItem("Exit"))
				{
					Exit();
				}

				ImGui.EndMenu();
			}

			if (ImGui.BeginMenu("Create"))
			{
				if (ImGui.MenuItem("Box"))
				{
					CreateBoxObject();
				}

				// Додаємо новий пункт меню для створення редагованого боксу
				if (ImGui.MenuItem("Editable Box"))
				{
					ShowEditableBoxDialog();
				}

				ImGui.EndMenu();
			}

			if (ImGui.BeginMenu("Edit"))
			{
				//if (ImGui.MenuItem("Undo", "Ctrl+Z", false, _commandManager.CanUndo()))
				//{
				//	_commandManager.Undo();
				//}

				//if (ImGui.MenuItem("Redo", "Ctrl+Y", false, _commandManager.CanRedo()))
				//{
				//	_commandManager.Redo();
				//}

				ImGui.Separator();

				// Додаємо підменю для режимів редагування меша
				if (_selectManager.SelectObject != null && _selectManager.SelectedSceneObject.GetComponent<EditableMeshComponent>() != null)
				{
					var editableMesh = _selectManager.SelectedSceneObject.GetComponent<EditableMeshComponent>();

					if (ImGui.BeginMenu("Edit Mode"))
					{
						if (ImGui.MenuItem("Vertex", null, editableMesh.CurrentEditMode == EditMode.Vertex))
						{
							editableMesh.CurrentEditMode = EditMode.Vertex;
							// Очищаємо поточний вибір
							editableMesh.SelectedVertices.Clear();
							editableMesh.SelectedEdges.Clear();
							editableMesh.SelectedFaces.Clear();
						}

						if (ImGui.MenuItem("Edge", null, editableMesh.CurrentEditMode == EditMode.Edge))
						{
							editableMesh.CurrentEditMode = EditMode.Edge;
							// Очищаємо поточний вибір
							editableMesh.SelectedVertices.Clear();
							editableMesh.SelectedEdges.Clear();
							editableMesh.SelectedFaces.Clear();
						}

						if (ImGui.MenuItem("Face", null, editableMesh.CurrentEditMode == EditMode.Face))
						{
							editableMesh.CurrentEditMode = EditMode.Face;
							// Очищаємо поточний вибір
							editableMesh.SelectedVertices.Clear();
							editableMesh.SelectedEdges.Clear();
							editableMesh.SelectedFaces.Clear();
						}

						ImGui.EndMenu();
					}

					ImGui.Separator();

					// Додаємо операції для редагування меша
					if (ImGui.MenuItem("Delete Selected", "Del"))
					{
						if (editableMesh != null)
						{
							switch (editableMesh.CurrentEditMode)
							{
								case EditMode.Vertex:
									editableMesh.DeleteSelectedVertices();
									break;
								case EditMode.Edge:
									editableMesh.DeleteSelectedEdges();
									break;
								case EditMode.Face:
									editableMesh.DeleteSelectedFaces();
									break;
							}
						}
					}

					if (ImGui.MenuItem("Extrude", "X", false, editableMesh.CurrentEditMode == EditMode.Face && editableMesh.SelectedFaces.Count > 0))
					{
						// Екструзія вибраних граней
						if (editableMesh != null)
						{
							// Створюємо гізмо для редагування меша, якщо воно ще не створене
							_gizmoSystem.SetCurrentGizmo(GizmoSystem.GizmoType.MeshEdit);
							MeshEditGizmo meshGizmo = (MeshEditGizmo)_gizmoSystem.CurrentGizmo;
							if (meshGizmo != null)
							{
								meshGizmo.ExtrudeSelected(0.5f);
							}
						}
					}
				}

				ImGui.EndMenu();
			}

			ImGui.EndMainMenuBar();
		}

		// Відображення діалогу налаштування параметрів редагованого боксу
		if (_showEditableBoxDialog)
		{
			ImGui.SetNextWindowSize(new System.Numerics.Vector2(300, 200), ImGuiCond.FirstUseEver);
			if (ImGui.Begin("Editable Box Settings", ref _showEditableBoxDialog))
			{
				// Конвертуємо Vector3 в System.Numerics.Vector3 для ImGui
				System.Numerics.Vector3 boxSize = new System.Numerics.Vector3(
					_editableBoxSize.X,
					_editableBoxSize.Y,
					_editableBoxSize.Z
				);

				// Поля для розміру боксу
				if (ImGui.DragFloat3("Size", ref boxSize, 0.1f, 0.1f, 10.0f))
				{
					_editableBoxSize = new Vector3(
						boxSize.X,
						boxSize.Y,
						boxSize.Z
					);
				}

				// Поля для кількості сегментів
				ImGui.DragInt("Segments X", ref _editableBoxSegmentsX, 0.1f, 1, 10);
				ImGui.DragInt("Segments Y", ref _editableBoxSegmentsY, 0.1f, 1, 10);
				ImGui.DragInt("Segments Z", ref _editableBoxSegmentsZ, 0.1f, 1, 10);

				ImGui.Separator();

				// Кнопки для створення або скасування
				if (ImGui.Button("Create"))
				{
					CreateEditableBoxObject();
					_showEditableBoxDialog = false;
				}

				ImGui.SameLine();

				if (ImGui.Button("Cancel"))
				{
					_showEditableBoxDialog = false;
				}
			}
			ImGui.End();
		}

		// Інші елементи GUI...

	
		//if (ImGui.BeginMainMenuBar())
		//{
		//	if (ImGui.BeginMenu("Create"))
		//	{
		//		if (ImGui.MenuItem("Box"))
		//		{
		//			CreateBoxObject();
		//		}
		//		ImGui.EndMenu();
		//	}

		//	if (ImGui.BeginMenu("Window"))
		//	{
		//		if (ImGui.MenuItem("Inspector", "", _showInspector))
		//			_showInspector = !_showInspector;

		//		if (ImGui.MenuItem("Scene Hierarchy", "", _showHierarchy))
		//			_showHierarchy = !_showHierarchy;

		//		ImGui.EndMenu();
		//	}
		//	ImGui.EndMainMenuBar();
		//}
	}

	// Метод для додавання керування фізикою в меню
	private void DrawPhysicsMenu()
	{
		if (ImGui.BeginMenu("Physics"))
		{
			if (ImGui.MenuItem("Enable Physics", "", _physicsEnabled))
			{
				_physicsEnabled = !_physicsEnabled;
				_physicsSystem.SetPaused(!_physicsEnabled);
			}

			if (ImGui.MenuItem("Reset Physics"))
			{
				_physicsSystem.Reset();
			}

			if (ImGui.BeginMenu("Settings"))
			{
				var gravity = _physicsSystem.Gravity;
				if (ImGui.DragFloat3("Gravity", ref gravity, 0.1f))
				{
					_physicsSystem.Gravity = gravity;
				}

				var timeStep = _physicsSystem.FixedTimeStep;
				if (ImGui.DragFloat("Time Step", ref timeStep, 0.001f, 0.001f, 0.1f))
				{
					_physicsSystem.FixedTimeStep = timeStep;
				}

				ImGui.EndMenu();
			}

			ImGui.EndMenu();
		}
	}

	private void DrawSceneHierarchy()
	{
		ImGui.Begin("Scene Hierarchy");

		foreach (var obj in _currentScene.GetRootObjects())
		{
			DrawHierarchyNode(obj);
		}

		if (ImGui.BeginPopupContextWindow())
		{
			if (ImGui.MenuItem("Add Empty Object"))
			{
				var newObj = new SceneObject($"New Object {_currentScene.GetRootObjects().Count()}");
				_currentScene.RegisterObject(newObj);
			}
			ImGui.EndPopup();
		}

		ImGui.End();
	}

	private void DrawHierarchyNode(SceneObject obj)
	{
		ImGuiTreeNodeFlags flags = ImGuiTreeNodeFlags.OpenOnArrow |
								 ImGuiTreeNodeFlags.SpanAvailWidth;

		if (obj.Children.Count == 0)
			flags |= ImGuiTreeNodeFlags.Leaf;

		if (_selectManager.SelectedObjects.Contains(obj))
			flags |= ImGuiTreeNodeFlags.Selected;

		bool isOpen = ImGui.TreeNodeEx($"{obj.Name}###{obj.Id}", flags);

		if (ImGui.IsItemClicked())
		{
			_selectManager.HandleSceneHierarchySelection(obj,
				ImGui.GetIO().KeyAlt);
		}

		if (ImGui.BeginPopupContextItem())
		{
			if (ImGui.MenuItem("Add Child"))
			{
				var newChild = new SceneObject($"New Child {obj.Children.Count}");
				_currentScene.RegisterObject(newChild);
				obj.AddChild(newChild);
			}

			if (ImGui.MenuItem("Delete"))
			{
				if (_selectManager.SelectedObjects.Contains(obj))
					_selectManager.ClearSelection();
				_currentScene.UnregisterObject(obj.Id);
			}

			if (ImGui.MenuItem("Rename"))
			{
				// TODO: Додати логіку перейменування
			}

			ImGui.EndPopup();
		}

		if (isOpen)
		{
			foreach (var child in obj.Children)
			{
				DrawHierarchyNode(child);
			}
			ImGui.TreePop();
		}
	}






	

	private void DrawEmpty()
	{
		// Головне вікно редактора
		ImGui.Begin("Scene Editor");

		// Ваш існуючий код редактора

		ImGui.End();
	}

	private void DrawStaticSettingsWindow()
	{
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
	}

	private void DrawEditorCameraSettings()
	{
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
			MathHelper.ToDegrees(_camera.RotationRadians.X),
			MathHelper.ToDegrees(_camera.RotationRadians.Y),
			MathHelper.ToDegrees(_camera.RotationRadians.Z));
		if (ImGui.DragFloat3("Rotation (degrees)", ref rotation, 0.1f))
		{
			_camera.RotationRadians = new Vector3(
				MathHelper.ToRadians(rotation.X),
				MathHelper.ToRadians(rotation.Y),
				MathHelper.ToRadians(rotation.Z));
		}

		//var fov = MathHelper.ToDegrees(_camera.FieldOfView);
		//if (ImGui.SliderFloat("Field of View", ref fov, 1f, 120f))
		//{
		//	_camera.FieldOfView = MathHelper.ToRadians(fov);
		//}

		ImGui.End();
	}

	// У вікні інспектора
	private void DrawInspector()
	{
		if (_selectManager.SelectedSceneObject == null) return;

		ImGui.Begin("Inspector");

		foreach (var component in _selectManager.SelectedSceneObject.Components)
		{
			if (ImGui.CollapsingHeader(component.GetType().Name))
			{
				var editor = ComponentEditorFactory.CreateEditor(component);
				editor?.OnInspectorGUI();
			}
		}

		ImGui.End();
	}

	private void DrawSceneHierarchy2()
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
		if (obj == _selectManager.SelectedSceneObject)
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

		// Обробка подвійного кліку
		if (ImGui.IsItemHovered() && ImGui.IsMouseDoubleClicked(0))
		{
			_selectManager.SelectObject(obj);
			var keyboard = Keyboard.GetState();
			bool withFocus = keyboard.IsKeyDown(Keys.LeftShift) ||
							keyboard.IsKeyDown(Keys.RightShift);

			_camera.FocusOn(obj.Transform.Position, withTransition: keyboard.IsKeyDown(Keys.LeftShift));
		}


		// Повертаємо колір тексту до стандартного
		if (obj == _currentScene.MainCamera || obj == _currentScene.DirectionalLight)
		{
			ImGui.PopStyleColor();
		}

		// Обробка кліку на об'єкті
		if (ImGui.IsItemClicked())
		{
			_selectManager.SelectObject(obj);
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
				if (_selectManager.SelectedSceneObject == obj)
					_selectManager.SelectObject(null);

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

	private void DrawCommandHistory()
	{
		if (ImGui.Begin("History"))
		{
			if (ImGui.Button("Undo"))
				_commandManager.Undo();

			ImGui.SameLine();

			if (ImGui.Button("Redo"))
				_commandManager.Redo();

			ImGui.Separator();

			// Можна додати відображення списку команд
			// TODO: Додати доступ до списку команд в CommandManager
		}
		ImGui.End();
	}

	#region ProBuilder
	// Додаємо метод для створення редагованого боксу
	private void CreateEditableBoxObject()
	{
		_log.Log("Creating editable box object");

		// Створюємо новий об'єкт сцени
		var boxObject = new SceneObject($"EditableBox_{_currentScene.GetRootObjects().Count()}");
		_log.Log($"Creating editable box object: {boxObject.Name}");

		// Додаємо компонент EditableMeshComponent замість стандартного MeshComponent
		var meshComponent = boxObject.AddComponent<EditableMeshComponent>();
		_log.Log("EditableMeshComponent added");

		meshComponent.SetGraphicsDevice(GraphicsDevice);
		_log.Log("GraphicsDevice set");

		// Генеруємо меш боксу з сегментами
		GenerateEditableBoxMesh(meshComponent, _editableBoxSize, _editableBoxSegmentsX, _editableBoxSegmentsY, _editableBoxSegmentsZ);
		_log.Log("Editable box mesh generated");

		// Реєструємо об'єкт у сцені
		_currentScene.RegisterObject(boxObject);
		_log.Log("Object registered in scene");

		// Вибираємо створений об'єкт
		_selectManager.SelectObject(boxObject);
		_log.Log("Object selected");

		// Встановлюємо режим редагування вершин за замовчуванням
		if (meshComponent is EditableMeshComponent editableMesh)
		{
			editableMesh.CurrentEditMode = EditMode.Vertex;
			_log.Log("Edit mode set to Vertex");
		}
	}

	// Метод для генерації меша боксу з сегментами
	private void GenerateEditableBoxMesh(EditableMeshComponent meshComponent, Vector3 size, int segmentsX, int segmentsY, int segmentsZ)
	{
		// Очищаємо попередні дані меша
		meshComponent.Vertices.Clear();
		meshComponent.Indices.Clear();

		// Обчислюємо розмір одного сегмента
		float segmentWidth = size.X / segmentsX;
		float segmentHeight = size.Y / segmentsY;
		float segmentDepth = size.Z / segmentsZ;

		// Створюємо вершини
		for (int y = 0; y <= segmentsY; y++)
		{
			for (int z = 0; z <= segmentsZ; z++)
			{
				for (int x = 0; x <= segmentsX; x++)
				{
					// Обчислюємо позицію вершини
					float xPos = -size.X / 2 + x * segmentWidth;
					float yPos = -size.Y / 2 + y * segmentHeight;
					float zPos = -size.Z / 2 + z * segmentDepth;

					meshComponent.AddVertex(new Vector3(xPos, yPos, zPos));
				}
			}
		}

		// Кількість вершин у одному ряду по X
		int xVertCount = segmentsX + 1;
		// Кількість вершин у одному шарі (X-Z площина)
		int layerVertCount = (segmentsX + 1) * (segmentsZ + 1);

		// Створюємо індекси для трикутників
		// Проходимо по всіх сегментах
		for (int y = 0; y < segmentsY; y++)
		{
			for (int z = 0; z < segmentsZ; z++)
			{
				for (int x = 0; x < segmentsX; x++)
				{
					// Індекси вершин поточного квадрата
					int i0 = y * layerVertCount + z * xVertCount + x;
					int i1 = i0 + 1;
					int i2 = i0 + xVertCount;
					int i3 = i2 + 1;
					int i4 = i0 + layerVertCount;
					int i5 = i4 + 1;
					int i6 = i4 + xVertCount;
					int i7 = i6 + 1;

					// Верхня грань (Y+)
					if (y == segmentsY - 1)
					{
						meshComponent.AddTriangle(i4, i5, i7);
						meshComponent.AddTriangle(i4, i7, i6);
					}

					// Нижня грань (Y-)
					if (y == 0)
					{
						meshComponent.AddTriangle(i0, i2, i1);
						meshComponent.AddTriangle(i1, i2, i3);
					}

					// Передня грань (Z+)
					if (z == segmentsZ - 1)
					{
						meshComponent.AddTriangle(i2, i6, i3);
						meshComponent.AddTriangle(i3, i6, i7);
					}

					// Задня грань (Z-)
					if (z == 0)
					{
						meshComponent.AddTriangle(i0, i1, i4);
						meshComponent.AddTriangle(i1, i5, i4);
					}

					// Права грань (X+)
					if (x == segmentsX - 1)
					{
						meshComponent.AddTriangle(i1, i3, i5);
						meshComponent.AddTriangle(i3, i7, i5);
					}

					// Ліва грань (X-)
					if (x == 0)
					{
						meshComponent.AddTriangle(i0, i4, i2);
						meshComponent.AddTriangle(i2, i4, i6);
					}
				}
			}
		}
	}

	// Додаємо метод для відображення діалогу налаштування параметрів боксу
	private void ShowEditableBoxDialog()
	{
		_showEditableBoxDialog = true;
	}
	#endregion


	//private void DrawMeshEditTools()
	//{
	//	if (_selectManager.SelectedSceneObject != null &&
	//		_selectManager.SelectedSceneObject.HasComponent<EditableMeshComponent>())
	//	{
	//		var meshComponent = _selectManager.SelectedSceneObject.GetComponent<EditableMeshComponent>();

	//		ImGui.Begin("Mesh Edit Tools");

	//		// Режими редагування
	//		ImGui.SameLine();
	//		if (ImGui.Button("Vertex Mode"))
	//			meshComponent.CurrentEditMode = EditMode.Vertex;

	//		ImGui.SameLine();
	//		if (ImGui.Button("Edge Mode"))
	//			meshComponent.CurrentEditMode = EditMode.Edge;

	//		ImGui.SameLine();
	//		if (ImGui.Button("Face Mode"))
	//			meshComponent.CurrentEditMode = EditMode.Face;

	//		// Інструменти залежно від режиму
	//		switch (meshComponent.CurrentEditMode)
	//		{
	//			case EditMode.Vertex:
	//				if (ImGui.Button("Create Vertex"))
	//				{
	//					// Логіка створення вершини
	//				}
	//				ImGui.SameLine();
	//				if (ImGui.Button("Merge Vertices"))
	//				{
	//					// Логіка об'єднання вершин
	//				}
	//				break;

	//			case EditMode.Edge:
	//				if (ImGui.Button("Bevel"))
	//				{
	//					// Логіка скошення ребра
	//				}
	//				ImGui.SameLine();
	//				if (ImGui.Button("Subdivide"))
	//				{
	//					// Логіка поділу ребра
	//				}
	//				break;

	//			case EditMode.Face:
	//				if (ImGui.Button("Extrude"))
	//				{
	//					// Логіка витягування грані
	//				}
	//				ImGui.SameLine();
	//				if (ImGui.Button("Inset"))
	//				{
	//					// Логіка вставки грані
	//				}
	//				break;
	//		}

	//		ImGui.End();
	//	}
	//}
}
