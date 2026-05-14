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

    private readonly Starfield _starfield = new(120);
    private readonly ParticleSystem _particles = new();

    // LEVEL 3C CHANGE:
    // The player can choose the difficulty from the start screen.
    // Normal is the default mode.
    private DifficultyLevel _selectedDifficulty = DifficultyLevel.Normal;

    // LEVEL 3C CHANGE:
    // These settings control lives, obstacle speed, spawn rate,
    // and how fast the game becomes harder.
    private DifficultySettings _difficultySettings =
        DifficultySettings.Get(DifficultyLevel.Normal);

    private float _collisionCooldown;
    private float _screenShakeTimer;

    private readonly System.Random _visualRandom = new();

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
        float deltaTime = (float)gameTime.ElapsedGameTime.TotalSeconds;

        _starfield.Update(deltaTime);
        _particles.Update(deltaTime);

        if (_screenShakeTimer > 0f)
        {
            _screenShakeTimer -= deltaTime;
        }

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

    // LEVEL 3C CHANGE:
    // The start screen now works as a simple difficulty menu.
    // Player can press A/D, Left/Right, or number keys 1/2/3
    // to select Easy, Normal, or Hard before starting.
    private void UpdateStartState(GameTime gameTime)
    {
        if (_inputManager.IsKeyPressed(Keys.Left) ||
            _inputManager.IsKeyPressed(Keys.A))
        {
            SelectPreviousDifficulty();
        }

        if (_inputManager.IsKeyPressed(Keys.Right) ||
            _inputManager.IsKeyPressed(Keys.D))
        {
            SelectNextDifficulty();
        }

        if (_inputManager.IsKeyPressed(Keys.D1))
        {
            SetDifficulty(DifficultyLevel.Easy);
        }

        if (_inputManager.IsKeyPressed(Keys.D2))
        {
            SetDifficulty(DifficultyLevel.Normal);
        }

        if (_inputManager.IsKeyPressed(Keys.D3))
        {
            SetDifficulty(DifficultyLevel.Hard);
        }

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

        // LEVEL 3C CHANGE:
        // Obstacle spawning now uses the current difficulty settings.
        // The game keeps getting harder as score and time increase.
        _obstacleSpawner.Update(
            deltaTime,
            _obstacles,
            _scoreManager.Score,
            _difficultySettings
        );

        UpdateObstacles(deltaTime);
        CheckCollision();

        // LEVEL 3C CHANGE:
        // We removed the fixed win score.
        // The game is now endless and only ends when the player loses all lives.

        Window.Title =
            $"Score: {_scoreManager.Score} | " +
            $"Lives: {_gameState.Lives} | " +
            $"Best: {_scoreManager.HighScore} | " +
            $"Mode: {_difficultySettings.Name}";
    }

    private void StartNewGame()
    {
        // LEVEL 3C CHANGE:
        // Starting lives now depends on the selected difficulty.
        _gameState.StartGame(_difficultySettings.StartingLives);

        _scoreManager.ResetScore();

        _drone.Reset(
            GameSettings.StartDroneX,
            GameSettings.StartDroneY
        );

        _obstacles.Clear();

        // LEVEL 3C CHANGE:
        // Reset obstacle spawning using the selected difficulty settings.
        _obstacleSpawner.Reset(_difficultySettings);

        _collisionCooldown = 0f;
        _screenShakeTimer = 0f;
    }

    // LEVEL 3C CHANGE:
    // Updates the selected difficulty and loads the matching settings.
    private void SetDifficulty(DifficultyLevel level)
    {
        _selectedDifficulty = level;
        _difficultySettings = DifficultySettings.Get(level);
    }

    // LEVEL 3C CHANGE:
    // Move to the next difficulty option in the start menu.
    private void SelectNextDifficulty()
    {
        DifficultyLevel next = _selectedDifficulty switch
        {
            DifficultyLevel.Easy => DifficultyLevel.Normal,
            DifficultyLevel.Normal => DifficultyLevel.Hard,
            _ => DifficultyLevel.Easy
        };

        SetDifficulty(next);
    }

    // LEVEL 3C CHANGE:
    // Move to the previous difficulty option in the start menu.
    private void SelectPreviousDifficulty()
    {
        DifficultyLevel previous = _selectedDifficulty switch
        {
            DifficultyLevel.Hard => DifficultyLevel.Normal,
            DifficultyLevel.Normal => DifficultyLevel.Easy,
            _ => DifficultyLevel.Hard
        };

        SetDifficulty(previous);
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

                // LEVEL 3C CHANGE:
                // Points per obstacle now depends on difficulty.
                // Hard mode can reward more points because it is harder.
                _scoreManager.AddScore(_difficultySettings.PointsPerObstacle);
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

        Vector2 crashPosition = new Vector2(
            _drone.Position.X + _drone.Width / 2f,
            _drone.Position.Y + _drone.Height / 2f
        );

        _particles.EmitCrash(crashPosition);
        _screenShakeTimer = 0.25f;

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

    protected override void Draw(GameTime gameTime)
    {
        GraphicsDevice.Clear(new Color(12, 18, 32));

        if (_spriteBatch is null || _pixel is null)
        {
            return;
        }

        Vector2 shakeOffset = GetScreenShakeOffset();

        _spriteBatch.Begin(
            transformMatrix: Matrix.CreateTranslation(
                shakeOffset.X,
                shakeOffset.Y,
                0f
            )
        );

        DrawBackground();
        DrawDrone();
        DrawObstacles();
        _particles.Draw(_spriteBatch, _pixel);
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

        // LEVEL 3C CHANGE:
        // Start screen now also works as the difficulty selection menu.
        if (_gameState.Current == GameStateType.Start)
        {
            DrawPanel(new Color(30, 70, 120, 210));

            PixelText.DrawCenteredText(
                _spriteBatch!,
                _pixel!,
                "DRONE GAME",
                GameSettings.ScreenWidth,
                165,
                5,
                Color.White
            );

            PixelText.DrawCenteredText(
                _spriteBatch!,
                _pixel!,
                $"MODE {_difficultySettings.Name}",
                GameSettings.ScreenWidth,
                230,
                4,
                new Color(255, 214, 10)
            );

            PixelText.DrawCenteredText(
                _spriteBatch!,
                _pixel!,
                "A D OR LEFT RIGHT TO CHANGE",
                GameSettings.ScreenWidth,
                285,
                2,
                new Color(0, 217, 255)
            );

            PixelText.DrawCenteredText(
                _spriteBatch!,
                _pixel!,
                "1 EASY  2 NORMAL  3 HARD",
                GameSettings.ScreenWidth,
                315,
                2,
                Color.White
            );

            PixelText.DrawCenteredText(
                _spriteBatch!,
                _pixel!,
                "ENTER TO START",
                GameSettings.ScreenWidth,
                350,
                3,
                new Color(0, 217, 255)
            );

            PixelText.DrawCenteredText(
                _spriteBatch!,
                _pixel!,
                "ENDLESS SCORE MODE",
                GameSettings.ScreenWidth,
                390,
                2,
                new Color(255, 214, 10)
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
        _starfield.Draw(_spriteBatch!, _pixel!);

        for (int y = 0; y < GameSettings.ScreenHeight; y += 60)
        {
            DrawRect(
                new Rectangle(0, y, GameSettings.ScreenWidth, 2),
                new Color(255, 255, 255, 18)
            );
        }

        for (int x = 0; x < GameSettings.ScreenWidth; x += 90)
        {
            DrawRect(
                new Rectangle(x, 0, 2, GameSettings.ScreenHeight),
                new Color(255, 255, 255, 12)
            );
        }
    }

    // LEVEL 3E CHANGE:
    // HUD now shows:
    // - Score
    // - Lives
    // - Best score
    // - Difficulty mode
    // - Obstacle pressure progression bar
    private void DrawHud()
    {
        DrawRect(
            new Rectangle(0, 0, GameSettings.ScreenWidth, 104),
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

        PixelText.DrawText(
            _spriteBatch!,
            _pixel!,
            $"MODE {_difficultySettings.Name}",
            new Vector2(20, 52),
            2,
            Color.White
        );

        PixelText.DrawText(
            _spriteBatch!,
            _pixel!,
            $"OBSTACLES {_obstacleSpawner.CurrentMaxObstacles}/{_difficultySettings.MaxObstacles}",
            new Vector2(20, 78),
            2,
            new Color(255, 214, 10)
        );

        DrawProgressBar(
            x: 360,
            y: 80,
            width: 320,
            height: 14,
            progress: _obstacleSpawner.ProgressPercent
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
            Rectangle body = obstacle.GetBounds();

            DrawRect(body, new Color(255, 80, 100));

            var inner = new Rectangle(
                body.X + 4,
                body.Y + 4,
                body.Width - 8,
                body.Height - 8
            );

            DrawRect(inner, new Color(120, 20, 45));

            var warningStripe = new Rectangle(
                body.X,
                body.Y,
                body.Width,
                5
            );

            DrawRect(warningStripe, new Color(255, 214, 10));
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

    private Vector2 GetScreenShakeOffset()
    {
        if (_screenShakeTimer <= 0f)
        {
            return Vector2.Zero;
        }

        float strength = 5f * (_screenShakeTimer / 0.25f);

        return new Vector2(
            _visualRandom.NextSingle() * strength - strength / 2f,
            _visualRandom.NextSingle() * strength - strength / 2f
        );
    }

    // LEVEL 3E CHANGE:
    // Draws the obstacle pressure progress bar.
    // The bar fills as the player survives longer.
    // When full, the game has reached the maximum obstacle limit.
    private void DrawProgressBar(int x, int y, int width, int height, float progress)
    {
        progress = MathHelper.Clamp(progress, 0f, 1f);

        DrawRect(
            new Rectangle(x, y, width, height),
            new Color(255, 255, 255, 40)
        );

        DrawRect(
            new Rectangle(x + 2, y + 2, width - 4, height - 4),
            new Color(20, 20, 20, 200)
        );

        int fillWidth = (int)((width - 4) * progress);

        DrawRect(
            new Rectangle(x + 2, y + 2, fillWidth, height - 4),
            new Color(255, 214, 10)
        );
    }

    private void DrawRect(Rectangle rectangle, Color color)
    {
        _spriteBatch!.Draw(_pixel!, rectangle, color);
    }
}