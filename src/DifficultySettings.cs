namespace DroneGameLocal;

public enum DifficultyLevel
{
    Easy,
    Normal,
    Hard
}

public sealed class DifficultySettings
{
    public string Name { get; init; } = "Normal";

    public int StartingLives { get; init; }

    public int StartingMaxObstacles { get; init; }
    public int MaxObstacles { get; init; }
    public float SecondsToReachMaxObstacles { get; init; }

    public int PointsPerObstacle { get; init; }

    public float InitialSpawnInterval { get; init; }
    public float MinimumSpawnInterval { get; init; }
    public float SpawnIntervalDecrease { get; init; }
    public float DifficultyIncreaseEverySeconds { get; init; }

    public float MinObstacleSpeed { get; init; }
    public float MaxObstacleSpeed { get; init; }
    public float ScoreSpeedMultiplier { get; init; }

    public int StartingMaxEnemies { get; init; }
    public int MaxEnemies { get; init; }
    public float SecondsToReachMaxEnemies { get; init; }

    public float InitialEnemySpawnInterval { get; init; }
    public float MinimumEnemySpawnInterval { get; init; }
    public float EnemySpawnIntervalDecrease { get; init; }
    public float EnemyDifficultyIncreaseEverySeconds { get; init; }

    public float MinEnemySpeed { get; init; }
    public float MaxEnemySpeed { get; init; }
    public float EnemyScoreSpeedMultiplier { get; init; }

    public int PointsPerEnemy { get; init; }

    // LEVEL 4B CHANGE:
    // Enemy shooting interval.
    // Smaller number = enemies shoot more often.
    public float MinEnemyShootInterval { get; init; }
    public float MaxEnemyShootInterval { get; init; }

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
                ScoreSpeedMultiplier = 0.20f,

                StartingMaxEnemies = 0,
                MaxEnemies = 2,
                SecondsToReachMaxEnemies = 90f,

                InitialEnemySpawnInterval = 5.0f,
                MinimumEnemySpawnInterval = 3.8f,
                EnemySpawnIntervalDecrease = 0.15f,
                EnemyDifficultyIncreaseEverySeconds = 18f,

                MinEnemySpeed = 90f,
                MaxEnemySpeed = 140f,
                EnemyScoreSpeedMultiplier = 0.08f,

                PointsPerEnemy = 25,

                MinEnemyShootInterval = 3.5f,
                MaxEnemyShootInterval = 5.5f
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
                ScoreSpeedMultiplier = 0.60f,

                StartingMaxEnemies = 2,
                MaxEnemies = 6,
                SecondsToReachMaxEnemies = 55f,

                InitialEnemySpawnInterval = 3.0f,
                MinimumEnemySpawnInterval = 1.8f,
                EnemySpawnIntervalDecrease = 0.20f,
                EnemyDifficultyIncreaseEverySeconds = 10f,

                MinEnemySpeed = 150f,
                MaxEnemySpeed = 240f,
                EnemyScoreSpeedMultiplier = 0.18f,

                PointsPerEnemy = 40,

                MinEnemyShootInterval = 1.5f,
                MaxEnemyShootInterval = 2.8f
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
                ScoreSpeedMultiplier = 0.40f,

                StartingMaxEnemies = 1,
                MaxEnemies = 4,
                SecondsToReachMaxEnemies = 70f,

                InitialEnemySpawnInterval = 4.0f,
                MinimumEnemySpawnInterval = 2.5f,
                EnemySpawnIntervalDecrease = 0.18f,
                EnemyDifficultyIncreaseEverySeconds = 14f,

                MinEnemySpeed = 110f,
                MaxEnemySpeed = 180f,
                EnemyScoreSpeedMultiplier = 0.12f,

                PointsPerEnemy = 30,

                MinEnemyShootInterval = 2.5f,
                MaxEnemyShootInterval = 4.0f
            }
        };
    }
}