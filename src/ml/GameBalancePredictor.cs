using System;
using System.IO;
using Microsoft.ML;

namespace DroneGameLocal;

// ML-6 CHANGE:
// Loads the trained ML.NET model and predicts whether the current game
// feels TooEasy, Balanced, or TooHard.
public sealed class GameBalancePredictor
{
    private readonly MLContext _mlContext = new(seed: 1);

    private PredictionEngine<GameBalanceModelInput, GameBalanceModelOutput> _predictionEngine;

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
                LastError = "Model file not found.";
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
            System.Console.WriteLine("ML model failed to load:");
            System.Console.WriteLine(ex.Message);
        }
    }

    public string Predict(GameBalanceModelInput input)
    {
        if (!IsLoaded)
        {
            return "NO MODEL";
        }

        try
        {
            GameBalanceModelOutput prediction = _predictionEngine.Predict(input);

            if (string.IsNullOrWhiteSpace(prediction.PredictedLabel))
            {
                return "UNKNOWN";
            }

            return prediction.PredictedLabel;
        }
        catch (Exception ex)
        {
            LastError = ex.Message;
            return "ERROR";
        }
    }

    private static string FindProjectRoot()
    {
        DirectoryInfo directory = new DirectoryInfo(Directory.GetCurrentDirectory());

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