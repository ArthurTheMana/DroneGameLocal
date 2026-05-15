using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;

namespace DroneGameLocal;

public sealed class InputManager
{
    private KeyboardState _previousKeyboard;

    public KeyboardState CurrentKeyboard { get; private set; }

    public void Update()
    {
        _previousKeyboard = CurrentKeyboard;
        CurrentKeyboard = Keyboard.GetState();
    }

    public bool IsKeyPressed(Keys key)
    {
        return CurrentKeyboard.IsKeyDown(key) &&
               !_previousKeyboard.IsKeyDown(key);
    }

    // LEVEL 4A CHANGE:
    // Needed for charged shot.
    // We fire the shot when player releases J.
    public bool IsKeyReleased(Keys key)
    {
        return !CurrentKeyboard.IsKeyDown(key) &&
               _previousKeyboard.IsKeyDown(key);
    }

    public bool IsKeyDown(Keys key)
    {
        return CurrentKeyboard.IsKeyDown(key);
    }

    public Vector2 GetMovementDirection()
    {
        Vector2 direction = Vector2.Zero;

        if (IsKeyDown(Keys.Up) || IsKeyDown(Keys.W))
        {
            direction.Y -= 1;
        }

        if (IsKeyDown(Keys.Down) || IsKeyDown(Keys.S))
        {
            direction.Y += 1;
        }

        if (IsKeyDown(Keys.Left) || IsKeyDown(Keys.A))
        {
            direction.X -= 1;
        }

        if (IsKeyDown(Keys.Right) || IsKeyDown(Keys.D))
        {
            direction.X += 1;
        }

        return direction;
    }
}