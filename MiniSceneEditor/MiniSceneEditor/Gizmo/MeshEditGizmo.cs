using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework;
using MiniSceneEditor.Camera;
using MiniSceneEditor.Commands;
using MiniSceneEditor.Core.Components.Impls;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MiniSceneEditor.Gizmo;

public class MeshEditGizmo : IGizmo
{
	private EditableMeshComponent _targetMesh;
	private GraphicsDevice _graphicsDevice;
	private BasicEffect _effect;
	private CommandManager _commandManager;

	// Кольори для різних елементів гізмо
	private Color _primaryColor = Color.White;
	private Color _secondaryColor = Color.Gray;
	private Color _highlightColor = Color.Yellow;

	// Візуальні елементи для вершин, ребер та граней
	private VertexBuffer _vertexMarkersBuffer;
	private VertexBuffer _edgeMarkersBuffer;
	private VertexBuffer _faceMarkersBuffer;

	public MeshEditGizmo(GraphicsDevice graphicsDevice, CommandManager commandManager, SnapSystem snapSystem)
	{
		_graphicsDevice = graphicsDevice;
		_commandManager = commandManager;
		_effect = new BasicEffect(graphicsDevice) { VertexColorEnabled = true };
	}

	public void SetTarget(EditableMeshComponent mesh)
	{
		_targetMesh = mesh;
		UpdateVisuals();
	}

	// Метод для зміни режиму редагування
	public void SetEditMode(EditMode mode)
	{
		if (_targetMesh != null)
		{
			_targetMesh.CurrentEditMode = mode;
			// Очищаємо вибір при зміні режиму
			_targetMesh.SelectedVertices.Clear();
			_targetMesh.SelectedEdges.Clear();
			_targetMesh.SelectedFaces.Clear();
		}
	}

	// Реалізація IGizmo.SetColor
	public void SetColor(Color primaryColor, Color secondaryColor, Color highlightColor)
	{
		_primaryColor = primaryColor;
		_secondaryColor = secondaryColor;
		_highlightColor = highlightColor;
		UpdateVisuals();
	}

	// Метод для видалення вибраних елементів
	public void DeleteSelected()
	{
		if (_targetMesh == null)
			return;

		switch (_targetMesh.CurrentEditMode)
		{
			case EditMode.Vertex:
				_targetMesh.DeleteSelectedVertices();
				break;
			case EditMode.Edge:
				_targetMesh.DeleteSelectedEdges();
				break;
			case EditMode.Face:
				_targetMesh.DeleteSelectedFaces();
				break;
		}

		UpdateVisuals();
	}

	// Метод для переміщення вибраних елементів
	public void MoveSelected(Vector3 offset)
	{
		if (_targetMesh == null)
			return;

		switch (_targetMesh.CurrentEditMode)
		{
			case EditMode.Vertex:
				_targetMesh.MoveSelectedVertices(offset);
				break;
			case EditMode.Edge:
				_targetMesh.MoveSelectedEdges(offset);
				break;
			case EditMode.Face:
				_targetMesh.MoveSelectedFaces(offset);
				break;
		}

		UpdateVisuals();
	}

