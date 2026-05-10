using System;
using System.IO;
using System.Text.Json;

namespace DroneGameLocal;

public static class LocalSaveManager
{
    private sealed class SaveData
    {
        public int HighScore { get; set; }
    }

    private static string GetSaveFilePath()
    {
        string appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        string folder = Path.Combine(appData, "DroneGameLocal");

        Directory.CreateDirectory(folder);

        return Path.Combine(folder, "save.json");
    }

    public static int LoadHighScore()
    {
        string path = GetSaveFilePath();

        if (!File.Exists(path))
        {
            return 0;
        }

        try
        {
            string json = File.ReadAllText(path);
            SaveData? data = JsonSerializer.Deserialize<SaveData>(json);

            return data?.HighScore ?? 0;
        }
        catch
        {
            return 0;
        }
    }

    public static void SaveHighScore(int highScore)
    {
        string path = GetSaveFilePath();

        var data = new SaveData
        {
            HighScore = highScore
        };

        string json = JsonSerializer.Serialize(data, new JsonSerializerOptions
        {
            WriteIndented = true
        });

        File.WriteAllText(path, json);
    }
}