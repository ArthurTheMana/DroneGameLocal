using System.Collections.Generic;

namespace DroneGameLocal;

public static class CollisionChecker
{
    public static bool HasCollision(Drone drone, IEnumerable<Obstacle> obstacles)
    {
        var droneBox = drone.GetBounds();

        foreach (var obstacle in obstacles)
        {
            if (droneBox.Intersects(obstacle.GetBounds()))
            {
                return true;
            }
        }

        return false;
    }
}