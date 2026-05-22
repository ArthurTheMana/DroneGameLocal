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

    // LEVEL 5A POLISH:
    // Sniper ambush state.
    // Sniper enters, aims, fires once, then retreats.
    private readonly float _sniperStopX;
    private float _sniperAimTimer;
    private bool _sniperReadyToFire;
    private bool _sniperHasFired;

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

        // LEVEL 5A POLISH:
        // Sniper stops near the right side, aims, then fires.
        _sniperStopX = GameSettings.ScreenWidth - GameSettings.SniperStopXOffset;
        _sniperAimTimer = GameSettings.SniperAimSeconds;
    }

    public void Update(float deltaTime)
    {
        if (Type == EnemyType.Sniper)
        {
            UpdateSniper(deltaTime);
            return;
        }

        float verticalMovement = Type switch
        {
            // LEVEL 5A POLISH:
            // ZigZag moves wider and more aggressively.
            EnemyType.ZigZag => (float)Math.Sin(Position.X * 0.12f) * 260f * deltaTime,

            // LEVEL 5A CHANGE:
            // Tank moves more steadily because it is heavier.
            EnemyType.Tank => (float)Math.Sin(Position.X * 0.015f) * 12f * deltaTime,

            // LEVEL 4A CHANGE:
            // Scout uses the original small wave movement.
            _ => (float)Math.Sin(Position.X * 0.03f) * 35f * deltaTime
        };

        Position = new Vector2(
            Position.X - Speed * deltaTime,
            Position.Y + verticalMovement
        );

        ClampToPlayableArea();

        // LEVEL 4B CHANGE:
        // Enemy shooting/deployment timer.
        _shootTimer -= deltaTime;
    }

    // LEVEL 5A POLISH:
    // Sniper enters from the right, stops, aims, fires once,
    // then retreats back to the right.
    private void UpdateSniper(float deltaTime)
    {
        if (!_sniperHasFired && Position.X > _sniperStopX)
        {
            Position = new Vector2(
                Position.X - Speed * deltaTime,
                Position.Y + (float)Math.Sin(Position.X * 0.01f) * 8f * deltaTime
            );

            ClampToPlayableArea();
            return;
        }

        if (!_sniperHasFired)
        {
            _sniperAimTimer -= deltaTime;

            Position = new Vector2(
                _sniperStopX,
                Position.Y + (float)Math.Sin(_sniperAimTimer * 12f) * 4f * deltaTime
            );

            ClampToPlayableArea();

            if (_sniperAimTimer <= 0f)
            {
                _sniperReadyToFire = true;
            }

            return;
        }

        Position = new Vector2(
            Position.X + Speed * GameSettings.SniperRetreatSpeedMultiplier * deltaTime,
            Position.Y
        );

        ClampToPlayableArea();
    }

    private void ClampToPlayableArea()
    {
        Position = new Vector2(
            Position.X,
            MathHelper.Clamp(Position.Y, 135, GameSettings.ScreenHeight - Height - 20)
        );
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
        if (Type == EnemyType.Sniper)
        {
            return _sniperReadyToFire;
        }

        return _shootTimer <= 0f;
    }

    public void ResetShootTimer()
    {
        if (Type == EnemyType.Sniper)
        {
            _sniperReadyToFire = false;
            _sniperHasFired = true;
            return;
        }

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
    // ZigZag bullets are faster than Scout bullets.
    // Tank does not shoot bullets after Level 5A shield system.
    public float GetBulletSpeedMultiplier()
    {
        return Type switch
        {
            EnemyType.Sniper => 1.35f,
            EnemyType.ZigZag => 1.18f,
            EnemyType.Tank => 0.85f,
            _ => 1.0f
        };
    }

    public bool IsOffScreen()
    {
        if (Type == EnemyType.Sniper && _sniperHasFired)
        {
            return Position.X > GameSettings.ScreenWidth + Width + 40;
        }

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