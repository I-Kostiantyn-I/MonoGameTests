using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;

namespace MiniSceneEditor;

public class SnapGrid
{
	private GraphicsDevice _graphicsDevice;
	private BasicEffect _effect;
	private VertexBuffer _vertexBuffer;
	private int _gridSize = 100;
	private SnapSettings _settings;

	public SnapGrid(GraphicsDevice graphicsDevice, SnapSettings settings)
	{
		_graphicsDevice = graphicsDevice;
		_settings = settings;
		_effect = new BasicEffect(graphicsDevice);
		CreateGrid();
	}

	private void CreateGrid()
	{
		var vertices = new List<VertexPositionColor>();
		float size = _gridSize * _settings.PositionSnapValue;
		int lines = _gridSize * 2 + 1;

		for (int i = -_gridSize; i <= _gridSize; i++)
		{
			float pos = i * _settings.PositionSnapValue;
			Color color = i == 0 ? Color.White : Color.Gray * 0.5f;

			// X axis lines
			vertices.Add(new VertexPositionColor(new Vector3(pos, 0, -size), color));
			vertices.Add(new VertexPositionColor(new Vector3(pos, 0, size), color));

			// Z axis lines
			vertices.Add(new VertexPositionColor(new Vector3(-size, 0, pos), color));
			vertices.Add(new VertexPositionColor(new Vector3(size, 0, pos), color));
		}

		_vertexBuffer = new VertexBuffer(
			_graphicsDevice,
			typeof(VertexPositionColor),
			vertices.Count,
			BufferUsage.WriteOnly
		);
		_vertexBuffer.SetData(vertices.ToArray());
	}

	public void Draw(Matrix view, Matrix projection)
	{
		if (!_settings.SnapToGrid || !_settings.EnablePositionSnap)
			return;

		_effect.View = view;
		_effect.Projection = projection;
		_effect.World = Matrix.Identity;
		_effect.VertexColorEnabled = true;

		foreach (var pass in _effect.CurrentTechnique.Passes)
		{
			pass.Apply();
			_graphicsDevice.SetVertexBuffer(_vertexBuffer);
			_graphicsDevice.DrawPrimitives(
				PrimitiveType.LineList,
				0,
				_vertexBuffer.VertexCount / 2
			);
		}
	}

	public void UpdateGridSize(float newSize)
	{
		if (Math.Abs(_settings.PositionSnapValue - newSize) > float.Epsilon)
		{
			_settings.PositionSnapValue = newSize;
			CreateGrid();
		}
	}
}