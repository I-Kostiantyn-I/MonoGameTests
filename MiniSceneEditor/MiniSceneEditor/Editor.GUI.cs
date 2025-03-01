using ImGuiNET;
using Microsoft.Core;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using MiniSceneEditor.Commands;
using MiniSceneEditor.Core.Components;
using MiniSceneEditor.Core.Components.Impls;
using System;
using System.Linq;

namespace MiniSceneEditor;

public partial class Editor
{
	private bool _showInspector = true;
	private bool _showHierarchy = true;

	private static void ComponentRegister()
	{
		ComponentEditorFactory.RegisterEditor<TransformComponent, TransformComponentEditor>();
		ComponentEditorFactory.RegisterEditor<MeshComponent, MeshComponentEditor>();
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


		

		// Відображення властивостей вибраного об'єкта
		

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
		if (ImGui.BeginMainMenuBar())
		{
			if (ImGui.BeginMenu("Create"))
			{
				if (ImGui.MenuItem("Box"))
				{
					CreateBoxObject();
				}
				ImGui.EndMenu();
			}

			if (ImGui.BeginMenu("Window"))
			{
				if (ImGui.MenuItem("Inspector", "", _showInspector))
					_showInspector = !_showInspector;

				if (ImGui.MenuItem("Scene Hierarchy", "", _showHierarchy))
					_showHierarchy = !_showHierarchy;

				ImGui.EndMenu();
			}
			ImGui.EndMainMenuBar();
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
}