	// Метод для екструзії вибраних елементів
	public void ExtrudeSelected(float distance)
	{
		if (_targetMesh == null)
			return;

		// Реалізуємо тільки екструзію граней для простоти
		if (_targetMesh.CurrentEditMode == EditMode.Face && _targetMesh.SelectedFaces.Count > 0)
		{
			// Для кожної вибраної грані
			List<int> newFaceIndices = new List<int>();

			foreach (int faceIndex in _targetMesh.SelectedFaces.ToList()) // Використовуємо ToList() щоб уникнути модифікації під час ітерації
			{
				int baseIndex = faceIndex * 3;

				// Отримуємо індекси вершин трикутника
				int idx1 = _targetMesh.Indices[baseIndex];
				int idx2 = _targetMesh.Indices[baseIndex + 1];
				int idx3 = _targetMesh.Indices[baseIndex + 2];

				// Отримуємо вершини трикутника
				Vector3 v1 = _targetMesh.Vertices[idx1];
				Vector3 v2 = _targetMesh.Vertices[idx2];
				Vector3 v3 = _targetMesh.Vertices[idx3];

				// Обчислюємо нормаль грані
				Vector3 edge1 = v2 - v1;
				Vector3 edge2 = v3 - v1;
				Vector3 normal = Vector3.Normalize(Vector3.Cross(edge1, edge2));

				// Створюємо нові вершини, зміщені на відстань у напрямку нормалі
				Vector3 offset = normal * distance;

				// Додаємо нові вершини
				int newIdx1 = _targetMesh.AddVertex(v1 + offset);
				int newIdx2 = _targetMesh.AddVertex(v2 + offset);
				int newIdx3 = _targetMesh.AddVertex(v3 + offset);

				// Додаємо бічні грані, що з'єднують старі та нові вершини
				_targetMesh.AddTriangle(idx1, idx2, newIdx1);
				_targetMesh.AddTriangle(newIdx1, idx2, newIdx2);

				_targetMesh.AddTriangle(idx2, idx3, newIdx2);
				_targetMesh.AddTriangle(newIdx2, idx3, newIdx3);

				_targetMesh.AddTriangle(idx3, idx1, newIdx3);
				_targetMesh.AddTriangle(newIdx3, idx1, newIdx1);

				// Додаємо нову грань (верхню)
				_targetMesh.AddTriangle(newIdx1, newIdx2, newIdx3);

				// Зберігаємо індекс нової грані для подальшого вибору
				newFaceIndices.Add((_targetMesh.Indices.Count / 3) - 1);
			}

			// Очищаємо поточний вибір і вибираємо нові грані
			_targetMesh.SelectedFaces.Clear();
			foreach (int newFaceIndex in newFaceIndices)
			{
				_targetMesh.SelectedFaces.Add(newFaceIndex);
			}
		}

		UpdateVisuals();
	}

	// Метод для створення нової вершини
	public void CreateVertex(Vector3 position)
	{
		if (_targetMesh == null)
			return;

		// Додаємо нову вершину
		int newVertexIndex = _targetMesh.AddVertex(position);

		// Вибираємо нову вершину
		_targetMesh.SelectedVertices.Clear();
		_targetMesh.SelectedVertices.Add(newVertexIndex);

		UpdateVisuals();
	}

	// Метод для створення ребра між вибраними вершинами
	public void CreateEdge()
	{
		if (_targetMesh == null || _targetMesh.CurrentEditMode != EditMode.Vertex || _targetMesh.SelectedVertices.Count != 2)
			return;

		// Отримуємо індекси двох вибраних вершин
		int[] selectedIndices = new int[2];
		_targetMesh.SelectedVertices.CopyTo(selectedIndices, 0);

		// Додаємо ребро як трикутник з третьою вершиною
		// Для простоти створимо тимчасову третю вершину посередині між двома вибраними
		Vector3 v1 = _targetMesh.Vertices[selectedIndices[0]];
		Vector3 v2 = _targetMesh.Vertices[selectedIndices[1]];
		Vector3 midPoint = (v1 + v2) / 2;

		// Додаємо невеликий зсув, щоб створити видимий трикутник
		Vector3 offset = Vector3.Cross(v2 - v1, Vector3.Up);
		if (offset.Length() < 0.001f)
			offset = Vector3.Cross(v2 - v1, Vector3.Right);

		offset = Vector3.Normalize(offset) * 0.1f;

		int midPointIndex = _targetMesh.AddVertex(midPoint + offset);

		// Додаємо трикутник
		_targetMesh.AddTriangle(selectedIndices[0], selectedIndices[1], midPointIndex);

		// Переходимо в режим редагування ребер і вибираємо нове ребро
		_targetMesh.CurrentEditMode = EditMode.Edge;
		_targetMesh.SelectedEdges.Clear();
		_targetMesh.SelectedEdges.Add(new Tuple<int, int>(selectedIndices[0], selectedIndices[1]));

		UpdateVisuals();
	}

