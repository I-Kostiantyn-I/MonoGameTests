using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MiniSceneEditor.Commands;

	public class CommandManager
{
	private Stack<ICommand> _undoStack = new Stack<ICommand>();
	private Stack<ICommand> _redoStack = new Stack<ICommand>();
	private const int MAX_HISTORY = 100;

	public void ExecuteCommand(ICommand command)
	{
		command.Execute();
		_undoStack.Push(command);
		_redoStack.Clear(); // Очищаємо redo stack після нової команди

		// Обмежуємо розмір історії
		if (_undoStack.Count > MAX_HISTORY)
		{
			var tempStack = new Stack<ICommand>();
			for (int i = 0; i < MAX_HISTORY; i++)
			{
				tempStack.Push(_undoStack.Pop());
			}
			_undoStack = new Stack<ICommand>(tempStack);
		}
	}

	public void Undo()
	{
		if (_undoStack.Count > 0)
		{
			var command = _undoStack.Pop();
			command.Undo();
			_redoStack.Push(command);
		}
	}

	public void Redo()
	{
		if (_redoStack.Count > 0)
		{
			var command = _redoStack.Pop();
			command.Execute();
			_undoStack.Push(command);
		}
	}

	public void Clear()
	{
		_undoStack.Clear();
		_redoStack.Clear();
	}
}