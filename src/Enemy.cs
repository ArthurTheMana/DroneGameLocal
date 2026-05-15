using System;
using Microsoft.Xna.Framework;

namespace DroneGameLocal;

public sealed class Enemy
{
    public Vector2 Position { get; private set; }

    public int Width { get; } = 36;
    public int Height { get; } = 36;

    public float Speed { get; }
    public int ScoreReward { get; }

    public Enemy(float x, float y, float speed, int scoreReward)
    {
        Position = new Vector2(x, y);
        Speed = speed;
        ScoreReward = scoreReward;
    }

    public void Update(float deltaTime)
    {
        // LEVEL 4A CHANGE:
        // Scout enemy moves from right to left.
        // It also moves slightly up/down so it feels different from obstacles.
        float wave = (float)Math.Sin(Position.X * 0.03f) * 35f * deltaTime;

        Position = new Vector2(
            Position.X - Speed * deltaTime,
            Position.Y + wave
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