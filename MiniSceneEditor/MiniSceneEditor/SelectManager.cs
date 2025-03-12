using System.Collections.Generic;
using Microsoft.Core;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using MiniSceneEditor.Gizmo;
using MiniSceneEditor;
using ImGuiNET;
using System;
using MiniSceneEditor.Camera;
using System.Linq;
using MiniSceneEditor.Core.Components.Impls;

public class SelectManager
{
	private readonly Scene _scene;
	private readonly List<SceneObject> _selectedObjects = new List<SceneObject>();
	private SceneObject _selectedObject = null;
	private SceneObject _hoveredObject;

	//public event Action<List<SceneObject>> OnMultiSelectionChanged;
	public event Action<SceneObject> OnSingleSelectionChanged;
	public IReadOnlyList<SceneObject> SelectedObjects => _selectedObjects;
	public bool HasSelection => _selectedObjects.Count > 0 || _selectedObject != null;

	public SceneObject SelectedSceneObject => _selectedObject;

	// Додаємо підтримку вибору елементів меша
	private EditableMeshComponent _currentEditableMesh;

	public SelectManager(Scene scene)
	{
		_scene = scene;
	}

	public void HandleMeshElementSelection(Ray ray)
	{
		if (_selectedObject != null && _selectedObject.HasComponent<EditableMeshComponent>())
		{
			var meshComponent = _selectedObject.GetComponent<EditableMeshComponent>();
			_currentEditableMesh = meshComponent;

			// Далі делегуємо вибір елементів до компонента меша
			switch (meshComponent.CurrentEditMode)
			{
				case EditMode.Vertex:
					// Логіка вибору вершин
					break;
				case EditMode.Edge:
					// Логіка вибору ребер
					break;
				case EditMode.Face:
					// Логіка вибору граней
					break;
			}
		}
	}

	private void SelectObject(SceneObject obj, bool addToSelection = false)
	{
		SelectObject(obj);
		//if (obj == null)
		//{
		//	if (!addToSelection)
		//	{
		//		ClearSelection();
		//	}
		//	return;
		//}

		//if (addToSelection)
		//{
		//	if (_selectedObjects.Contains(obj))
		//	{
		//		_selectedObjects.Remove(obj);
		//	}
		//	else
		//	{
		//		_selectedObjects.Add(obj);
		//	}
		//}
		//else
		//{
		//	//ClearSelection();
		//	_selectedObjects.Add(obj);
		//}

		
	}

	//public void ClearSelection()
	//{
	//	_selectedObjects.Clear();
	//	OnSelectionChanged?.Invoke(_selectedObjects);
	//}

	public void HandleSceneHierarchySelection(SceneObject obj, bool isCtrlPressed)
	{
		SelectObject(obj, isCtrlPressed);
	}

	public void Update(CameraMatricesState camera)
	{
		if (ImGui.GetIO().WantCaptureMouse)
			return;

		// Обробляємо вибір тільки при кліку лівою кнопкою миші
		if (InputManager.Instance.IsMouseButtonPressed(ButtonState.Pressed))
		{
			Ray ray = CreatePickingRay(InputManager.Instance.CurrentState.MousePosition, in camera);
			var hitObject = FindNearestObject(ray);

			// Змінюємо вибір тільки якщо попали в якийсь об'єкт
			if (hitObject != null)
			{
				SelectObject(hitObject);
			}
			// Якщо не попали в об'єкт - залишаємо поточний вибір без змін
		}
	}

	public void SelectObject(SceneObject obj)
	{
		if (obj != null && (_selectedObject == null || _selectedObject.Id != obj.Id)) // Додаємо перевірку на null
		{
			_selectedObject = null;
			_selectedObject = obj;
			OnSingleSelectionChanged?.Invoke(_selectedObject);
			System.Diagnostics.Debug.WriteLine($"Selected object changed to: {obj.Name}");
		}

		if (obj == null)
			OnSingleSelectionChanged?.Invoke(obj);
	}

	// Явний метод для очищення вибору, якщо потрібно
	public void ClearSelection()
	{
		if (_selectedObjects != null)
		{
			_selectedObjects.Clear();
			OnSingleSelectionChanged?.Invoke(null);
			System.Diagnostics.Debug.WriteLine("Selection cleared");
		}
	}

