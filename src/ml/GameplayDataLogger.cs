using System;
using System.IO;
using System.Linq;

namespace DroneGameLocal;

// ML-1 CHANGE:
// This class writes gameplay samples into a CSV file.
// The CSV file becomes the dataset for ML.NET training later.
//
// ML-1 POLISH:
// The CSV is saved inside the project folder instead of AppData.
//
// ML-3 CHANGE:
// If the CSV header is old, it creates a backup and starts a new CSV
// with ControlMode and AutoLabelReason columns.
public sealed class GameplayDataLogger
{
    private readonly string _filePath;
    private string _lastError = "";

    public GameplayDataLogger()
    {
        string projectRoot = FindProjectRoot();

        string folder = Path.Combine(projectRoot, "ml-data");

        Directory.CreateDirectory(folder);

        _filePath = Path.Combine(folder, "gameplay-training-data.csv");

        EnsureCsvHasCorrectHeader();
    }

    public bool Log(GameplaySample sample)
    {
        try
        {
            File.AppendAllText(_filePath, sample.ToCsvRow() + Environment.NewLine);
            _lastError = "";
            return true;
        }
        catch (IOException ex)
        {
            _lastError = ex.Message;
            return false;
        }
        catch (UnauthorizedAccessException ex)
        {
            _lastError = ex.Message;
            return false;
        }
    }

    public string GetFilePath()
    {
        return _filePath;
    }

    public string GetLastError()
    {
        return _lastError;
    }

    private void EnsureCsvHasCorrectHeader()
    {
        if (!File.Exists(_filePath))
        {
            File.WriteAllText(_filePath, GameplaySample.CsvHeader + Environment.NewLine);
            return;
        }

        string firstLine = File.ReadLines(_filePath).FirstOrDefault() ?? "";

        if (firstLine == GameplaySample.CsvHeader)
        {
            return;
        }

        string folder = Path.GetDirectoryName(_filePath) ?? "";
        string timestamp = DateTime.Now.ToString("yyyyMMdd-HHmmss");

        string backupPath = Path.Combine(
            folder,
            $"gameplay-training-data-backup-{timestamp}.csv"
        );

        File.Copy(_filePath, backupPath, overwrite: true);

        File.WriteAllText(_filePath, GameplaySample.CsvHeader + Environment.NewLine);
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