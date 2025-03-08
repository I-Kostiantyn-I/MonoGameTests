using Microsoft.Xna.Framework;
using MiniSceneEditor.Core.Components.Impls;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MiniSceneEditor.Commands;

public class AddVertexCommand : ICommand
{
	private EditableMeshComponent _mesh;
	private Vector3 _position;
	private int _addedVertexIndex;

	public AddVertexCommand(EditableMeshComponent mesh, Vector3 position)
	{
		_mesh = mesh;
		_position = position;
	}

	public void Execute()
	{
		_addedVertexIndex = _mesh.AddVertex(_position);
	}

	public void Undo()
	{
		_mesh.RemoveVertex(_addedVertexIndex);
	}
}

public class MoveVerticesCommand : ICommand
{
	private EditableMeshComponent _mesh;
	private Dictionary<int, Vector3> _originalPositions;
	private Dictionary<int, Vector3> _newPositions;

	public MoveVerticesCommand(EditableMeshComponent mesh, Dictionary<int, Vector3> originalPositions, Dictionary<int, Vector3> newPositions)
	{
		_mesh = mesh;
		_originalPositions = originalPositions;
		_newPositions = newPositions;
	}

	public void Execute()
	{
		foreach (var kvp in _newPositions)
		{
			_mesh.MoveVertex(kvp.Key, kvp.Value);
		}
	}

	public void Undo()
	{
		foreach (var kvp in _originalPositions)
		{
			_mesh.MoveVertex(kvp.Key, kvp.Value);
		}
	}
}

// Аналогічно для інших операцій: ExtrudeFaceCommand, BevelEdgeCommand, тощо