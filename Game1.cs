using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace DroneGameLocal;

public sealed class Game1 : Game
{
    private readonly GraphicsDeviceManager _graphics;

    private SpriteBatch? _spriteBatch;
    private Texture2D? _pixel;

    private readonly Drone _drone = new(
        GameSettings.StartDroneX,
        GameSettings.StartDroneY
    );

    private readonly List<Obstacle> _obstacles = new();

    private readonly GameState _gameState = new();
    private readonly InputManager _inputManager = new();
    private readonly ScoreManager _scoreManager = new();
    private readonly ObstacleSpawner _obstacleSpawner = new();

    private float _collisionCooldown;

    public Game1()
    {
        _graphics = new GraphicsDeviceManager(this);
        Content.RootDirectory = "Content";
        IsMouseVisible = true;
        Window.Title = "Drone Game";
    }

    protected override void Initialize()
    {
        _graphics.PreferredBackBufferWidth = GameSettings.ScreenWidth;
        _graphics.PreferredBackBufferHeight = GameSettings.ScreenHeight;
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
        _inputManager.Update();

        if (_inputManager.IsKeyPressed(Keys.Escape))
        {
            Exit();
        }

        if (_gameState.Current == GameStateType.Start)
        {
            UpdateStartState(gameTime);
            return;
        }

        if (_gameState.IsGameOver())
        {
            UpdateGameOverState(gameTime);
            return;
        }

        if (_inputManager.IsKeyPressed(Keys.P))
        {
            TogglePause();
        }

        if (_gameState.Current == GameStateType.Paused)
        {
            base.Update(gameTime);
            return;
        }

        UpdatePlayingState(gameTime);

        base.Update(gameTime);
    }

    private void UpdateStartState(GameTime gameTime)
    {
        if (_inputManager.IsKeyPressed(Keys.Enter) ||
            _inputManager.IsKeyPressed(Keys.Space))
        {
            StartNewGame();
        }

        base.Update(gameTime);
    }

    private void UpdateGameOverState(GameTime gameTime)
    {
        if (_inputManager.IsKeyPressed(Keys.Enter) ||
            _inputManager.IsKeyPressed(Keys.Space))
        {
            StartNewGame();
        }

        base.Update(gameTime);
    }

    private void UpdatePlayingState(GameTime gameTime)
    {
        float deltaTime = (float)gameTime.ElapsedGameTime.TotalSeconds;

        if (_collisionCooldown > 0f)
        {
            _collisionCooldown -= deltaTime;
            base.Update(gameTime);
            return;
        }

        HandleDroneMovement(deltaTime);
        _obstacleSpawner.Update(deltaTime, _obstacles, _scoreManager.Score);
        UpdateObstacles(deltaTime);
        CheckCollision();
        CheckWin();

        Window.Title =
            $"Score: {_scoreManager.Score}/{GameSettings.WinScore} | " +
            $"Lives: {_gameState.Lives} | " +
            $"Best: {_scoreManager.HighScore}";
    }

    private void StartNewGame()
    {
        _gameState.StartGame();
        _scoreManager.ResetScore();

        _drone.Reset(
            GameSettings.StartDroneX,
            GameSettings.StartDroneY
        );

        _obstacles.Clear();
        _obstacleSpawner.Reset();

        _collisionCooldown = 0f;
    }

    private void TogglePause()
    {
        if (_gameState.Current == GameStateType.Playing)
        {
            _gameState.Pause();
            return;
        }

        if (_gameState.Current == GameStateType.Paused)
        {
            _gameState.Resume();
        }
    }

    private void HandleDroneMovement(float deltaTime)
    {
        Vector2 direction = _inputManager.GetMovementDirection();

        _drone.Move(direction, deltaTime);

        _drone.ClampToScreen(
            GameSettings.ScreenWidth,
            GameSettings.ScreenHeight
        );
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
                _scoreManager.AddScore(10);
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

        _gameState.LoseLife();

        _obstacles.Clear();

        _drone.Reset(
            GameSettings.StartDroneX,
            GameSettings.StartDroneY
        );

        _collisionCooldown = GameSettings.CollisionCooldownSeconds;

        if (_gameState.Current == GameStateType.Fail)
        {
            _scoreManager.SaveHighScoreIfNeeded();
        }
    }

    private void CheckWin()
    {
        if (!_scoreManager.HasReachedWinScore())
        {
            return;
        }

        _gameState.Win();
        _scoreManager.SaveHighScoreIfNeeded();
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
        DrawStateOverlay();

        _spriteBatch.End();

        base.Draw(gameTime);
    }

