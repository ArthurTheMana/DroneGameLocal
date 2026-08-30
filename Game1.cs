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

    private float _lastHasShield;
    private float _lastShieldTimeLeft;

    private float _lastDashCooldown;
    private float _lastDashReady;
    private float _lastDashInvulnerable;

    private float _lastBossActive;
    private float _lastBossHealth;

    private float _lastBuffsOnScreen;

    private float _lastDashUsesThisRun;
    private float _lastShieldPickupsThisRun;
    private float _lastShieldActiveSecondsThisRun;

    // LEVEL 4C CHANGE:
    // Auto charge system.
    // The game slowly builds shot charges up to 3.
    // Press J to spend 1 charge and shoot.
    private int _shotCharges = GameSettings.MaxShotCharges;
    private float _shotRechargeTimer;

    private readonly System.Random _visualRandom = new();

    private readonly List<BuffPickup> _buffs = new();

    private bool _hasShield;
    private float _shieldTimer;

    private int _shieldDropFailCount;

    private const float ShieldDurationSeconds = 20f;
    private const float ShieldExplosionRadius = 130f;

    private const int BaseShieldDropChancePercent = 12;
    private const int ShieldDropChanceIncreasePerFail = 8;
    private const int MaxShieldDropChancePercent = 80;

    private float _shieldInvulnerableTimer;

    private const float ShieldInvulnerableSeconds = 0.45f;

    private Vector2 _shieldExplosionCenter;
    private float _shieldExplosionTimer;

    private const float ShieldExplosionAuraSeconds = 0.45f;

    private Vector2 _lastDashDirection = Vector2.UnitX;
    private Vector2 _dashVelocity = Vector2.Zero;

    private int _dashUsesThisRun;
    private int _shieldPickupsThisRun;
    private float _shieldActiveSecondsThisRun;

    private float _dashActiveTimer;
    private float _dashCooldownTimer;
    private float _dashInvulnerableTimer;

    private const float DashDistance = 135f;
    private const float DashDurationSeconds = 0.12f;
    private const float DashCooldownSeconds = 2.00f;
    private const float DashInvulnerableSeconds = 0.18f;

    private Boss? _boss;
    private bool _bossSpawnedThisRun;
    private float _bossShotTimer;

    // ML-6 CHANGE:
    // The trained ML model is loaded into the game.
    // For now, it only displays prediction in HUD.
    // It does not control difficulty yet.
    private readonly GameBalancePredictor _balancePredictor = new();

    private string _mlPredictionText = "READY";

    // ML-7 CHANGE:
    // Confidence value from the ML model.
    // 0.0 = not confident, 1.0 = very confident.
    private float _mlPredictionConfidence;

    private float _mlPredictionTimer;

    private const float MlPredictionRefreshSeconds = 2.0f;

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

        if (_inputManager.IsKeyPressed(Keys.Enter))
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

        if (_inputManager.IsKeyPressed(Keys.Enter))
        {
            StartNewGame();
        }

        base.Update(gameTime);
    }

    private void UpdatePlayingState(GameTime gameTime)
    {
        float deltaTime = (float)gameTime.ElapsedGameTime.TotalSeconds;

        _survivalSeconds += deltaTime;

        UpdateDashTimers(deltaTime);
        UpdateShieldTimers(deltaTime);

        if (_collisionCooldown > 0f)
        {
            _collisionCooldown -= deltaTime;
            base.Update(gameTime);
            return;
        }

        HandleChargeShot(deltaTime);
        HandlePlayerOrBotControl(deltaTime);

        UpdateSpawners(deltaTime);

        UpdateObstacles(deltaTime);
        UpdateEnemies(deltaTime);
        HandleEnemyShooting();
        UpdateBoss(deltaTime);

        UpdateShields(deltaTime);
        UpdateShots(deltaTime);
        UpdateEnemyBullets(deltaTime);
        UpdateBuffs(deltaTime);

        UpdateMlPrediction(deltaTime);

        CheckShotHits();
        CaptureGameplaySnapshot();
        CheckCollision();

        UpdateWindowTitle();
    }

    private void UpdateDashTimers(float deltaTime)
    {
        if (_dashCooldownTimer > 0f)
        {
            _dashCooldownTimer -= deltaTime;

            if (_dashCooldownTimer < 0f)
            {
                _dashCooldownTimer = 0f;
            }
        }

        if (_dashInvulnerableTimer > 0f)
        {
            _dashInvulnerableTimer -= deltaTime;

            if (_dashInvulnerableTimer < 0f)
            {
                _dashInvulnerableTimer = 0f;
            }
        }
    }

    private void UpdateShieldTimers(float deltaTime)
    {
        UpdateActiveShield(deltaTime);

        if (_shieldInvulnerableTimer > 0f)
        {
            _shieldInvulnerableTimer -= deltaTime;

            if (_shieldInvulnerableTimer < 0f)
            {
                _shieldInvulnerableTimer = 0f;
            }
        }

        if (_shieldExplosionTimer > 0f)
        {
            _shieldExplosionTimer -= deltaTime;

            if (_shieldExplosionTimer < 0f)
            {
                _shieldExplosionTimer = 0f;
            }
        }
    }

    private void UpdateSpawners(float deltaTime)
    {
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
    }

    private void UpdateBoss(float deltaTime)
    {
        if (!_bossSpawnedThisRun && _scoreManager.Score >= 1000)
        {
            _bossSpawnedThisRun = true;

            _boss = new Boss
            {
                Position = new Vector2(
                    GameSettings.ScreenWidth - 180,
                    GameSettings.PlayAreaTop + 40
                )
            };
        }

        if (_boss == null)
        {
            return;
        }

        _boss.Update(deltaTime);

        _bossShotTimer += deltaTime;

        if (_bossShotTimer < 1.8f)
        {
            return;
        }

        _bossShotTimer = 0f;

        _enemyBullets.Add(new EnemyBullet(
            new Vector2(
                _boss.Position.X,
                _boss.Position.Y + _boss.Height / 2f
            ),
            420f
        ));
    }

    private void ResetRunFeatures()
    {
        _buffs.Clear();

        _hasShield = false;
        _shieldTimer = 0f;
        _shieldInvulnerableTimer = 0f;
        _shieldExplosionCenter = Vector2.Zero;
        _shieldExplosionTimer = 0f;
        _shieldDropFailCount = 0;

        _boss = null;
        _bossSpawnedThisRun = false;
        _bossShotTimer = 0f;

        _lastDashDirection = Vector2.UnitX;
        _dashVelocity = Vector2.Zero;
        _dashActiveTimer = 0f;
        _dashCooldownTimer = 0f;
        _dashInvulnerableTimer = 0f;
    }

    private void UpdateWindowTitle()
    {
        Window.Title =
            $"Score: {_scoreManager.Score} | " +
            $"Lives: {_gameState.Lives} | " +
            $"Best: {_scoreManager.HighScore} | " +
            $"Mode: {_difficultySettings.Name}";
    }

    // BUFF CHANGE:
    // Spawns and updates Energy Core pickups during gameplay.
    private void UpdateBuffs(float deltaTime)
    {
        for (int i = _buffs.Count - 1; i >= 0; i--)
        {
            BuffPickup buff = _buffs[i];

            buff.Update(deltaTime);

            Vector2 droneCenter = new Vector2(
                _drone.Position.X + _drone.Width / 2f,
                _drone.Position.Y + _drone.Height / 2f
            );

            if (IsRectNearPoint(buff.GetBounds(), droneCenter, 70f))
            {
                ActivateShield();
                _shotCharges = GameSettings.MaxShotCharges;

                _buffs.RemoveAt(i);
                continue;
            }

            if (buff.IsOffScreen())
            {
                _buffs.RemoveAt(i);
            }
        }
    }

    private void ActivateShield()
    {
        _hasShield = true;

        // BUFF CHANGE:
        // Shield does not stack.
        // Picking up another shield only refreshes the timer.
        _shieldTimer = ShieldDurationSeconds;

        _shieldPickupsThisRun++;
    }

    private void UpdateActiveShield(float deltaTime)
    {
        if (!_hasShield)
        {
            return;
        }

        float activeTimeThisFrame = Math.Min(deltaTime, _shieldTimer);
        _shieldActiveSecondsThisRun += activeTimeThisFrame;

        _shieldTimer -= deltaTime;

        if (_shieldTimer <= 0f)
        {
            _hasShield = false;
            _shieldTimer = 0f;
        }
    }

    private void DestroyEnemiesNear(Vector2 center, float radius)
    {
        for (int i = _enemies.Count - 1; i >= 0; i--)
        {
            Enemy enemy = _enemies[i];

            if (!IsRectNearPoint(enemy.GetBounds(), center, radius))
            {
                continue;
            }

            Vector2 hitPosition = new Vector2(
                enemy.Position.X + enemy.Width / 2f,
                enemy.Position.Y + enemy.Height / 2f
            );

            _particles.EmitCrash(hitPosition);

            _scoreManager.AddScore(enemy.ScoreReward);

            _enemies.RemoveAt(i);
        }
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
        _botPlayer.Reset();

        _survivalSeconds = 0f;
        _botAutoReplayTimer = -1f;
        _hasSavedMlFeedback = false;

        _wasBotUsedThisRun = _isBotEnabled;

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

        ResetRunFeatures();

        _obstacleSpawner.Reset(_difficultySettings);
        _enemySpawner.Reset(_difficultySettings);

        _collisionCooldown = 0f;
        _screenShakeTimer = 0f;

        _shotCharges = GameSettings.MaxShotCharges;
        _shotRechargeTimer = 0f;

        _dashUsesThisRun = 0;
        _shieldPickupsThisRun = 0;
        _shieldActiveSecondsThisRun = 0f;

        _hasShield = false;
        _shieldTimer = 0f;
        _shieldInvulnerableTimer = 0f;
        _shieldExplosionTimer = 0f;
        _shieldDropFailCount = 0;

        _dashUsesThisRun = 0;
        _shieldPickupsThisRun = 0;
        _shieldActiveSecondsThisRun = 0f;
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

        if (direction != Vector2.Zero)
        {
            _lastDashDirection = direction;
            _lastDashDirection.Normalize();
        }

        if (_inputManager.IsKeyPressed(Keys.Space))
        {
            TryStartDash();
        }

        _drone.Move(direction, deltaTime);

        ApplyDash(deltaTime);

        _drone.ClampToScreen(
            GameSettings.ScreenWidth,
            GameSettings.ScreenHeight
        );
    }

    private void TryStartDash()
    {
        if (_dashCooldownTimer > 0f)
        {
            return;
        }

        if (_dashActiveTimer > 0f)
        {
            return;
        }

        Vector2 dashDirection = _lastDashDirection;

        if (dashDirection == Vector2.Zero)
        {
            dashDirection = Vector2.UnitX;
        }

        dashDirection.Normalize();

        _dashVelocity = dashDirection * (DashDistance / DashDurationSeconds);

        _dashActiveTimer = DashDurationSeconds;
        _dashCooldownTimer = DashCooldownSeconds;

        // DASH CHANGE:
        // The drone is briefly invincible during dash.
        _dashInvulnerableTimer = DashInvulnerableSeconds;

        _dashUsesThisRun++;
    }

    private void ApplyDash(float deltaTime)
    {
        if (_dashActiveTimer <= 0f)
        {
            return;
        }

        _drone.MoveBy(_dashVelocity * deltaTime);

        _dashActiveTimer -= deltaTime;

        if (_dashActiveTimer <= 0f)
        {
            _dashActiveTimer = 0f;
            _dashVelocity = Vector2.Zero;
        }
    }

    private bool IsDashInvulnerable()
    {
        return _dashInvulnerableTimer > 0f;
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

                    TryDropShieldBuffProgressive(hitPosition);

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

            // BOSS CHANGE:
            // Player shots can damage the boss.
            // Put this after normal enemy checks,
            // so smaller enemies/shields can still block shots first.
            if (_boss != null && shotBox.Intersects(_boss.GetBounds()))
            {
                Vector2 hitPosition = new Vector2(
                    _boss.Position.X + _boss.Width / 2f,
                    _boss.Position.Y + _boss.Height / 2f
                );

                _boss.Health--;

                _particles.EmitCrash(hitPosition);

                _shots.RemoveAt(shotIndex);

                shotRemoved = true;

                if (_boss.Health <= 0)
                {
                    _scoreManager.AddScore(500);
                    _boss = null;
                }
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
                TryDropShieldBuffProgressive(hitPosition);

                _obstacles.RemoveAt(obstacleIndex);
                _shots.RemoveAt(shotIndex);

                _scoreManager.AddScore(GameSettings.DestroyObstacleScore);

                break;
            }
        }
    }

    private void CheckCollision()
    {

        if (IsDashInvulnerable())
        {
            return;
        }

        if (_shieldInvulnerableTimer > 0f)
        {
            return;
        }

        bool crashed = CollisionChecker.HasCollision(_drone, _obstacles);

        Rectangle droneBox = _drone.GetBounds();

        if (!crashed)
        {
            foreach (Enemy enemy in _enemies)
            {
                if (droneBox.Intersects(enemy.GetBounds()))
                {
                    crashed = true;
                    break;
                }
            }
        }

        if (!crashed && _boss != null && droneBox.Intersects(_boss.GetBounds()))
        {
            crashed = true;
        }

        if (!crashed)
        {
            foreach (EnemyBullet bullet in _enemyBullets)
            {
                if (droneBox.Intersects(bullet.GetSweptBounds()))
                {
                    crashed = true;
                    break;
                }
            }
        }

        if (!crashed)
        {
            foreach (EnergyShield shield in _shields)
            {
                if (droneBox.Intersects(shield.GetBounds()))
                {
                    crashed = true;
                    break;
                }
            }
        }

        if (!crashed)
        {
            return;
        }

        if (_hasShield)
        {
            TriggerShieldExplosion();
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
    }

    private void TriggerShieldExplosion()
    {
        _hasShield = false;
        _shieldTimer = 0f;
        _shieldInvulnerableTimer = ShieldInvulnerableSeconds;

        Vector2 explosionCenter = new Vector2(
            _drone.Position.X + _drone.Width / 2f,
            _drone.Position.Y + _drone.Height / 2f
        );

        _shieldExplosionCenter = explosionCenter;
        _shieldExplosionTimer = ShieldExplosionAuraSeconds;

        _particles.EmitCrash(explosionCenter);
        _screenShakeTimer = 0.12f;

        DestroyObstaclesNear(explosionCenter, ShieldExplosionRadius);
        DestroyEnemiesNear(explosionCenter, ShieldExplosionRadius);
        DestroyEnemyBulletsNear(explosionCenter, ShieldExplosionRadius);
        DestroyEnemyShieldsNear(explosionCenter, ShieldExplosionRadius);

        Window.Title = "Shield exploded - game continues";
    }

    private void DestroyObstaclesNear(Vector2 center, float radius)
    {
        for (int i = _obstacles.Count - 1; i >= 0; i--)
        {
            Obstacle obstacle = _obstacles[i];

            if (!IsRectNearPoint(obstacle.GetBounds(), center, radius))
            {
                continue;
            }

            Vector2 hitPosition = new Vector2(
                obstacle.Position.X + obstacle.Width / 2f,
                obstacle.Position.Y + obstacle.Height / 2f
            );

            _particles.EmitCrash(hitPosition);
            _scoreManager.AddScore(GameSettings.DestroyObstacleScore);

            _obstacles.RemoveAt(i);
        }
    }

    private void DestroyEnemyBulletsNear(Vector2 center, float radius)
    {
        for (int i = _enemyBullets.Count - 1; i >= 0; i--)
        {
            EnemyBullet bullet = _enemyBullets[i];

            if (!IsRectNearPoint(bullet.GetBounds(), center, radius))
            {
                continue;
            }

            _particles.EmitCrash(new Vector2(
                bullet.Position.X + bullet.Width / 2f,
                bullet.Position.Y + bullet.Height / 2f
            ));

            _enemyBullets.RemoveAt(i);
        }
    }

    private void DestroyEnemyShieldsNear(Vector2 center, float radius)
    {
        for (int i = _shields.Count - 1; i >= 0; i--)
        {
            EnergyShield shield = _shields[i];

            if (!IsRectNearPoint(shield.GetBounds(), center, radius))
            {
                continue;
            }

            _particles.EmitCrash(new Vector2(
                shield.Position.X + shield.Width / 2f,
                shield.Position.Y + shield.Height / 2f
            ));

            _shields.RemoveAt(i);
        }
    }

    private static bool IsRectNearPoint(Rectangle rect, Vector2 point, float radius)
    {
        Vector2 rectCenter = new Vector2(
            rect.X + rect.Width / 2f,
            rect.Y + rect.Height / 2f
        );

        float distanceSquared = Vector2.DistanceSquared(rectCenter, point);

        return distanceSquared <= radius * radius;
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

        if (_gameState.Current == GameStateType.Playing)
        {
            DrawBuffs();
        }

        DrawShieldExplosionAura();

        DrawObstacles();
        DrawEnemies();
        DrawEnemyBullets();

        if (_boss != null)
        {
            _boss.Draw(_spriteBatch!, _pixel!);
        }

        DrawActiveShieldAura();
        DrawDrone();

        DrawShots();
        DrawShields();

        _particles.Draw(_spriteBatch, _pixel);

        if (_gameState.Current != GameStateType.Start)
        {
            DrawHud();
            DrawShieldStatusCorner();
        }

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
            DrawStartMenuOverlay();
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

    private void DrawShieldExplosionAura()
    {
        if (_shieldExplosionTimer <= 0f)
        {
            return;
        }

        float progress = 1f - (_shieldExplosionTimer / ShieldExplosionAuraSeconds);

        int alpha = (int)MathHelper.Lerp(
            180f,
            0f,
            progress
        );

        // VISUAL FIX:
        // This fixed circle shows the real gameplay explosion radius.
        // It matches ShieldExplosionRadius used by DestroyEnemiesNear / DestroyEnemyBulletsNear.
        DrawFilledCircle(
            _shieldExplosionCenter,
            (int)ShieldExplosionRadius,
            new Color(80, 180, 255, alpha / 5)
        );

        DrawCircleOutline(
            _shieldExplosionCenter,
            (int)ShieldExplosionRadius,
            new Color(180, 245, 255, alpha),
            3
        );

        // Extra style ring.
        // This expands inside the real radius, but the main circle already shows the true hit area.
        int waveRadius = (int)MathHelper.Lerp(
            30f,
            ShieldExplosionRadius,
            progress
        );

        DrawCircleOutline(
            _shieldExplosionCenter,
            waveRadius,
            new Color(255, 255, 255, alpha),
            2
        );
    }

    private void DrawActiveShieldAura()
    {
        if (!_hasShield)
        {
            return;
        }

        Rectangle droneBox = _drone.GetBounds();

        Vector2 center = new Vector2(
            droneBox.X + droneBox.Width / 2f,
            droneBox.Y + droneBox.Height / 2f
        );

        float pulse = 1f + 0.08f * MathF.Sin(_shieldTimer * 8f);
        int radius = (int)(42 * pulse);

        DrawFilledCircle(
            center,
            radius,
            new Color(80, 180, 255, 35)
        );

        DrawCircleOutline(
            center,
            radius,
            new Color(150, 230, 255, 210),
            2
        );

        DrawCircleOutline(
            center,
            radius + 5,
            new Color(80, 180, 255, 100),
            1
        );
    }

    // UI POLISH:
    // Dynamic start menu layout.
    // Text positions are calculated from screen size,
    // so the menu still looks correct when ScreenWidth/ScreenHeight changes.
    private void DrawStartMenuOverlay()
    {
        int menuWidth = 760;
        int menuHeight = 340;

        Rectangle menuBox = new Rectangle(
            GameSettings.ScreenWidth / 2 - menuWidth / 2,
            GameSettings.ScreenHeight / 2 - menuHeight / 2,
            menuWidth,
            menuHeight
        );

        DrawRect(
            menuBox,
            new Color(30, 70, 120, 220)
        );

        int currentY = menuBox.Y + 34;

        PixelText.DrawCenteredText(
            _spriteBatch!,
            _pixel!,
            "DRONE GAME",
            GameSettings.ScreenWidth,
            currentY,
            4,
            Color.White
        );

        currentY += 58;

        string modeText = _isBotEnabled
            ? "BOT MODE RANDOM"
            : $"MODE {_difficultySettings.Name.ToUpper()}";

        PixelText.DrawCenteredText(
            _spriteBatch!,
            _pixel!,
            modeText,
            GameSettings.ScreenWidth,
            currentY,
            3,
            new Color(255, 214, 10)
        );

        currentY += 46;

        string changeText = _isBotEnabled
            ? "BOT PICKS EASY NORMAL HARD"
            : "A D OR LEFT RIGHT TO CHANGE";

        PixelText.DrawCenteredText(
            _spriteBatch!,
            _pixel!,
            changeText,
            GameSettings.ScreenWidth,
            currentY,
            2,
            new Color(0, 217, 255)
        );

        currentY += 34;

        string difficultyText = _isBotEnabled
            ? "RANDOM DIFFICULTY EACH RUN"
            : "1 EASY   2 NORMAL   3 HARD";

        PixelText.DrawCenteredText(
            _spriteBatch!,
            _pixel!,
            difficultyText,
            GameSettings.ScreenWidth,
            currentY,
            2,
            Color.White
        );

        currentY += 52;

        PixelText.DrawCenteredText(
            _spriteBatch!,
            _pixel!,
            "ENTER TO START",
            GameSettings.ScreenWidth,
            currentY,
            3,
            new Color(0, 217, 255)
        );

        currentY += 48;

        PixelText.DrawCenteredText(
            _spriteBatch!,
            _pixel!,
            "J SHOOT   SPACE DASH   B BOT",
            GameSettings.ScreenWidth,
            currentY,
            2,
            new Color(255, 214, 10)
        );
    }

    private void DrawBackground()
    {
        _starfield.Draw(_spriteBatch!, _pixel!);
    }

    private void DrawHud()
    {
        // Solid HUD bar so stars do not show through.
        DrawRect(
            new Rectangle(0, 0, GameSettings.ScreenWidth, GameSettings.HudHeight),
            new Color(6, 10, 20, 255)
        );

        // A clean divider line between HUD and play area.
        DrawRect(
            new Rectangle(0, GameSettings.HudHeight - 2, GameSettings.ScreenWidth, 2),
            new Color(50, 80, 120, 255)
        );

        int panelWidth = 360;
        int panelHeight = 110;
        int gap = 30;

        int totalWidth = panelWidth * 3 + gap * 2;
        int startX = (GameSettings.ScreenWidth - totalWidth) / 2;
        int panelY = 18;

        if (_boss != null)
        {
            PixelText.DrawText(
                _spriteBatch!,
                _pixel!,
                $"BOSS HP {_boss.Health}",
                new Vector2(GameSettings.ScreenWidth / 2 - 80, GameSettings.HudHeight + 10),
                1,
                new Color(255, 80, 180)
            );
        }

        Rectangle leftPanel = new Rectangle(startX, panelY, panelWidth, panelHeight);
        Rectangle centerPanel = new Rectangle(startX + panelWidth + gap, panelY, panelWidth, panelHeight);
        Rectangle rightPanel = new Rectangle(startX + (panelWidth + gap) * 2, panelY, panelWidth, panelHeight);

        DrawPanel(leftPanel);
        DrawPanel(centerPanel);
        DrawPanel(rightPanel);

        DrawLeftHudPanel(leftPanel);
        DrawCenterHudPanel(centerPanel);
        DrawRightHudPanel(rightPanel);
    }

    private void DrawPanel(Rectangle rect)
    {
        DrawRect(rect, new Color(0, 0, 0, 180));

        DrawRect(
            new Rectangle(rect.X, rect.Y, rect.Width, 2),
            new Color(70, 110, 170)
        );

        DrawRect(
            new Rectangle(rect.X, rect.Bottom - 2, rect.Width, 2),
            new Color(70, 110, 170)
        );

        DrawRect(
            new Rectangle(rect.X, rect.Y, 2, rect.Height),
            new Color(70, 110, 170)
        );

        DrawRect(
            new Rectangle(rect.Right - 2, rect.Y, 2, rect.Height),
            new Color(70, 110, 170)
        );
    }

    private void DrawLeftHudPanel(Rectangle panel)
    {
        int x = panel.X + 16;
        int y = panel.Y + 12;

        PixelText.DrawText(
            _spriteBatch!,
            _pixel!,
            $"SCORE {_scoreManager.Score}",
            new Vector2(x, y),
            2,
            Color.White
        );

        PixelText.DrawText(
            _spriteBatch!,
            _pixel!,
            $"MODE {_difficultySettings.Name}",
            new Vector2(x, y + 28),
            1,
            Color.White
        );

        PixelText.DrawText(
            _spriteBatch!,
            _pixel!,
            $"OBSTACLES {_obstacleSpawner.CurrentMaxObstacles}/{_difficultySettings.MaxObstacles}",
            new Vector2(x, y + 52),
            1,
            new Color(255, 214, 10)
        );

        PixelText.DrawText(
            _spriteBatch!,
            _pixel!,
            $"ENEMIES {_enemySpawner.CurrentMaxEnemies}/{_difficultySettings.MaxEnemies}",
            new Vector2(x, y + 76),
            1,
            new Color(255, 140, 40)
        );
    }

    private void DrawCenterHudPanel(Rectangle panel)
    {
        int titleY = panel.Y + 10;
        int x = panel.X + 20;

        PixelText.DrawCenteredText(
            _spriteBatch!,
            _pixel!,
            $"LIVES {_gameState.Lives}",
            GameSettings.ScreenWidth,
            titleY,
            2,
            new Color(0, 217, 255)
        );

        PixelText.DrawText(
            _spriteBatch!,
            _pixel!,
            "OBSTACLE PRESSURE",
            new Vector2(x, panel.Y + 40),
            1,
            new Color(255, 214, 10)
        );

        DrawProgressBar(
            x,
            panel.Y + 58,
            panel.Width - 40,
            12,
            _obstacleSpawner.ProgressPercent
        );

        PixelText.DrawText(
            _spriteBatch!,
            _pixel!,
            "ENEMY PRESSURE",
            new Vector2(x, panel.Y + 76),
            1,
            new Color(255, 140, 40)
        );

        DrawProgressBar(
            x,
            panel.Y + 94,
            panel.Width - 40,
            12,
            _enemySpawner.ProgressPercent
        );
    }

    private void DrawRightHudPanel(Rectangle panel)
    {
        int x = panel.X + 16;
        int y = panel.Y + 12;

        PixelText.DrawText(
            _spriteBatch!,
            _pixel!,
            $"BEST {_scoreManager.HighScore}",
            new Vector2(x, y),
            1,
            new Color(255, 214, 10)
        );

        PixelText.DrawText(
            _spriteBatch!,
            _pixel!,
            _isBotEnabled ? "BOT ON" : "BOT OFF",
            new Vector2(x, y + 24),
            1,
            _isBotEnabled ? new Color(0, 217, 255) : Color.White
        );

        PixelText.DrawText(
            _spriteBatch!,
            _pixel!,
            $"SHOT {_shotCharges}/{GameSettings.MaxShotCharges}",
            new Vector2(x, y + 48),
            1,
            new Color(0, 217, 255)
        );

        float rechargeProgress = _shotCharges >= GameSettings.MaxShotCharges
            ? 1f
            : MathHelper.Clamp(_shotRechargeTimer / GameSettings.ShotRechargeSeconds, 0f, 1f);

        DrawProgressBar(
            x,
            y + 66,
            panel.Width - 32,
            12,
            rechargeProgress
        );

        string shotText = _shotCharges > 0 ? "PRESS J TO SHOOT" : "RECHARGING";

        PixelText.DrawText(
            _spriteBatch!,
            _pixel!,
            shotText,
            new Vector2(x, y + 84),
            1,
            _shotCharges > 0 ? new Color(255, 214, 10) : new Color(255, 80, 100)
        );

        string dashText = _dashCooldownTimer <= 0f
            ? "DASH READY"
            : $"DASH {(int)Math.Ceiling(_dashCooldownTimer)}s";

        PixelText.DrawText(
            _spriteBatch!,
            _pixel!,
            dashText,
            new Vector2(x + 170, y + 84),
            1,
            _dashCooldownTimer <= 0f
                ? new Color(0, 217, 255)
                : new Color(255, 214, 10)
        );

        string mlHudText = string.IsNullOrWhiteSpace(FormatMlConfidenceText())
            ? $"ML {FormatMlPredictionText()}"
            : $"ML {FormatMlPredictionText()} {FormatMlConfidenceText()}";

        PixelText.DrawText(
            _spriteBatch!,
            _pixel!,
            mlHudText,
            new Vector2(x, y + 104),
            1,
            GetMlPredictionColor()
        );
    }

    private void DrawShieldStatusCorner()
    {
        if (!_hasShield)
        {
            return;
        }

        int boxWidth = 190;
        int boxHeight = 46;

        int x = GameSettings.ScreenWidth - boxWidth - 24;
        int y = GameSettings.PlayAreaTop + 16;

        DrawRect(
            new Rectangle(x, y, boxWidth, boxHeight),
            new Color(0, 0, 0, 170)
        );

        DrawRect(
            new Rectangle(x, y, boxWidth, 2),
            new Color(80, 180, 255)
        );

        PixelText.DrawText(
            _spriteBatch!,
            _pixel!,
            $"SHIELD {(int)Math.Ceiling(_shieldTimer)}s",
            new Vector2(x + 12, y + 8),
            1,
            new Color(150, 230, 255)
        );

        float shieldProgress = MathHelper.Clamp(
            _shieldTimer / ShieldDurationSeconds,
            0f,
            1f
        );

        DrawProgressBar(
            x + 12,
            y + 28,
            boxWidth - 24,
            10,
            shieldProgress
        );
    }




    // ML-6 POLISH:
    // Make ML labels easier to read in the HUD.
    // Internal model labels stay as TooHard / TooEasy / Balanced.
    private string FormatMlPredictionText()
    {
        return _mlPredictionText switch
        {
            "TooHard" => "TOO HARD",
            "TooEasy" => "TOO EASY",
            "Balanced" => "BALANCED",
            "NO MODEL" => "NO MODEL",
            "UNKNOWN" => "UNKNOWN",
            "ERROR" => "ERROR",
            _ => _mlPredictionText.ToUpperInvariant()
        };
    }

    // ML-6 CHANGE:
    // Color-code ML prediction for readability.
    private Color GetMlPredictionColor()
    {
        return _mlPredictionText switch
        {
            "TooHard" => new Color(255, 80, 100),
            "TooEasy" => new Color(255, 214, 10),
            "Balanced" => new Color(0, 217, 255),
            _ => Color.White
        };
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

    private void DrawBuffs()
    {
        foreach (BuffPickup buff in _buffs)
        {
            buff.Draw(_spriteBatch!, _pixel!);
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

    private void DrawFilledCircle(Vector2 center, int radius, Color color)
    {
        for (int y = -radius; y <= radius; y++)
        {
            for (int x = -radius; x <= radius; x++)
            {
                if (x * x + y * y <= radius * radius)
                {
                    DrawRect(
                        new Rectangle(
                            (int)center.X + x,
                            (int)center.Y + y,
                            1,
                            1
                        ),
                        color
                    );
                }
            }
        }
    }

    private void DrawCircleOutline(
        Vector2 center,
        int radius,
        Color color,
        int thickness
    )
    {
        int outer = radius * radius;
        int innerRadius = radius - thickness;
        int inner = innerRadius * innerRadius;

        for (int y = -radius; y <= radius; y++)
        {
            for (int x = -radius; x <= radius; x++)
            {
                int distance = x * x + y * y;

                if (distance <= outer && distance >= inner)
                {
                    DrawRect(
                        new Rectangle(
                            (int)center.X + x,
                            (int)center.Y + y,
                            1,
                            1
                        ),
                        color
                    );
                }
            }
        }
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

        _lastHasShield = _hasShield ? 1f : 0f;
        _lastShieldTimeLeft = _hasShield ? _shieldTimer : 0f;
        _lastShieldPickupsThisRun = _shieldPickupsThisRun;
        _lastShieldActiveSecondsThisRun = _shieldActiveSecondsThisRun;

        _lastDashCooldown = _dashCooldownTimer;
        _lastDashReady = _dashCooldownTimer <= 0.05f ? 1f : 0f;
        _lastDashInvulnerable = IsDashInvulnerable() ? 1f : 0f;
        _lastDashUsesThisRun = _dashUsesThisRun;

        _lastBossActive = _boss != null ? 1f : 0f;
        _lastBossHealth = _boss?.Health ?? 0f;

        _lastBuffsOnScreen = _buffs.Count;
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

    // ML-6 CHANGE:
    // Predict current game difficulty feeling every few seconds.
    // This is only shown in HUD for now.
    //
    // ML-7 CHANGE:
    // Also stores confidence percentage.
    private void UpdateMlPrediction(float deltaTime)
    {
        if (!_balancePredictor.IsLoaded)
        {
            _mlPredictionText = "NO MODEL";
            _mlPredictionConfidence = 0f;

            Window.Title = string.IsNullOrWhiteSpace(_balancePredictor.LastError)
                ? "ML model not loaded"
                : $"ML model not loaded: {_balancePredictor.LastError}";

            return;
        }

        _mlPredictionTimer -= deltaTime;

        if (_mlPredictionTimer > 0f)
        {
            return;
        }

        _mlPredictionTimer = MlPredictionRefreshSeconds;

        var input = new GameBalanceModelInput
        {
            SurvivalSeconds = _survivalSeconds,
            Score = _scoreManager.Score,
            Lives = _gameState.Lives,

            ActiveObstacles = _obstacles.Count,
            CurrentMaxObstacles = _obstacleSpawner.CurrentMaxObstacles,
            ObstaclePressure = _obstacleSpawner.ProgressPercent,

            ActiveEnemies = _enemies.Count,
            CurrentMaxEnemies = _enemySpawner.CurrentMaxEnemies,
            EnemyPressure = _enemySpawner.ProgressPercent,

            ActiveEnemyBullets = _enemyBullets.Count,
            ActivePlayerShots = _shots.Count,
            ShotCharges = _shotCharges,
            ActiveShields = _shields.Count,

            HasShield = _hasShield ? 1f : 0f,
            ShieldTimeLeft = _hasShield ? _shieldTimer : 0f,
            ShieldPickupsThisRun = _shieldPickupsThisRun,
            ShieldActiveSecondsThisRun = _shieldActiveSecondsThisRun,

            DashCooldown = _dashCooldownTimer,
            DashReady = _dashCooldownTimer <= 0.05f ? 1f : 0f,
            DashInvulnerable = IsDashInvulnerable() ? 1f : 0f,
            DashUsesThisRun = _dashUsesThisRun,

            BossActive = _boss != null ? 1f : 0f,
            BossHealth = _boss?.Health ?? 0f,

            BuffsOnScreen = _buffs.Count,

            Difficulty = _difficultySettings.Name,
            ControlMode = _isBotEnabled ? "Bot" : "Human",

            // ML-6 FIX:
            // Dummy label needed because the trained pipeline expects a Label column.
            Label = "Balanced"
        };

        GameBalancePredictionResult result = _balancePredictor.Predict(input);

        _mlPredictionText = result.Label;
        _mlPredictionConfidence = result.Confidence;

        // ML-7 POLISH:
        // If live gameplay looks fair and manageable,
        // show Balanced even if the model leans TooHard / TooEasy.
        // This makes the HUD more useful during active play.
        if (ShouldShowLiveBalancedPrediction())
        {
            _mlPredictionText = "Balanced";

            // Keep confidence reasonable because this is a live correction.
            _mlPredictionConfidence = Math.Min(_mlPredictionConfidence, 0.78f);
        }


    }

    // ML-7 POLISH:
    // Blocks TooEasy only when the player is clearly under real danger.
    // This version is less aggressive than the previous one.

    // ML-7 CHANGE:
    // Convert model confidence from 0.0-1.0 to percentage text.
    private string FormatMlConfidenceText()
    {
        if (_mlPredictionConfidence <= 0f)
        {
            return "";
        }

        int percent = (int)Math.Round(_mlPredictionConfidence * 100f);

        return $"{percent}%";
    }

    // ML-7 POLISH:
    // Live gameplay can be different from Game Over training rows.
    // If the current game state looks playable and fair,
    // show BALANCED instead of forcing TooHard / TooEasy.
    private bool ShouldShowLiveBalancedPrediction()
    {
        bool playerStillAlive =
            _gameState.Lives > 0;

        bool pressureIsMedium =
            _obstacleSpawner.ProgressPercent >= 0.30f &&
            _obstacleSpawner.ProgressPercent <= 0.85f &&
            _enemySpawner.ProgressPercent >= 0.30f &&
            _enemySpawner.ProgressPercent <= 0.85f;

        bool hasSomeShotResource =
            _shotCharges >= 1;

        bool bulletPressureManageable =
            _enemyBullets.Count <= 3;

        bool enemyPressureManageable =
            _enemies.Count <= _enemySpawner.CurrentMaxEnemies;

        return playerStillAlive &&
               pressureIsMedium &&
               hasSomeShotResource &&
               bulletPressureManageable &&
               enemyPressureManageable;
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

            HasShield = _lastHasShield,
            ShieldTimeLeft = _lastShieldTimeLeft,
            ShieldPickupsThisRun = _lastShieldPickupsThisRun,
            ShieldActiveSecondsThisRun = _lastShieldActiveSecondsThisRun,

            DashCooldown = _lastDashCooldown,
            DashReady = _lastDashReady,
            DashInvulnerable = _lastDashInvulnerable,
            DashUsesThisRun = _lastDashUsesThisRun,

            BossActive = _lastBossActive,
            BossHealth = _lastBossHealth,

            BuffsOnScreen = _lastBuffsOnScreen,

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

    private void TryDropShieldBuffProgressive(Vector2 position)
    {
        // If a buff is already on screen, don't spawn another.
        if (_buffs.Count > 0)
        {
            return;
        }

        int chance = BaseShieldDropChancePercent +
                     (_shieldDropFailCount * ShieldDropChanceIncreasePerFail);

        chance = Math.Min(chance, MaxShieldDropChancePercent);

        if (_visualRandom.Next(100) < chance)
        {
            float clampedY = MathHelper.Clamp(
                position.Y,
                GameSettings.PlayAreaTop + 20,
                GameSettings.ScreenHeight - 50
            );

            _buffs.Add(new BuffPickup
            {
                Position = new Vector2(position.X, clampedY)
            });

            // Reset the pity counter because a buff finally spawned.
            _shieldDropFailCount = 0;
        }
        else
        {
            // No drop this time, so next kill has better chance.
            _shieldDropFailCount++;
        }
    }

}
