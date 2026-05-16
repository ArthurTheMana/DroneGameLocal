namespace DroneGameLocal;

public static class GameSettings
{
    public const int ScreenWidth = 900;
    public const int ScreenHeight = 600;

    public const int StartDroneX = 100;
    public const int StartDroneY = 280;

    public const float CollisionCooldownSeconds = 1.0f;

    // LEVEL 4A CHANGE:
    // Charged shot settings.
    public const float ShotMinChargeSeconds = 0.35f;
    public const float ShotMaxChargeSeconds = 1.50f;
    public const float ShotCooldownSeconds = 0.80f;
    public const float ShotSpeed = 650f;

    // LEVEL 4B CHANGE:
    // Enemy bullet settings.
    // Enemy bullets move from right to left.
    public const float EnemyBulletSpeed = 360f;
    public const int EnemyBulletWidth = 14;
    public const int EnemyBulletHeight = 6;
}