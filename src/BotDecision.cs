using Microsoft.Xna.Framework;

namespace DroneGameLocal;

// ML-2 CHANGE:
// BotDecision is the result of the rule-based bot thinking.
// It tells Game1 how the bot wants to move and whether it wants to shoot.
public sealed class BotDecision
{
    public Vector2 MovementDirection { get; init; } = Vector2.Zero;
    public bool ShouldFire { get; init; }
}