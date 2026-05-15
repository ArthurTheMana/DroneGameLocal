using System;
using Microsoft.Xna.Framework;

namespace DroneGameLocal;

public sealed class ChargeShot
{
    public Vector2 Position { get; private set; }

    public float ChargeRatio { get; }

    public int Width => 18 + (int)(ChargeRatio * 22);
    public int Height => 8 + (int)(ChargeRatio * 10);

    // LEVEL 4A CHANGE:
    // Any charged shot can destroy Scout enemies.
    // Only a strong charged shot can destroy obstacles.
    public bool CanBreakObstacle => ChargeRatio >= 0.75f;

    public ChargeShot(Vector2 position, float chargeRatio)
    {
        Position = position;
        ChargeRatio = Math.Clamp(chargeRatio, 0f, 1f);
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