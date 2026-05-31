using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;

namespace DroneGameLocal;

// ML-2 CHANGE:
// This is a rule-based autoplayer used to collect gameplay data faster.
// This is NOT Machine Learning yet.
// It uses simple rules to dodge danger and shoot when useful.
public sealed class BotPlayer
{
    private const int BulletDangerRange = 320;
    private const int ObstacleDangerRange = 260;
    private const int EnemyDangerRange = 230;
    private const int VerticalDangerMargin = 32;

    private const float PreferredX = 120f;
    private const float PreferredY = 330f;

    public BotDecision GetDecision(
        Drone drone,
        IReadOnlyList<Obstacle> obstacles,
        IReadOnlyList<Enemy> enemies,
        IReadOnlyList<EnemyBullet> enemyBullets,
        IReadOnlyList<EnergyShield> shields,
        int shotCharges,
        int activePlayerShots)
    {
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

        bool shouldFire = ShouldFire(
            droneBox,
            enemies,
            enemyBullets,
            shields,
            obstacles,
            shotCharges,
            activePlayerShots
        );

        return new BotDecision
        {
            MovementDirection = movement,
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
        // Highest priority: dodge enemy bullets.
        foreach (EnemyBullet bullet in enemyBullets)
        {
            Rectangle bulletBox = bullet.GetBounds();

            if (IsThreatInFront(droneBox, bulletBox, BulletDangerRange) &&
                HasVerticalOverlap(droneBox, bulletBox, VerticalDangerMargin))
            {
                return DodgeAwayFrom(droneBox, bulletBox);
            }
        }

        // Next priority: dodge tank shields.
        foreach (EnergyShield shield in shields)
        {
            Rectangle shieldBox = shield.GetBounds();

            if (IsThreatInFront(droneBox, shieldBox, ObstacleDangerRange) &&
                HasVerticalOverlap(droneBox, shieldBox, VerticalDangerMargin))
            {
                return DodgeAwayFrom(droneBox, shieldBox);
            }
        }

        // Next priority: dodge obstacles.
        foreach (Obstacle obstacle in obstacles)
        {
            Rectangle obstacleBox = obstacle.GetBounds();

            if (IsThreatInFront(droneBox, obstacleBox, ObstacleDangerRange) &&
                HasVerticalOverlap(droneBox, obstacleBox, VerticalDangerMargin))
            {
                return DodgeAwayFrom(droneBox, obstacleBox);
            }
        }

        // Last priority: avoid enemy body collision.
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

        // Prevent the bot from filling the screen with too many player shots.
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

        if (Math.Abs(droneCenterY - threatCenterY) < 8f)
        {
            return droneCenterY < GameSettings.ScreenHeight / 2f
                ? new Vector2(0f, -1f)
                : new Vector2(0f, 1f);
        }

        return droneCenterY < threatCenterY
            ? new Vector2(0f, -1f)
            : new Vector2(0f, 1f);
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