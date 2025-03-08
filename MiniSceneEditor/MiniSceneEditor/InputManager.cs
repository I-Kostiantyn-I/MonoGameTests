using Microsoft.Xna.Framework.Input;

namespace MiniSceneEditor;

public class InputManager
{
	public InputState CurrentState { get; private set; }

	public InputManager()
	{
		CurrentState = new InputState(
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
			currentKeyboard
		);
	}
}