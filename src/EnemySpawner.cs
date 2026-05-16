using System;
using System.Collections.Generic;

namespace DroneGameLocal;

public sealed class EnemySpawner
{
    private readonly Random _random = new();

    private float _spawnTimer;
    private float _difficultyTimer;
    private float _spawnInterval;
    private float _survivalTimer;

    private DifficultySettings _settings =
        DifficultySettings.Get(DifficultyLevel.Normal);

    public int CurrentMaxEnemies { get; private set; }
    public float ProgressPercent { get; private set; }

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

        Enemy enemy = CreateEnemy(currentScore);
        enemies.Add(enemy);
    }

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

    private Enemy CreateEnemy(int currentScore)
    {
        int y = _random.Next(130, GameSettings.ScreenHeight - 80);

        float baseSpeed = _random.Next(
            (int)_settings.MinEnemySpeed,
            (int)_settings.MaxEnemySpeed
        );

        float speed =
            baseSpeed +
            currentScore * _settings.EnemyScoreSpeedMultiplier;

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
}