	// Отримати перший вибраний об'єкт (активний)
	public SceneObject GetActiveObject()
	{
		return _selectedObjects.FirstOrDefault();
	}

	// Перевірити, чи є конкретний об'єкт вибраним
	public bool IsSelected(SceneObject obj)
	{
		return _selectedObjects.Contains(obj);
	}


	public void DrawDebugInfo()
	{
		if (ImGui.Begin("Selection Debug"))
		{
			if (_selectedObjects.Any())
			{
				ImGui.Text($"Selected: {_selectedObjects[0].Name}");
				ImGui.Text($"Position: {_selectedObjects[0].Transform.Position}");
				ImGui.Text($"Rotation: {_selectedObjects[0].Transform.Rotation}");
				ImGui.Text($"Scale: {_selectedObjects[0].Transform.Scale}");
			}
			else
			{
				ImGui.Text("No object selected");
			}
		}
		ImGui.End();
	}

	private Ray CreatePickingRay(in Vector2 mousePosition, in CameraMatricesState camera)
	{
		var viewport = _scene.GraphicsDevice.Viewport;
		Vector3 nearPoint = viewport.Unproject(
			new Vector3(mousePosition, 0),
			camera.ProjectionMatrix,
			camera.ViewMatrix,
			Matrix.Identity);

		Vector3 farPoint = viewport.Unproject(
			new Vector3(mousePosition, 1),
			camera.ProjectionMatrix,
			camera.ViewMatrix,
			Matrix.Identity);

		Vector3 direction = Vector3.Normalize(farPoint - nearPoint);
		return new Ray(nearPoint, direction);
	}

	private SceneObject FindNearestObject(Ray ray)
	{
		float nearestDistance = float.MaxValue;
		SceneObject nearestObject = null;

		foreach (var obj in _scene.GetRootObjects())
		{
			CheckObjectAndChildren(obj, ray, ref nearestDistance, ref nearestObject);
		}

		return nearestObject;
	}

	private void CheckObjectAndChildren(SceneObject obj, Ray ray, ref float nearestDistance, ref SceneObject nearestObject)
	{
		if (CheckObjectIntersection(obj, ray, out float distance))
		{
			if (distance < nearestDistance)
			{
				nearestDistance = distance;
				nearestObject = obj;
			}
		}

		foreach (var child in obj.Children)
		{
			CheckObjectAndChildren(child, ray, ref nearestDistance, ref nearestObject);
		}
	}

	private bool CheckObjectIntersection(SceneObject obj, Ray ray, out float distance)
	{
		// Отримуємо обмежувальну сферу об'єкта
		BoundingSphere bounds = GetObjectBounds(obj);

		// Трансформуємо сферу відповідно до світової матриці об'єкта
		bounds = TransformBoundingSphere(bounds, obj.Transform.GetWorldMatrix());

		// Перевіряємо перетин променя зі сферою
		float? intersection = ray.Intersects(bounds);
		if (intersection.HasValue)
		{
			distance = intersection.Value;
			return true;
		}

		distance = float.MaxValue;
		return false;
	}

	private BoundingSphere GetObjectBounds(SceneObject obj)
	{
		// За замовчуванням використовуємо сферу радіусом 1
		// В реальному проекті тут має бути логіка отримання
		// реальних меж об'єкта
		return new BoundingSphere(Vector3.Zero, 1f);
	}

	private BoundingSphere TransformBoundingSphere(BoundingSphere sphere, Matrix worldMatrix)
	{
		Vector3 center = Vector3.Transform(sphere.Center, worldMatrix);
		float radius = sphere.Radius * GetMaxScale(worldMatrix);
		return new BoundingSphere(center, radius);
	}

	private float GetMaxScale(Matrix matrix)
	{
		Vector3 scale = new Vector3(
			new Vector4(matrix.M11, matrix.M12, matrix.M13, 0).Length(),
			new Vector4(matrix.M21, matrix.M22, matrix.M23, 0).Length(),
			new Vector4(matrix.M31, matrix.M32, matrix.M33, 0).Length()
		);
		return Math.Max(Math.Max(scale.X, scale.Y), scale.Z);
	}
}