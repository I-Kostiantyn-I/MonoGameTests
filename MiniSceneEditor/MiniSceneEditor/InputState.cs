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
}