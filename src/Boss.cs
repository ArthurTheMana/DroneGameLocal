using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace DroneGameLocal;

public class Boss
{
    public Vector2 Position;
    public int Width = 120;
    public int Height = 80;
    public int Health = 20;
    public float VerticalSpeed = 120f;
    public bool MovingDown = true;

    public void Update(float deltaTime)
    {
        if (MovingDown)
        {
            Position.Y += VerticalSpeed * deltaTime;
            if (Position.Y >= GameSettings.ScreenHeight - Height - 20)
            {
                Position.Y = GameSettings.ScreenHeight - Height - 20;
                MovingDown = false;
            }
        }
        else
        {
            Position.Y -= VerticalSpeed * deltaTime;
            if (Position.Y <= GameSettings.PlayAreaTop + 20)
            {
                Position.Y = GameSettings.PlayAreaTop + 20;
                MovingDown = true;
            }
        }
    }

    public Rectangle GetBounds()
    {
        return new Rectangle((int)Position.X, (int)Position.Y, Width, Height);
    }

    public void Draw(SpriteBatch spriteBatch, Texture2D pixel)
    {
        spriteBatch.Draw(
            pixel,
            new Rectangle((int)Position.X, (int)Position.Y, Width, Height),
            new Color(120, 20, 120)
        );

        spriteBatch.Draw(
            pixel,
            new Rectangle((int)Position.X + 8, (int)Position.Y + 8, Width - 16, Height - 16),
            new Color(220, 60, 220)
        );
    }
}