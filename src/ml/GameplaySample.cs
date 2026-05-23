using System.Globalization;

namespace DroneGameLocal;

// ML-1 CHANGE:
// This class represents one row of gameplay training data.
// Each row captures the current game state and a player-provided label.
// Later, ML.NET can train a model using this data.
public sealed class GameplaySample
{
    public float SurvivalSeconds { get; init; }
    public int Score { get; init; }
    public int Lives { get; init; }

    public int ActiveObstacles { get; init; }
    public int CurrentMaxObstacles { get; init; }
    public float ObstaclePressure { get; init; }

    public int ActiveEnemies { get; init; }
    public int CurrentMaxEnemies { get; init; }
    public float EnemyPressure { get; init; }

    public int ActiveEnemyBullets { get; init; }
    public int ActivePlayerShots { get; init; }
    public int ShotCharges { get; init; }
    public int ActiveShields { get; init; }

    public string Difficulty { get; init; } = "Normal";
    public string Label { get; init; } = "Balanced";

    public static string CsvHeader =>
        "SurvivalSeconds,Score,Lives," +
        "ActiveObstacles,CurrentMaxObstacles,ObstaclePressure," +
        "ActiveEnemies,CurrentMaxEnemies,EnemyPressure," +
        "ActiveEnemyBullets,ActivePlayerShots,ShotCharges,ActiveShields," +
        "Difficulty,Label";

    public string ToCsvRow()
    {
        return string.Join(",",
            SurvivalSeconds.ToString(CultureInfo.InvariantCulture),
            Score,
            Lives,
            ActiveObstacles,
            CurrentMaxObstacles,
            ObstaclePressure.ToString(CultureInfo.InvariantCulture),
            ActiveEnemies,
            CurrentMaxEnemies,
            EnemyPressure.ToString(CultureInfo.InvariantCulture),
            ActiveEnemyBullets,
            ActivePlayerShots,
            ShotCharges,
            ActiveShields,
            Difficulty,
            Label
        );
    }
}