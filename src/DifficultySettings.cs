namespace DroneGameLocal;

// LEVEL 3C CHANGE:
// DifficultyLevel is used by the start menu.
// The player can choose Easy, Normal, or Hard before starting the game.
public enum DifficultyLevel
{
    Easy,
    Normal,
    Hard
}

// LEVEL 3E CHANGE:
// DifficultySettings now has StartingMaxObstacles and SecondsToReachMaxObstacles.
// This allows the game to start with fewer obstacles,
// then slowly increase the active obstacle limit over time.
public sealed class DifficultySettings
{
    public string Name { get; init; } = "Normal";

    public int StartingLives { get; init; }

    // LEVEL 3E CHANGE:
    // StartingMaxObstacles = how many obstacles can appear near the beginning.
    // MaxObstacles = final maximum obstacle pressure.
    public int StartingMaxObstacles { get; init; }
    public int MaxObstacles { get; init; }

    public int PointsPerObstacle { get; init; }

    public float InitialSpawnInterval { get; init; }
    public float MinimumSpawnInterval { get; init; }
    public float SpawnIntervalDecrease { get; init; }
    public float DifficultyIncreaseEverySeconds { get; init; }

    public float MinObstacleSpeed { get; init; }
    public float MaxObstacleSpeed { get; init; }
    public float ScoreSpeedMultiplier { get; init; }

    // LEVEL 3E CHANGE:
    // Time needed to reach the maximum obstacle pressure.
    public float SecondsToReachMaxObstacles { get; init; }

    public static DifficultySettings Get(DifficultyLevel level)
    {
        return level switch
        {
            DifficultyLevel.Easy => new DifficultySettings
            {
                Name = "Easy",
                StartingLives = 5,

                StartingMaxObstacles = 3,
                MaxObstacles = 8,
                SecondsToReachMaxObstacles = 100f,

                PointsPerObstacle = 10,

                InitialSpawnInterval = 1.25f,
                MinimumSpawnInterval = 0.75f,
                SpawnIntervalDecrease = 0.05f,
                DifficultyIncreaseEverySeconds = 13f,

                MinObstacleSpeed = 140f,
                MaxObstacleSpeed = 230f,
                ScoreSpeedMultiplier = 0.20f
            },

            DifficultyLevel.Hard => new DifficultySettings
            {
                Name = "Hard",
                StartingLives = 2,

                StartingMaxObstacles = 5,
                MaxObstacles = 16,
                SecondsToReachMaxObstacles = 70f,

                PointsPerObstacle = 15,

                InitialSpawnInterval = 0.80f,
                MinimumSpawnInterval = 0.35f,
                SpawnIntervalDecrease = 0.10f,
                DifficultyIncreaseEverySeconds = 7f,

                MinObstacleSpeed = 220f,
                MaxObstacleSpeed = 360f,
                ScoreSpeedMultiplier = 0.60f
            },

            _ => new DifficultySettings
            {
                Name = "Normal",
                StartingLives = 3,

                StartingMaxObstacles = 4,
                MaxObstacles = 12,
                SecondsToReachMaxObstacles = 90f,

                PointsPerObstacle = 10,

                InitialSpawnInterval = 1.00f,
                MinimumSpawnInterval = 0.55f,
                SpawnIntervalDecrease = 0.08f,
                DifficultyIncreaseEverySeconds = 9f,

                MinObstacleSpeed = 180f,
                MaxObstacleSpeed = 300f,
                ScoreSpeedMultiplier = 0.40f
            }
        };
    }
}