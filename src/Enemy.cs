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

    private readonly float _shootInterval;
    private float _shootTimer;

    public Enemy(float x, float y, float speed, int scoreReward, float shootInterval)
    {
        Position = new Vector2(x, y);
        Speed = speed;
        ScoreReward = scoreReward;

        _shootInterval = shootInterval;

        // LEVEL 4B CHANGE:
        // Start halfway through cooldown so enemies do not shoot immediately
        // the moment they spawn.
        _shootTimer = shootInterval * 0.5f;
    }

    public void Update(float deltaTime)
    {
        float wave = (float)Math.Sin(Position.X * 0.03f) * 35f * deltaTime;

        Position = new Vector2(
            Position.X - Speed * deltaTime,
            Position.Y + wave
        );

        // LEVEL 4B CHANGE:
        // Enemy shooting timer.
        _shootTimer -= deltaTime;
    }

    public bool CanShoot()
    {
        return _shootTimer <= 0f;
    }

    public void ResetShootTimer()
    {
        _shootTimer = _shootInterval;
    }

    public Vector2 GetShootPosition()
    {
        return new Vector2(
            Position.X - GameSettings.EnemyBulletWidth,
            Position.Y + Height / 2f - GameSettings.EnemyBulletHeight / 2f
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