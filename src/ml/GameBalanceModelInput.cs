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
}