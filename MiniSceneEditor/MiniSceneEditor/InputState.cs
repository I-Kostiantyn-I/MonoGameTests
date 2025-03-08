using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;

namespace MiniSceneEditor;

public class InputState
{
	public MouseState CurrentMouse { get; }
	public MouseState PreviousMouse { get; }
	public KeyboardState CurrentKeyboard { get; }
	public KeyboardState PreviousKeyboard { get; }

	public Vector2 MousePosition => new Vector2(CurrentMouse.X, CurrentMouse.Y);
	public Vector2 PreviousMousePosition => new Vector2(PreviousMouse.X, PreviousMouse.Y);
	public Vector2 MouseDelta => MousePosition - PreviousMousePosition;

	public InputState(MouseState currentMouse, KeyboardState currenKeyboard)
	{
		PreviousMouse = CurrentMouse;
		CurrentMouse = currentMouse;
		PreviousKeyboard = CurrentKeyboard;
		CurrentKeyboard = currenKeyboard;
	}

	// Перевірка натискання (кнопка була відпущена, стала натиснутою)
	public bool IsMouseButtonPressed(ButtonState button) =>
		CurrentMouse.LeftButton == ButtonState.Pressed &&
		PreviousMouse.LeftButton == ButtonState.Released;

	// Перевірка відпускання (кнопка була натиснута, стала відпущеною)
	public bool IsMouseButtonReleased(ButtonState button) =>
		CurrentMouse.LeftButton == ButtonState.Released &&
		PreviousMouse.LeftButton == ButtonState.Pressed;

	// Перевірка, чи утримується кнопка
	public bool IsMouseButtonDown(ButtonState button) =>
		CurrentMouse.LeftButton == ButtonState.Pressed &&
		PreviousMouse.LeftButton == ButtonState.Pressed;

	public bool IsShiftDown() =>
		CurrentKeyboard.IsKeyDown(Keys.LeftShift) || CurrentKeyboard.IsKeyDown(Keys.RightShift);

	public bool IsControlDown() =>
		CurrentKeyboard.IsKeyDown(Keys.LeftControl) || CurrentKeyboard.IsKeyDown(Keys.RightControl);

	public bool IsAltDown() =>
		CurrentKeyboard.IsKeyDown(Keys.LeftAlt) || CurrentKeyboard.IsKeyDown(Keys.RightAlt);

	public bool IsKeyDown(Keys key)
	{
		return CurrentKeyboard.IsKeyDown(key);
	}

	public bool IsKeyPressed(Keys key)
	{
		return CurrentKeyboard.IsKeyDown(key) && !PreviousKeyboard.IsKeyDown(key);
	}

	public bool IsKeyReleased(Keys key)
	{
		return !CurrentKeyboard.IsKeyDown(key) && PreviousKeyboard.IsKeyDown(key);
	}

	// Методи для перевірки клавіш керування камерою
	public bool IsForwardPressed() => CurrentKeyboard.IsKeyDown(Keys.W);
	public bool IsBackwardPressed() => CurrentKeyboard.IsKeyDown(Keys.S);
	public bool IsLeftPressed() => CurrentKeyboard.IsKeyDown(Keys.A);
	public bool IsRightPressed() => CurrentKeyboard.IsKeyDown(Keys.D);
	public bool IsUpPressed() => CurrentKeyboard.IsKeyDown(Keys.Space);
	public bool IsDownPressed() => CurrentKeyboard.IsKeyDown(Keys.LeftControl);

	// Для визначення, чи використовуються клавіші для керування камерою
	public bool IsAnyCameraKeyPressed()
	{
		return CurrentKeyboard.IsKeyDown(Keys.W) ||
			   CurrentKeyboard.IsKeyDown(Keys.A) ||
			   CurrentKeyboard.IsKeyDown(Keys.S) ||
			   CurrentKeyboard.IsKeyDown(Keys.D) ||
			   CurrentKeyboard.IsKeyDown(Keys.Space) ||
			   CurrentKeyboard.IsKeyDown(Keys.LeftControl) ||
			   CurrentKeyboard.IsKeyDown(Keys.LeftShift) ||
			   CurrentKeyboard.IsKeyDown(Keys.RightShift) ||
			   CurrentKeyboard.IsKeyDown(Keys.LeftAlt) ||
			   CurrentKeyboard.IsKeyDown(Keys.RightAlt);
	}
}