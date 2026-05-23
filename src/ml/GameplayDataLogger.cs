using System;
using System.IO;

namespace DroneGameLocal;

// ML-1 CHANGE:
// This class saves gameplay samples into a local CSV file.
// The CSV file will become the training dataset for ML.NET later.
public sealed class GameplayDataLogger
{
    private readonly string _filePath;

    public GameplayDataLogger()
    {
        string appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        string folder = Path.Combine(appData, "DroneGameLocal", "ml-data");

        Directory.CreateDirectory(folder);

        _filePath = Path.Combine(folder, "gameplay-training-data.csv");

        if (!File.Exists(_filePath))
        {
            File.WriteAllText(_filePath, GameplaySample.CsvHeader + Environment.NewLine);
        }
    }

    public void Log(GameplaySample sample)
    {
        File.AppendAllText(_filePath, sample.ToCsvRow() + Environment.NewLine);
    }

    public string GetFilePath()
    {
        return _filePath;
    }
}