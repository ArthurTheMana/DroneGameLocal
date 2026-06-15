using Microsoft.Xna.Framework;

namespace DroneGameLocal;

public sealed class ChargeShot
{
    public Vector2 Position { get; private set; }

    // COLLISION POLISH:
    // PreviousPosition is used for swept collision.
    // This prevents fast shots from slipping through enemies or obstacles.
    public Vector2 PreviousPosition { get; private set; }

    public int Width { get; } = 34;
    public int Height { get; } = 12;

    public bool CanBreakObstacle => true;

    public ChargeShot(Vector2 position)
    {
        Position = position;
        PreviousPosition = position;
    }

    public void Update(float deltaTime)
    {
        // COLLISION POLISH:
        // Store old position before moving.
        PreviousPosition = Position;

        Position = new Vector2(
            Position.X + GameSettings.ShotSpeed * deltaTime,
            Position.Y
        );
    }

    public bool IsOffScreen()
    {
        return Position.X > GameSettings.ScreenWidth;
    }

    public Rectangle GetBounds()
    {
        return new Rectangle(
            (int)Position.X,
            (int)Position.Y,
            Width,
            Height
        );
    }

    // COLLISION POLISH:
    // This rectangle covers the shot's movement path between frames.
    // It reduces the chance of the shot visually passing through a target.
    public Rectangle GetSweptBounds()
    {
        Rectangle previous = new Rectangle(
            (int)PreviousPosition.X,
            (int)PreviousPosition.Y,
            Width,
            Height
        );

        Rectangle current = GetBounds();

        int left = MathHelper.Min(previous.Left, current.Left);
        int top = MathHelper.Min(previous.Top, current.Top);
        int right = MathHelper.Max(previous.Right, current.Right);
        int bottom = MathHelper.Max(previous.Bottom, current.Bottom);

        // Small padding makes collision feel fairer.
        int padding = 4;

        return new Rectangle(
            left - padding,
            top - padding,
            right - left + padding * 2,
            bottom - top + padding * 2
        );
    }
}