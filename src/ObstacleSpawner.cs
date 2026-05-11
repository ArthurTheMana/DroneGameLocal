using System;
using System.Collections.Generic;

namespace DroneGameLocal;

public sealed class ObstacleSpawner
{
    private readonly Random _random = new();

    private float _spawnTimer;
    private float _difficultyTimer;
    private float _spawnInterval = GameSettings.InitialSpawnInterval;

    public void Reset()
    {
        _spawnTimer = 0f;
        _difficultyTimer = 0f;
        _spawnInterval = GameSettings.InitialSpawnInterval;
    }

    public void Update(float deltaTime, List<Obstacle> obstacles, int currentScore)
    {
        _spawnTimer += deltaTime;
        _difficultyTimer += deltaTime;

        IncreaseDifficultyIfNeeded();

        if (_spawnTimer < _spawnInterval)
        {
            return;
        }

        if (obstacles.Count >= GameSettings.MaxObstacles)
        {
            return;
        }

        _spawnTimer = 0f;

        Obstacle obstacle = CreateObstacle(currentScore);
        obstacles.Add(obstacle);
    }

    private void IncreaseDifficultyIfNeeded()
    {
        if (_difficultyTimer < GameSettings.DifficultyIncreaseEverySeconds)
        {
            return;
        }

        _difficultyTimer = 0f;

        _spawnInterval = Math.Max(
            GameSettings.MinimumSpawnInterval,
            _spawnInterval - GameSettings.SpawnIntervalDecrease
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

        float speed = _random.Next(180, 280) + (currentScore * 0.35f);

        return new Obstacle(
            GameSettings.ScreenWidth,
            y,
            width,
            height,
            speed
        );
    }
}