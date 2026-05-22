using System;
using System.Collections.Generic;

namespace DroneGameLocal;

public sealed class EnemySpawner
{
    // LEVEL 4D POLISH:
    // These values help enemies spawn in a more readable way.
    private const int MinEnemyVerticalGap = 80;
    private const int MaxSpawnAttempts = 12;

    private readonly Random _random = new();

    private float _spawnTimer;
    private float _difficultyTimer;
    private float _spawnInterval;

    // LEVEL 4A CHANGE:
    // This tracks how long the player has survived.
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
        EnemyType type = ChooseEnemyType();

        int width = GetEnemyWidth(type);
        int height = GetEnemyHeight(type);

        int y = FindFairYPosition(existingEnemies);

        float baseSpeed = _random.Next(
            (int)_settings.MinEnemySpeed,
            (int)_settings.MaxEnemySpeed
        );

        float speed =
            baseSpeed +
            currentScore * _settings.EnemyScoreSpeedMultiplier;

        // LEVEL 5A CHANGE:
        // Each enemy type adjusts base speed.
        // Tank is slower.
        // ZigZag is faster.
        // Sniper is slower because its bullets are the real threat.
        speed *= type switch
        {
            EnemyType.Tank => 0.72f,

            // LEVEL 5A POLISH:
            // ZigZag is faster so its movement pattern feels more obvious.
            EnemyType.ZigZag => 1.32f,

            EnemyType.Sniper => 0.82f,
            _ => 1.0f
        };

        // LEVEL 4B CHANGE:
        // Each enemy gets a random shoot interval based on difficulty.
        float shootInterval =
            _settings.MinEnemyShootInterval +
            (float)_random.NextDouble() *
            (_settings.MaxEnemyShootInterval - _settings.MinEnemyShootInterval);

        // LEVEL 5A CHANGE:
        // Tank deploys shield slower.
        // ZigZag shoots slightly faster.
        // Sniper shoots less often, but its bullets fly faster.
        shootInterval *= type switch
        {
            EnemyType.Tank => 1.45f,
            EnemyType.ZigZag => 0.88f,
            EnemyType.Sniper => 1.30f,
            _ => 1.0f
        };

        return new Enemy(
            type,
            GameSettings.ScreenWidth + 40,
            y,
            width,
            height,
            speed,
            GetScoreReward(type),
            GetHealth(type),
            shootInterval
        );
    }

    // LEVEL 5A CHANGE:
    // Enemy type selection.
    // Early game = mostly Scout.
    // Mid game = Tank and ZigZag can appear.
    // Late game = Sniper can appear too.
    private EnemyType ChooseEnemyType()
    {
        int roll = _random.Next(100);

        if (ProgressPercent < 0.25f)
        {
            return EnemyType.Scout;
        }

        if (ProgressPercent < 0.50f)
        {
            if (roll < 16)
            {
                return EnemyType.Tank;
            }

            return EnemyType.Scout;
        }

        if (ProgressPercent < 0.75f)
        {
            if (roll < 16)
            {
                return EnemyType.Tank;
            }

            if (roll < 36)
            {
                return EnemyType.ZigZag;
            }

            return EnemyType.Scout;
        }

        if (roll < 16)
        {
            return EnemyType.Tank;
        }

        if (roll < 36)
        {
            return EnemyType.ZigZag;
        }

        if (roll < 50)
        {
            return EnemyType.Sniper;
        }

        return EnemyType.Scout;
    }

    private static int GetEnemyWidth(EnemyType type)
    {
        return type switch
        {
            EnemyType.Tank => 58,
            EnemyType.ZigZag => 32,
            EnemyType.Sniper => 42,
            _ => 36
        };
    }

    private static int GetEnemyHeight(EnemyType type)
    {
        return type switch
        {
            EnemyType.Tank => 44,
            EnemyType.ZigZag => 30,
            EnemyType.Sniper => 38,
            _ => 36
        };
    }

    private static int GetHealth(EnemyType type)
    {
        return type switch
        {
            EnemyType.Tank => 2,
            _ => 1
        };
    }

    private int GetScoreReward(EnemyType type)
    {
        return type switch
        {
            EnemyType.Tank => _settings.PointsPerEnemy + 25,
            EnemyType.ZigZag => _settings.PointsPerEnemy + 15,
            EnemyType.Sniper => _settings.PointsPerEnemy + 20,
            _ => _settings.PointsPerEnemy
        };
    }

    // LEVEL 4D POLISH:
    // Avoid spawning enemies too close to each other.
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