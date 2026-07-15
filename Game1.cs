using System;
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

    // ML-1 CHANGE:
    // Logger for collecting gameplay training data.
    // This is the first real step before training an ML model.
    private readonly GameplayDataLogger _gameplayDataLogger = new();

    // ML-1 CHANGE:
    // Tracks how long the current run has lasted.
    // This becomes one of the ML model features.
    private float _survivalSeconds;

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

    // LEVEL 5A CHANGE:
    // Temporary shields created by Tank enemies.
    // Shields dissolve after a few seconds and block player shots.
    private readonly List<EnergyShield> _shields = new();

    private readonly GameState _gameState = new();
    private readonly InputManager _inputManager = new();
    private readonly ScoreManager _scoreManager = new();

    // ML-2 CHANGE:
    // Rule-based bot used to collect gameplay data faster.
    // This bot is not ML. It is an autoplayer helper.
    private readonly BotPlayer _botPlayer = new();

    private bool _isBotEnabled;

    // ML-2 POLISH:
    // If bot was used at any time during this run,
    // the score should not be saved as BEST.
    // This keeps human high score fair.
    private bool _wasBotUsedThisRun;

    // ML-5 CHANGE:
    // Bot auto replay is used to collect ML data faster.
    // When bot is ON, the game can restart automatically after Game Over.
    private bool _isBotAutoReplayEnabled = true;
    private float _botAutoReplayTimer = -1f;

    private const float BotAutoReplayDelaySeconds = 2.0f;

    // ML-2 POLISH:
    // Random generator for bot difficulty selection.
    private readonly System.Random _botRandom = new();

    // ML-3 POLISH:
    // Balanced random difficulty bag.
    // This prevents the bot from randomly picking the same mode too many times.
    // Every bag contains Easy, Normal, and Hard once.
    private readonly List<DifficultyLevel> _botDifficultyBag = new();

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

    // ML-1 CHANGE:
    // After Game Over, the player can give feedback once.
    // This prevents duplicate feedback rows for the same run.
    private bool _hasSavedMlFeedback;

    // ML-1 CHANGE:
    // These fields store the latest gameplay state before Game Over.
    // This is useful because enemies/bullets may be cleared after collision.
    private int _lastActiveObstacles;
    private int _lastCurrentMaxObstacles;
    private float _lastObstaclePressure;

    private int _lastActiveEnemies;
    private int _lastCurrentMaxEnemies;
    private float _lastEnemyPressure;

    private int _lastActiveEnemyBullets;
    private int _lastActivePlayerShots;
    private int _lastShotCharges;
    private int _lastActiveShields;

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

        // ML-2 CHANGE:
        // Press B to turn the rule-based bot on or off.
        if (_inputManager.IsKeyPressed(Keys.B))
        {
            _isBotEnabled = !_isBotEnabled;

            if (_isBotEnabled)
            {
                _wasBotUsedThisRun = true;
                _botPlayer.Reset();
            }
        }

        // ML-5 CHANGE:
        // Press V to turn bot auto replay on/off.
        // Useful when collecting data, but also lets you pause the data farm.
        if (_inputManager.IsKeyPressed(Keys.V))
        {
            _isBotAutoReplayEnabled = !_isBotAutoReplayEnabled;
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
        // ML-2 POLISH:
        // Manual difficulty selection is only for human mode.
        // When bot is ON, the bot will randomly choose difficulty when the run starts.
        if (!_isBotEnabled)
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
        float deltaTime = (float)gameTime.ElapsedGameTime.TotalSeconds;

        // ML-2 POLISH:
        // Bot runs are automatically labeled and saved.
        HandleBotAutoFeedback();

        // ML-1 CHANGE:
        // Human-only runs still ask the player to label the whole run.
        HandleGameOverFeedback();

        // UI POLISH:
        // After feedback is saved, allow human player to change difficulty
        // before restarting the next run.
        if (_hasSavedMlFeedback && !_isBotEnabled)
        {
            HandleGameOverDifficultySelection();
        }

        // ML-5 CHANGE:
        // If this was a bot run, feedback is saved, and auto replay is enabled,
        // restart automatically after a short delay.
        if (_wasBotUsedThisRun &&
            _hasSavedMlFeedback &&
            _isBotAutoReplayEnabled)
        {
            if (_botAutoReplayTimer < 0f)
            {
                _botAutoReplayTimer = BotAutoReplayDelaySeconds;
            }

            _botAutoReplayTimer -= deltaTime;

            if (_botAutoReplayTimer <= 0f)
            {
                StartNewGame();
                base.Update(gameTime);
                return;
            }
        }

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

        // ML-1 CHANGE:
        // Track survival time as gameplay data.
        _survivalSeconds += deltaTime;

        if (_collisionCooldown > 0f)
        {
            _collisionCooldown -= deltaTime;
            base.Update(gameTime);
            return;
        }

        // LEVEL 4C CHANGE:
        // Shots recharge automatically.
        // Manual shooting only happens when bot is OFF.
        HandleChargeShot(deltaTime);

        // ML-2 CHANGE:
        // If bot is ON, bot controls movement and shooting.
        // If bot is OFF, player controls movement normally.
        HandlePlayerOrBotControl(deltaTime);

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

        UpdateShields(deltaTime);
        UpdateShots(deltaTime);
        UpdateEnemyBullets(deltaTime);

        CheckShotHits();

        // ML-1 CHANGE:
        // Capture the latest gameplay state before collision may clear objects.
        CaptureGameplaySnapshot();

        CheckCollision();

        Window.Title =
            $"Score: {_scoreManager.Score} | " +
            $"Lives: {_gameState.Lives} | " +
            $"Best: {_scoreManager.HighScore} | " +
            $"Mode: {_difficultySettings.Name}";
    }

    // UI POLISH:
    // Allows difficulty selection from the Game Over screen.
    // We use A/D or Left/Right because 1/2/3 are already used for ML feedback.
    private void HandleGameOverDifficultySelection()
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
    }

    private void StartNewGame()
    {

        // ML-2 POLISH:
        // Reset bot smoothing state when a new run starts.
        _botPlayer.Reset();

        // ML-1 CHANGE:
        // Reset survival timer for the new run.
        _survivalSeconds = 0f;

        // ML-5 CHANGE:
        // Reset auto replay timer for the new run.
        _botAutoReplayTimer = -1f;

        // ML-1 CHANGE:
        // Reset feedback flag for the new run.
        _hasSavedMlFeedback = false;

        // ML-2 POLISH:
        // If bot is enabled before the run starts,
        // this run is counted as a bot run.
        _wasBotUsedThisRun = _isBotEnabled;

        // ML-2 POLISH:
        // Bot chooses a random difficulty at the start of every bot run.
        if (_isBotEnabled)
        {
            SelectRandomDifficultyForBotRun();
        }

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
        _shields.Clear();

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

    // ML-3 POLISH:
    // Bot uses balanced random difficulty selection.
    // Instead of pure 1/3 random every run, the bot uses a shuffled bag:
    // Easy + Normal + Hard.
    // This gives better ML data distribution.
    private void SelectRandomDifficultyForBotRun()
    {
        if (_botDifficultyBag.Count == 0)
        {
            RefillBotDifficultyBag();
        }

        DifficultyLevel selectedDifficulty = _botDifficultyBag[^1];
        _botDifficultyBag.RemoveAt(_botDifficultyBag.Count - 1);

        SetDifficulty(selectedDifficulty);

        Window.Title = $"BOT BALANCED MODE: {_difficultySettings.Name}";
    }

    // ML-2 POLISH:
    // Refill and shuffle the bot difficulty bag.
    // This keeps difficulty selection balanced but still random-looking.
    private void RefillBotDifficultyBag()
    {
        _botDifficultyBag.Clear();

        _botDifficultyBag.Add(DifficultyLevel.Easy);
        _botDifficultyBag.Add(DifficultyLevel.Normal);
        _botDifficultyBag.Add(DifficultyLevel.Hard);

        // Fisher-Yates shuffle.
        for (int i = _botDifficultyBag.Count - 1; i > 0; i--)
        {
            int j = _botRandom.Next(i + 1);

            DifficultyLevel temp = _botDifficultyBag[i];
            _botDifficultyBag[i] = _botDifficultyBag[j];
            _botDifficultyBag[j] = temp;
        }
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

    // ML-4 POLISH:
    // Score should not create TooEasy by itself too early.
    // The bot must survive long enough before high score can mean TooEasy.
    private float GetMinimumSecondsForScoreBasedTooEasy()
    {
        return _selectedDifficulty switch
        {
            DifficultyLevel.Easy => 170f,
            DifficultyLevel.Hard => 50f,
            _ => 78f
        };
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

    // ML-2 CHANGE:
    // Chooses between human control and bot control.
    // Human mode uses keyboard.
    // Bot mode uses rule-based decisions.
    private void HandlePlayerOrBotControl(float deltaTime)
    {
        if (!_isBotEnabled)
        {
            HandleDroneMovement(deltaTime);
            return;
        }

        BotDecision decision = _botPlayer.GetDecision(
            deltaTime,
            _drone,
            _obstacles,
            _enemies,
            _enemyBullets,
            _shields,
            _shotCharges,
            _shots.Count
        );

        _drone.Move(decision.MovementDirection, deltaTime);

        _drone.ClampToScreen(
            GameSettings.ScreenWidth,
            GameSettings.ScreenHeight
        );

        if (decision.ShouldFire)
        {
            TryFireChargedShot();
        }
    }

    // LEVEL 4C CHANGE:
    // Charges build automatically over time.
    // The player no longer needs to hold J.
    // Press J to spend 1 charge and shoot.
    //
    // ML-2 CHANGE:
    // If bot is enabled, manual J shooting is disabled.
    // Bot will call TryFireChargedShot() by itself.
    private void HandleChargeShot(float deltaTime)
    {
        RechargeShot(deltaTime);

        if (_isBotEnabled)
        {
            return;
        }

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

    // LEVEL 5A CHANGE:
    // Update Tank shields and remove them when they dissolve,
    // get destroyed, or leave the screen.
    private void UpdateShields(float deltaTime)
    {
        for (int i = _shields.Count - 1; i >= 0; i--)
        {
            EnergyShield shield = _shields[i];

            shield.Update(deltaTime);

            if (shield.IsExpired())
            {
                _shields.RemoveAt(i);
            }
        }
    }

    // LEVEL 4B CHANGE:
    // Enemies fire bullets on a timer.
    // They only shoot after they enter the visible screen.
    //
    // LEVEL 5A CHANGE:
    // Tank enemies no longer shoot bullets.
    // Instead, Tank deploys temporary shields.
    // Sniper shoots less often, but its bullets are faster.
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

            if (enemy.Type == EnemyType.Tank)
            {
                TryDeployTankShield(enemy);
                enemy.ResetShootTimer();
                continue;
            }

            float bulletSpeed = MathHelper.Lerp(
                _difficultySettings.EnemyBulletStartSpeed,
                _difficultySettings.EnemyBulletMaxSpeed,
                _enemySpawner.ProgressPercent
            );

            // LEVEL 5A CHANGE:
            // Enemy type can modify bullet speed.
            // Sniper bullets are faster.
            bulletSpeed *= enemy.GetBulletSpeedMultiplier();

            _enemyBullets.Add(new EnemyBullet(
                enemy.GetShootPosition(),
                bulletSpeed
            ));

            enemy.ResetShootTimer();
        }
    }

    // LEVEL 5A CHANGE:
    // Tank creates a temporary shield in front of itself.
    // The shield blocks player shots and dissolves after a short time.
    private void TryDeployTankShield(Enemy tank)
    {
        if (_shields.Count >= GameSettings.MaxActiveTankShields)
        {
            return;
        }

        float shieldX =
            tank.Position.X -
            GameSettings.TankShieldWidth -
            8;

        float shieldY =
            tank.Position.Y +
            tank.Height / 2f -
            GameSettings.TankShieldHeight / 2f;

        shieldY = MathHelper.Clamp(
            shieldY,
            135f,
            GameSettings.ScreenHeight - GameSettings.TankShieldHeight - 20f
        );

        float shieldSpeed = tank.Speed * 0.80f;

        _shields.Add(new EnergyShield(
            new Vector2(shieldX, shieldY),
            shieldSpeed
        ));
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
            // COLLISION POLISH:
            // Use swept bounds instead of current bounds only.
            // This prevents fast shots from slipping through targets.
            Rectangle shotBox = shot.GetSweptBounds();

            bool shotRemoved = false;

            // LEVEL 5A POLISH:
            // Player shots can destroy enemy bullets and gain small points.
            // This works for both human and bot because both use the same shot system.
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

                _scoreManager.AddScore(GameSettings.DestroyEnemyBulletScore);

                shotRemoved = true;
                break;
            }

            if (shotRemoved)
            {
                continue;
            }

            // LEVEL 5A POLISH:
            // Tank shields / barricades block shots.
            // If the shield is destroyed, the player gets points.
            // Human and bot both receive the reward.
            for (int shieldIndex = _shields.Count - 1; shieldIndex >= 0; shieldIndex--)
            {
                EnergyShield shield = _shields[shieldIndex];

                if (!shotBox.Intersects(shield.GetBounds()))
                {
                    continue;
                }

                Vector2 hitPosition = new Vector2(
                    shield.Position.X + shield.Width / 2f,
                    shield.Position.Y + shield.Height / 2f
                );

                shield.TakeDamage(1);
                _particles.EmitCrash(hitPosition);

                if (shield.IsDestroyed())
                {
                    _shields.RemoveAt(shieldIndex);
                    _scoreManager.AddScore(GameSettings.DestroyShieldScore);
                }

                _shots.RemoveAt(shotIndex);

                shotRemoved = true;
                break;
            }

            if (shotRemoved)
            {
                continue;
            }

            // LEVEL 5A POLISH:
            // Destroying enemies gives score.
            // Scout, ZigZag, and Sniper usually die in 1 hit.
            // Tank needs 2 hits, so score is only awarded when Tank is fully destroyed.
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

                enemy.TakeDamage(1);

                _particles.EmitCrash(hitPosition);

                if (enemy.IsDestroyed())
                {
                    _scoreManager.AddScore(enemy.ScoreReward);
                    _enemies.RemoveAt(enemyIndex);
                }

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

            // LEVEL 5A POLISH:
            // Destroying obstacles / barricades now gives points too.
            // This makes shooting useful even when there are no enemies nearby.
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

                _scoreManager.AddScore(GameSettings.DestroyObstacleScore);

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

        foreach (EnemyBullet bullet in _enemyBullets)
        {
            // COLLISION POLISH:
            // Use swept bounds so fast enemy bullets cannot visually pass through the drone.
            if (droneBox.Intersects(bullet.GetSweptBounds()))
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
        _shields.Clear();

        _drone.Reset(
            GameSettings.StartDroneX,
            GameSettings.StartDroneY
        );

        _collisionCooldown = GameSettings.CollisionCooldownSeconds;

        if (_gameState.Current == GameStateType.Fail &&
            !_wasBotUsedThisRun)
        {
            _scoreManager.SaveHighScoreIfNeeded();
        }

        // LEVEL 5A CHANGE:
        // Tank shields also act like temporary hazards.
        // Touching a shield costs one life.
        foreach (EnergyShield shield in _shields)
        {
            if (droneBox.Intersects(shield.GetBounds()))
            {
                crashed = true;
                break;
            }
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

        DrawShields();

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
            DrawStartPanel(new Color(30, 70, 120, 210));



            PixelText.DrawCenteredText(
                _spriteBatch!,
                _pixel!,
                "DRONE GAME",
                GameSettings.ScreenWidth,
                150,
                5,
                Color.White
            );

            string modeText = _isBotEnabled
                ? "BOT MODE RANDOM"
                : $"MODE {_difficultySettings.Name}";

            PixelText.DrawCenteredText(
                _spriteBatch!,
                _pixel!,
                modeText,
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

            // ML-2 POLISH:
            // In human mode, show manual difficulty selection.
            // In bot mode, show that the bot will choose difficulty randomly.
            string difficultyHelpText = _isBotEnabled
                ? "BOT WILL PICK EASY NORMAL OR HARD"
                : "1 EASY  2 NORMAL  3 HARD";

            PixelText.DrawCenteredText(
                _spriteBatch!,
                _pixel!,
                difficultyHelpText,
                GameSettings.ScreenWidth,
                300,
                2,
                Color.White
            );

            PixelText.DrawCenteredText(
                _spriteBatch!,
                _pixel!,
                "PRESS B TO TOGGLE BOT",
                GameSettings.ScreenWidth,
                440,
                2,
                new Color(0, 217, 255)
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
                "CHARGES AUTO BUILD PRESS J",
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
                185,
                5,
                Color.White
            );

            PixelText.DrawCenteredText(
                _spriteBatch!,
                _pixel!,
                $"SCORE {_scoreManager.Score}",
                GameSettings.ScreenWidth,
                255,
                3,
                new Color(255, 214, 10)
            );

            string feedbackText;

            if (_hasSavedMlFeedback && _wasBotUsedThisRun)
            {
                feedbackText = "BOT FEEDBACK AUTO SAVED";
            }
            else if (_hasSavedMlFeedback)
            {
                feedbackText = "FEEDBACK SAVED THANK YOU";
            }
            else
            {
                feedbackText = "1 TOO EASY  2 NORMAL  3 TOO HARD";
            }

            PixelText.DrawCenteredText(
                _spriteBatch!,
                _pixel!,
                feedbackText,
                GameSettings.ScreenWidth,
                310,
                2,
                _hasSavedMlFeedback
                    ? new Color(0, 217, 255)
                    : Color.White
            );

            string restartText;

            if (_wasBotUsedThisRun && _isBotAutoReplayEnabled)
            {
                float timer = _botAutoReplayTimer < 0f
                    ? BotAutoReplayDelaySeconds
                    : _botAutoReplayTimer;

                restartText = $"AUTO RESTART IN {Math.Ceiling(timer)}";
            }
            else if (_wasBotUsedThisRun)
            {
                restartText = "BOT AUTO REPLAY OFF";
            }
            else if (_hasSavedMlFeedback)
            {
                restartText = $"A D CHANGE MODE   CURRENT {_difficultySettings.Name}";
            }
            else
            {
                restartText = "SAVE FEEDBACK FIRST";
            }

            PixelText.DrawCenteredText(
                _spriteBatch!,
                _pixel!,
                restartText,
                GameSettings.ScreenWidth,
                355,
                2,
                Color.White
            );

            PixelText.DrawCenteredText(
                _spriteBatch!,
                _pixel!,
                restartText,
                GameSettings.ScreenWidth,
                355,
                2,
                Color.White
            );

            PixelText.DrawCenteredText(
                _spriteBatch!,
                _pixel!,
                "PRESS ENTER TO RESTART",
                GameSettings.ScreenWidth,
                390,
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
        // UI POLISH:
        // Taller HUD so text does not overlap.
        DrawRect(
            new Rectangle(0, 0, GameSettings.ScreenWidth, 150),
            new Color(0, 0, 0, 145)
        );

        // LEFT SIDE: score and game pressure numbers.
        PixelText.DrawText(
            _spriteBatch!,
            _pixel!,
            $"SCORE {_scoreManager.Score}",
            new Vector2(24, 18),
            3,
            Color.White
        );

        PixelText.DrawText(
            _spriteBatch!,
            _pixel!,
            $"MODE {_difficultySettings.Name}",
            new Vector2(24, 54),
            2,
            Color.White
        );

        PixelText.DrawText(
            _spriteBatch!,
            _pixel!,
            $"OBSTACLES {_obstacleSpawner.CurrentMaxObstacles}/{_difficultySettings.MaxObstacles}",
            new Vector2(24, 84),
            2,
            new Color(255, 214, 10)
        );

        PixelText.DrawText(
            _spriteBatch!,
            _pixel!,
            $"ENEMIES {_enemySpawner.CurrentMaxEnemies}/{_difficultySettings.MaxEnemies}",
            new Vector2(24, 112),
            2,
            new Color(255, 140, 40)
        );

        // CENTER: lives and pressure bars.
        PixelText.DrawText(
            _spriteBatch!,
            _pixel!,
            $"LIVES {_gameState.Lives}",
            new Vector2(360, 18),
            3,
            new Color(0, 217, 255)
        );

        PixelText.DrawText(
            _spriteBatch!,
            _pixel!,
            "OBSTACLE PRESSURE",
            new Vector2(330, 62),
            2,
            new Color(255, 214, 10)
        );

        DrawProgressBar(
            x: 330,
            y: 84,
            width: 330,
            height: 16,
            progress: _obstacleSpawner.ProgressPercent
        );

        PixelText.DrawText(
            _spriteBatch!,
            _pixel!,
            "ENEMY PRESSURE",
            new Vector2(330, 108),
            2,
            new Color(255, 140, 40)
        );

        DrawProgressBar(
            x: 330,
            y: 130,
            width: 330,
            height: 16,
            progress: _enemySpawner.ProgressPercent
        );

        // RIGHT SIDE: best, bot, and shot status.
        PixelText.DrawText(
            _spriteBatch!,
            _pixel!,
            $"BEST {_scoreManager.HighScore}",
            new Vector2(700, 18),
            2,
            new Color(255, 214, 10)
        );

        PixelText.DrawText(
            _spriteBatch!,
            _pixel!,
            _isBotEnabled ? "BOT ON" : "BOT OFF",
            new Vector2(700, 44),
            2,
            _isBotEnabled
                ? new Color(0, 217, 255)
                : Color.White
        );

        if (_wasBotUsedThisRun)
        {
            PixelText.DrawText(
                _spriteBatch!,
                _pixel!,
                "NO BEST",
                new Vector2(700, 70),
                2,
                new Color(255, 80, 100)
            );
        }

        PixelText.DrawText(
            _spriteBatch!,
            _pixel!,
            $"SHOT {_shotCharges}/{GameSettings.MaxShotCharges}",
            new Vector2(700, 96),
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
            y: 120,
            width: 160,
            height: 14,
            progress: rechargeProgress
        );

        // UI POLISH:
        // Show shot instruction/status under the charge bar.
        // If no charges are available, tell the player to wait.
        string shotInstructionText = _shotCharges > 0
            ? "PRESS J TO SHOOT"
            : "RECHARGING";

        PixelText.DrawText(
            _spriteBatch!,
            _pixel!,
            shotInstructionText,
            new Vector2(700, 138),
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

            Color bodyColor = enemy.Type switch
            {
                EnemyType.Tank => new Color(150, 70, 220),
                EnemyType.ZigZag => new Color(255, 210, 70),

                // LEVEL 5A POLISH:
                // Sniper is green so it does not look like red obstacles.
                EnemyType.Sniper => new Color(80, 230, 120),

                _ => new Color(255, 140, 40)
            };

            // LEVEL 5A CHANGE:
            // Damaged Tank becomes darker so the player can see it was hit.
            if (enemy.Type == EnemyType.Tank && enemy.Health < enemy.MaxHealth)
            {
                bodyColor = new Color(95, 35, 145);
            }

            DrawRect(body, bodyColor);

            var inner = new Rectangle(
                body.X + 5,
                body.Y + 5,
                body.Width - 10,
                body.Height - 10
            );

            DrawRect(inner, new Color(55, 35, 35));

            var eye = new Rectangle(
                body.X + body.Width - 11,
                body.Y + 10,
                5,
                5
            );

            DrawRect(eye, Color.White);

            if (enemy.Type == EnemyType.Tank)
            {
                DrawTankHealthMarks(enemy);
            }

            if (enemy.Type == EnemyType.Sniper)
            {
                DrawSniperMark(enemy);
            }
        }
    }

    // LEVEL 5A CHANGE:
    // Draw temporary Tank shields.
    // The shield becomes more transparent as it dissolves.
    private void DrawShields()
    {
        foreach (EnergyShield shield in _shields)
        {
            Rectangle body = shield.GetBounds();

            int alpha = (int)(80 + 140 * shield.LifePercent);

            DrawRect(
                body,
                new Color(80, 180, 255, alpha)
            );

            var inner = new Rectangle(
                body.X + 5,
                body.Y + 5,
                body.Width - 10,
                body.Height - 10
            );

            DrawRect(
                inner,
                new Color(20, 80, 140, alpha)
            );

            for (int i = 0; i < shield.MaxHealth; i++)
            {
                Color markColor = i < shield.Health
                    ? new Color(255, 255, 255, alpha)
                    : new Color(40, 40, 40, alpha);

                var mark = new Rectangle(
                    body.X + 5,
                    body.Y + 8 + i * 12,
                    body.Width - 10,
                    5
                );

                DrawRect(mark, markColor);
            }
        }
    }

    // LEVEL 5A CHANGE:
    // Simple visual HP marks for Tank.
    private void DrawTankHealthMarks(Enemy enemy)
    {
        for (int i = 0; i < enemy.MaxHealth; i++)
        {
            Color markColor = i < enemy.Health
                ? new Color(255, 214, 10)
                : new Color(60, 60, 60);

            var mark = new Rectangle(
                (int)enemy.Position.X + 6 + i * 10,
                (int)enemy.Position.Y - 8,
                7,
                5
            );

            DrawRect(mark, markColor);
        }
    }

    // LEVEL 5A CHANGE:
    // Simple visual mark for Sniper enemy.
    // It looks like a small targeting line.
    private void DrawSniperMark(Enemy enemy)
    {
        var scopeLine = new Rectangle(
            (int)enemy.Position.X + 6,
            (int)enemy.Position.Y + enemy.Height / 2,
            enemy.Width - 12,
            3
        );

        DrawRect(scopeLine, Color.White);

        var scopeDot = new Rectangle(
            (int)enemy.Position.X + enemy.Width / 2 - 2,
            (int)enemy.Position.Y + enemy.Height / 2 - 2,
            5,
            5
        );

        DrawRect(scopeDot, new Color(20, 80, 35));
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

    // UI POLISH:
    // Larger panel for the start menu.
    // This gives enough room for difficulty, shooting, bot, and ML instructions.
    private void DrawStartPanel(Color color)
    {
        var panel = new Rectangle(
            GameSettings.ScreenWidth / 2 - 390,
            GameSettings.ScreenHeight / 2 - 200,
            780,
            400
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



    // ML-1 CHANGE:
    // Stores the latest gameplay state.
    // This snapshot is used when the player gives Game Over feedback.
    private void CaptureGameplaySnapshot()
    {
        _lastActiveObstacles = _obstacles.Count;
        _lastCurrentMaxObstacles = _obstacleSpawner.CurrentMaxObstacles;
        _lastObstaclePressure = _obstacleSpawner.ProgressPercent;

        _lastActiveEnemies = _enemies.Count;
        _lastCurrentMaxEnemies = _enemySpawner.CurrentMaxEnemies;
        _lastEnemyPressure = _enemySpawner.ProgressPercent;

        _lastActiveEnemyBullets = _enemyBullets.Count;
        _lastActivePlayerShots = _shots.Count;
        _lastShotCharges = _shotCharges;
        _lastActiveShields = _shields.Count;
    }

    // ML-1 CHANGE:
    // Game Over feedback for supervised learning.
    // 1 = Too Easy
    // 2 = Normal / Balanced
    // 3 = Too Hard
    private void HandleGameOverFeedback()
    {
        if (_hasSavedMlFeedback)
        {
            return;
        }

        if (_wasBotUsedThisRun)
        {
            return;
        }

        if (_inputManager.IsKeyPressed(Keys.D1) ||
            _inputManager.IsKeyPressed(Keys.NumPad1))
        {
            LogMlSample(GameBalanceLabel.TooEasy, "ManualHumanFeedback");
            _hasSavedMlFeedback = true;
        }

        if (_inputManager.IsKeyPressed(Keys.D2) ||
            _inputManager.IsKeyPressed(Keys.NumPad2))
        {
            LogMlSample(GameBalanceLabel.Balanced, "ManualHumanFeedback");
            _hasSavedMlFeedback = true;
        }

        if (_inputManager.IsKeyPressed(Keys.D3) ||
            _inputManager.IsKeyPressed(Keys.NumPad3))
        {
            LogMlSample(GameBalanceLabel.TooHard, "ManualHumanFeedback");
            _hasSavedMlFeedback = true;
        }
    }

    // ML-2 POLISH:
    // Bot runs can be automatically labeled at Game Over.
    // This helps generate CSV training data faster.
    private void HandleBotAutoFeedback()
    {
        if (_hasSavedMlFeedback)
        {
            return;
        }

        if (!_wasBotUsedThisRun)
        {
            return;
        }

        BotAutoFeedbackResult result = GetBotAutoFeedbackResult();

        LogMlSample(result.Label, result.Reason);

        _hasSavedMlFeedback = true;
    }

    // ML-4 POLISH:
    // Bot auto-feedback separates time rating and score rating.
    // Final label is decided from both ratings.
    //
    // Rules:
    // 1. Too short survival = TooHard.
    // 2. Balanced survival but very low score = TooHard.
    // 3. Very long survival = TooEasy.
    // 4. High score only means TooEasy if survival time is also long enough.
    // 5. Otherwise = Balanced.
    private BotAutoFeedbackResult GetBotAutoFeedbackResult()
    {
        GameBalanceLabel timeRating = GetTimeRating();
        GameBalanceLabel scoreRating = GetScoreRating();

        if (timeRating == GameBalanceLabel.TooHard)
        {
            return new BotAutoFeedbackResult
            {
                Label = GameBalanceLabel.TooHard,
                TimeRating = timeRating,
                ScoreRating = scoreRating,
                Reason = "ShortSurvival"
            };
        }

        if (scoreRating == GameBalanceLabel.TooHard &&
            timeRating != GameBalanceLabel.TooEasy)
        {
            return new BotAutoFeedbackResult
            {
                Label = GameBalanceLabel.TooHard,
                TimeRating = timeRating,
                ScoreRating = scoreRating,
                Reason = "LowScoreAfterOkaySurvival"
            };
        }

        if (timeRating == GameBalanceLabel.TooEasy)
        {
            return new BotAutoFeedbackResult
            {
                Label = GameBalanceLabel.TooEasy,
                TimeRating = timeRating,
                ScoreRating = scoreRating,
                Reason = "LongSurvival"
            };
        }

        float minimumSecondsForHighScore =
            GetMinimumSecondsForScoreBasedTooEasy();

        if (scoreRating == GameBalanceLabel.TooEasy &&
            _survivalSeconds >= minimumSecondsForHighScore)
        {
            return new BotAutoFeedbackResult
            {
                Label = GameBalanceLabel.TooEasy,
                TimeRating = timeRating,
                ScoreRating = scoreRating,
                Reason = "HighScoreAfterEnoughSurvival"
            };
        }

        return new BotAutoFeedbackResult
        {
            Label = GameBalanceLabel.Balanced,
            TimeRating = timeRating,
            ScoreRating = scoreRating,
            Reason = "BalancedTimeAndScore"
        };
    }

    // ML-3 CHANGE:
    // Small result object for bot auto-labeling.
    // It stores both the label and the reason.
    // ML-4 CHANGE:
    // Bot auto-feedback now keeps time rating and score rating separately.
    // Final Label is still used as the ML training target.
    private sealed class BotAutoFeedbackResult
    {
        public GameBalanceLabel Label { get; init; }
        public GameBalanceLabel TimeRating { get; init; }
        public GameBalanceLabel ScoreRating { get; init; }

        public string Reason { get; init; } = "MiddleRange";
    }

    // ML-1 CHANGE:
    // Saves one run-level feedback row into CSV after Game Over.
    //
    // ML-4 CHANGE:
    // Saves TimeRating and ScoreRating separately.
    private void LogMlSample(GameBalanceLabel label, string autoLabelReason)
    {
        GameBalanceLabel timeRating = GetTimeRating();
        GameBalanceLabel scoreRating = GetScoreRating();

        var sample = new GameplaySample
        {
            SurvivalSeconds = _survivalSeconds,
            Score = _scoreManager.Score,
            Lives = _gameState.Lives,

            ActiveObstacles = _lastActiveObstacles,
            CurrentMaxObstacles = _lastCurrentMaxObstacles,
            ObstaclePressure = _lastObstaclePressure,

            ActiveEnemies = _lastActiveEnemies,
            CurrentMaxEnemies = _lastCurrentMaxEnemies,
            EnemyPressure = _lastEnemyPressure,

            ActiveEnemyBullets = _lastActiveEnemyBullets,
            ActivePlayerShots = _lastActivePlayerShots,
            ShotCharges = _lastShotCharges,
            ActiveShields = _lastActiveShields,

            Difficulty = _difficultySettings.Name,
            ControlMode = _wasBotUsedThisRun ? "Bot" : "Human",

            TimeRating = timeRating.ToString(),
            ScoreRating = scoreRating.ToString(),

            AutoLabelReason = autoLabelReason,
            Label = label.ToString()
        };

        bool saved = _gameplayDataLogger.Log(sample);

        Window.Title = saved
            ? $"ML saved: {label} | Time {timeRating} | Score {scoreRating}"
            : "ML feedback failed - close CSV file";
    }

    // ML-4 CHANGE:
    // Time rating only looks at survival time.
    // It does not care about score.
    // ML-4 POLISH:
    // Time rating only looks at survival time.
    // It does not care about score.
    private GameBalanceLabel GetTimeRating()
    {
        float tooHardSeconds;
        float tooEasySeconds;

        switch (_selectedDifficulty)
        {
            case DifficultyLevel.Easy:
                tooHardSeconds = 115f;
                tooEasySeconds = 180f;
                break;

            case DifficultyLevel.Hard:
                tooHardSeconds = 33f;
                tooEasySeconds = 50f;
                break;

            default:
                tooHardSeconds = 55f;
                tooEasySeconds = 85f;
                break;
        }

        if (_survivalSeconds <= tooHardSeconds)
        {
            return GameBalanceLabel.TooHard;
        }

        if (_survivalSeconds >= tooEasySeconds)
        {
            return GameBalanceLabel.TooEasy;
        }

        return GameBalanceLabel.Balanced;
    }

    // ML-4 CHANGE:
    // Score rating only looks at score.
    // It does not care about survival time.
    // ML-4 POLISH:
    // Score rating only looks at score.
    // It does not care about survival time.
    private GameBalanceLabel GetScoreRating()
    {
        int tooHardScore;
        int tooEasyScore;

        switch (_selectedDifficulty)
        {
            case DifficultyLevel.Easy:
                tooHardScore = 1450;
                tooEasyScore = 2600;
                break;

            case DifficultyLevel.Hard:
                tooHardScore = 620;
                tooEasyScore = 1200;
                break;

            default:
                tooHardScore = 850;
                tooEasyScore = 1550;
                break;
        }

        if (_scoreManager.Score <= tooHardScore)
        {
            return GameBalanceLabel.TooHard;
        }

        if (_scoreManager.Score >= tooEasyScore)
        {
            return GameBalanceLabel.TooEasy;
        }

        return GameBalanceLabel.Balanced;
    }

}