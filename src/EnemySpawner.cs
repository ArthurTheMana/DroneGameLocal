using System;
using System.Collections.Generic;

namespace DroneGameLocal;

public sealed class EnemySpawner
{
    // LEVEL 4D POLISH:
    // These values help enemies spawn in a more readable way.
    // It avoids too many enemies stacking on the same vertical line.
    private const int MinEnemyVerticalGap = 80;
    private const int MaxSpawnAttempts = 12;

    private readonly Random _random = new();

    private float _spawnTimer;
    private float _difficultyTimer;
    private float _spawnInterval;

    // LEVEL 4A CHANGE:
    // This tracks how long the player has survived.
    // More time means more enemies are allowed.
    private float _survivalTimer;

    private DifficultySettings _settings =
        DifficultySettings.Get(DifficultyLevel.Normal);

    // LEVEL 4A CHANGE:
    // CurrentMaxEnemies grows over time from StartingMaxEnemies to MaxEnemies.
    public int CurrentMaxEnemies { get; private set; }

    // LEVEL 4A CHANGE:
    // ProgressPercent is used by the enemy pressure bar in the HUD.
    public float ProgressPercent { get; private set; }

    public EnemySpawner()
    {
        Reset(_settings);
    }

    public void Reset(DifficultySettings settings)
    {
        _settings = settings;

        _spawnTimer = 0f;
        _difficultyTimer = 0f;
        _survivalTimer = 0f;

        _spawnInterval = settings.InitialEnemySpawnInterval;

        CurrentMaxEnemies = settings.StartingMaxEnemies;
        ProgressPercent = 0f;
    }

    public void Update(
        float deltaTime,
        List<Enemy> enemies,
        int currentScore,
        DifficultySettings settings)
    {
        _settings = settings;

        _spawnTimer += deltaTime;
        _difficultyTimer += deltaTime;
        _survivalTimer += deltaTime;

        UpdateEnemyProgression();
        IncreaseEnemyDifficultyIfNeeded();

        if (_spawnTimer < _spawnInterval)
        {
            return;
        }

        if (enemies.Count >= CurrentMaxEnemies)
        {
            return;
        }

        _spawnTimer = 0f;

        Enemy enemy = CreateEnemy(currentScore, enemies);
        enemies.Add(enemy);
    }

    // LEVEL 4A CHANGE:
    // More survival time = more active enemies allowed.
    private void UpdateEnemyProgression()
    {
        if (_settings.SecondsToReachMaxEnemies <= 0f)
        {
            ProgressPercent = 1f;
        }
        else
        {
            ProgressPercent = Math.Clamp(
                _survivalTimer / _settings.SecondsToReachMaxEnemies,
                0f,
                1f
            );
        }

        int enemyRange =
            _settings.MaxEnemies - _settings.StartingMaxEnemies;

        CurrentMaxEnemies =
            _settings.StartingMaxEnemies +
            (int)Math.Round(enemyRange * ProgressPercent);

        CurrentMaxEnemies = Math.Clamp(
            CurrentMaxEnemies,
            _settings.StartingMaxEnemies,
            _settings.MaxEnemies
        );
    }

    // LEVEL 4A CHANGE:
    // Enemy spawn interval becomes shorter over time.
    private void IncreaseEnemyDifficultyIfNeeded()
    {
        if (_difficultyTimer < _settings.EnemyDifficultyIncreaseEverySeconds)
        {
            return;
        }

        _difficultyTimer = 0f;

        _spawnInterval = Math.Max(
            _settings.MinimumEnemySpawnInterval,
            _spawnInterval - _settings.EnemySpawnIntervalDecrease
        );
    }

    private Enemy CreateEnemy(
        int currentScore,
        List<Enemy> existingEnemies)
    {
        int y = FindFairYPosition(existingEnemies);

        float baseSpeed = _random.Next(
            (int)_settings.MinEnemySpeed,
            (int)_settings.MaxEnemySpeed
        );

        // LEVEL 4A CHANGE:
        // Enemy speed increases as score becomes higher.
        float speed =
            baseSpeed +
            currentScore * _settings.EnemyScoreSpeedMultiplier;

        // LEVEL 4B CHANGE:
        // Each enemy gets a random shoot interval based on difficulty.
        float shootInterval =
            _settings.MinEnemyShootInterval +
            (float)_random.NextDouble() *
            (_settings.MaxEnemyShootInterval - _settings.MinEnemyShootInterval);

        return new Enemy(
            GameSettings.ScreenWidth + 40,
            y,
            speed,
            _settings.PointsPerEnemy,
            shootInterval
        );
    }

    // LEVEL 4D POLISH:
    // Avoid spawning enemies too close to each other.
    // This keeps enemy bullets more readable and fair.
    private int FindFairYPosition(List<Enemy> existingEnemies)
    {
        int minY = 150;
        int maxY = GameSettings.ScreenHeight - 90;

        for (int attempt = 0; attempt < MaxSpawnAttempts; attempt++)
        {
            int candidateY = _random.Next(minY, maxY);

            if (IsFarEnoughFromExistingEnemies(candidateY, existingEnemies))
            {
                return candidateY;
            }
        }

        return _random.Next(minY, maxY);
    }

    private static bool IsFarEnoughFromExistingEnemies(
        int candidateY,
        List<Enemy> existingEnemies)
    {
        foreach (Enemy enemy in existingEnemies)
        {
            if (Math.Abs(candidateY - enemy.Position.Y) < MinEnemyVerticalGap)
            {
                return false;
            }
        }

        return true;
    }
}