    private void DrawStateOverlay()
    {
        if (_collisionCooldown > 0f &&
            _gameState.Current == GameStateType.Playing)
        {
            PixelText.DrawCenteredText(
                _spriteBatch!,
                _pixel!,
                "CRASH",
                GameSettings.ScreenWidth,
                255,
                5,
                new Color(255, 214, 10)
            );
        }

        if (_gameState.Current == GameStateType.Paused)
        {
            DrawPanel(new Color(20, 20, 20, 210));

            PixelText.DrawCenteredText(
                _spriteBatch!,
                _pixel!,
                "PAUSED",
                GameSettings.ScreenWidth,
                230,
                5,
                Color.White
            );

            PixelText.DrawCenteredText(
                _spriteBatch!,
                _pixel!,
                "PRESS P TO RESUME",
                GameSettings.ScreenWidth,
                290,
                3,
                new Color(0, 217, 255)
            );
        }

        if (_gameState.Current == GameStateType.Start)
        {
            DrawPanel(new Color(30, 70, 120, 210));

            PixelText.DrawCenteredText(
                _spriteBatch!,
                _pixel!,
                "DRONE GAME",
                GameSettings.ScreenWidth,
                190,
                5,
                Color.White
            );

            PixelText.DrawCenteredText(
                _spriteBatch!,
                _pixel!,
                "PRESS ENTER TO START",
                GameSettings.ScreenWidth,
                270,
                3,
                new Color(0, 217, 255)
            );

            PixelText.DrawCenteredText(
                _spriteBatch!,
                _pixel!,
                "WASD OR ARROWS TO MOVE",
                GameSettings.ScreenWidth,
                315,
                2,
                new Color(255, 214, 10)
            );

            PixelText.DrawCenteredText(
                _spriteBatch!,
                _pixel!,
                "P TO PAUSE / ESC TO QUIT",
                GameSettings.ScreenWidth,
                345,
                2,
                Color.White
            );
        }

        if (_gameState.Current == GameStateType.Fail)
        {
            DrawPanel(new Color(120, 30, 30, 220));

            PixelText.DrawCenteredText(
                _spriteBatch!,
                _pixel!,
                "GAME OVER",
                GameSettings.ScreenWidth,
                205,
                5,
                Color.White
            );

            PixelText.DrawCenteredText(
                _spriteBatch!,
                _pixel!,
                $"SCORE {_scoreManager.Score}",
                GameSettings.ScreenWidth,
                275,
                3,
                new Color(255, 214, 10)
            );

            PixelText.DrawCenteredText(
                _spriteBatch!,
                _pixel!,
                "PRESS ENTER TO RESTART",
                GameSettings.ScreenWidth,
                325,
                3,
                new Color(0, 217, 255)
            );
        }

        if (_gameState.Current == GameStateType.Win)
        {
            DrawPanel(new Color(30, 120, 70, 220));

            PixelText.DrawCenteredText(
                _spriteBatch!,
                _pixel!,
                "YOU WIN",
                GameSettings.ScreenWidth,
                205,
                5,
                Color.White
            );

            PixelText.DrawCenteredText(
                _spriteBatch!,
                _pixel!,
                $"SCORE {_scoreManager.Score}",
                GameSettings.ScreenWidth,
                275,
                3,
                new Color(255, 214, 10)
            );

            PixelText.DrawCenteredText(
                _spriteBatch!,
                _pixel!,
                "PRESS ENTER TO RESTART",
                GameSettings.ScreenWidth,
                325,
                3,
                new Color(0, 217, 255)
            );
        }
    }

    private void DrawBackground()
    {
        for (int y = 0; y < GameSettings.ScreenHeight; y += 60)
        {
            DrawRect(
                new Rectangle(0, y, GameSettings.ScreenWidth, 2),
                new Color(255, 255, 255, 25)
            );
        }

        for (int x = 0; x < GameSettings.ScreenWidth; x += 90)
        {
            DrawRect(
                new Rectangle(x, 0, 2, GameSettings.ScreenHeight),
                new Color(255, 255, 255, 18)
            );
        }
    }

    private void DrawHud()
    {
        DrawRect(
            new Rectangle(0, 0, GameSettings.ScreenWidth, 52),
            new Color(0, 0, 0, 120)
        );

        PixelText.DrawText(
            _spriteBatch!,
            _pixel!,
            $"SCORE {_scoreManager.Score}",
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
            $"BEST {_scoreManager.HighScore}",
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
            GameSettings.ScreenWidth / 2 - 300,
            GameSettings.ScreenHeight / 2 - 140,
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