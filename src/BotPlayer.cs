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
// Bot now uses roaming movement.
// Instead of only staying near one center point,
// it moves around the playable area with patrol targets.
// It can move up, down, forward, and backward.
public sealed class BotPlayer
{
    private const int BulletDangerRange = 360;
    private const int ObstacleDangerRange = 300;
    private const int EnemyDangerRange = 260;
    private const int ShieldDangerRange = 300;

    private const int VerticalDangerMargin = 38;

    private const float DecisionRefreshSeconds = 0.14f;
    private const float MovementSmoothSpeed = 4.8f;
    private const float BotShootCooldownSeconds = 0.45f;

    // ML-2 POLISH:
    // Bot patrol area.
    // It does not use the full right side because that would be too dangerous,
    // but it now moves much more than before.
    private const float PatrolMinX = 60f;
    private const float PatrolMaxX = 330f;
    private const float PatrolMinY = 165f;
    private const float PatrolMaxY = 535f;

    private const float PatrolTargetReachDistance = 34f;
    private const float PatrolRetargetSeconds = 1.25f;

    private readonly Random _random = new();

    private Vector2 _smoothedMovement = Vector2.Zero;
    private Vector2 _lastDesiredMovement = Vector2.Zero;
    private Vector2 _patrolTarget = new(120f, 330f);

    private float _decisionTimer;
    private float _shootCooldownTimer;
    private float _patrolTimer;
    private float _driftTimer;

