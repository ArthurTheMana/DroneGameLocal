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
    // Bullet speed comes from DifficultySettings because Easy / Normal / Hard should feel different.
    public const int EnemyBulletWidth = 14;
    public const int EnemyBulletHeight = 6;

    // LEVEL 5A CHANGE:
    // Tank shield settings.
    // Tank creates temporary shields instead of shooting bullets.
    // Shields dissolve after a few seconds.
    public const int TankShieldWidth = 30;
    public const int TankShieldHeight = 72;
    public const int TankShieldHealth = 2;
    public const int MaxActiveTankShields = 4;
    public const float TankShieldLifetimeSeconds = 4.0f;

    // LEVEL 5A POLISH:
    // Sniper ambush behavior.
    // Sniper enters the screen, aims briefly, fires once, then retreats.
    public const float SniperStopXOffset = 260f;
    public const float SniperAimSeconds = 0.85f;
    public const float SniperRetreatSpeedMultiplier = 1.55f;
}