using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace DroneGameLocal;

public sealed class Starfield
{
    private sealed class Star
    {
        public Vector2 Position { get; set; }
        public float Speed { get; set; }
        public int Size { get; set; }
        public Color Color { get; set; }
    }

    private readonly List<Star> _stars = new();
    private readonly Random _random = new();

    public Starfield(int starCount)
    {
        for (int i = 0; i < starCount; i++)
        {
            _stars.Add(CreateRandomStar(
                _random.Next(0, GameSettings.ScreenWidth),
                _random.Next(0, GameSettings.ScreenHeight)
            ));
        }
    }

    public void Update(float deltaTime)
    {
        foreach (Star star in _stars)
        {
            star.Position = new Vector2(
                star.Position.X - star.Speed * deltaTime,
                star.Position.Y
            );

            if (star.Position.X < 0)
            {
                star.Position = new Vector2(
                    GameSettings.ScreenWidth + _random.Next(0, 80),
                    _random.Next(55, GameSettings.ScreenHeight)
                );

                star.Speed = _random.Next(30, 180);
            }
        }
    }

    public void Draw(SpriteBatch spriteBatch, Texture2D pixel)
    {
        foreach (Star star in _stars)
        {
            var rectangle = new Rectangle(
                (int)star.Position.X,
                (int)star.Position.Y,
                star.Size,
                star.Size
            );

            spriteBatch.Draw(pixel, rectangle, star.Color);
        }
    }

    private Star CreateRandomStar(int x, int y)
    {
        int brightness = _random.Next(80, 220);

        return new Star
        {
            Position = new Vector2(x, y),
            Speed = _random.Next(30, 180),
            Size = _random.Next(1, 4),
            Color = new Color(brightness, brightness, brightness)
        };
    }
}