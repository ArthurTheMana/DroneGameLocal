namespace DroneGameLocal;

public static class GameSettings
{
    public const int ScreenWidth = 900;
    public const int ScreenHeight = 600;

    public const int StartDroneX = 100;
    public const int StartDroneY = 280;

    public const float CollisionCooldownSeconds = 1.0f;

    // LEVEL 4C CHANGE:
    // Auto charge shot system.
    // Charges build by themselves up to MaxShotCharges.
    // Press J to spend 1 charge and shoot.
    public const int MaxShotCharges = 3;
    public const float ShotRechargeSeconds = 1.20f;
    public const float ShotSpeed = 650f;

    // LEVEL 4B CHANGE:
    // Enemy bullet settings.
    public const float EnemyBulletSpeed = 360f;
    public const int EnemyBulletWidth = 14;
    public const int EnemyBulletHeight = 6;
}