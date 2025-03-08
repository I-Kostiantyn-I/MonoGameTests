using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MiniSceneEditor.Core.Components.Impls;

public class EditableMeshComponent : MeshComponent
{
	// Структури даних для зберігання вершин, ребер та граней
	private List<Vector3> _vertices = new List<Vector3>();
	private List<Edge> _edges = new List<Edge>();
	private List<Face> _faces = new List<Face>();

	// Вибрані елементи
	private HashSet<int> _selectedVertices = new HashSet<int>();
	private HashSet<int> _selectedEdges = new HashSet<int>();
	private HashSet<int> _selectedFaces = new HashSet<int>();

	// Властивості для зберігання даних меша
	public List<Vector3> Vertices { get; private set; } = new List<Vector3>();
	public List<int> Indices { get; private set; } = new List<int>();

	// Списки для вибраних елементів
	public HashSet<int> SelectedVertices { get; private set; } = new HashSet<int>();
	public HashSet<Tuple<int, int>> SelectedEdges { get; private set; } = new HashSet<Tuple<int, int>>();
	public HashSet<int> SelectedFaces { get; private set; } = new HashSet<int>();

	// Поточний режим редагування
	public EditMode CurrentEditMode { get; set; } = EditMode.Vertex;

	// Методи для редагування геометрії
	public int AddVertex(Vector3 position)
	{
		_vertices.Add(position);
		int index = _vertices.Count - 1;

		// Перебудовуємо меш після додавання вершини
		RebuildMesh();

		return index;
	}

	public void RemoveVertex(int index) { /* ... */ }
	public void MoveVertex(int index, Vector3 newPosition) { /* ... */ }

	// Методи для роботи з ребрами
	public void AddEdge(int v1, int v2) { /* ... */ }
	public void RemoveEdge(int index) { /* ... */ }

	// Методи для роботи з гранями
	public void AddFace(int[] vertexIndices) { /* ... */ }
	public void RemoveFace(int index) { /* ... */ }
	public void ExtrudeFace(int index, Vector3 direction, float amount) { /* ... */ }

	// Методи для вибору елементів
	public void SelectVertex(int index, bool addToSelection = false) { /* ... */ }
	public void SelectEdge(int index, bool addToSelection = false) { /* ... */ }
	public void SelectFace(int index, bool addToSelection = false) { /* ... */ }

	// Метод для додавання трикутника
	public void AddTriangle(int index1, int index2, int index3)
	{
		// Перевіряємо, чи індекси в межах масиву вершин
		if (index1 >= 0 && index1 < Vertices.Count &&
			index2 >= 0 && index2 < Vertices.Count &&
			index3 >= 0 && index3 < Vertices.Count)
		{
			Indices.Add(index1);
			Indices.Add(index2);
			Indices.Add(index3);

			// Перебудовуємо меш після додавання трикутника
			RebuildMesh();
		}
	}

	// Метод для видалення вибраних вершин
	public void DeleteSelectedVertices()
	{
		if (SelectedVertices.Count == 0)
			return;

		// Створюємо відображення старих індексів на нові
		Dictionary<int, int> indexMapping = new Dictionary<int, int>();
		List<Vector3> newVertices = new List<Vector3>();

		// Копіюємо невибрані вершини
		for (int i = 0; i < Vertices.Count; i++)
		{
			if (!SelectedVertices.Contains(i))
			{
				indexMapping[i] = newVertices.Count;
				newVertices.Add(Vertices[i]);
			}
		}

		// Оновлюємо індекси трикутників
		List<int> newIndices = new List<int>();
		for (int i = 0; i < Indices.Count; i += 3)
		{
			int idx1 = Indices[i];
			int idx2 = Indices[i + 1];
			int idx3 = Indices[i + 2];

			// Додаємо трикутник, тільки якщо всі його вершини не вибрані
			if (!SelectedVertices.Contains(idx1) &&
				!SelectedVertices.Contains(idx2) &&
				!SelectedVertices.Contains(idx3))
			{
				newIndices.Add(indexMapping[idx1]);
				newIndices.Add(indexMapping[idx2]);
				newIndices.Add(indexMapping[idx3]);
			}
		}

		// Оновлюємо меш
		Vertices = newVertices;
		Indices = newIndices;

		// Очищаємо вибір
		SelectedVertices.Clear();
		SelectedEdges.Clear();
		SelectedFaces.Clear();

		// Перебудовуємо меш
		RebuildMesh();
	}

