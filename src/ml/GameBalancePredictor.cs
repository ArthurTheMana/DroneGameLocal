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
    // ML-7 POLISH:
    // The raw ML.NET score can look too confident, often showing 99%.
    // This method softens the confidence for HUD display only.
    // It does NOT change the predicted label.
    private static float GetConfidence(float[] scores)
    {
        if (scores.Length == 0)
        {
            return 0f;
        }

        const float confidenceTemperature = 2.5f;

        if (LooksLikeProbabilityVector(scores))
        {
            return GetSoftenedProbabilityConfidence(
                scores,
                confidenceTemperature
            );
        }

        return GetSoftmaxConfidence(
            scores,
            confidenceTemperature
        );
    }

    private static bool LooksLikeProbabilityVector(float[] scores)
    {
        float sum = 0f;

        for (int i = 0; i < scores.Length; i++)
        {
            if (scores[i] < 0f || scores[i] > 1f)
            {
                return false;
            }

            sum += scores[i];
        }

        return sum > 0.98f && sum < 1.02f;
    }

    private static float GetSoftenedProbabilityConfidence(
        float[] probabilities,
        float temperature)
    {
        double total = 0.0;
        double best = 0.0;

        for (int i = 0; i < probabilities.Length; i++)
        {
            double safeProbability = Math.Max(probabilities[i], 0.000001f);

            double softened = Math.Exp(
                Math.Log(safeProbability) / temperature
            );

            total += softened;

            if (softened > best)
            {
                best = softened;
            }
        }

        if (total <= 0.0)
        {
            return 0f;
        }

        return Clamp01(best / total);
    }

    private static float GetSoftmaxConfidence(
        float[] scores,
        float temperature)
    {
        double maxScore = scores[0];

        for (int i = 1; i < scores.Length; i++)
        {
            if (scores[i] > maxScore)
            {
                maxScore = scores[i];
            }
        }

        double total = 0.0;
        double best = 0.0;

        for (int i = 0; i < scores.Length; i++)
        {
            double value = Math.Exp(
                (scores[i] - maxScore) / temperature
            );

            total += value;

            if (value > best)
            {
                best = value;
            }
        }

        if (total <= 0.0)
        {
            return 0f;
        }

        return Clamp01(best / total);
    }

    private static float Clamp01(double value)
    {
        if (value < 0.0)
        {
            return 0f;
        }

        if (value > 1.0)
        {
            return 1f;
        }

        return (float)value;
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