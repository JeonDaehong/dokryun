using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;

namespace Dokryun.Engine;

public static class InputManager
{
    private static KeyboardState _currentKeyboard;
    private static KeyboardState _previousKeyboard;
    private static MouseState _currentMouse;
    private static MouseState _previousMouse;

    public static Vector2 MousePosition => _currentMouse.Position.ToVector2();

    public static void Update()
    {
        _previousKeyboard = _currentKeyboard;
        _currentKeyboard = Keyboard.GetState();
        _previousMouse = _currentMouse;
        _currentMouse = Mouse.GetState();
    }

    public static bool IsKeyDown(Keys key) => _currentKeyboard.IsKeyDown(key);
    public static bool IsKeyPressed(Keys key) => _currentKeyboard.IsKeyDown(key) && !_previousKeyboard.IsKeyDown(key);
    public static bool IsKeyReleased(Keys key) => !_currentKeyboard.IsKeyDown(key) && _previousKeyboard.IsKeyDown(key);

    public static bool IsLeftClick() => _currentMouse.LeftButton == ButtonState.Pressed && _previousMouse.LeftButton == ButtonState.Released;
    public static bool IsRightClick() => _currentMouse.RightButton == ButtonState.Pressed && _previousMouse.RightButton == ButtonState.Released;
    public static bool IsLeftHeld() => _currentMouse.LeftButton == ButtonState.Pressed;

    public static Vector2 GetMovementDirection()
    {
        var dir = Vector2.Zero;
        if (IsKeyDown(Keys.W) || IsKeyDown(Keys.Up)) dir.Y -= 1;
        if (IsKeyDown(Keys.S) || IsKeyDown(Keys.Down)) dir.Y += 1;
        if (IsKeyDown(Keys.A) || IsKeyDown(Keys.Left)) dir.X -= 1;
        if (IsKeyDown(Keys.D) || IsKeyDown(Keys.Right)) dir.X += 1;

        if (dir != Vector2.Zero)
            dir.Normalize();

        return dir;
    }
}
