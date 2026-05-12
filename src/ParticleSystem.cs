using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace DroneGameLocal;

public sealed class ParticleSystem
{
    private sealed class Particle
    {
        public Vector2 Position { get; set; }
        public Vector2 Velocity { get; set; }
        public float Life { get; set; }
        public float MaxLife { get; set; }
        public int Size { get; set; }
        public Color Color { get; set; }
    }

    private readonly List<Particle> _particles = new();
    private readonly Random _random = new();

    public void EmitCrash(Vector2 position)
    {
        for (int i = 0; i < 24; i++)
        {
            double angle = _random.NextDouble() * Math.PI * 2;
            float speed = _random.Next(80, 260);

            var velocity = new Vector2(
                (float)Math.Cos(angle) * speed,
                (float)Math.Sin(angle) * speed
            );

            Color color = i % 2 == 0
                ? new Color(255, 214, 10)
                : new Color(255, 80, 100);

            _particles.Add(new Particle
            {
                Position = position,
                Velocity = velocity,
                Life = 0.6f,
                MaxLife = 0.6f,
                Size = _random.Next(3, 8),
                Color = color
            });
        }
    }

    public void Update(float deltaTime)
    {
        for (int i = _particles.Count - 1; i >= 0; i--)
        {
            Particle particle = _particles[i];

            particle.Life -= deltaTime;

            if (particle.Life <= 0)
            {
                _particles.RemoveAt(i);
                continue;
            }

            particle.Position += particle.Velocity * deltaTime;
            particle.Velocity *= 0.92f;
        }
    }

    public void Draw(SpriteBatch spriteBatch, Texture2D pixel)
    {
        foreach (Particle particle in _particles)
        {
            float alpha = particle.Life / particle.MaxLife;

            var rectangle = new Rectangle(
                (int)particle.Position.X,
                (int)particle.Position.Y,
                particle.Size,
                particle.Size
            );

            spriteBatch.Draw(pixel, rectangle, particle.Color * alpha);
        }
    }
}