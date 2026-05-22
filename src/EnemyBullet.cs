using Microsoft.Xna.Framework;

namespace DroneGameLocal;

public sealed class EnemyBullet
{
    public Vector2 Position { get; private set; }

    public int Width { get; } = GameSettings.EnemyBulletWidth;
    public int Height { get; } = GameSettings.EnemyBulletHeight;

    // LEVEL 4E POLISH:
    // Enemy bullet speed is no longer fixed globally.
    // It now comes from the selected difficulty and time progression.
    public float Speed { get; }

    public EnemyBullet(Vector2 position, float speed)
    {
        Position = position;
        Speed = speed;
    }

    public void Update(float deltaTime)
    {
        Position = new Vector2(
            Position.X - Speed * deltaTime,
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