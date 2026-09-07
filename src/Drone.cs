using Microsoft.Xna.Framework;

namespace DroneGameLocal;

public sealed class Drone {
    public Vector2 Position { get; private set; }

    public float Speed { get; set; } = 300f;

    public int Width { get; } = 42;
    public int Height { get; } = 28;

    public Drone(float x, float y) {
        Position = new Vector2(x, y);
    }

    public void Reset(float x, float y) {
        Position = new Vector2(x, y);
    }

    public void Move(Vector2 direction, float deltaTime) {
        if (direction.LengthSquared() > 1f)
        {
            direction.Normalize();
        }

        Position += direction * Speed * deltaTime;
    }

    public void MoveBy(Vector2 offset)
    {
        Position += offset;
    }

    public void ClampToScreen(int screenWidth, int screenHeight)
    {
        float minX = GameSettings.PlayAreaSidePadding;
        float maxX = screenWidth - Width - GameSettings.PlayAreaSidePadding;

        float minY = GameSettings.PlayAreaTop + 10;
        float maxY = screenHeight - Height - GameSettings.PlayAreaBottomPadding;

        Position = new Vector2(
            MathHelper.Clamp(Position.X, minX, maxX),
            MathHelper.Clamp(Position.Y, minY, maxY)
        );
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


}