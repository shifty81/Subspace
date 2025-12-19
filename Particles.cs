using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;

namespace Subspace;

/// <summary>
/// A single particle
/// </summary>
public class Particle
{
    public float X { get; set; }
    public float Y { get; set; }
    public float VX { get; set; }
    public float VY { get; set; }
    public float Lifetime { get; set; }
    public float MaxLifetime { get; set; }
    public float Size { get; set; }
    public Color Color { get; set; }
    public bool Fade { get; set; }
    public bool Shrink { get; set; }
    public float Gravity { get; set; }

    public Particle(float x, float y, float vx, float vy, float lifetime, float size, Color color, 
                   bool fade = true, bool shrink = false, float gravity = 0f)
    {
        X = x;
        Y = y;
        VX = vx;
        VY = vy;
        Lifetime = lifetime;
        MaxLifetime = lifetime;
        Size = size;
        Color = color;
        Fade = fade;
        Shrink = shrink;
        Gravity = gravity;
    }
}

/// <summary>
/// Manages all particles in the game
/// </summary>
public class ParticleSystem
{
    private const float PARTICLE_DRAG = 0.98f;
    private List<Particle> particles = new List<Particle>();
    private Random random = new Random();

    public void Update(float dt)
    {
        // Iterate backwards to safely remove particles
        for (int i = particles.Count - 1; i >= 0; i--)
        {
            var particle = particles[i];
            particle.Lifetime -= dt;

            if (particle.Lifetime <= 0)
            {
                particles.RemoveAt(i);
                continue;
            }

            // Update position
            particle.X += particle.VX * dt;
            particle.Y += particle.VY * dt;

            // Apply gravity
            if (particle.Gravity != 0)
                particle.VY += particle.Gravity * dt;

            // Apply drag
            particle.VX *= PARTICLE_DRAG;
            particle.VY *= PARTICLE_DRAG;
        }
    }

    public void Render(SpriteBatch spriteBatch, Texture2D pixelTexture, float cameraX, float cameraY, int screenWidth, int screenHeight)
    {
        foreach (var particle in particles)
        {
            int screenX = (int)(particle.X - cameraX);
            int screenY = (int)(particle.Y - cameraY);

            // Skip if off screen
            if (screenX < -10 || screenX > screenWidth + 10 || screenY < -10 || screenY > screenHeight + 10)
                continue;

            // Calculate alpha based on lifetime
            float alpha = 1.0f;
            if (particle.Fade)
                alpha = particle.Lifetime / particle.MaxLifetime;

            // Calculate size
            float size = particle.Size;
            if (particle.Shrink)
                size = particle.Size * (particle.Lifetime / particle.MaxLifetime);

            // Render particle
            Color color = particle.Color * alpha;
            int radius = (int)size;

            for (int y = -radius; y <= radius; y++)
            {
                for (int x = -radius; x <= radius; x++)
                {
                    if (x * x + y * y <= radius * radius)
                    {
                        spriteBatch.Draw(pixelTexture, new Rectangle(screenX + x, screenY + y, 1, 1), color);
                    }
                }
            }
        }
    }

    public void CreateWeaponFireEffect(float x, float y, float angle, string weaponType)
    {
        if (weaponType == "laser")
        {
            // Laser muzzle flash - bright and quick
            for (int i = 0; i < 8; i++)
            {
                float speed = 30f + (float)random.NextDouble() * 30f;
                float spread = -0.3f + (float)random.NextDouble() * 0.6f;
                float particleAngle = angle + spread;
                float vx = (float)Math.Cos(particleAngle) * speed;
                float vy = (float)Math.Sin(particleAngle) * speed;

                Color[] colors = { new Color(255, 100, 100), new Color(255, 150, 150), new Color(255, 200, 200) };
                Color color = colors[random.Next(colors.Length)];

                particles.Add(new Particle(x, y, vx, vy, 0.2f, 2f + (float)random.NextDouble() * 2f, color, true, true));
            }
        }
        else  // cannon
        {
            // Cannon smoke and fire
            for (int i = 0; i < 15; i++)
            {
                float speed = 20f + (float)random.NextDouble() * 30f;
                float spread = -0.5f + (float)random.NextDouble() * 1.0f;
                float particleAngle = angle + spread;
                float vx = (float)Math.Cos(particleAngle) * speed;
                float vy = (float)Math.Sin(particleAngle) * speed;

                Color color;
                if (random.NextDouble() < 0.6)
                {
                    Color[] fireColors = { new Color(255, 200, 0), new Color(255, 150, 0), new Color(255, 100, 0) };
                    color = fireColors[random.Next(fireColors.Length)];
                }
                else
                {
                    Color[] smokeColors = { new Color(100, 100, 100), new Color(150, 150, 150) };
                    color = smokeColors[random.Next(smokeColors.Length)];
                }

                particles.Add(new Particle(x, y, vx, vy, 0.3f + (float)random.NextDouble() * 0.2f, 
                    3f + (float)random.NextDouble() * 3f, color, true, false));
            }
        }
    }

