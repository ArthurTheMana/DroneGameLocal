using System.Globalization;

namespace DroneGameLocal;

// ML-1 CHANGE:
// This class represents one row of gameplay data.
// Each row records the current game state and the label chosen by the player or bot.
//
// ML-3 CHANGE:
// ControlMode tells us whether the row came from a Human run or Bot run.
// AutoLabelReason explains why the label was chosen.
//
// ML-4 CHANGE:
// TimeRating and ScoreRating are stored separately.
// This makes the dataset easier to understand and debug.
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
    public string ControlMode { get; init; } = "Human";

    // ML-4 CHANGE:
    // Time-based rating and score-based rating are separated.
    public string TimeRating { get; init; } = "Balanced";
    public string ScoreRating { get; init; } = "Balanced";

    public string AutoLabelReason { get; init; } = "ManualHumanFeedback";
    public string Label { get; init; } = "Balanced";

    public static string CsvHeader =>
        "SurvivalSeconds,Score,Lives," +
        "ActiveObstacles,CurrentMaxObstacles,ObstaclePressure," +
        "ActiveEnemies,CurrentMaxEnemies,EnemyPressure," +
        "ActiveEnemyBullets,ActivePlayerShots,ShotCharges,ActiveShields," +
        "Difficulty,ControlMode,TimeRating,ScoreRating,AutoLabelReason,Label";

    public string ToCsvRow()
    {
        return string.Join(",",
            SurvivalSeconds.ToString("0.00", CultureInfo.InvariantCulture),
            Score,
            Lives,
            ActiveObstacles,
            CurrentMaxObstacles,
            ObstaclePressure.ToString("0.00", CultureInfo.InvariantCulture),
            ActiveEnemies,
            CurrentMaxEnemies,
            EnemyPressure.ToString("0.00", CultureInfo.InvariantCulture),
            ActiveEnemyBullets,
            ActivePlayerShots,
            ShotCharges,
            ActiveShields,
            Difficulty,
            ControlMode,
            TimeRating,
            ScoreRating,
            AutoLabelReason,
            Label
        );
    }
}