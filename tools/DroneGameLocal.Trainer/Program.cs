using Microsoft.ML;
using Microsoft.ML.Data;

namespace DroneGameLocal.Trainer;

public sealed class GameplayTrainingRow
{
    [LoadColumn(0)]
    public float SurvivalSeconds { get; set; }

    [LoadColumn(1)]
    public float Score { get; set; }

    [LoadColumn(2)]
    public float Lives { get; set; }

    [LoadColumn(3)]
    public float ActiveObstacles { get; set; }

    [LoadColumn(4)]
    public float CurrentMaxObstacles { get; set; }

    [LoadColumn(5)]
    public float ObstaclePressure { get; set; }

    [LoadColumn(6)]
    public float ActiveEnemies { get; set; }

    [LoadColumn(7)]
    public float CurrentMaxEnemies { get; set; }

    [LoadColumn(8)]
    public float EnemyPressure { get; set; }

    [LoadColumn(9)]
    public float ActiveEnemyBullets { get; set; }

    [LoadColumn(10)]
    public float ActivePlayerShots { get; set; }

    [LoadColumn(11)]
    public float ShotCharges { get; set; }

    [LoadColumn(12)]
    public float ActiveShields { get; set; }

    [LoadColumn(13)]
    public string Difficulty { get; set; } = "";

    [LoadColumn(14)]
    public string ControlMode { get; set; } = "";

    // NOTE:
    // Columns 15, 16, and 17 are TimeRating, ScoreRating, and AutoLabelReason.
    // We do NOT use them as model features.
    // They are useful for debugging, but using them for training would leak the answer.

    [LoadColumn(18)]
    public string Label { get; set; } = "";
}

public sealed class GameplayPrediction
{
    [ColumnName("PredictedLabel")]
    public string PredictedLabel { get; set; } = "";

    public float[] Score { get; set; } = Array.Empty<float>();
}

public static class Program
{
    public static void Main()
    {
        string root = FindProjectRoot();

        string csvPath = Path.Combine(
            root,
            "ml-data",
            "gameplay-training-data.csv"
        );

        string modelFolder = Path.Combine(root, "ml-models");
        string modelPath = Path.Combine(modelFolder, "game-balance-model.zip");

        Directory.CreateDirectory(modelFolder);

        if (!File.Exists(csvPath))
        {
            Console.WriteLine("CSV file not found.");
            Console.WriteLine(csvPath);
            return;
        }

        MLContext mlContext = new(seed: 1);

        IDataView data = mlContext.Data.LoadFromTextFile<GameplayTrainingRow>(
            path: csvPath,
            hasHeader: true,
            separatorChar: ','
        );

        int rowCount = mlContext.Data
            .CreateEnumerable<GameplayTrainingRow>(data, reuseRowObject: false)
            .Count();

        Console.WriteLine($"Loaded rows: {rowCount}");

        if (rowCount < 10)
        {
            Console.WriteLine("You have very little data. Collect more rows before trusting the model.");
        }

        if (rowCount < 3)
        {
            Console.WriteLine("Not enough rows to train.");
            return;
        }

        DataOperationsCatalog.TrainTestData split =
            mlContext.Data.TrainTestSplit(data, testFraction: 0.2, seed: 1);

        IEstimator<ITransformer> pipeline =
            mlContext.Transforms.Conversion.MapValueToKey(
                    outputColumnName: "LabelKey",
                    inputColumnName: nameof(GameplayTrainingRow.Label)
                )

                .Append(mlContext.Transforms.Categorical.OneHotEncoding(
                    outputColumnName: "DifficultyEncoded",
                    inputColumnName: nameof(GameplayTrainingRow.Difficulty)
                ))

                .Append(mlContext.Transforms.Categorical.OneHotEncoding(
                    outputColumnName: "ControlModeEncoded",
                    inputColumnName: nameof(GameplayTrainingRow.ControlMode)
                ))

                .Append(mlContext.Transforms.Concatenate(
                    "Features",

                    nameof(GameplayTrainingRow.SurvivalSeconds),
                    nameof(GameplayTrainingRow.Score),
                    nameof(GameplayTrainingRow.Lives),

                    nameof(GameplayTrainingRow.ActiveObstacles),
                    nameof(GameplayTrainingRow.CurrentMaxObstacles),
                    nameof(GameplayTrainingRow.ObstaclePressure),

                    nameof(GameplayTrainingRow.ActiveEnemies),
                    nameof(GameplayTrainingRow.CurrentMaxEnemies),
                    nameof(GameplayTrainingRow.EnemyPressure),

                    nameof(GameplayTrainingRow.ActiveEnemyBullets),
                    nameof(GameplayTrainingRow.ActivePlayerShots),
                    nameof(GameplayTrainingRow.ShotCharges),
                    nameof(GameplayTrainingRow.ActiveShields),

                    "DifficultyEncoded",
                    "ControlModeEncoded"
                ))

                .Append(mlContext.Transforms.NormalizeMinMax("Features"))

                .Append(mlContext.MulticlassClassification.Trainers.SdcaMaximumEntropy(
                    labelColumnName: "LabelKey",
                    featureColumnName: "Features"
                ))

                .Append(mlContext.Transforms.Conversion.MapKeyToValue(
                    outputColumnName: "PredictedLabel",
                    inputColumnName: "PredictedLabel"
                ));

        Console.WriteLine("Training model...");

        ITransformer model = pipeline.Fit(split.TrainSet);

        Console.WriteLine("Evaluating model...");

        IDataView predictions = model.Transform(split.TestSet);

        MulticlassClassificationMetrics metrics =
            mlContext.MulticlassClassification.Evaluate(
                predictions,
                labelColumnName: "LabelKey",
                predictedLabelColumnName: "PredictedLabel"
            );

        Console.WriteLine();
        Console.WriteLine("=== Model Evaluation ===");
        Console.WriteLine($"MicroAccuracy: {metrics.MicroAccuracy:0.000}");
        Console.WriteLine($"MacroAccuracy: {metrics.MacroAccuracy:0.000}");
        Console.WriteLine($"LogLoss:       {metrics.LogLoss:0.000}");

        mlContext.Model.Save(model, data.Schema, modelPath);

        Console.WriteLine();
        Console.WriteLine("Model saved:");
        Console.WriteLine(modelPath);

        TestSinglePrediction(mlContext, model);
    }

    private static void TestSinglePrediction(MLContext mlContext, ITransformer model)
    {
        PredictionEngine<GameplayTrainingRow, GameplayPrediction> predictor =
            mlContext.Model.CreatePredictionEngine<GameplayTrainingRow, GameplayPrediction>(model);

        var sample = new GameplayTrainingRow
        {
            SurvivalSeconds = 45f,
            Score = 650f,
            Lives = 0f,

            ActiveObstacles = 6f,
            CurrentMaxObstacles = 11f,
            ObstaclePressure = 0.55f,

            ActiveEnemies = 2f,
            CurrentMaxEnemies = 4f,
            EnemyPressure = 0.60f,

            ActiveEnemyBullets = 2f,
            ActivePlayerShots = 0f,
            ShotCharges = 1f,
            ActiveShields = 0f,

            Difficulty = "Normal",
            ControlMode = "Bot"
        };

        GameplayPrediction prediction = predictor.Predict(sample);

        Console.WriteLine();
        Console.WriteLine("=== Test Prediction ===");
        Console.WriteLine($"Predicted difficulty feeling: {prediction.PredictedLabel}");
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
