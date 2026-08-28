using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

public class BuffPickup
{
    public Vector2 Position;
    public float Speed = 180f;
    public int Size = 24;

    public int Width = 32;
    public int Height = 32;

    public Rectangle GetBounds()
    {
        return new Rectangle(
            (int)Position.X,
            (int)Position.Y,
            Width,
            Height
        );
    }

    public void Update(float deltaTime)
    {
        Position.X -= Speed * deltaTime;
    }

    public bool IsOffScreen()
    {
        return Position.X + Size < 0;
    }

    public void Draw(SpriteBatch spriteBatch, Texture2D pixel)
    {
        Vector2 center = new Vector2(
            Position.X + Width / 2f,
            Position.Y + Height / 2f
        );

        int outerRadius = 16;
        int innerRadius = 13;

        DrawFilledCircle(
            spriteBatch,
            pixel,
            center,
            outerRadius,
            new Color(120, 220, 255, 40)
        );

        DrawCircleOutline(
            spriteBatch,
            pixel,
            center,
            outerRadius,
            new Color(180, 240, 255)
        );

        DrawFilledCircle(
            spriteBatch,
            pixel,
            center,
            innerRadius,
            new Color(90, 180, 255, 25)
        );

        // Shield icon inside the bubble.
        spriteBatch.Draw(
            pixel,
            new Rectangle((int)Position.X + 9, (int)Position.Y + 6, 10, 14),
            new Color(80, 220, 255)
        );

        spriteBatch.Draw(
            pixel,
            new Rectangle((int)Position.X + 7, (int)Position.Y + 9, 14, 8),
            new Color(180, 255, 255)
        );

        spriteBatch.Draw(
            pixel,
            new Rectangle((int)Position.X + 11, (int)Position.Y + 9, 6, 8),
            Color.White
        );
    }

    private void DrawFilledCircle(
        SpriteBatch spriteBatch,
        Texture2D pixel,
        Vector2 center,
        int radius,
        Color color
    )
    {
        for (int y = -radius; y <= radius; y++)
        {
            for (int x = -radius; x <= radius; x++)
            {
                if (x * x + y * y <= radius * radius)
                {
                    spriteBatch.Draw(
                        pixel,
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
        SpriteBatch spriteBatch,
        Texture2D pixel,
        Vector2 center,
        int radius,
        Color color
    )
    {
        int thickness = 2;
        int outer = radius * radius;
        int inner = (radius - thickness) * (radius - thickness);

        for (int y = -radius; y <= radius; y++)
        {
            for (int x = -radius; x <= radius; x++)
            {
                int dist = x * x + y * y;

                if (dist <= outer && dist >= inner)
                {
                    spriteBatch.Draw(
                        pixel,
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
}