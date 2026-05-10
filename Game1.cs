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
    private const int WinScore = 200;
    private const int MaxObstacles = 8;

    private readonly GraphicsDeviceManager _graphics;

    private SpriteBatch? _spriteBatch;
    private Texture2D? _pixel;

    private readonly Drone _drone = new(100, 280);
    private readonly List<Obstacle> _obstacles = new();
    private readonly GameState _gameState = new();
    private readonly Random _random = new();

    private KeyboardState _previousKeyboard;

    private float _spawnTimer;
    private float _spawnInterval = 1.15f;
    private float _difficultyTimer;
    private float _collisionCooldown;

    private int _highScore;
    private bool _isPaused;

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

        _highScore = LocalSaveManager.LoadHighScore();

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
        KeyboardState keyboard = Keyboard.GetState();

        if (IsKeyPressed(keyboard, Keys.Escape))
        {
            Exit();
        }

        if (_gameState.Current == GameStateType.Start)
        {
            if (IsKeyPressed(keyboard, Keys.Enter) || IsKeyPressed(keyboard, Keys.Space))
            {
                StartNewGame();
            }

            _previousKeyboard = keyboard;
            base.Update(gameTime);
            return;
        }

        if (_gameState.Current == GameStateType.Fail ||
            _gameState.Current == GameStateType.Win)
        {
            if (IsKeyPressed(keyboard, Keys.Enter) || IsKeyPressed(keyboard, Keys.Space))
            {
                StartNewGame();
            }

            _previousKeyboard = keyboard;
            base.Update(gameTime);
            return;
        }

        if (IsKeyPressed(keyboard, Keys.P))
        {
            _isPaused = !_isPaused;
        }

        if (_isPaused)
        {
            _previousKeyboard = keyboard;
            base.Update(gameTime);
            return;
        }

        float deltaTime = (float)gameTime.ElapsedGameTime.TotalSeconds;

        if (_collisionCooldown > 0f)
        {
            _collisionCooldown -= deltaTime;

            _previousKeyboard = keyboard;
            base.Update(gameTime);
            return;
        }

        HandleInput(keyboard, deltaTime);
        SpawnObstacles(deltaTime);
        UpdateObstacles(deltaTime);
        CheckCollision();
        CheckWin();

        Window.Title = $"Score: {_gameState.Score}/{WinScore} | Lives: {_gameState.Lives} | Best: {_highScore}";

        _previousKeyboard = keyboard;
        base.Update(gameTime);
    }

    private bool IsKeyPressed(KeyboardState keyboard, Keys key)
    {
        return keyboard.IsKeyDown(key) && !_previousKeyboard.IsKeyDown(key);
    }

    private void StartNewGame()
    {
        _gameState.StartGame();

        _drone.Reset(100, 280);
        _obstacles.Clear();

        _spawnTimer = 0f;
        _spawnInterval = 1.15f;
        _difficultyTimer = 0f;
        _collisionCooldown = 0f;

        _isPaused = false;
    }

    private void HandleInput(KeyboardState keyboard, float deltaTime)
    {
        Vector2 direction = Vector2.Zero;

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
        _difficultyTimer += deltaTime;

        if (_difficultyTimer >= 10f)
        {
            _difficultyTimer = 0f;
            _spawnInterval = Math.Max(0.65f, _spawnInterval - 0.08f);
        }

        if (_spawnTimer < _spawnInterval)
        {
            return;
        }

        if (_obstacles.Count >= MaxObstacles)
        {
            return;
        }

        _spawnTimer = 0f;

        int width = _random.Next(35, 80);
        int height = _random.Next(45, 140);
        int y = _random.Next(60, ScreenHeight - height - 40);

        float speed = _random.Next(180, 280) + (_gameState.Score * 0.35f);

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
            Obstacle obstacle = _obstacles[i];

            obstacle.Update(deltaTime);

            if (!obstacle.ScoreCounted &&
                obstacle.Position.X + obstacle.Width < _drone.Position.X)
            {
                obstacle.ScoreCounted = true;
                _gameState.Score += 10;
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
        _collisionCooldown = 1.0f;

        if (_gameState.Lives <= 0)
        {
            _gameState.Current = GameStateType.Fail;
            SaveHighScoreIfNeeded();
        }
    }

    private void CheckWin()
    {
        if (_gameState.Score >= WinScore)
        {
            _gameState.Current = GameStateType.Win;
            SaveHighScoreIfNeeded();
        }
    }

    private void SaveHighScoreIfNeeded()
    {
        if (_gameState.Score <= _highScore)
        {
            return;
        }

        _highScore = _gameState.Score;
        LocalSaveManager.SaveHighScore(_highScore);
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
        DrawHud();

        if (_collisionCooldown > 0f && _gameState.Current == GameStateType.Playing)
        {
            PixelText.DrawCenteredText(
                _spriteBatch,
                _pixel,
                "CRASH",
                ScreenWidth,
                255,
                5,
                new Color(255, 214, 10)
            );
        }

        if (_isPaused)
        {
            DrawPanel(new Color(20, 20, 20, 210));

            PixelText.DrawCenteredText(_spriteBatch, _pixel, "PAUSED", ScreenWidth, 230, 5, Color.White);
            PixelText.DrawCenteredText(_spriteBatch, _pixel, "PRESS P TO RESUME", ScreenWidth, 290, 3, new Color(0, 217, 255));
        }

        if (_gameState.Current == GameStateType.Start)
        {
            DrawPanel(new Color(30, 70, 120, 210));

            PixelText.DrawCenteredText(_spriteBatch, _pixel, "DRONE GAME", ScreenWidth, 190, 5, Color.White);
            PixelText.DrawCenteredText(_spriteBatch, _pixel, "PRESS ENTER TO START", ScreenWidth, 270, 3, new Color(0, 217, 255));
            PixelText.DrawCenteredText(_spriteBatch, _pixel, "WASD OR ARROWS TO MOVE", ScreenWidth, 315, 2, new Color(255, 214, 10));
            PixelText.DrawCenteredText(_spriteBatch, _pixel, "P TO PAUSE / ESC TO QUIT", ScreenWidth, 345, 2, new Color(255, 255, 255));
        }

        if (_gameState.Current == GameStateType.Fail)
        {
            DrawPanel(new Color(120, 30, 30, 220));

            PixelText.DrawCenteredText(_spriteBatch, _pixel, "GAME OVER", ScreenWidth, 205, 5, Color.White);
            PixelText.DrawCenteredText(_spriteBatch, _pixel, $"SCORE { _gameState.Score }", ScreenWidth, 275, 3, new Color(255, 214, 10));
            PixelText.DrawCenteredText(_spriteBatch, _pixel, "PRESS ENTER TO RESTART", ScreenWidth, 325, 3, new Color(0, 217, 255));
        }

        if (_gameState.Current == GameStateType.Win)
        {
            DrawPanel(new Color(30, 120, 70, 220));

            PixelText.DrawCenteredText(_spriteBatch, _pixel, "YOU WIN", ScreenWidth, 205, 5, Color.White);
            PixelText.DrawCenteredText(_spriteBatch, _pixel, $"SCORE { _gameState.Score }", ScreenWidth, 275, 3, new Color(255, 214, 10));
            PixelText.DrawCenteredText(_spriteBatch, _pixel, "PRESS ENTER TO RESTART", ScreenWidth, 325, 3, new Color(0, 217, 255));
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

    private void DrawHud()
    {
        DrawRect(new Rectangle(0, 0, ScreenWidth, 52), new Color(0, 0, 0, 120));

        PixelText.DrawText(
            _spriteBatch!,
            _pixel!,
            $"SCORE {_gameState.Score}",
            new Vector2(20, 18),
            3,
            Color.White
        );

        PixelText.DrawText(
            _spriteBatch!,
            _pixel!,
            $"LIVES {_gameState.Lives}",
            new Vector2(300, 18),
            3,
            new Color(0, 217, 255)
        );

        PixelText.DrawText(
            _spriteBatch!,
            _pixel!,
            $"BEST {_highScore}",
            new Vector2(570, 18),
            3,
            new Color(255, 214, 10)
        );
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
        foreach (Obstacle obstacle in _obstacles)
        {
            DrawRect(obstacle.GetBounds(), new Color(255, 80, 100));
        }
    }

    private void DrawPanel(Color color)
    {
        var panel = new Rectangle(
            ScreenWidth / 2 - 300,
            ScreenHeight / 2 - 140,
            600,
            280
        );

        DrawRect(panel, color);
    }

    private void DrawRect(Rectangle rectangle, Color color)
    {
        _spriteBatch!.Draw(_pixel!, rectangle, color);
    }
}