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

// LEVEL 3C CHANGE:
// DifficultySettings contains the gameplay numbers for each mode.
// Instead of hardcoding everything inside Game1.cs,
// we keep the difficulty values here.
public sealed class DifficultySettings
{
    public string Name { get; init; } = "Normal";

    public int StartingLives { get; init; }

    // LEVEL 3E CHANGE:
    // StartingMaxObstacles = how many obstacles can appear near the beginning.
    // MaxObstacles = final maximum obstacle pressure.
    public int StartingMaxObstacles { get; init; }
    public int MaxObstacles { get; init; }

    // LEVEL 3E CHANGE:
    // Time needed to reach the maximum obstacle pressure.
    public float SecondsToReachMaxObstacles { get; init; }

    public int PointsPerObstacle { get; init; }

    public float InitialSpawnInterval { get; init; }
    public float MinimumSpawnInterval { get; init; }
    public float SpawnIntervalDecrease { get; init; }
    public float DifficultyIncreaseEverySeconds { get; init; }

    public float MinObstacleSpeed { get; init; }
    public float MaxObstacleSpeed { get; init; }
    public float ScoreSpeedMultiplier { get; init; }

    // LEVEL 4A CHANGE:
    // Enemy progression settings.
    // Enemies start low, then increase over time up to MaxEnemies.
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

    // LEVEL 4E POLISH:
    // Enemy bullet speed now depends on difficulty and time survived.
    // EnemyBulletStartSpeed = bullet speed near the beginning.
    // EnemyBulletMaxSpeed = bullet speed after enemy pressure reaches maximum.
    public float EnemyBulletStartSpeed { get; init; }
    public float EnemyBulletMaxSpeed { get; init; }

    public static DifficultySettings Get(DifficultyLevel level)
    {
        return level switch
        {
            DifficultyLevel.Easy => new DifficultySettings
            {
                Name = "Easy",
                StartingLives = 2,

                // LEVEL 4D POLISH:
                // Easy starts calm and grows slowly.
                StartingMaxObstacles = 3,
                MaxObstacles = 7,
                SecondsToReachMaxObstacles = 120f,

                PointsPerObstacle = 10,

                InitialSpawnInterval = 1.35f,
                MinimumSpawnInterval = 0.85f,
                SpawnIntervalDecrease = 0.04f,
                DifficultyIncreaseEverySeconds = 15f,

                MinObstacleSpeed = 130f,
                MaxObstacleSpeed = 210f,
                ScoreSpeedMultiplier = 0.16f,

                StartingMaxEnemies = 0,
                MaxEnemies = 2,
                SecondsToReachMaxEnemies = 110f,

                InitialEnemySpawnInterval = 5.5f,
                MinimumEnemySpawnInterval = 4.0f,
                EnemySpawnIntervalDecrease = 0.12f,
                EnemyDifficultyIncreaseEverySeconds = 20f,

                MinEnemySpeed = 80f,
                MaxEnemySpeed = 125f,
                EnemyScoreSpeedMultiplier = 0.06f,

                PointsPerEnemy = 25,

                MinEnemyShootInterval = 4.0f,
                MaxEnemyShootInterval = 6.2f,

                // LEVEL 4E POLISH:
                // Easy bullets start slow and only become moderately faster over time.
                EnemyBulletStartSpeed = 240f,
                EnemyBulletMaxSpeed = 300f
            },

            DifficultyLevel.Hard => new DifficultySettings
            {
                Name = "Hard",
                StartingLives = 2,

                // LEVEL 4D POLISH:
                // Hard is intense, but still avoids instant chaos.
                StartingMaxObstacles = 5,
                MaxObstacles = 15,
                SecondsToReachMaxObstacles = 85f,

                PointsPerObstacle = 15,

                InitialSpawnInterval = 0.90f,
                MinimumSpawnInterval = 0.42f,
                SpawnIntervalDecrease = 0.08f,
                DifficultyIncreaseEverySeconds = 8f,

                MinObstacleSpeed = 210f,
                MaxObstacleSpeed = 340f,
                ScoreSpeedMultiplier = 0.48f,

                StartingMaxEnemies = 1,
                MaxEnemies = 6,
                SecondsToReachMaxEnemies = 70f,

                InitialEnemySpawnInterval = 3.4f,
                MinimumEnemySpawnInterval = 2.0f,
                EnemySpawnIntervalDecrease = 0.16f,
                EnemyDifficultyIncreaseEverySeconds = 12f,

                MinEnemySpeed = 140f,
                MaxEnemySpeed = 220f,
                EnemyScoreSpeedMultiplier = 0.14f,

                PointsPerEnemy = 40,

                MinEnemyShootInterval = 1.9f,
                MaxEnemyShootInterval = 3.1f,

                // LEVEL 4E POLISH:
                // Normal bullets start fair, then become faster as the player survives longer.
                EnemyBulletStartSpeed = 300f,
                EnemyBulletMaxSpeed = 380f
            },

            _ => new DifficultySettings
            {
                Name = "Normal",
                StartingLives = 2,

                // LEVEL 4D POLISH:
                // Normal should be the best default experience.
                StartingMaxObstacles = 4,
                MaxObstacles = 11,
                SecondsToReachMaxObstacles = 100f,

                PointsPerObstacle = 10,

                InitialSpawnInterval = 1.10f,
                MinimumSpawnInterval = 0.62f,
                SpawnIntervalDecrease = 0.06f,
                DifficultyIncreaseEverySeconds = 10f,

                MinObstacleSpeed = 170f,
                MaxObstacleSpeed = 280f,
                ScoreSpeedMultiplier = 0.32f,

                StartingMaxEnemies = 1,
                MaxEnemies = 4,
                SecondsToReachMaxEnemies = 85f,

                InitialEnemySpawnInterval = 4.4f,
                MinimumEnemySpawnInterval = 2.8f,
                EnemySpawnIntervalDecrease = 0.14f,
                EnemyDifficultyIncreaseEverySeconds = 16f,

                MinEnemySpeed = 100f,
                MaxEnemySpeed = 165f,
                EnemyScoreSpeedMultiplier = 0.10f,

                PointsPerEnemy = 30,

                MinEnemyShootInterval = 2.8f,
                MaxEnemyShootInterval = 4.4f,

                // LEVEL 4E POLISH:
                // Hard bullets start fast and become very dangerous later.
                EnemyBulletStartSpeed = 370f,
                EnemyBulletMaxSpeed = 480f
            }
        };
    }
}