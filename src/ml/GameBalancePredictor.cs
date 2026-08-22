using System;
using System.IO;
using Microsoft.ML;

namespace DroneGameLocal;

// ML-6 CHANGE:
// Loads the trained ML.NET model and predicts whether the current game
// feels TooEasy, Balanced, or TooHard.
//
// ML-7 CHANGE:
// Also returns prediction confidence as a percentage value.
public sealed class GameBalancePredictor
{
    private readonly MLContext _mlContext = new(seed: 1);

    private PredictionEngine<GameBalanceModelInput, GameBalanceModelOutput>? _predictionEngine;

    public bool IsLoaded { get; private set; }
    public string LastError { get; private set; } = "";

    public GameBalancePredictor()
    {
        TryLoad();
    }

    public void TryLoad()
    {
        try
        {
            string root = FindProjectRoot();

            string modelPath = Path.Combine(
                root,
                "ml-models",
                "game-balance-model.zip"
            );

            if (!File.Exists(modelPath))
            {
                LastError = $"Model file not found: {modelPath}";
                IsLoaded = false;
                return;
            }

            ITransformer model = _mlContext.Model.Load(modelPath, out _);

            _predictionEngine =
                _mlContext.Model.CreatePredictionEngine<GameBalanceModelInput, GameBalanceModelOutput>(model);

            LastError = "";
            IsLoaded = true;
        }
        catch (Exception ex)
        {
            LastError = ex.Message;
            IsLoaded = false;

            // ML-6 DEBUG:
            // Show model loading errors in the terminal.
            Console.WriteLine("ML model failed to load:");
            Console.WriteLine(ex.Message);
        }
    }

    public GameBalancePredictionResult Predict(GameBalanceModelInput input)
    {
        if (!IsLoaded || _predictionEngine is null)
        {
            return new GameBalancePredictionResult
            {
                Label = "NO MODEL",
                Confidence = 0f
            };
        }

        try
        {
            GameBalanceModelOutput prediction = _predictionEngine.Predict(input);

            string label = string.IsNullOrWhiteSpace(prediction.PredictedLabel)
                ? "UNKNOWN"
                : prediction.PredictedLabel;

            float confidence = GetConfidence(prediction.Score);

            return new GameBalancePredictionResult
            {
                Label = label,
                Confidence = confidence
            };
        }
        catch (Exception ex)
        {
            LastError = ex.Message;

            return new GameBalancePredictionResult
            {
                Label = "ERROR",
                Confidence = 0f
            };
        }
    }

    // ML-7 CHANGE:
    // Reads the highest score from the ML.NET prediction output.
    // For SDCA Maximum Entropy, these scores usually behave like probabilities.
    private static float GetConfidence(float[] scores)
    {
        if (scores.Length == 0)
        {
            return 0f;
        }

        float max = scores[0];

        for (int i = 1; i < scores.Length; i++)
        {
            if (scores[i] > max)
            {
                max = scores[i];
            }
        }

        // Keep it safe between 0 and 1.
        if (max < 0f)
        {
            return 0f;
        }

        if (max > 1f)
        {
            return 1f;
        }

        return max;
    }

    private static string FindProjectRoot()
    {
        DirectoryInfo? directory = new DirectoryInfo(Directory.GetCurrentDirectory());

        while (directory is not null)
        {
            string projectFile = Path.Combine(directory.FullName, "DroneGameLocal.csproj");

            if (File.Exists(projectFile))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        return Directory.GetCurrentDirectory();
    }
}

// ML-7 CHANGE:
// Simple result object for ML prediction.
// Label = TooEasy / Balanced / TooHard.
// Confidence = 0.0 to 1.0.
public sealed class GameBalancePredictionResult
{
    public string Label { get; init; } = "UNKNOWN";
    public float Confidence { get; init; }
}