	// Метод для створення грані між вибраними вершинами
	public void CreateFace()
	{
		if (_targetMesh == null || _targetMesh.CurrentEditMode != EditMode.Vertex || _targetMesh.SelectedVertices.Count != 3)
			return;

		// Отримуємо індекси трьох вибраних вершин
		int[] selectedIndices = new int[3];
		_targetMesh.SelectedVertices.CopyTo(selectedIndices, 0);

		// Додаємо трикутник
		_targetMesh.AddTriangle(selectedIndices[0], selectedIndices[1], selectedIndices[2]);

		// Переходимо в режим редагування граней і вибираємо нову грань
		_targetMesh.CurrentEditMode = EditMode.Face;
		_targetMesh.SelectedFaces.Clear();
		_targetMesh.SelectedFaces.Add((_targetMesh.Indices.Count / 3) - 1);

		UpdateVisuals();
	}

	// Реалізація IGizmo.Draw
	public void Draw(BasicEffect effect, TransformComponent transform)
	{
		if (_targetMesh == null) return;

		// Використовуємо переданий ефект замість власного
		effect.VertexColorEnabled = true;
		effect.World = transform.GetWorldMatrix();

		// Малюємо відповідні маркери залежно від режиму редагування
		switch (_targetMesh.CurrentEditMode)
		{
			case EditMode.Vertex:
				DrawVertexMarkers(effect);
				break;
			case EditMode.Edge:
				DrawEdgeMarkers(effect);
				break;
			case EditMode.Face:
				DrawFaceMarkers(effect);
				break;
		}
	}

	// Оригінальний метод Draw для внутрішнього використання
	public void Draw(CameraMatricesState camera)
	{
		if (_targetMesh == null) return;

		_effect.View = camera.ViewMatrix;
		_effect.Projection = camera.ProjectionMatrix;
		_effect.World = _targetMesh.Owner.Transform.GetWorldMatrix();

		// Малюємо відповідні маркери залежно від режиму редагування
		switch (_targetMesh.CurrentEditMode)
		{
			case EditMode.Vertex:
				DrawVertexMarkers(_effect);
				break;
			case EditMode.Edge:
				DrawEdgeMarkers(_effect);
				break;
			case EditMode.Face:
				DrawFaceMarkers(_effect);
				break;
		}
	}

	// Метод для обробки клавіатурного вводу
	public bool HandleKeyboardInput(InputState input)
	{
		if (_targetMesh == null)
			return false;

		bool handled = false;

		// Перемикання режимів редагування
		if (InputManager.Instance.IsKeyPressed(Keys.D1))
		{
			SetEditMode(EditMode.Vertex);
			handled = true;
		}
		else if (InputManager.Instance.IsKeyPressed(Keys.D2))
		{
			SetEditMode(EditMode.Edge);
			handled = true;
		}
		else if (InputManager.Instance.IsKeyPressed(Keys.D3))
		{
			SetEditMode(EditMode.Face);
			handled = true;
		}

		// Видалення вибраних елементів
		if (InputManager.Instance.IsKeyPressed(Keys.Delete))
		{
			DeleteSelected();
			handled = true;
		}

		// Переміщення вибраних елементів
		Vector3 moveOffset = Vector3.Zero;

		if (InputManager.Instance.IsKeyDown(Keys.W))
		{
			moveOffset += Vector3.Up * 0.1f;
			handled = true;
		}
		if (InputManager.Instance.IsKeyDown(Keys.S))
		{
			moveOffset += Vector3.Down * 0.1f;
			handled = true;
		}
		if (InputManager.Instance.IsKeyDown(Keys.A))
		{
			moveOffset += Vector3.Left * 0.1f;
			handled = true;
		}
		if (InputManager.Instance.IsKeyDown(Keys.D))
		{
			moveOffset += Vector3.Right * 0.1f;
			handled = true;
		}
		if (InputManager.Instance.IsKeyDown(Keys.Q))
		{
			moveOffset += Vector3.Backward * 0.1f;
			handled = true;
		}
		if (InputManager.Instance.IsKeyDown(Keys.E))
		{
			moveOffset += Vector3.Forward * 0.1f;
			handled = true;
		}

		if (moveOffset != Vector3.Zero)
		{
			MoveSelected(moveOffset);
		}

		// Екструзія вибраних елементів
		if (InputManager.Instance.IsKeyPressed(Keys.X))
		{
			ExtrudeSelected(0.5f);
			handled = true;
		}

		return handled;
	}

