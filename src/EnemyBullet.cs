using Microsoft.Xna.Framework;

namespace DroneGameLocal;

public sealed class EnemyBullet
{
    public Vector2 Position { get; private set; }

    // COLLISION POLISH:
    // PreviousPosition is used for swept collision.
    public Vector2 PreviousPosition { get; private set; }

    public int Width { get; } = GameSettings.EnemyBulletWidth;
    public int Height { get; } = GameSettings.EnemyBulletHeight;

    public float Speed { get; }

    public EnemyBullet(Vector2 position, float speed)
    {
        Position = position;
        PreviousPosition = position;
        Speed = speed;
    }

    public void Update(float deltaTime)
    {
        PreviousPosition = Position;

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

    // COLLISION POLISH:
    // Covers the bullet path between frames.
    // This prevents fast enemy bullets from missing the drone unfairly.
    public Rectangle GetSweptBounds()
    {
        Rectangle previous = new Rectangle(
            (int)PreviousPosition.X,
            (int)PreviousPosition.Y,
            Width,
            Height
        );

        Rectangle current = GetBounds();

        int left = MathHelper.Min(previous.Left, current.Left);
        int top = MathHelper.Min(previous.Top, current.Top);
        int right = MathHelper.Max(previous.Right, current.Right);
        int bottom = MathHelper.Max(previous.Bottom, current.Bottom);

        int padding = 3;

        return new Rectangle(
            left - padding,
            top - padding,
            right - left + padding * 2,
            bottom - top + padding * 2
        );
    }
}