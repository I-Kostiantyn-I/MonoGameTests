using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MiniSceneEditor;

public class InputState
{
	public MouseState CurrentMouse { get; }
	public MouseState PreviousMouse { get; }
	public KeyboardState CurrentKeyboard { get; }

	public Vector2 MousePosition => new Vector2(CurrentMouse.X, CurrentMouse.Y);
	public Vector2 PreviousMousePosition => new Vector2(PreviousMouse.X, PreviousMouse.Y);
	public Vector2 MouseDelta => MousePosition - PreviousMousePosition;

	public InputState(MouseState currentMouse, MouseState previousMouse, KeyboardState keyboard)
	{
		CurrentMouse = currentMouse;
		PreviousMouse = previousMouse;
		CurrentKeyboard = keyboard;
	}

	public bool IsMouseButtonPressed(ButtonState button) =>
		CurrentMouse.LeftButton == button && PreviousMouse.LeftButton != button;

	public bool IsKeyPressed(Keys key) =>
		CurrentKeyboard.IsKeyDown(key);

	public bool IsShiftDown() =>
		CurrentKeyboard.IsKeyDown(Keys.LeftShift) || CurrentKeyboard.IsKeyDown(Keys.RightShift);

	public bool IsControlDown() =>
		CurrentKeyboard.IsKeyDown(Keys.LeftControl) || CurrentKeyboard.IsKeyDown(Keys.RightControl);

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