	// Оригінальний метод HandleInput для внутрішнього використання
	public bool HandleInput(InputState input, CameraMatricesState camera, TransformComponent transform)
	{
		if (_targetMesh == null) return false;
		return HandleInput(input, camera, _targetMesh.Owner.Transform);
	}

	private void DrawVertexMarkers(BasicEffect effect)
	{
		if (_targetMesh == null || _targetMesh.Vertices.Count == 0)
			return;

		// Створюємо тимчасовий список вершин для візуалізації
		List<VertexPositionColor> markers = new List<VertexPositionColor>();

		// Розмір маркера вершини
		float markerSize = 0.1f;

		foreach (var vertex in _targetMesh.Vertices)
		{
			// Визначаємо колір вершини залежно від того, чи вона вибрана
			Color vertexColor = _targetMesh.SelectedVertices.Contains(_targetMesh.Vertices.IndexOf(vertex))
				? _highlightColor
				: _primaryColor;

			// Додаємо маркер у вигляді хрестика
			markers.Add(new VertexPositionColor(vertex + new Vector3(-markerSize, 0, 0), vertexColor));
			markers.Add(new VertexPositionColor(vertex + new Vector3(markerSize, 0, 0), vertexColor));

			markers.Add(new VertexPositionColor(vertex + new Vector3(0, -markerSize, 0), vertexColor));
			markers.Add(new VertexPositionColor(vertex + new Vector3(0, markerSize, 0), vertexColor));

			markers.Add(new VertexPositionColor(vertex + new Vector3(0, 0, -markerSize), vertexColor));
			markers.Add(new VertexPositionColor(vertex + new Vector3(0, 0, markerSize), vertexColor));
		}

		// Малюємо маркери
		if (markers.Count > 0)
		{
			foreach (EffectPass pass in effect.CurrentTechnique.Passes)
			{
				pass.Apply();
				_graphicsDevice.DrawUserPrimitives(PrimitiveType.LineList, markers.ToArray(), 0, markers.Count / 2);
			}
		}
	}

	private void DrawEdgeMarkers(BasicEffect effect)
	{
		if (_targetMesh == null || _targetMesh.Indices.Count < 3)
			return;

		// Створюємо тимчасовий список вершин для візуалізації ребер
		List<VertexPositionColor> edgeLines = new List<VertexPositionColor>();

		// Проходимо по всіх трикутниках (кожні 3 індекси)
		for (int i = 0; i < _targetMesh.Indices.Count; i += 3)
		{
			// Отримуємо індекси вершин трикутника
			int idx1 = _targetMesh.Indices[i];
			int idx2 = _targetMesh.Indices[i + 1];
			int idx3 = _targetMesh.Indices[i + 2];

			// Отримуємо вершини трикутника
			Vector3 v1 = _targetMesh.Vertices[idx1];
			Vector3 v2 = _targetMesh.Vertices[idx2];
			Vector3 v3 = _targetMesh.Vertices[idx3];

			// Перевіряємо, чи вибрано ребро
			bool isEdge1Selected = _targetMesh.SelectedEdges.Contains(new Tuple<int, int>(idx1, idx2)) ||
								   _targetMesh.SelectedEdges.Contains(new Tuple<int, int>(idx2, idx1));
			bool isEdge2Selected = _targetMesh.SelectedEdges.Contains(new Tuple<int, int>(idx2, idx3)) ||
								   _targetMesh.SelectedEdges.Contains(new Tuple<int, int>(idx3, idx2));
			bool isEdge3Selected = _targetMesh.SelectedEdges.Contains(new Tuple<int, int>(idx3, idx1)) ||
								   _targetMesh.SelectedEdges.Contains(new Tuple<int, int>(idx1, idx3));

			// Додаємо ребра з відповідним кольором
			edgeLines.Add(new VertexPositionColor(v1, isEdge1Selected ? _highlightColor : _secondaryColor));
			edgeLines.Add(new VertexPositionColor(v2, isEdge1Selected ? _highlightColor : _secondaryColor));

			edgeLines.Add(new VertexPositionColor(v2, isEdge2Selected ? _highlightColor : _secondaryColor));
			edgeLines.Add(new VertexPositionColor(v3, isEdge2Selected ? _highlightColor : _secondaryColor));

			edgeLines.Add(new VertexPositionColor(v3, isEdge3Selected ? _highlightColor : _secondaryColor));
			edgeLines.Add(new VertexPositionColor(v1, isEdge3Selected ? _highlightColor : _secondaryColor));
		}

		// Малюємо ребра
		if (edgeLines.Count > 0)
		{
			foreach (EffectPass pass in effect.CurrentTechnique.Passes)
			{
				pass.Apply();
				_graphicsDevice.DrawUserPrimitives(PrimitiveType.LineList, edgeLines.ToArray(), 0, edgeLines.Count / 2);
			}
		}
	}

