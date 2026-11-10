using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;

namespace EchoesOfThePast.Core;

/// <summary>One frame of input: keyboard, and the mouse already converted to world pixels. Polled once per frame by the game.</summary>
public sealed class InputState
{
    private KeyboardState _previousKeyboard;
    private MouseState _previousMouse;

    public KeyboardState KeyboardState { get; private set; }
    public MouseState MouseState { get; private set; }

    /// <summary>Mouse position in world pixels.</summary>
    public Point Cursor { get; private set; }

    /// <summary>True on the frame the left button goes down while the window is active.</summary>
    public bool Clicked { get; private set; }

    public void Update(bool windowActive, ViewTransform view)
    {
        _previousKeyboard = KeyboardState;
        _previousMouse = MouseState;
        KeyboardState = Keyboard.GetState();
        MouseState = Mouse.GetState();

        Cursor = view.ToWorld(new Point(MouseState.X, MouseState.Y));
        Clicked = windowActive
            && MouseState.LeftButton == ButtonState.Pressed
            && _previousMouse.LeftButton == ButtonState.Released;
    }

    /// <summary>True on the frame the key goes down.</summary>
    public bool Pressed(Keys key) => KeyboardState.IsKeyDown(key) && _previousKeyboard.IsKeyUp(key);
}
