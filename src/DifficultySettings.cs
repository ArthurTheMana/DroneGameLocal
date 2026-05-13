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
    public int MaxObstacles { get; init; }
    public int PointsPerObstacle { get; init; }

    public float InitialSpawnInterval { get; init; }
    public float MinimumSpawnInterval { get; init; }
    public float SpawnIntervalDecrease { get; init; }
    public float DifficultyIncreaseEverySeconds { get; init; }

    public float MinObstacleSpeed { get; init; }
    public float MaxObstacleSpeed { get; init; }
    public float ScoreSpeedMultiplier { get; init; }

    // LEVEL 3C CHANGE:
    // This method returns the correct settings based on the selected difficulty.
    // Easy is slower and gives more lives.
    // Normal is balanced.
    // Hard is faster and gives fewer lives.
    public static DifficultySettings Get(DifficultyLevel level)
    {
        return level switch
        {
            DifficultyLevel.Easy => new DifficultySettings
            {
                Name = "Easy",
                StartingLives = 5,
                MaxObstacles = 6,
                PointsPerObstacle = 10,

                InitialSpawnInterval = 1.45f,
                MinimumSpawnInterval = 0.85f,
                SpawnIntervalDecrease = 0.05f,
                DifficultyIncreaseEverySeconds = 14f,

                MinObstacleSpeed = 140f,
                MaxObstacleSpeed = 220f,
                ScoreSpeedMultiplier = 0.18f
            },

            DifficultyLevel.Hard => new DifficultySettings
            {
                Name = "Hard",
                StartingLives = 2,
                MaxObstacles = 10,
                PointsPerObstacle = 15,

                InitialSpawnInterval = 0.95f,
                MinimumSpawnInterval = 0.45f,
                SpawnIntervalDecrease = 0.09f,
                DifficultyIncreaseEverySeconds = 8f,

                MinObstacleSpeed = 220f,
                MaxObstacleSpeed = 340f,
                ScoreSpeedMultiplier = 0.55f
            },

            _ => new DifficultySettings
            {
                Name = "Normal",
                StartingLives = 3,
                MaxObstacles = 8,
                PointsPerObstacle = 10,

                InitialSpawnInterval = 1.15f,
                MinimumSpawnInterval = 0.65f,
                SpawnIntervalDecrease = 0.08f,
                DifficultyIncreaseEverySeconds = 10f,

                MinObstacleSpeed = 180f,
                MaxObstacleSpeed = 280f,
                ScoreSpeedMultiplier = 0.35f
            }
        };
    }
}