	private void DrawFaceMarkers(BasicEffect effect)
	{
		if (_targetMesh == null || _targetMesh.Indices.Count < 3)
			return;

		// Створюємо тимчасовий список вершин для візуалізації граней
		List<VertexPositionColor> faceTriangles = new List<VertexPositionColor>();

		// Проходимо по всіх трикутниках (кожні 3 індекси)
		for (int i = 0; i < _targetMesh.Indices.Count; i += 3)
		{
			// Отримуємо індекси вершин трикутника
			int idx1 = _targetMesh.Indices[i];
			int idx2 = _targetMesh.Indices[i + 1];
			int idx3 = _targetMesh.Indices[i + 2];

			// Перевіряємо, чи вибрана грань
			bool isFaceSelected = _targetMesh.SelectedFaces.Contains(i / 3);

			// Визначаємо колір грані
			Color faceColor = isFaceSelected ? _highlightColor : _secondaryColor;
			faceColor = new Color(faceColor, 100); // Напівпрозорий

			// Отримуємо вершини трикутника
			Vector3 v1 = _targetMesh.Vertices[idx1];
			Vector3 v2 = _targetMesh.Vertices[idx2];
			Vector3 v3 = _targetMesh.Vertices[idx3];

			// Додаємо грань
			faceTriangles.Add(new VertexPositionColor(v1, faceColor));
			faceTriangles.Add(new VertexPositionColor(v2, faceColor));
			faceTriangles.Add(new VertexPositionColor(v3, faceColor));
		}

		// Малюємо грані
		if (faceTriangles.Count > 0)
		{
			// Зберігаємо поточний стан рендерингу
			RasterizerState prevRasterizerState = _graphicsDevice.RasterizerState;
			DepthStencilState prevDepthStencilState = _graphicsDevice.DepthStencilState;
			BlendState prevBlendState = _graphicsDevice.BlendState;

			// Налаштовуємо стан для напівпрозорого рендерингу
			RasterizerState rasterizerState = new RasterizerState();
			rasterizerState.CullMode = CullMode.None;
			_graphicsDevice.RasterizerState = rasterizerState;
			_graphicsDevice.DepthStencilState = DepthStencilState.DepthRead;
			_graphicsDevice.BlendState = BlendState.AlphaBlend;

			foreach (EffectPass pass in effect.CurrentTechnique.Passes)
			{
				pass.Apply();
				_graphicsDevice.DrawUserPrimitives(PrimitiveType.TriangleList, faceTriangles.ToArray(), 0, faceTriangles.Count / 3);
			}

			// Відновлюємо попередній стан
			_graphicsDevice.RasterizerState = prevRasterizerState;
			_graphicsDevice.DepthStencilState = prevDepthStencilState;
			_graphicsDevice.BlendState = prevBlendState;
		}
	}

