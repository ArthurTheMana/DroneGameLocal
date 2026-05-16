using Microsoft.Xna.Framework;

namespace DroneGameLocal;

public sealed class EnemyBullet
{
    public Vector2 Position { get; private set; }

    public int Width { get; } = GameSettings.EnemyBulletWidth;
    public int Height { get; } = GameSettings.EnemyBulletHeight;

    public EnemyBullet(Vector2 position)
    {
        Position = position;
    }

    public void Update(float deltaTime)
    {
        Position = new Vector2(
            Position.X - GameSettings.EnemyBulletSpeed * deltaTime,
            Position.Y
        );
    }

    public bool IsOffScreen()
    {
        return Position.X + Width < 0;
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