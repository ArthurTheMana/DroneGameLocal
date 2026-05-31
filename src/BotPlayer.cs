using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;

namespace DroneGameLocal;

// ML-2 CHANGE:
// This is a rule-based autoplayer used to collect gameplay data faster.
// This is NOT Machine Learning yet.
// It uses simple rules to dodge danger and shoot when useful.
//
// ML-2 POLISH:
// Bot movement is now smoother.
// Instead of instantly changing direction every frame,
// the bot updates decisions in small intervals and blends movement over time.
public sealed class BotPlayer
{
    private const int BulletDangerRange = 320;
    private const int ObstacleDangerRange = 260;
    private const int EnemyDangerRange = 230;
    private const int VerticalDangerMargin = 32;

    private const float PreferredX = 120f;
    private const float PreferredY = 330f;

    private const float DecisionRefreshSeconds = 0.10f;
    private const float MovementSmoothSpeed = 7.5f;
    private const float BotShootCooldownSeconds = 0.35f;

    private Vector2 _smoothedMovement = Vector2.Zero;
    private Vector2 _lastDesiredMovement = Vector2.Zero;

    private float _decisionTimer;
    private float _shootCooldownTimer;

    public void Reset()
    {
        _smoothedMovement = Vector2.Zero;
        _lastDesiredMovement = Vector2.Zero;
        _decisionTimer = 0f;
        _shootCooldownTimer = 0f;
    }

    public BotDecision GetDecision(
        float deltaTime,
        Drone drone,
        IReadOnlyList<Obstacle> obstacles,
        IReadOnlyList<Enemy> enemies,
        IReadOnlyList<EnemyBullet> enemyBullets,
        IReadOnlyList<EnergyShield> shields,
        int shotCharges,
        int activePlayerShots)
    {
        if (_shootCooldownTimer > 0f)
        {
            _shootCooldownTimer -= deltaTime;
        }

        _decisionTimer -= deltaTime;

        if (_decisionTimer <= 0f)
        {
            _decisionTimer = DecisionRefreshSeconds;

            Rectangle droneBox = drone.GetBounds();

            Vector2 movement = GetDefensiveMovement(
                droneBox,
                obstacles,
                enemies,
                enemyBullets,
                shields
            );

            if (movement == Vector2.Zero)
            {
                movement = GetCenteringMovement(drone);
            }

            if (movement.LengthSquared() > 1f)
            {
                movement.Normalize();
            }

            _lastDesiredMovement = movement;
        }

        float lerpAmount = MathHelper.Clamp(
            deltaTime * MovementSmoothSpeed,
            0f,
            1f
        );

        _smoothedMovement = Vector2.Lerp(
            _smoothedMovement,
            _lastDesiredMovement,
            lerpAmount
        );

        if (_smoothedMovement.LengthSquared() > 1f)
        {
            _smoothedMovement.Normalize();
        }

        bool shouldFire = false;

        if (_shootCooldownTimer <= 0f)
        {
            Rectangle droneBox = drone.GetBounds();

            shouldFire = ShouldFire(
                droneBox,
                enemies,
                enemyBullets,
                shields,
                obstacles,
                shotCharges,
                activePlayerShots
            );

            if (shouldFire)
            {
                _shootCooldownTimer = BotShootCooldownSeconds;
            }
        }

        return new BotDecision
        {
            MovementDirection = _smoothedMovement,
            ShouldFire = shouldFire
        };
    }

    private static Vector2 GetDefensiveMovement(
        Rectangle droneBox,
        IReadOnlyList<Obstacle> obstacles,
        IReadOnlyList<Enemy> enemies,
        IReadOnlyList<EnemyBullet> enemyBullets,
        IReadOnlyList<EnergyShield> shields)
    {
        foreach (EnemyBullet bullet in enemyBullets)
        {
            Rectangle bulletBox = bullet.GetBounds();

            if (IsThreatInFront(droneBox, bulletBox, BulletDangerRange) &&
                HasVerticalOverlap(droneBox, bulletBox, VerticalDangerMargin))
            {
                return DodgeAwayFrom(droneBox, bulletBox);
            }
        }

        foreach (EnergyShield shield in shields)
        {
            Rectangle shieldBox = shield.GetBounds();

            if (IsThreatInFront(droneBox, shieldBox, ObstacleDangerRange) &&
                HasVerticalOverlap(droneBox, shieldBox, VerticalDangerMargin))
            {
                return DodgeAwayFrom(droneBox, shieldBox);
            }
        }

        foreach (Obstacle obstacle in obstacles)
        {
            Rectangle obstacleBox = obstacle.GetBounds();

            if (IsThreatInFront(droneBox, obstacleBox, ObstacleDangerRange) &&
                HasVerticalOverlap(droneBox, obstacleBox, VerticalDangerMargin))
            {
                return DodgeAwayFrom(droneBox, obstacleBox);
            }
        }

        foreach (Enemy enemy in enemies)
        {
            Rectangle enemyBox = enemy.GetBounds();

            if (IsThreatInFront(droneBox, enemyBox, EnemyDangerRange) &&
                HasVerticalOverlap(droneBox, enemyBox, VerticalDangerMargin))
            {
                return DodgeAwayFrom(droneBox, enemyBox);
            }
        }

        return Vector2.Zero;
    }

