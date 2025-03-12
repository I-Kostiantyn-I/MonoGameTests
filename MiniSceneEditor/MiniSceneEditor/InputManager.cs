using Microsoft.Xna.Framework.Input;

namespace MiniSceneEditor;

public class InputManager
{
	private static InputManager _instance;

	public static InputManager Instance
	{ 
		get 
		{
			if (_instance == null)
				_instance = new InputManager();
			return _instance;
		} 
	}

	public InputState CurrentState { get; private set; }
	public InputState PreviousState { get; private set; }

	public InputManager()
	{
		CurrentState = new InputState(
			Mouse.GetState(),
			Keyboard.GetState()
		);
		PreviousState = CurrentState;
	}

	public void Update()
	{
		PreviousState = CurrentState;

		var currentMouse = Mouse.GetState();
		var currentKeyboard = Keyboard.GetState();

		CurrentState = new InputState(
			currentMouse,
			currentKeyboard
		);
	}

	// Перевірка натискання (кнопка була відпущена, стала натиснутою)
	public bool IsMouseButtonPressed(ButtonState button) =>
		CurrentState.CurrentMouse.LeftButton == ButtonState.Pressed &&
		PreviousState.CurrentMouse.LeftButton == ButtonState.Released;

	// Перевірка відпускання (кнопка була натиснута, стала відпущеною)
	public bool IsMouseButtonReleased(ButtonState button) =>
		CurrentState.CurrentMouse.LeftButton == ButtonState.Released &&
		PreviousState.CurrentMouse.LeftButton == ButtonState.Pressed;

	// Перевірка, чи утримується кнопка
	public bool IsMouseButtonDown(ButtonState button) =>
		CurrentState.CurrentMouse.LeftButton == ButtonState.Pressed &&
		PreviousState.CurrentMouse.LeftButton == ButtonState.Pressed;

	public bool IsShiftDown() =>
		CurrentState.CurrentKeyboard.IsKeyDown(Keys.LeftShift) || CurrentState.CurrentKeyboard.IsKeyDown(Keys.RightShift);

	public bool IsControlDown() =>
		CurrentState.CurrentKeyboard.IsKeyDown(Keys.LeftControl) || CurrentState.CurrentKeyboard.IsKeyDown(Keys.RightControl);

	public bool IsAltDown() =>
		CurrentState.CurrentKeyboard.IsKeyDown(Keys.LeftAlt) || CurrentState.CurrentKeyboard.IsKeyDown(Keys.RightAlt);

	public bool IsKeyDown(Keys key)
	{
		return CurrentState.CurrentKeyboard.IsKeyDown(key);
	}

	public bool IsKeyPressed(Keys key)
	{
		return CurrentState.CurrentKeyboard.IsKeyDown(key) && !PreviousState.CurrentKeyboard.IsKeyDown(key);
	}

	public bool IsKeyReleased(Keys key)
	{
		return !CurrentState.CurrentKeyboard.IsKeyDown(key) && PreviousState.CurrentKeyboard.IsKeyDown(key);
	}

	// Методи для перевірки клавіш керування камерою
	public bool IsForwardPressed() => CurrentState.CurrentKeyboard.IsKeyDown(Keys.W);
	public bool IsBackwardPressed() => CurrentState.CurrentKeyboard.IsKeyDown(Keys.S);
	public bool IsLeftPressed() => CurrentState.CurrentKeyboard.IsKeyDown(Keys.A);
	public bool IsRightPressed() => CurrentState.CurrentKeyboard.IsKeyDown(Keys.D);
	public bool IsUpPressed() => CurrentState.CurrentKeyboard.IsKeyDown(Keys.Space);
	public bool IsDownPressed() => CurrentState.CurrentKeyboard.IsKeyDown(Keys.LeftControl);

	// Для визначення, чи використовуються клавіші для керування камерою
	public bool IsAnyCameraKeyPressed()
	{
		return CurrentState.CurrentKeyboard.IsKeyDown(Keys.W) ||
			   CurrentState.CurrentKeyboard.IsKeyDown(Keys.A) ||
			   CurrentState.CurrentKeyboard.IsKeyDown(Keys.S) ||
			   CurrentState.CurrentKeyboard.IsKeyDown(Keys.D) ||
			   CurrentState.CurrentKeyboard.IsKeyDown(Keys.Space) ||
			   CurrentState.CurrentKeyboard.IsKeyDown(Keys.LeftControl) ||
			   CurrentState.CurrentKeyboard.IsKeyDown(Keys.LeftShift) ||
			   CurrentState.CurrentKeyboard.IsKeyDown(Keys.RightShift) ||
			   CurrentState.CurrentKeyboard.IsKeyDown(Keys.LeftAlt) ||
			   CurrentState.CurrentKeyboard.IsKeyDown(Keys.RightAlt);
	}
}