	private bool HandleVertexSelection(Ray ray, bool addToSelection, TransformComponent transform = null)
	{
		if (_targetMesh == null || _targetMesh.Vertices.Count == 0)
			return false;

		TransformComponent actualTransform = transform ?? _targetMesh.Owner.Transform;
		Matrix worldMatrix = actualTransform.GetWorldMatrix();

		// Радіус для визначення попадання в вершину
		float pickingRadius = 0.2f;

		// Знаходимо найближчу вершину до променя
		int closestVertexIndex = -1;
		float closestDistance = float.MaxValue;

		for (int i = 0; i < _targetMesh.Vertices.Count; i++)
		{
			// Трансформуємо вершину в світові координати
			Vector3 worldVertex = Vector3.Transform(_targetMesh.Vertices[i], worldMatrix);

			// Знаходимо найближчу точку на промені до вершини
			Vector3 v = worldVertex - ray.Position;
			float t = Vector3.Dot(v, ray.Direction);
			Vector3 nearestPoint = ray.Position + ray.Direction * t;

			// Обчислюємо відстань від вершини до найближчої точки на промені
			float distance = Vector3.Distance(worldVertex, nearestPoint);

			// Якщо відстань менша за радіус вибору і менша за поточну найближчу відстань
			if (distance < pickingRadius && distance < closestDistance)
			{
				closestDistance = distance;
				closestVertexIndex = i;
			}
		}

		// Якщо знайдено вершину
		if (closestVertexIndex != -1)
		{
			if (!addToSelection)
			{
				// Якщо не додаємо до вибору, очищаємо поточний вибір
				_targetMesh.SelectedVertices.Clear();
			}

			// Додаємо або видаляємо вершину з вибору
			if (_targetMesh.SelectedVertices.Contains(closestVertexIndex))
			{
				_targetMesh.SelectedVertices.Remove(closestVertexIndex);
			}
			else
			{
				_targetMesh.SelectedVertices.Add(closestVertexIndex);
			}

			return true;
		}

		// Якщо не вибрано вершину і не додаємо до вибору, очищаємо вибір
		if (!addToSelection)
		{
			_targetMesh.SelectedVertices.Clear();
		}

		return false;
	}

	private bool HandleEdgeSelection(Ray ray, bool addToSelection, TransformComponent transform = null)
	{
		if (_targetMesh == null || _targetMesh.Indices.Count < 3)
			return false;

		TransformComponent actualTransform = transform ?? _targetMesh.Owner.Transform;
		Matrix worldMatrix = actualTransform.GetWorldMatrix();

		// Максимальна відстань для вибору ребра
		float maxDistance = 0.2f;

		// Знаходимо найближче ребро до променя
		Tuple<int, int> closestEdge = null;
		float closestDistance = float.MaxValue;

		// Проходимо по всіх трикутниках
		for (int i = 0; i < _targetMesh.Indices.Count; i += 3)
		{
			// Отримуємо індекси вершин трикутника
			int idx1 = _targetMesh.Indices[i];
			int idx2 = _targetMesh.Indices[i + 1];
			int idx3 = _targetMesh.Indices[i + 2];

			// Отримуємо світові координати вершин
			Vector3 v1 = Vector3.Transform(_targetMesh.Vertices[idx1], worldMatrix);
			Vector3 v2 = Vector3.Transform(_targetMesh.Vertices[idx2], worldMatrix);
			Vector3 v3 = Vector3.Transform(_targetMesh.Vertices[idx3], worldMatrix);

			// Перевіряємо кожне ребро трикутника
			CheckEdge(ray, idx1, idx2, v1, v2, ref closestEdge, ref closestDistance, maxDistance);
			CheckEdge(ray, idx2, idx3, v2, v3, ref closestEdge, ref closestDistance, maxDistance);
			CheckEdge(ray, idx3, idx1, v3, v1, ref closestEdge, ref closestDistance, maxDistance);
		}

		// Якщо знайдено ребро
		if (closestEdge != null)
		{
			if (!addToSelection)
			{
				// Якщо не додаємо до вибору, очищаємо поточний вибір
				_targetMesh.SelectedEdges.Clear();
			}

			// Додаємо або видаляємо ребро з вибору
			if (_targetMesh.SelectedEdges.Contains(closestEdge) ||
				_targetMesh.SelectedEdges.Contains(new Tuple<int, int>(closestEdge.Item2, closestEdge.Item1)))
			{
				_targetMesh.SelectedEdges.Remove(closestEdge);
				_targetMesh.SelectedEdges.Remove(new Tuple<int, int>(closestEdge.Item2, closestEdge.Item1));
			}
			else
			{
				_targetMesh.SelectedEdges.Add(closestEdge);
			}

			return true;
		}

		// Якщо не вибрано ребро і не додаємо до вибору, очищаємо вибір
		if (!addToSelection)
		{
			_targetMesh.SelectedEdges.Clear();
		}

		return false;
	}

