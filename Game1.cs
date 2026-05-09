using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace DroneGameLocal;

public sealed class Game1 : Game
{
    private const int ScreenWidth = 900;
    private const int ScreenHeight = 600;
    private const int WinScore = 20;

    private readonly GraphicsDeviceManager _graphics;

    private SpriteBatch? _spriteBatch;
    private Texture2D? _pixel;

    private readonly Drone _drone = new(100, 280);
    private readonly List<Obstacle> _obstacles = new();
    private readonly GameState _gameState = new();
    private readonly Random _random = new();

    private float _spawnTimer;
    private float _spawnInterval = 1.15f;

    public Game1()
    {
        _graphics = new GraphicsDeviceManager(this);
        Content.RootDirectory = "Content";
        IsMouseVisible = true;
        Window.Title = "Drone Game";
    }

    protected override void Initialize()
    {
        _graphics.PreferredBackBufferWidth = ScreenWidth;
        _graphics.PreferredBackBufferHeight = ScreenHeight;
        _graphics.ApplyChanges();

        base.Initialize();
    }

    protected override void LoadContent()
    {
        _spriteBatch = new SpriteBatch(GraphicsDevice);

        _pixel = new Texture2D(GraphicsDevice, 1, 1);
        _pixel.SetData(new[] { Color.White });
    }

    protected override void Update(GameTime gameTime)
    {
        var keyboard = Keyboard.GetState();

        if (keyboard.IsKeyDown(Keys.Escape))
        {
            Exit();
        }

        if (_gameState.Current == GameStateType.Start)
        {
            Window.Title = "Press Enter to Start";

            if (keyboard.IsKeyDown(Keys.Enter))
            {
                StartNewGame();
            }

            base.Update(gameTime);
            return;
        }

        if (_gameState.Current == GameStateType.Fail ||
            _gameState.Current == GameStateType.Win)
        {
            Window.Title = $"{_gameState.Current} | Score: {_gameState.Score} | Press Enter to Restart";

            if (keyboard.IsKeyDown(Keys.Enter))
            {
                StartNewGame();
            }

            base.Update(gameTime);
            return;
        }

        float deltaTime = (float)gameTime.ElapsedGameTime.TotalSeconds;

        HandleInput(keyboard, deltaTime);
        SpawnObstacles(deltaTime);
        UpdateObstacles(deltaTime);
        CheckCollision();
        CheckWin();

        Window.Title = $"Score: {_gameState.Score}/{WinScore} | Lives: {_gameState.Lives}";

        base.Update(gameTime);
    }

    private void StartNewGame()
    {
        _gameState.StartGame();
        _drone.Reset(100, 280);
        _obstacles.Clear();
        _spawnTimer = 0f;
        _spawnInterval = 1.15f;
    }

    private void HandleInput(KeyboardState keyboard, float deltaTime)
    {
        var direction = Vector2.Zero;

        if (keyboard.IsKeyDown(Keys.Up) || keyboard.IsKeyDown(Keys.W))
        {
            direction.Y -= 1;
        }

        if (keyboard.IsKeyDown(Keys.Down) || keyboard.IsKeyDown(Keys.S))
        {
            direction.Y += 1;
        }

        if (keyboard.IsKeyDown(Keys.Left) || keyboard.IsKeyDown(Keys.A))
        {
            direction.X -= 1;
        }

        if (keyboard.IsKeyDown(Keys.Right) || keyboard.IsKeyDown(Keys.D))
        {
            direction.X += 1;
        }

        _drone.Move(direction, deltaTime);
        _drone.ClampToScreen(ScreenWidth, ScreenHeight);
    }

    private void SpawnObstacles(float deltaTime)
    {
        _spawnTimer += deltaTime;

        if (_spawnTimer < _spawnInterval)
        {
            return;
        }

        _spawnTimer = 0f;

        int width = _random.Next(35, 80);
        int height = _random.Next(45, 140);
        int y = _random.Next(40, ScreenHeight - height - 40);
        float speed = _random.Next(180, 280);

        var obstacle = new Obstacle(
            ScreenWidth,
            y,
            width,
            height,
            speed
        );

        _obstacles.Add(obstacle);
    }

    private void UpdateObstacles(float deltaTime)
    {
        for (int i = _obstacles.Count - 1; i >= 0; i--)
        {
            var obstacle = _obstacles[i];
            obstacle.Update(deltaTime);

            if (!obstacle.ScoreCounted &&
                obstacle.Position.X + obstacle.Width < _drone.Position.X)
            {
                obstacle.ScoreCounted = true;
                _gameState.Score += 1;
            }

            if (obstacle.IsOffScreen())
            {
                _obstacles.RemoveAt(i);
            }
        }
    }

    private void CheckCollision()
    {
        bool crashed = CollisionChecker.HasCollision(_drone, _obstacles);

        if (!crashed)
        {
            return;
        }

        _gameState.Lives -= 1;
        _obstacles.Clear();
        _drone.Reset(100, 280);

        if (_gameState.Lives <= 0)
        {
            _gameState.Current = GameStateType.Fail;
        }
    }

    private void CheckWin()
    {
        if (_gameState.Score >= WinScore)
        {
            _gameState.Current = GameStateType.Win;
        }
    }

    protected override void Draw(GameTime gameTime)
    {
        GraphicsDevice.Clear(new Color(12, 18, 32));

        if (_spriteBatch is null || _pixel is null)
        {
            return;
        }

        _spriteBatch.Begin();

        DrawBackground();
        DrawDrone();
        DrawObstacles();

        if (_gameState.Current == GameStateType.Start)
        {
            DrawPanel(new Color(30, 70, 120, 180));
        }

        if (_gameState.Current == GameStateType.Fail)
        {
            DrawPanel(new Color(120, 30, 30, 180));
        }

        if (_gameState.Current == GameStateType.Win)
        {
            DrawPanel(new Color(30, 120, 70, 180));
        }

        _spriteBatch.End();

        base.Draw(gameTime);
    }

    private void DrawBackground()
    {
        for (int y = 0; y < ScreenHeight; y += 60)
        {
            DrawRect(new Rectangle(0, y, ScreenWidth, 2), new Color(255, 255, 255, 25));
        }

        for (int x = 0; x < ScreenWidth; x += 90)
        {
            DrawRect(new Rectangle(x, 0, 2, ScreenHeight), new Color(255, 255, 255, 18));
        }
    }

    private void DrawDrone()
    {
        DrawRect(_drone.GetBounds(), new Color(0, 217, 255));

        var nose = new Rectangle(
            (int)_drone.Position.X + _drone.Width - 10,
            (int)_drone.Position.Y + 8,
            10,
            12
        );

        DrawRect(nose, Color.White);
    }

    private void DrawObstacles()
    {
        foreach (var obstacle in _obstacles)
        {
            DrawRect(obstacle.GetBounds(), new Color(255, 80, 100));
        }
    }

    private void DrawPanel(Color color)
    {
        var panel = new Rectangle(
            ScreenWidth / 2 - 170,
            ScreenHeight / 2 - 60,
            340,
            120
        );

        DrawRect(panel, color);
    }

    private void DrawRect(Rectangle rectangle, Color color)
    {
        _spriteBatch!.Draw(_pixel!, rectangle, color);
    }
}