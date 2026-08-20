namespace DroneGameLocal;

// ML-6 CHANGE:
// This input class is used by the trained ML.NET model.
// Property names must match the trainer project.
public sealed class GameBalanceModelInput
{
    public float SurvivalSeconds { get; set; }
    public float Score { get; set; }
    public float Lives { get; set; }

    public float ActiveObstacles { get; set; }
    public float CurrentMaxObstacles { get; set; }
    public float ObstaclePressure { get; set; }

    public float ActiveEnemies { get; set; }
    public float CurrentMaxEnemies { get; set; }
    public float EnemyPressure { get; set; }

    public float ActiveEnemyBullets { get; set; }
    public float ActivePlayerShots { get; set; }
    public float ShotCharges { get; set; }
    public float ActiveShields { get; set; }

    public string Difficulty { get; set; } = "Normal";
    public string ControlMode { get; set; } = "Human";

    // ML-6 FIX:
    // The trained pipeline includes a Label column because it was used during training.
    // During live prediction, we do not know the real label yet,
    // so we provide a dummy value to satisfy the model schema.
    public string Label { get; set; } = "Balanced";
}