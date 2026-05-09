using Microsoft.Xna.Framework;

namespace DroneGameLocal;

public sealed class Obstacle
{
    public Vector2 Position { get; private set; }

    public int Width { get; }
    public int Height { get; }

    public float Speed { get; }

    public bool ScoreCounted { get; set; }

    public Obstacle(float x, float y, int width, int height, float speed)
    {
        Position = new Vector2(x, y);
        Width = width;
        Height = height;
        Speed = speed;
        ScoreCounted = false;
    }

    public void Update(float deltaTime)
    {
        Position = new Vector2(Position.X - Speed * deltaTime, Position.Y);
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