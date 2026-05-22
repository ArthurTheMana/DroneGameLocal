using System;
using System.Collections.Generic;

namespace DroneGameLocal;

public sealed class ObstacleSpawner
{
    // LEVEL 4D POLISH:
    // These values help reduce unfair obstacle stacking.
    private const int MinObstacleVerticalGap = 75;
    private const int MaxSpawnAttempts = 12;

    private readonly Random _random = new();

    private float _spawnTimer;
    private float _difficultyTimer;
    private float _spawnInterval;

    // LEVEL 3E CHANGE:
    // This tracks how long the player has survived in the current run.
    // The longer the player survives, the more obstacles are allowed.
    private float _survivalTimer;

    private DifficultySettings _settings =
        DifficultySettings.Get(DifficultyLevel.Normal);

    // LEVEL 3E CHANGE:
    // CurrentMaxObstacles grows over time from StartingMaxObstacles to MaxObstacles.
    public int CurrentMaxObstacles { get; private set; }

    // LEVEL 3E CHANGE:
    // ProgressPercent is used by the HUD progress bar.
    // 0 = start of run, 1 = reached maximum obstacle pressure.
    public float ProgressPercent { get; private set; }

    public ObstacleSpawner()
    {
        Reset(_settings);
    }

    public void Reset(DifficultySettings settings)
    {
        _settings = settings;

        _spawnTimer = 0f;
        _difficultyTimer = 0f;
        _survivalTimer = 0f;

        _spawnInterval = settings.InitialSpawnInterval;

        CurrentMaxObstacles = settings.StartingMaxObstacles;
        ProgressPercent = 0f;
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
        _survivalTimer += deltaTime;

        UpdateObstacleProgression();
        IncreaseDifficultyIfNeeded();

        if (_spawnTimer < _spawnInterval)
        {
            return;
        }

        // LEVEL 3E CHANGE:
        // Instead of using the final MaxObstacles immediately,
        // we use CurrentMaxObstacles, which grows over time.
        if (obstacles.Count >= CurrentMaxObstacles)
        {
            return;
        }

        _spawnTimer = 0f;

        Obstacle obstacle = CreateObstacle(currentScore, obstacles);
        obstacles.Add(obstacle);
    }

    // LEVEL 3E CHANGE:
    // This is the core of the progression bar system.
    // More survival time = higher ProgressPercent = more allowed obstacles.
    private void UpdateObstacleProgression()
    {
        if (_settings.SecondsToReachMaxObstacles <= 0f)
        {
            ProgressPercent = 1f;
        }
        else
        {
            ProgressPercent = Math.Clamp(
                _survivalTimer / _settings.SecondsToReachMaxObstacles,
                0f,
                1f
            );
        }

        int obstacleRange =
            _settings.MaxObstacles - _settings.StartingMaxObstacles;

        CurrentMaxObstacles =
            _settings.StartingMaxObstacles +
            (int)Math.Round(obstacleRange * ProgressPercent);

        CurrentMaxObstacles = Math.Clamp(
            CurrentMaxObstacles,
            _settings.StartingMaxObstacles,
            _settings.MaxObstacles
        );
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

    private Obstacle CreateObstacle(
        int currentScore,
        List<Obstacle> existingObstacles)
    {
        int width = _random.Next(35, 80);
        int height = _random.Next(45, 140);

        int y = FindFairYPosition(height, existingObstacles);

        float baseSpeed = _random.Next(
            (int)_settings.MinObstacleSpeed,
            (int)_settings.MaxObstacleSpeed
        );

        // LEVEL 3C CHANGE:
        // Obstacle speed increases as the player's score gets higher.
        // This creates endless progressive difficulty.
        float speed =
            baseSpeed +
            currentScore * _settings.ScoreSpeedMultiplier;

        return new Obstacle(
            GameSettings.ScreenWidth,
            y,
            width,
            height,
            speed
        );
    }

    // LEVEL 4D POLISH:
    // Try to avoid spawning obstacles too close to each other vertically.
    // This reduces unfair "wall" patterns.
    private int FindFairYPosition(
        int height,
        List<Obstacle> existingObstacles)
    {
        int minY = 140;
        int maxY = GameSettings.ScreenHeight - height - 40;

        for (int attempt = 0; attempt < MaxSpawnAttempts; attempt++)
        {
            int candidateY = _random.Next(minY, maxY);

            if (IsFarEnoughFromExistingObstacles(candidateY, existingObstacles))
            {
                return candidateY;
            }
        }

        return _random.Next(minY, maxY);
    }

    private static bool IsFarEnoughFromExistingObstacles(
        int candidateY,
        List<Obstacle> existingObstacles)
    {
        foreach (Obstacle obstacle in existingObstacles)
        {
            if (Math.Abs(candidateY - obstacle.Position.Y) < MinObstacleVerticalGap)
            {
                return false;
            }
        }

        return true;
    }
}