	// Допоміжний метод для перевірки відстані від променя до ребра
	private void CheckEdge(Ray ray, int idx1, int idx2, Vector3 v1, Vector3 v2,
						  ref Tuple<int, int> closestEdge, ref float closestDistance, float maxDistance)
	{
		// Знаходимо найближчу точку на промені до ребра
		Vector3 edge = v2 - v1;
		Vector3 w0 = ray.Position - v1;

		float a = Vector3.Dot(edge, edge);
		float b = Vector3.Dot(edge, ray.Direction);
		float c = Vector3.Dot(ray.Direction, ray.Direction);
		float d = Vector3.Dot(edge, w0);
		float e = Vector3.Dot(ray.Direction, w0);

		float denominator = a * c - b * b;

		// Якщо ребро і промінь не паралельні
		if (Math.Abs(denominator) > float.Epsilon)
		{
			float sc = (b * e - c * d) / denominator;
			float tc = (a * e - b * d) / denominator;

			// Перевіряємо, чи точка лежить на ребрі
			if (sc >= 0 && sc <= 1)
			{
				// Знаходимо найближчі точки на ребрі і промені
				Vector3 pointOnEdge = v1 + sc * edge;
				Vector3 pointOnRay = ray.Position + tc * ray.Direction;

				// Обчислюємо відстань між ними
				float distance = Vector3.Distance(pointOnEdge, pointOnRay);

				// Якщо відстань менша за максимальну і менша за поточну найближчу відстань
				if (distance < maxDistance && distance < closestDistance)
				{
					closestDistance = distance;
					closestEdge = new Tuple<int, int>(idx1, idx2);
				}
			}
		}
	}

	private bool HandleFaceSelection(Ray ray, bool addToSelection, TransformComponent transform = null)
	{
		if (_targetMesh == null || _targetMesh.Indices.Count < 3)
			return false;

		TransformComponent actualTransform = transform ?? _targetMesh.Owner.Transform;
		Matrix worldMatrix = actualTransform.GetWorldMatrix();

		// Знаходимо найближчу грань до променя
		int closestFaceIndex = -1;
		float closestDistance = float.MaxValue;

		// Проходимо по всіх трикутниках
		for (int i = 0; i < _targetMesh.Indices.Count; i += 3)
		{
			// Отримуємо індекси вершин трикутника
			int idx1 = _targetMesh.Indices[i];
			int idx2 = _targetMesh.Indices[i + 1];
			int idx3 = _targetMesh.Indices[i + 2];

			// Отримуємо світові координати вершин
			Vector3 v1 = Vector3.Transform(_targetMesh.Vertices[idx1], worldMatrix);
			Vector3 v2 = Vector3.Transform(_targetMesh.Vertices[idx2], worldMatrix);
			Vector3 v3 = Vector3.Transform(_targetMesh.Vertices[idx3], worldMatrix);

			// Перевіряємо перетин променя з трикутником
			if (RayIntersectsTriangle(ray, v1, v2, v3, out float distance))
			{
				if (distance < closestDistance)
				{
					closestDistance = distance;
					closestFaceIndex = i / 3; // Індекс грані (трикутника)
				}
			}
		}

		// Якщо знайдено грань
		if (closestFaceIndex != -1)
		{
			if (!addToSelection)
			{
				// Якщо не додаємо до вибору, очищаємо поточний вибір
				_targetMesh.SelectedFaces.Clear();
			}

			// Додаємо або видаляємо грань з вибору
			if (_targetMesh.SelectedFaces.Contains(closestFaceIndex))
			{
				_targetMesh.SelectedFaces.Remove(closestFaceIndex);
			}
			else
			{
				_targetMesh.SelectedFaces.Add(closestFaceIndex);
			}

			return true;
		}

		// Якщо не вибрано грань і не додаємо до вибору, очищаємо вибір
		if (!addToSelection)
		{
			_targetMesh.SelectedFaces.Clear();
		}

		return false;
	}

