using System;
using System.Collections.Generic;

namespace DroneGameLocal;

public sealed class ObstacleSpawner
{
    private readonly Random _random = new();

    private float _spawnTimer;
    private float _difficultyTimer;
    private float _spawnInterval;

    // LEVEL 3C CHANGE:
    // The spawner now uses DifficultySettings.
    // This lets Easy, Normal, and Hard have different spawn rates and speeds.
    private DifficultySettings _settings =
        DifficultySettings.Get(DifficultyLevel.Normal);

    // LEVEL 3C CHANGE:
    // Reset uses the selected difficulty when a new game starts.
    public void Reset(DifficultySettings settings)
    {
        _settings = settings;

        _spawnTimer = 0f;
        _difficultyTimer = 0f;
        _spawnInterval = settings.InitialSpawnInterval;
    }

    public void Update(
        float deltaTime,
        List<Obstacle> obstacles,
        int currentScore,
        DifficultySettings settings)
    {
        _settings = settings;

        _spawnTimer += deltaTime;
        _difficultyTimer += deltaTime;

        IncreaseDifficultyIfNeeded();

        if (_spawnTimer < _spawnInterval)
        {
            return;
        }

        if (obstacles.Count >= _settings.MaxObstacles)
        {
            return;
        }

        _spawnTimer = 0f;

        Obstacle obstacle = CreateObstacle(currentScore);
        obstacles.Add(obstacle);
    }

    // LEVEL 3C CHANGE:
    // The game gets harder over time.
    // Every few seconds, the spawn interval becomes shorter,
    // so obstacles appear more often.
    private void IncreaseDifficultyIfNeeded()
    {
        if (_difficultyTimer < _settings.DifficultyIncreaseEverySeconds)
        {
            return;
        }

        _difficultyTimer = 0f;

        _spawnInterval = Math.Max(
            _settings.MinimumSpawnInterval,
            _spawnInterval - _settings.SpawnIntervalDecrease
        );
    }

    private Obstacle CreateObstacle(int currentScore)
    {
        int width = _random.Next(35, 80);
        int height = _random.Next(45, 140);

        int y = _random.Next(
            60,
            GameSettings.ScreenHeight - height - 40
        );

        float baseSpeed = _random.Next(
            (int)_settings.MinObstacleSpeed,
            (int)_settings.MaxObstacleSpeed
        );

        // LEVEL 3C CHANGE:
        // Obstacle speed increases as the player's score gets higher.
        // This creates endless progressive difficulty.
        float speed = baseSpeed + currentScore * _settings.ScoreSpeedMultiplier;

        return new Obstacle(
            GameSettings.ScreenWidth,
            y,
            width,
            height,
            speed
        );
    }
}