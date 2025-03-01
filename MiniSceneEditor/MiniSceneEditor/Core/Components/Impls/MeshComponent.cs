using ImGuiNET;
using Microsoft.Core;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MiniSceneEditor.Core.Components.Abstractions;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MiniSceneEditor.Core.Components.Impls;

public enum MeshType
{
	Box,
	Cylinder,
	Capsule
}

public class MeshComponent : Abstractions.IComponent, IRenderable
{
	public SceneObject Owner { get; set; }
	private GraphicsDevice _graphicsDevice;
	private BasicEffect _effect;
	private VertexBuffer _vertexBuffer;
	private IndexBuffer _indexBuffer;
	private MeshType _meshType = MeshType.Box;
	private RasterizerState _wireframeState;
	private Color _wireframeColor = Color.Black;

	private EditorLogger _log;

	// Параметри для різних типів мешів
	public Vector3 _boxDimensions = new Vector3(1, 1, 1);
	public float _cylinderRadius = 0.5f;
	public float _cylinderHeight = 2f;
	public float _capsuleRadius = 0.5f;
	public float _capsuleHeight = 2f;

	public MeshType MeshType
	{
		get => _meshType;
		set
		{
			if (_meshType != value)
			{
				_meshType = value;
				RebuildMesh();
			}
		}
	}

	public void Initialize()
	{
		_log = new EditorLogger(nameof(MeshComponent));
		RebuildMesh();
	}

	public void OnDestroy()
	{
		_vertexBuffer?.Dispose();
		_indexBuffer?.Dispose();
		_effect?.Dispose();
		_wireframeState?.Dispose();
	}

	public void SetGraphicsDevice(GraphicsDevice graphicsDevice)
	{
		_log.Log("Setting GraphicsDevice");
		_graphicsDevice = graphicsDevice;
		_effect = new BasicEffect(graphicsDevice)
		{
			VertexColorEnabled = true
		};

		_wireframeState = new RasterizerState
		{
			FillMode = FillMode.WireFrame,
			CullMode = CullMode.None
		};

		RebuildMesh(); // Викликаємо перебудову меша після встановлення GraphicsDevice
	}

	public void Render(Matrix view, Matrix projection)
	{
		if (_vertexBuffer == null)
		{
			_log.Log("VertexBuffer is null");
			return;
		}
		if (Owner == null)
		{
			_log.Log("Owner is null");
			return;
		}
		if (_effect == null)
		{
			_log.Log("Effect is null");
			return;
		}

		_log.Log($"Rendering mesh: Vertices={_vertexBuffer.VertexCount}, Indices={_indexBuffer?.IndexCount}");

		var originalRasterizerState = _graphicsDevice.RasterizerState;

		Matrix world = Owner.Transform.GetWorldMatrix();
		_effect.World = world;
		_effect.View = view;
		_effect.Projection = projection;

		_log.Log("Matrices set:");
		_log.Log($"World: {world}");
		_log.Log($"View: {view}");
		_log.Log($"Projection: {projection}");

		_graphicsDevice.RasterizerState = _wireframeState;

		foreach (var pass in _effect.CurrentTechnique.Passes)
		{
			pass.Apply();
			_graphicsDevice.SetVertexBuffer(_vertexBuffer);
			_graphicsDevice.Indices = _indexBuffer;
			_graphicsDevice.DrawIndexedPrimitives(
				PrimitiveType.TriangleList,
				0,
				0,
				_indexBuffer.IndexCount / 3
			);
		}

		_graphicsDevice.RasterizerState = originalRasterizerState;
	}

	private void RebuildMesh()
	{
		if (_graphicsDevice == null)
		{
			_log.Log("Cannot rebuild mesh: GraphicsDevice is null");
			return;
		}

		_log.Log($"Rebuilding mesh of type: {_meshType}");

		switch (_meshType)
		{
			case MeshType.Box:
				BuildBoxMesh();
				break;
			case MeshType.Cylinder:
				// BuildCylinderMesh();
				break;
			case MeshType.Capsule:
				// BuildCapsuleMesh();
				break;
		}
	}

	// Методи для налаштування параметрів
	public void SetBoxDimensions(Vector3 dimensions)
	{
		_boxDimensions = dimensions;
		if (_meshType == MeshType.Box)
			RebuildMesh();
	}

	public void SetCylinderParameters(float radius, float height)
	{
		_cylinderRadius = radius;
		_cylinderHeight = height;
		if (_meshType == MeshType.Cylinder)
			RebuildMesh();
	}