	// Метод для перевірки перетину променя з трикутником (алгоритм Möller–Trumbore)
	private bool RayIntersectsTriangle(Ray ray, Vector3 v1, Vector3 v2, Vector3 v3, out float distance)
	{
		distance = 0;

		// Обчислюємо вектори для двох сторін трикутника від v1
		Vector3 edge1 = v2 - v1;
		Vector3 edge2 = v3 - v1;

		// Обчислюємо детермінант
		Vector3 h = Vector3.Cross(ray.Direction, edge2);
		float a = Vector3.Dot(edge1, h);

		// Перевіряємо, чи промінь паралельний трикутнику
		if (a > -float.Epsilon && a < float.Epsilon)
			return false;

		float f = 1.0f / a;
		Vector3 s = ray.Position - v1;
		float u = f * Vector3.Dot(s, h);

		// Перевіряємо, чи точка перетину знаходиться поза трикутником
		if (u < 0.0f || u > 1.0f)
			return false;

		Vector3 q = Vector3.Cross(s, edge1);
		float v = f * Vector3.Dot(ray.Direction, q);

		// Перевіряємо, чи точка перетину знаходиться поза трикутником
		if (v < 0.0f || u + v > 1.0f)
			return false;

		// Обчислюємо t, щоб знайти точку перетину
		float t = f * Vector3.Dot(edge2, q);

		// Перевіряємо, чи t > 0 (перетин знаходиться перед початком променя)
		if (t > float.Epsilon)
		{
			distance = t;
			return true;
		}

		// Немає перетину
		return false;
	}

	private Ray CreatePickingRay(Vector2 mousePosition, CameraMatricesState camera)
	{
		// Перетворення координат миші у нормалізовані координати пристрою
		Vector2 screenSize = new Vector2(_graphicsDevice.Viewport.Width, _graphicsDevice.Viewport.Height);
		Vector2 normalizedPosition = new Vector2(
			(2.0f * mousePosition.X) / screenSize.X - 1.0f,
			1.0f - (2.0f * mousePosition.Y) / screenSize.Y
		);

		// Створення променя у світовому просторі
		Vector3 nearPoint = new Vector3(normalizedPosition.X, normalizedPosition.Y, 0.0f);
		Vector3 farPoint = new Vector3(normalizedPosition.X, normalizedPosition.Y, 1.0f);

		Matrix invertedViewProjection = Matrix.Invert(camera.ViewMatrix * camera.ProjectionMatrix);

		Vector3 nearWorld = Vector3.Transform(nearPoint, invertedViewProjection);
		Vector3 farWorld = Vector3.Transform(farPoint, invertedViewProjection);

		Vector3 direction = Vector3.Normalize(farWorld - nearWorld);
		return new Ray(nearWorld, direction);
	}

	private void UpdateVisuals()
	{
		if (_targetMesh == null)
			return;

		// Тут можна оновити буфери для візуалізації маркерів вершин, ребер та граней
		// Для простоти тестування ми будемо створювати їх безпосередньо в методах Draw

		// Якщо потрібно, можна створити буфери тут для оптимізації:
		/*
		// Створення буфера для маркерів вершин
		List<VertexPositionColor> vertexMarkers = new List<VertexPositionColor>();
		// Заповнення даних...
		_vertexMarkersBuffer = new VertexBuffer(_graphicsDevice, typeof(VertexPositionColor), vertexMarkers.Count, BufferUsage.WriteOnly);
		_vertexMarkersBuffer.SetData(vertexMarkers.ToArray());

		// Аналогічно для ребер та граней...
		*/
	}
}