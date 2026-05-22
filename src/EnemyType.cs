namespace DroneGameLocal;

// LEVEL 5A CHANGE:
// EnemyType allows the game to support different enemy behaviors.
// Scout = basic enemy.
// Tank = bigger enemy with more health and shield deployment.
// ZigZag = faster enemy with stronger up/down movement.
// Sniper = slower enemy that shoots faster bullets.
public enum EnemyType
{
    Scout,
    Tank,
    ZigZag,
    Sniper
}