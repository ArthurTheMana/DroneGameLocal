using Microsoft.Xna.Framework;

namespace DroneGameLocal;

public sealed class EnergyShield
{
    public Vector2 Position { get; private set; }

    public int Width { get; } = GameSettings.TankShieldWidth;
    public int Height { get; } = GameSettings.TankShieldHeight;

    public int Health { get; private set; } = GameSettings.TankShieldHealth;
    public int MaxHealth { get; } = GameSettings.TankShieldHealth;

    private readonly float _speed;
    private readonly float _maxLife;
    private float _lifeRemaining;

    public float LifePercent =>
        MathHelper.Clamp(_lifeRemaining / _maxLife, 0f, 1f);

    // LEVEL 5A CHANGE:
    // EnergyShield is created by Tank enemies.
    // It moves left slowly, blocks player shots,
    // damages the player on contact, and dissolves after a short time.
    public EnergyShield(Vector2 position, float speed)
    {
        Position = position;
        _speed = speed;

        _maxLife = GameSettings.TankShieldLifetimeSeconds;
        _lifeRemaining = _maxLife;
    }

    public void Update(float deltaTime)
    {
        Position = new Vector2(
            Position.X - _speed * deltaTime,
            Position.Y
        );

        _lifeRemaining -= deltaTime;
    }

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

    public bool IsExpired()
    {
        return _lifeRemaining <= 0f ||
               Health <= 0 ||
               Position.X + Width < 0;
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