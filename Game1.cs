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

    // LEVEL 4A CHANGE:
    // Enemies are separate from obstacles.
    // They move differently and can be destroyed by charged shots.
    private readonly List<Enemy> _enemies = new();

    // LEVEL 4A CHANGE:
    // Charged shots fired by the player.
    private readonly List<ChargeShot> _shots = new();

    // LEVEL 4B CHANGE:
    // Bullets fired by enemies.
    private readonly List<EnemyBullet> _enemyBullets = new();

    private readonly GameState _gameState = new();
    private readonly InputManager _inputManager = new();
    private readonly ScoreManager _scoreManager = new();

    private readonly ObstacleSpawner _obstacleSpawner = new();

    // LEVEL 4A CHANGE:
    // New enemy spawner. It controls enemy spawn rate and enemy progression.
    private readonly EnemySpawner _enemySpawner = new();

    private readonly Starfield _starfield = new(120);
    private readonly ParticleSystem _particles = new();

    private DifficultyLevel _selectedDifficulty = DifficultyLevel.Normal;

    private DifficultySettings _difficultySettings =
        DifficultySettings.Get(DifficultyLevel.Normal);

    private float _collisionCooldown;
    private float _screenShakeTimer;

    // LEVEL 4C CHANGE:
    // Auto charge system.
    // The game slowly builds shot charges up to 3.
    // Press J to spend 1 charge and shoot.
    private int _shotCharges = GameSettings.MaxShotCharges;
    private float _shotRechargeTimer;

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

        // LEVEL 4C CHANGE:
        // Charges build automatically over time.
        // Press J to spend 1 charge and shoot.
        HandleChargeShot(deltaTime);

        _obstacleSpawner.Update(
            deltaTime,
            _obstacles,
            _scoreManager.Score,
            _difficultySettings
        );

        _enemySpawner.Update(
            deltaTime,
            _enemies,
            _scoreManager.Score,
            _difficultySettings
        );

        UpdateObstacles(deltaTime);
        UpdateEnemies(deltaTime);

        HandleEnemyShooting();

        UpdateShots(deltaTime);
        UpdateEnemyBullets(deltaTime);

        CheckShotHits();
        CheckCollision();

        Window.Title =
            $"Score: {_scoreManager.Score} | " +
            $"Lives: {_gameState.Lives} | " +
            $"Best: {_scoreManager.HighScore} | " +
            $"Mode: {_difficultySettings.Name}";
    }

    private void StartNewGame()
    {
        _gameState.StartGame(_difficultySettings.StartingLives);

        _scoreManager.ResetScore();

        _drone.Reset(
            GameSettings.StartDroneX,
            GameSettings.StartDroneY
        );

        _obstacles.Clear();
        _enemies.Clear();
        _shots.Clear();
        _enemyBullets.Clear();

        _obstacleSpawner.Reset(_difficultySettings);
        _enemySpawner.Reset(_difficultySettings);

        _collisionCooldown = 0f;
        _screenShakeTimer = 0f;

        // LEVEL 4C CHANGE:
        // Start each run with full charges so the player has emergency shots.
        _shotCharges = GameSettings.MaxShotCharges;
        _shotRechargeTimer = 0f;
    }

    private void SetDifficulty(DifficultyLevel level)
    {
        _selectedDifficulty = level;
        _difficultySettings = DifficultySettings.Get(level);
    }

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

    // LEVEL 4C CHANGE:
    // Charges build automatically over time.
    // The player no longer needs to hold J.
    // Press J to spend 1 charge and shoot.
    private void HandleChargeShot(float deltaTime)
    {
        RechargeShot(deltaTime);

        if (_inputManager.IsKeyPressed(Keys.J))
        {
            TryFireChargedShot();
        }
    }

    private void RechargeShot(float deltaTime)
    {
        if (_shotCharges >= GameSettings.MaxShotCharges)
        {
            _shotRechargeTimer = 0f;
            return;
        }

        _shotRechargeTimer += deltaTime;

        if (_shotRechargeTimer < GameSettings.ShotRechargeSeconds)
        {
            return;
        }

        _shotRechargeTimer = 0f;
        _shotCharges++;
    }

    private void TryFireChargedShot()
    {
        if (_shotCharges <= 0)
        {
            return;
        }

        Vector2 shotPosition = new Vector2(
            _drone.Position.X + _drone.Width + 4,
            _drone.Position.Y + _drone.Height / 2f
        );

        _shots.Add(new ChargeShot(shotPosition));

        _shotCharges--;
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
                _scoreManager.AddScore(_difficultySettings.PointsPerObstacle);
            }

            if (obstacle.IsOffScreen())
            {
                _obstacles.RemoveAt(i);
            }
        }
    }

    private void UpdateEnemies(float deltaTime)
    {
        for (int i = _enemies.Count - 1; i >= 0; i--)
        {
            Enemy enemy = _enemies[i];

            enemy.Update(deltaTime);

            if (enemy.IsOffScreen())
            {
                _enemies.RemoveAt(i);
            }
        }
    }

    private void UpdateShots(float deltaTime)
    {
        for (int i = _shots.Count - 1; i >= 0; i--)
        {
            ChargeShot shot = _shots[i];

            shot.Update(deltaTime);

            if (shot.IsOffScreen())
            {
                _shots.RemoveAt(i);
            }
        }
    }

    // LEVEL 4B CHANGE:
    // Enemies fire bullets on a timer.
    // They only shoot after they enter the visible screen.
    private void HandleEnemyShooting()
    {
        foreach (Enemy enemy in _enemies)
        {
            if (enemy.Position.X > GameSettings.ScreenWidth - enemy.Width)
            {
                continue;
            }

            if (!enemy.CanShoot())
            {
                continue;
            }

            _enemyBullets.Add(new EnemyBullet(enemy.GetShootPosition()));
            enemy.ResetShootTimer();
        }
    }

    private void UpdateEnemyBullets(float deltaTime)
    {
        for (int i = _enemyBullets.Count - 1; i >= 0; i--)
        {
            EnemyBullet bullet = _enemyBullets[i];

            bullet.Update(deltaTime);

            if (bullet.IsOffScreen())
            {
                _enemyBullets.RemoveAt(i);
            }
        }
    }

    private void CheckShotHits()
    {
        for (int shotIndex = _shots.Count - 1; shotIndex >= 0; shotIndex--)
        {
            ChargeShot shot = _shots[shotIndex];
            Rectangle shotBox = shot.GetBounds();

            bool shotRemoved = false;

            // LEVEL 4B CHANGE:
            // Player shot can destroy enemy bullets.
            for (int bulletIndex = _enemyBullets.Count - 1; bulletIndex >= 0; bulletIndex--)
            {
                EnemyBullet bullet = _enemyBullets[bulletIndex];

                if (!shotBox.Intersects(bullet.GetBounds()))
                {
                    continue;
                }

                Vector2 hitPosition = new Vector2(
                    bullet.Position.X + bullet.Width / 2f,
                    bullet.Position.Y + bullet.Height / 2f
                );

                _particles.EmitCrash(hitPosition);

                _enemyBullets.RemoveAt(bulletIndex);
                _shots.RemoveAt(shotIndex);

                shotRemoved = true;
                break;
            }

            if (shotRemoved)
            {
                continue;
            }

            for (int enemyIndex = _enemies.Count - 1; enemyIndex >= 0; enemyIndex--)
            {
                Enemy enemy = _enemies[enemyIndex];

                if (!shotBox.Intersects(enemy.GetBounds()))
                {
                    continue;
                }

                Vector2 hitPosition = new Vector2(
                    enemy.Position.X + enemy.Width / 2f,
                    enemy.Position.Y + enemy.Height / 2f
                );

                _particles.EmitCrash(hitPosition);
                _scoreManager.AddScore(enemy.ScoreReward);

                _enemies.RemoveAt(enemyIndex);
                _shots.RemoveAt(shotIndex);

                shotRemoved = true;
                break;
            }

            if (shotRemoved)
            {
                continue;
            }

            if (!shot.CanBreakObstacle)
            {
                continue;
            }

            for (int obstacleIndex = _obstacles.Count - 1; obstacleIndex >= 0; obstacleIndex--)
            {
                Obstacle obstacle = _obstacles[obstacleIndex];

                if (!shotBox.Intersects(obstacle.GetBounds()))
                {
                    continue;
                }

                Vector2 hitPosition = new Vector2(
                    obstacle.Position.X + obstacle.Width / 2f,
                    obstacle.Position.Y + obstacle.Height / 2f
                );

                _particles.EmitCrash(hitPosition);

                _obstacles.RemoveAt(obstacleIndex);
                _shots.RemoveAt(shotIndex);

                break;
            }
        }
    }

    private void CheckCollision()
    {
        bool crashed = CollisionChecker.HasCollision(_drone, _obstacles);

        Rectangle droneBox = _drone.GetBounds();

        foreach (Enemy enemy in _enemies)
        {
            if (droneBox.Intersects(enemy.GetBounds()))
            {
                crashed = true;
                break;
            }
        }

        foreach (Enemy enemy in _enemies)
        {
            if (droneBox.Intersects(enemy.GetBounds()))
            {
                crashed = true;
                break;
            }
        }

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
        _enemies.Clear();
        _shots.Clear();
        _enemyBullets.Clear();

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

        // LEVEL 4A CHANGE:
        // Draw enemies and shots after obstacles.
        DrawEnemies();
        DrawEnemyBullets();
        DrawShots();

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

        if (_gameState.Current == GameStateType.Start)
        {
            DrawPanel(new Color(30, 70, 120, 210));

            PixelText.DrawCenteredText(
                _spriteBatch!,
                _pixel!,
                "DRONE GAME",
                GameSettings.ScreenWidth,
                150,
                5,
                Color.White
            );

            PixelText.DrawCenteredText(
                _spriteBatch!,
                _pixel!,
                $"MODE {_difficultySettings.Name}",
                GameSettings.ScreenWidth,
                215,
                4,
                new Color(255, 214, 10)
            );

            PixelText.DrawCenteredText(
                _spriteBatch!,
                _pixel!,
                "A D OR LEFT RIGHT TO CHANGE",
                GameSettings.ScreenWidth,
                270,
                2,
                new Color(0, 217, 255)
            );

            PixelText.DrawCenteredText(
                _spriteBatch!,
                _pixel!,
                "1 EASY  2 NORMAL  3 HARD",
                GameSettings.ScreenWidth,
                300,
                2,
                Color.White
            );

            PixelText.DrawCenteredText(
                _spriteBatch!,
                _pixel!,
                "ENTER TO START",
                GameSettings.ScreenWidth,
                335,
                3,
                new Color(0, 217, 255)
            );

            PixelText.DrawCenteredText(
                _spriteBatch!,
                _pixel!,
                "HOLD J RELEASE TO SHOOT",
                GameSettings.ScreenWidth,
                380,
                2,
                new Color(255, 214, 10)
            );

            PixelText.DrawCenteredText(
                _spriteBatch!,
                _pixel!,
                "ENDLESS SCORE MODE",
                GameSettings.ScreenWidth,
                410,
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

    private void DrawHud()
    {
        DrawRect(
            new Rectangle(0, 0, GameSettings.ScreenWidth, 132),
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

        PixelText.DrawText(
            _spriteBatch!,
            _pixel!,
            $"ENEMIES {_enemySpawner.CurrentMaxEnemies}/{_difficultySettings.MaxEnemies}",
            new Vector2(20, 104),
            2,
            new Color(255, 140, 40)
        );

        DrawProgressBar(
            x: 360,
            y: 106,
            width: 320,
            height: 14,
            progress: _enemySpawner.ProgressPercent
        );

        // LEVEL 4C CHANGE:
        // Show stored shot charges and recharge progress.
        PixelText.DrawText(
            _spriteBatch!,
            _pixel!,
            $"SHOT {_shotCharges}/{GameSettings.MaxShotCharges}",
            new Vector2(700, 52),
            2,
            new Color(0, 217, 255)
        );

        float rechargeProgress = _shotCharges >= GameSettings.MaxShotCharges
            ? 1f
            : MathHelper.Clamp(
                _shotRechargeTimer / GameSettings.ShotRechargeSeconds,
                0f,
                1f
            );

        DrawProgressBar(
            x: 700,
            y: 80,
            width: 150,
            height: 14,
            progress: rechargeProgress
        );

        PixelText.DrawText(
            _spriteBatch!,
            _pixel!,
            "PRESS J TO FIRE",
            new Vector2(700, 104),
            2,
            _shotCharges > 0
                ? new Color(255, 214, 10)
                : new Color(255, 80, 100)
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

    private void DrawEnemies()
    {
        foreach (Enemy enemy in _enemies)
        {
            Rectangle body = enemy.GetBounds();

            DrawRect(body, new Color(255, 140, 40));

            var inner = new Rectangle(
                body.X + 5,
                body.Y + 5,
                body.Width - 10,
                body.Height - 10
            );

            DrawRect(inner, new Color(100, 45, 10));

            var eye = new Rectangle(
                body.X + body.Width - 11,
                body.Y + 10,
                5,
                5
            );

            DrawRect(eye, Color.White);
        }
    }

    private void DrawShots()
    {
        foreach (ChargeShot shot in _shots)
        {
            Rectangle body = shot.GetBounds();

            DrawRect(body, new Color(0, 217, 255));

            var core = new Rectangle(
                body.X + 3,
                body.Y + 2,
                body.Width - 6,
                body.Height - 4
            );

            DrawRect(core, Color.White);
        }
    }

    private void DrawEnemyBullets()
    {
        foreach (EnemyBullet bullet in _enemyBullets)
        {
            DrawRect(bullet.GetBounds(), new Color(255, 60, 60));

            var core = new Rectangle(
                bullet.GetBounds().X + 2,
                bullet.GetBounds().Y + 2,
                bullet.GetBounds().Width - 4,
                MathHelper.Max(2, bullet.GetBounds().Height - 4)
            );

            DrawRect(core, new Color(255, 214, 10));
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

    private void DrawRect(Rectangle rectangle, Color color)
    {
        _spriteBatch!.Draw(_pixel!, rectangle, color);
    }
}