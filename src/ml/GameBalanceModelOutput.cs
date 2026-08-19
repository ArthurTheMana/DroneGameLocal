using Microsoft.ML.Data;

namespace DroneGameLocal;

// ML-6 CHANGE:
// This output class receives the prediction from the trained model.
public sealed class GameBalanceModelOutput
{
    [ColumnName("PredictedLabel")]
    public string PredictedLabel { get; set; } = "";

    public float[] Score { get; set; } = [];
}