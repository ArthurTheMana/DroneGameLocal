namespace DroneGameLocal;

public static class GameSettings
{
    public const int ScreenWidth = 900;
    public const int ScreenHeight = 600;

    public const int WinScore = 200;
    public const int MaxObstacles = 8;

    public const int StartDroneX = 100;
    public const int StartDroneY = 280;

    public const float InitialSpawnInterval = 1.15f;
    public const float MinimumSpawnInterval = 0.65f;
    public const float SpawnIntervalDecrease = 0.08f;
    public const float DifficultyIncreaseEverySeconds = 10f;

    public const float CollisionCooldownSeconds = 1.0f;
}