	// Метод для видалення вибраних ребер
	public void DeleteSelectedEdges()
	{
		if (SelectedEdges.Count == 0)
			return;

		// Створюємо список трикутників для видалення
		HashSet<int> trianglesToRemove = new HashSet<int>();

		// Проходимо по всіх трикутниках
		for (int i = 0; i < Indices.Count; i += 3)
		{
			int idx1 = Indices[i];
			int idx2 = Indices[i + 1];
			int idx3 = Indices[i + 2];

			// Перевіряємо, чи містить трикутник вибране ребро
			if (ContainsSelectedEdge(idx1, idx2) ||
				ContainsSelectedEdge(idx2, idx3) ||
				ContainsSelectedEdge(idx3, idx1))
			{
				trianglesToRemove.Add(i / 3);
			}
		}

		// Видаляємо трикутники, що містять вибрані ребра
		List<int> newIndices = new List<int>();
		for (int i = 0; i < Indices.Count; i += 3)
		{
			if (!trianglesToRemove.Contains(i / 3))
			{
				newIndices.Add(Indices[i]);
				newIndices.Add(Indices[i + 1]);
				newIndices.Add(Indices[i + 2]);
			}
		}

		// Оновлюємо індекси
		Indices = newIndices;

		// Очищаємо вибір
		SelectedEdges.Clear();
		SelectedFaces.Clear();

		// Перебудовуємо меш
		RebuildMesh();
	}

	// Допоміжний метод для перевірки, чи ребро вибране
	private bool ContainsSelectedEdge(int idx1, int idx2)
	{
		return SelectedEdges.Contains(new Tuple<int, int>(idx1, idx2)) ||
			   SelectedEdges.Contains(new Tuple<int, int>(idx2, idx1));
	}

	// Метод для видалення вибраних граней
	public void DeleteSelectedFaces()
	{
		if (SelectedFaces.Count == 0)
			return;

		// Видаляємо вибрані грані (трикутники)
		List<int> newIndices = new List<int>();
		for (int i = 0; i < Indices.Count; i += 3)
		{
			if (!SelectedFaces.Contains(i / 3))
			{
				newIndices.Add(Indices[i]);
				newIndices.Add(Indices[i + 1]);
				newIndices.Add(Indices[i + 2]);
			}
		}

		// Оновлюємо індекси
		Indices = newIndices;

		// Очищаємо вибір
		SelectedFaces.Clear();

		// Перебудовуємо меш
		RebuildMesh();
	}

	// Метод для переміщення вибраних вершин
	public void MoveSelectedVertices(Vector3 offset)
	{
		if (SelectedVertices.Count == 0)
			return;

		// Переміщуємо вибрані вершини
		foreach (int index in SelectedVertices)
		{
			if (index >= 0 && index < Vertices.Count)
			{
				Vertices[index] += offset;
			}
		}

		// Перебудовуємо меш
		RebuildMesh();
	}

	// Метод для переміщення вибраних ребер
	public void MoveSelectedEdges(Vector3 offset)
	{
		if (SelectedEdges.Count == 0)
			return;

		// Збираємо всі унікальні вершини вибраних ребер
		HashSet<int> verticesToMove = new HashSet<int>();

		foreach (var edge in SelectedEdges)
		{
			verticesToMove.Add(edge.Item1);
			verticesToMove.Add(edge.Item2);
		}

		// Переміщуємо вершини
		foreach (int index in verticesToMove)
		{
			if (index >= 0 && index < Vertices.Count)
			{
				Vertices[index] += offset;
			}
		}

		// Перебудовуємо меш
		RebuildMesh();
	}

