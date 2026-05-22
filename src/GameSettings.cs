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

    // LEVEL 4D POLISH:
    // Recharge is slightly slower now, so shots feel useful but not spammy.
    public const float ShotRechargeSeconds = 1.45f;

    public const float ShotSpeed = 650f;

    // LEVEL 4B CHANGE:
    // Enemy bullet settings.
    // Enemy bullets move from right to left.
    public const int EnemyBulletWidth = 14;
    public const int EnemyBulletHeight = 6;

    // LEVEL 4D POLISH:
    // Enemy bullets are slower than before.
    // This gives the player a fair chance to dodge.
    public const float EnemyBulletSpeed = 310f;
}