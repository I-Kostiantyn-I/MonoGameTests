using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MiniSceneEditor;

public class InputManager
{
	public InputState CurrentState { get; private set; }
	private MouseState _previousMouseState;
	private KeyboardState _previousKeyboardState;

	public InputManager()
	{
		CurrentState = new InputState(
			Mouse.GetState(),
			Mouse.GetState(),
			Keyboard.GetState()
		);
	}

	public void Update()
	{
		var currentMouse = Mouse.GetState();
		var currentKeyboard = Keyboard.GetState();

		CurrentState = new InputState(
			currentMouse,
			_previousMouseState,
			currentKeyboard
		);

		_previousMouseState = currentMouse;
		_previousKeyboardState = currentKeyboard;
	}
}