    public void Reset()
    {
        _smoothedMovement = Vector2.Zero;
        _lastDesiredMovement = Vector2.Zero;

        _decisionTimer = 0f;
        _shootCooldownTimer = 0f;
        _patrolTimer = 0f;
        _driftTimer = 0f;

        PickNewPatrolTarget();
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
        _driftTimer += deltaTime;
        _patrolTimer -= deltaTime;

        if (_shootCooldownTimer > 0f)
        {
            _shootCooldownTimer -= deltaTime;
        }

        UpdatePatrolTargetIfNeeded(drone);

        _decisionTimer -= deltaTime;

        if (_decisionTimer <= 0f)
        {
            _decisionTimer = DecisionRefreshSeconds;

            Rectangle droneBox = drone.GetBounds();

            Vector2 patrolMovement = GetPatrolMovement(drone);
            Vector2 dangerMovement = GetDangerAvoidanceMovement(
                droneBox,
                obstacles,
                enemies,
                enemyBullets,
                shields
            );

            // ML-2 POLISH:
            // Small natural drift prevents the bot from moving like a strict machine.
            Vector2 driftMovement = new Vector2(
                (float)Math.Sin(_driftTimer * 1.7f) * 0.18f,
                (float)Math.Sin(_driftTimer * 2.4f) * 0.22f
            );

            Vector2 movement =
                patrolMovement * 0.75f +
                dangerMovement * 1.65f +
                driftMovement;

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

    private void UpdatePatrolTargetIfNeeded(Drone drone)
    {
        Vector2 droneCenter = new Vector2(
            drone.Position.X + drone.Width / 2f,
            drone.Position.Y + drone.Height / 2f
        );

        float distanceToTarget = Vector2.Distance(droneCenter, _patrolTarget);

        if (_patrolTimer <= 0f ||
            distanceToTarget <= PatrolTargetReachDistance)
        {
            PickNewPatrolTarget();
        }
    }

    private void PickNewPatrolTarget()
    {
        float x = PatrolMinX +
                  (float)_random.NextDouble() * (PatrolMaxX - PatrolMinX);

        float y = PatrolMinY +
                  (float)_random.NextDouble() * (PatrolMaxY - PatrolMinY);

        _patrolTarget = new Vector2(x, y);
        _patrolTimer = PatrolRetargetSeconds +
                       (float)_random.NextDouble() * 1.10f;
    }

    private Vector2 GetPatrolMovement(Drone drone)
    {
        Vector2 droneCenter = new Vector2(
            drone.Position.X + drone.Width / 2f,
            drone.Position.Y + drone.Height / 2f
        );

        Vector2 direction = _patrolTarget - droneCenter;

        if (direction.LengthSquared() < 1f)
        {
            return Vector2.Zero;
        }

        direction.Normalize();
        return direction;
    }

    private static Vector2 GetDangerAvoidanceMovement(
        Rectangle droneBox,
        IReadOnlyList<Obstacle> obstacles,
        IReadOnlyList<Enemy> enemies,
        IReadOnlyList<EnemyBullet> enemyBullets,
        IReadOnlyList<EnergyShield> shields)
    {
        Vector2 avoidance = Vector2.Zero;

        foreach (EnemyBullet bullet in enemyBullets)
        {
            avoidance += GetAvoidanceFromThreat(
                droneBox,
                bullet.GetBounds(),
                BulletDangerRange,
                VerticalDangerMargin,
                2.7f
            );
        }

        foreach (EnergyShield shield in shields)
        {
            avoidance += GetAvoidanceFromThreat(
                droneBox,
                shield.GetBounds(),
                ShieldDangerRange,
                VerticalDangerMargin,
                2.2f
            );
        }

        foreach (Obstacle obstacle in obstacles)
        {
            avoidance += GetAvoidanceFromThreat(
                droneBox,
                obstacle.GetBounds(),
                ObstacleDangerRange,
                VerticalDangerMargin,
                2.0f
            );
        }

        foreach (Enemy enemy in enemies)
        {
            avoidance += GetAvoidanceFromThreat(
                droneBox,
                enemy.GetBounds(),
                EnemyDangerRange,
                VerticalDangerMargin,
                1.5f
            );
        }

        if (avoidance.LengthSquared() > 1f)
        {
            avoidance.Normalize();
        }

        return avoidance;
    }

    private static Vector2 GetAvoidanceFromThreat(
        Rectangle droneBox,
        Rectangle threatBox,
        int range,
        int verticalMargin,
        float weight)
    {
        if (!IsThreatRelevant(droneBox, threatBox, range, verticalMargin))
        {
            return Vector2.Zero;
        }

        Vector2 droneCenter = new Vector2(
            droneBox.Center.X,
            droneBox.Center.Y
        );

        Vector2 threatCenter = new Vector2(
            threatBox.Center.X,
            threatBox.Center.Y
        );

        Vector2 away = droneCenter - threatCenter;

        if (away.LengthSquared() < 1f)
        {
            away = new Vector2(-1f, 0f);
        }

        away.Normalize();

        float distanceX = Math.Abs(threatBox.Left - droneBox.Right);
        float closeness = 1f - MathHelper.Clamp(distanceX / range, 0f, 1f);

        // ML-2 POLISH:
        // Extra backward movement when danger is very close.
        // This lets the bot move forward/backward, not only up/down.
        if (threatBox.Left < droneBox.Right + 90)
        {
            away.X -= 0.65f;
        }

        return away * closeness * weight;
    }

    private static bool IsThreatRelevant(
        Rectangle droneBox,
        Rectangle threatBox,
        int range,
        int verticalMargin)
    {
        bool closeInFront =
            threatBox.Right >= droneBox.Left - 40 &&
            threatBox.Left <= droneBox.Right + range;

        bool verticalOverlap =
            droneBox.Top - verticalMargin < threatBox.Bottom &&
            droneBox.Bottom + verticalMargin > threatBox.Top;

        return closeInFront && verticalOverlap;
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
            if (IsGoodShotTarget(droneBox, bullet.GetBounds(), 320, 32))
            {
                return true;
            }
        }

        foreach (Enemy enemy in enemies)
        {
            if (IsGoodShotTarget(droneBox, enemy.GetBounds(), 540, 82))
            {
                return true;
            }
        }

        foreach (EnergyShield shield in shields)
        {
            if (IsGoodShotTarget(droneBox, shield.GetBounds(), 400, 88))
            {
                return true;
            }
        }

        foreach (Obstacle obstacle in obstacles)
        {
            if (IsGoodShotTarget(droneBox, obstacle.GetBounds(), 350, 85))
            {
                return true;
            }
        }

        return false;
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