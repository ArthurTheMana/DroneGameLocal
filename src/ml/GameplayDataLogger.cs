using System;
using System.IO;

namespace DroneGameLocal;

// ML-1 CHANGE:
// This class writes gameplay samples into a CSV file.
// The CSV file becomes the dataset for ML.NET training later.
//
// ML-1 POLISH:
// The CSV is now saved inside the project folder instead of AppData.
// This makes it easier to find, inspect, and use for ML training.
public sealed class GameplayDataLogger
{
    private readonly string _filePath;

    public GameplayDataLogger()
    {
        string projectRoot = FindProjectRoot();

        string folder = Path.Combine(projectRoot, "ml-data");

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

    private static string FindProjectRoot()
    {
        string currentDirectory = Directory.GetCurrentDirectory();

        DirectoryInfo? directory = new DirectoryInfo(currentDirectory);

        while (directory is not null)
        {
            string projectFilePath = Path.Combine(directory.FullName, "DroneGameLocal.csproj");

            if (File.Exists(projectFilePath))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        return currentDirectory;
    }
}