	// Метод для переміщення вибраних граней
	public void MoveSelectedFaces(Vector3 offset)
	{
		if (SelectedFaces.Count == 0)
			return;

		// Збираємо всі унікальні вершини вибраних граней
		HashSet<int> verticesToMove = new HashSet<int>();

		foreach (int faceIndex in SelectedFaces)
		{
			int baseIndex = faceIndex * 3;
			if (baseIndex + 2 < Indices.Count)
			{
				verticesToMove.Add(Indices[baseIndex]);
				verticesToMove.Add(Indices[baseIndex + 1]);
				verticesToMove.Add(Indices[baseIndex + 2]);
			}
		}

		// Переміщуємо вершини
		foreach (int index in verticesToMove)
		{
			if (index >= 0 && index < Vertices.Count)
			{
				Vertices[index] += offset;
			}
		}

		// Перебудовуємо меш
		RebuildMesh();
	}

	// Метод для перебудови меша
	private void RebuildMesh()
	{
		// Створюємо вершини для меша
		VertexPositionNormalTexture[] meshVertices = new VertexPositionNormalTexture[Vertices.Count];

		for (int i = 0; i < Vertices.Count; i++)
		{
			meshVertices[i] = new VertexPositionNormalTexture(
				Vertices[i],
				Vector3.Up, // Тимчасова нормаль, буде перерахована нижче
				new Vector2(0, 0) // Тимчасові текстурні координати
			);
		}

		// Обчислюємо нормалі
		CalculateNormals(meshVertices);

		// Оновлюємо меш
		UpdateMesh(meshVertices, Indices.ToArray());
	}

	// Метод для обчислення нормалей
	private void CalculateNormals(VertexPositionNormalTexture[] vertices)
	{
		// Спочатку обнуляємо всі нормалі
		for (int i = 0; i < vertices.Length; i++)
		{
			vertices[i].Normal = Vector3.Zero;
		}

		// Обчислюємо нормалі для кожного трикутника і додаємо їх до вершин
		for (int i = 0; i < Indices.Count; i += 3)
		{
			int idx1 = Indices[i];
			int idx2 = Indices[i + 1];
			int idx3 = Indices[i + 2];

			// Обчислюємо нормаль трикутника
			Vector3 side1 = vertices[idx2].Position - vertices[idx1].Position;
			Vector3 side2 = vertices[idx3].Position - vertices[idx1].Position;
			Vector3 normal = Vector3.Cross(side1, side2);

			// Додаємо нормаль до кожної вершини трикутника
			vertices[idx1].Normal += normal;
			vertices[idx2].Normal += normal;
			vertices[idx3].Normal += normal;
		}

		// Нормалізуємо всі нормалі
		for (int i = 0; i < vertices.Length; i++)
		{
			if (vertices[i].Normal != Vector3.Zero)
			{
				vertices[i].Normal = Vector3.Normalize(vertices[i].Normal);
			}
			else
			{
				// Якщо нормаль нульова, встановлюємо стандартну нормаль вгору
				vertices[i].Normal = Vector3.Up;
			}
		}
	}

	// Метод для оновлення меша
	private void UpdateMesh(VertexPositionNormalTexture[] vertices, int[] indices)
	{
		// Тут має бути код для оновлення буферів меша
		// Це залежить від реалізації базового класу MeshComponent

		// Приклад:
		// SetVertices(vertices);
		// SetIndices(indices);
	}
}

// Допоміжні структури
public struct Edge
{
	public int Vertex1;
	public int Vertex2;
}

public struct Face
{
	public int[] Vertices;
	public Vector3 Normal;
}

public enum EditMode
{
	Vertex,
	Edge,
	Face
}