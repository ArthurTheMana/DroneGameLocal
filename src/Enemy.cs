using System;
using Microsoft.Xna.Framework;

namespace DroneGameLocal;

public sealed class Enemy
{
    public EnemyType Type { get; }

    public Vector2 Position { get; private set; }

    public int Width { get; }
    public int Height { get; }

    public float Speed { get; }
    public int ScoreReward { get; }

    // LEVEL 5A CHANGE:
    // Enemies now have health.
    // Scout, ZigZag, and Sniper have 1 HP.
    // Tank has 2 HP.
    public int Health { get; private set; }
    public int MaxHealth { get; }

    private readonly float _shootInterval;
    private float _shootTimer;

    public Enemy(
        EnemyType type,
        float x,
        float y,
        int width,
        int height,
        float speed,
        int scoreReward,
        int health,
        float shootInterval)
    {
        Type = type;
        Position = new Vector2(x, y);
        Width = width;
        Height = height;
        Speed = speed;
        ScoreReward = scoreReward;
        Health = health;
        MaxHealth = health;

        _shootInterval = shootInterval;

        // LEVEL 4B CHANGE:
        // Start halfway through cooldown so enemies do not shoot immediately.
        // For Tank, this timer is used for shield deployment.
        _shootTimer = shootInterval * 0.5f;
    }

    public void Update(float deltaTime)
    {
        float verticalMovement = Type switch
        {
            // LEVEL 5A POLISH:
            // ZigZag now moves wider and more aggressively.
            // This makes its movement easier to notice during gameplay.
            EnemyType.ZigZag => (float)Math.Sin(Position.X * 0.12f) * 260f * deltaTime,

            // LEVEL 5A CHANGE:
            // Tank moves more steadily because it is heavier.
            EnemyType.Tank => (float)Math.Sin(Position.X * 0.015f) * 12f * deltaTime,

            // LEVEL 5A CHANGE:
            // Sniper moves very slightly so it feels focused and dangerous.
            EnemyType.Sniper => (float)Math.Sin(Position.X * 0.01f) * 8f * deltaTime,

            // LEVEL 4A CHANGE:
            // Scout uses the original small wave movement.
            _ => (float)Math.Sin(Position.X * 0.03f) * 35f * deltaTime
        };

        Position = new Vector2(
            Position.X - Speed * deltaTime,
            Position.Y + verticalMovement
        );

        Position = new Vector2(
            Position.X,
            MathHelper.Clamp(Position.Y, 135, GameSettings.ScreenHeight - Height - 20)
        );

        // LEVEL 4B CHANGE:
        // Enemy shooting/deployment timer.
        _shootTimer -= deltaTime;
    }

    // LEVEL 5A CHANGE:
    // Player shots now damage enemies.
    public void TakeDamage(int damage)
    {
        if (damage <= 0)
        {
            return;
        }

        Health -= damage;
    }

    public bool IsDestroyed()
    {
        return Health <= 0;
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

    // LEVEL 5A CHANGE:
    // Different enemy types can modify bullet speed.
    // Sniper bullets are faster.
    // Tank does not shoot bullets after Level 5A shield system,
    // but keeping the multiplier safe does not hurt.
    public float GetBulletSpeedMultiplier()
    {
        return Type switch
        {
            EnemyType.Sniper => 1.35f,
            EnemyType.Tank => 0.85f,
            _ => 1.0f
        };
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