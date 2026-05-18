using Microsoft.Xna.Framework;

namespace DroneGameLocal;

public sealed class ChargeShot
{
    public Vector2 Position { get; private set; }

    public int Width { get; } = 34;
    public int Height { get; } = 12;

    // LEVEL 4C CHANGE:
    // In the new auto-charge system, every stored charge fires a strong shot.
    // This keeps the system simple: 1 charge = 1 useful shot.
    public bool CanBreakObstacle => true;

    public ChargeShot(Vector2 position)
    {
        Position = position;
    }

    public void Update(float deltaTime)
    {
        Position = new Vector2(
            Position.X + GameSettings.ShotSpeed * deltaTime,
            Position.Y
        );
    }

    public bool IsOffScreen()
    {
        return Position.X > GameSettings.ScreenWidth;
    }

    public Rectangle GetBounds()
    {
        return new Rectangle(
            (int)Position.X,
            (int)Position.Y,
            Width,
            Height
        );
    }
}