    private static Vector2 GetCenteringMovement(Drone drone)
    {
        Vector2 direction = Vector2.Zero;

        if (drone.Position.X < PreferredX - 12f)
        {
            direction.X += 1f;
        }
        else if (drone.Position.X > PreferredX + 12f)
        {
            direction.X -= 1f;
        }

        if (drone.Position.Y < PreferredY - 18f)
        {
            direction.Y += 1f;
        }
        else if (drone.Position.Y > PreferredY + 18f)
        {
            direction.Y -= 1f;
        }

        return direction;
    }

    private static bool ShouldFire(
        Rectangle droneBox,
        IReadOnlyList<Enemy> enemies,
        IReadOnlyList<EnemyBullet> enemyBullets,
        IReadOnlyList<EnergyShield> shields,
        IReadOnlyList<Obstacle> obstacles,
        int shotCharges,
        int activePlayerShots)
    {
        if (shotCharges <= 0)
        {
            return false;
        }

        if (activePlayerShots >= 2)
        {
            return false;
        }

        foreach (EnemyBullet bullet in enemyBullets)
        {
            if (IsGoodShotTarget(droneBox, bullet.GetBounds(), 300, 28))
            {
                return true;
            }
        }

        foreach (Enemy enemy in enemies)
        {
            if (IsGoodShotTarget(droneBox, enemy.GetBounds(), 520, 75))
            {
                return true;
            }
        }

        foreach (EnergyShield shield in shields)
        {
            if (IsGoodShotTarget(droneBox, shield.GetBounds(), 380, 85))
            {
                return true;
            }
        }

        foreach (Obstacle obstacle in obstacles)
        {
            if (IsGoodShotTarget(droneBox, obstacle.GetBounds(), 330, 80))
            {
                return true;
            }
        }

        return false;
    }

    private static bool IsThreatInFront(Rectangle droneBox, Rectangle threatBox, int range)
    {
        return threatBox.Right >= droneBox.Left &&
               threatBox.Left <= droneBox.Right + range;
    }

    private static bool HasVerticalOverlap(Rectangle droneBox, Rectangle threatBox, int margin)
    {
        return droneBox.Top - margin < threatBox.Bottom &&
               droneBox.Bottom + margin > threatBox.Top;
    }

    private static Vector2 DodgeAwayFrom(Rectangle droneBox, Rectangle threatBox)
    {
        float droneCenterY = droneBox.Center.Y;
        float threatCenterY = threatBox.Center.Y;

        float yDirection;

        if (Math.Abs(droneCenterY - threatCenterY) < 8f)
        {
            yDirection = droneCenterY < GameSettings.ScreenHeight / 2f
                ? -1f
                : 1f;
        }
        else
        {
            yDirection = droneCenterY < threatCenterY
                ? -1f
                : 1f;
        }

        // ML-2 POLISH:
        // Add a small horizontal move so the bot feels less robotic.
        // It still mostly dodges vertically, but not in a perfectly stiff line.
        float xDirection = threatBox.Left < droneBox.Right + 100
            ? -0.25f
            : 0.10f;

        return new Vector2(xDirection, yDirection);
    }

    private static bool IsGoodShotTarget(
        Rectangle droneBox,
        Rectangle targetBox,
        int range,
        int verticalTolerance)
    {
        bool inFront =
            targetBox.Left > droneBox.Right &&
            targetBox.Left < droneBox.Right + range;

        if (!inFront)
        {
            return false;
        }

        int droneCenterY = droneBox.Center.Y;
        int targetCenterY = targetBox.Center.Y;

        return Math.Abs(droneCenterY - targetCenterY) <= verticalTolerance;
    }
}