    public void CreateExplosion(float x, float y, string size = "medium")
    {
        int numParticles;
        float minSpeed, maxSpeed, minSize, maxSize, minLifetime, maxLifetime;

        switch (size)
        {
            case "small":
                numParticles = 20;
                minSpeed = 50f; maxSpeed = 150f;
                minSize = 2f; maxSize = 5f;
                minLifetime = 0.3f; maxLifetime = 0.6f;
                break;
            case "large":
                numParticles = 50;
                minSpeed = 80f; maxSpeed = 250f;
                minSize = 4f; maxSize = 10f;
                minLifetime = 0.5f; maxLifetime = 1.0f;
                break;
            default:  // medium
                numParticles = 35;
                minSpeed = 60f; maxSpeed = 200f;
                minSize = 3f; maxSize = 7f;
                minLifetime = 0.4f; maxLifetime = 0.8f;
                break;
        }

        // Create explosion particles
        Color[] explosionColors = { 
            new Color(255, 200, 0), new Color(255, 150, 0), new Color(255, 100, 0), 
            new Color(255, 50, 0), new Color(200, 200, 200) 
        };

        for (int i = 0; i < numParticles; i++)
        {
            float angle = (float)(random.NextDouble() * Math.PI * 2);
            float speed = minSpeed + (float)random.NextDouble() * (maxSpeed - minSpeed);
            float vx = (float)Math.Cos(angle) * speed;
            float vy = (float)Math.Sin(angle) * speed;
            Color color = explosionColors[random.Next(explosionColors.Length)];
            float lifetime = minLifetime + (float)random.NextDouble() * (maxLifetime - minLifetime);
            float particleSize = minSize + (float)random.NextDouble() * (maxSize - minSize);

            particles.Add(new Particle(x, y, vx, vy, lifetime, particleSize, color, true, true));
        }

        // Add smoke particles
        Color[] smokeColors = { new Color(80, 80, 80), new Color(100, 100, 100), new Color(120, 120, 120) };
        for (int i = 0; i < numParticles / 2; i++)
        {
            float angle = (float)(random.NextDouble() * Math.PI * 2);
            float speed = 20f + (float)random.NextDouble() * 60f;
            float vx = (float)Math.Cos(angle) * speed;
            float vy = (float)Math.Sin(angle) * speed;
            Color color = smokeColors[random.Next(smokeColors.Length)];
            float lifetime = 0.5f + (float)random.NextDouble() * 0.7f;

            particles.Add(new Particle(x, y, vx, vy, lifetime, 4f + (float)random.NextDouble() * 4f, 
                color, true, false, 20f));
        }
    }

    public void CreateEngineTrust(float x, float y, float angle, float power = 1.0f)
    {
        // Create 2-3 particles per frame when engine is active
        int numParticles = 2 + random.Next(2);

        for (int i = 0; i < numParticles; i++)
        {
            // Thrust goes opposite to engine direction
            float thrustAngle = angle + (float)Math.PI;
            float spread = -0.2f + (float)random.NextDouble() * 0.4f;
            float particleAngle = thrustAngle + spread;

            float speed = (80f + (float)random.NextDouble() * 70f) * power;
            float vx = (float)Math.Cos(particleAngle) * speed;
            float vy = (float)Math.Sin(particleAngle) * speed;

            // Blue engine glow
            Color[] colors = { new Color(100, 150, 255), new Color(150, 200, 255), new Color(200, 220, 255) };
            Color color = colors[random.Next(colors.Length)];

            particles.Add(new Particle(x, y, vx, vy, 0.1f + (float)random.NextDouble() * 0.2f, 
                2f + (float)random.NextDouble() * 2f, color, true, true));
        }
    }

    public void CreateDamageSparks(float x, float y)
    {
        for (int i = 0; i < 10; i++)
        {
            float angle = (float)(random.NextDouble() * Math.PI * 2);
            float speed = 80f + (float)random.NextDouble() * 70f;
            float vx = (float)Math.Cos(angle) * speed;
            float vy = (float)Math.Sin(angle) * speed;

            Color[] colors = { Color.Yellow, new Color(255, 200, 0), Color.White };
            Color color = colors[random.Next(colors.Length)];

            particles.Add(new Particle(x, y, vx, vy, 0.2f + (float)random.NextDouble() * 0.2f, 
                1f + (float)random.NextDouble() * 2f, color, true, true, 200f));
        }
    }

    public void Clear()
    {
        particles.Clear();
    }
}