	public void SetCapsuleParameters(float radius, float height)
	{
		_capsuleRadius = radius;
		_capsuleHeight = height;
		if (_meshType == MeshType.Capsule)
			RebuildMesh();
	}

	// Методи побудови мешів...
	private void BuildBoxMesh()
	{
		_log.Log("Starting to build box mesh");

		var vertices = new List<VertexPositionColorNormal>();
		var indices = new List<int>();

		Vector3 halfSize = _boxDimensions * 0.5f;
		_log.LogVector3("Box half size", halfSize);

		// Передня грань (Z+)
		vertices.Add(new VertexPositionColorNormal(new Vector3(-halfSize.X, -halfSize.Y, halfSize.Z), _wireframeColor, Vector3.Forward));
		vertices.Add(new VertexPositionColorNormal(new Vector3(halfSize.X, -halfSize.Y, halfSize.Z), _wireframeColor, Vector3.Forward));
		vertices.Add(new VertexPositionColorNormal(new Vector3(halfSize.X, halfSize.Y, halfSize.Z), _wireframeColor, Vector3.Forward));
		vertices.Add(new VertexPositionColorNormal(new Vector3(-halfSize.X, halfSize.Y, halfSize.Z), _wireframeColor, Vector3.Forward));

		// Задня грань (Z-)
		vertices.Add(new VertexPositionColorNormal(new Vector3(-halfSize.X, -halfSize.Y, -halfSize.Z), _wireframeColor, Vector3.Backward));
		vertices.Add(new VertexPositionColorNormal(new Vector3(halfSize.X, -halfSize.Y, -halfSize.Z), _wireframeColor, Vector3.Backward));
		vertices.Add(new VertexPositionColorNormal(new Vector3(halfSize.X, halfSize.Y, -halfSize.Z), _wireframeColor, Vector3.Backward));
		vertices.Add(new VertexPositionColorNormal(new Vector3(-halfSize.X, halfSize.Y, -halfSize.Z), _wireframeColor, Vector3.Backward));

		// Індекси для всіх граней
		// Передня грань
		indices.AddRange(new[] { 0, 1, 2, 0, 2, 3 });
		// Задня грань
		indices.AddRange(new[] { 5, 4, 7, 5, 7, 6 });
		// Верхня грань
		indices.AddRange(new[] { 3, 2, 6, 3, 6, 7 });
		// Нижня грань
		indices.AddRange(new[] { 4, 5, 1, 4, 1, 0 });
		// Права грань
		indices.AddRange(new[] { 1, 5, 6, 1, 6, 2 });
		// Ліва грань
		indices.AddRange(new[] { 4, 0, 3, 4, 3, 7 });

		_log.Log($"Created {vertices.Count} vertices and {indices.Count} indices");

		try
		{
			_vertexBuffer?.Dispose();
			_indexBuffer?.Dispose();

			_vertexBuffer = new VertexBuffer(_graphicsDevice, VertexPositionColorNormal.VertexDeclaration,
				vertices.Count, BufferUsage.WriteOnly);
			_vertexBuffer.SetData(vertices.ToArray());

			_indexBuffer = new IndexBuffer(_graphicsDevice, IndexElementSize.ThirtyTwoBits,
				indices.Count, BufferUsage.WriteOnly);
			_indexBuffer.SetData(indices.ToArray());

			_log.Log("Successfully created vertex and index buffers");
		}
		catch (Exception ex)
		{
			_log.Log($"Error creating buffers: {ex.Message}");
		}
	}

	private void BuildCylinderMesh()
	{
		// Реалізація побудови циліндра
	}

	private void BuildCapsuleMesh()
	{
		// Реалізація побудови капсули
	}
}

public struct VertexPositionColorNormal
{
	public Vector3 Position;
	public Color Color;
	public Vector3 Normal;

	public readonly static VertexDeclaration VertexDeclaration = new VertexDeclaration(
		new VertexElement(0, VertexElementFormat.Vector3, VertexElementUsage.Position, 0),
		new VertexElement(12, VertexElementFormat.Color, VertexElementUsage.Color, 0),
		new VertexElement(16, VertexElementFormat.Vector3, VertexElementUsage.Normal, 0)
	);

	public VertexPositionColorNormal(Vector3 position, Color color, Vector3 normal)
	{
		Position = position;
		Color = color;
		Normal = normal;
	}
}
