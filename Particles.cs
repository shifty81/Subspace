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
            // Laser muzzle flash - bright and quick with glow
            // Core bright particles
            for (int i = 0; i < 12; i++)
            {
                float speed = 40f + (float)random.NextDouble() * 40f;
                float spread = -0.4f + (float)random.NextDouble() * 0.8f;
                float particleAngle = angle + spread;
                float vx = (float)Math.Cos(particleAngle) * speed;
                float vy = (float)Math.Sin(particleAngle) * speed;

                Color[] colors = { new Color(255, 50, 50), new Color(255, 100, 100), new Color(255, 150, 150), new Color(255, 200, 200) };
                Color color = colors[random.Next(colors.Length)];

                particles.Add(new Particle(x, y, vx, vy, 0.25f, 2f + (float)random.NextDouble() * 3f, color, true, true));
            }
            // Outer glow particles
            for (int i = 0; i < 8; i++)
            {
                float speed = 20f + (float)random.NextDouble() * 30f;
                float spread = -0.6f + (float)random.NextDouble() * 1.2f;
                float particleAngle = angle + spread;
                float vx = (float)Math.Cos(particleAngle) * speed;
                float vy = (float)Math.Sin(particleAngle) * speed;

                particles.Add(new Particle(x, y, vx, vy, 0.3f, 3f + (float)random.NextDouble() * 2f, 
                    new Color(255, 100, 100, 150), true, true));
            }
        }
        else  // cannon
        {
            // Cannon smoke and fire - bigger blast
            // Fire core
            for (int i = 0; i < 20; i++)
            {
                float speed = 25f + (float)random.NextDouble() * 40f;
                float spread = -0.6f + (float)random.NextDouble() * 1.2f;
                float particleAngle = angle + spread;
                float vx = (float)Math.Cos(particleAngle) * speed;
                float vy = (float)Math.Sin(particleAngle) * speed;

                Color color;
                if (random.NextDouble() < 0.7)
                {
                    Color[] fireColors = { new Color(255, 220, 0), new Color(255, 180, 0), new Color(255, 140, 0), new Color(255, 80, 0) };
                    color = fireColors[random.Next(fireColors.Length)];
                }
                else
                {
                    Color[] smokeColors = { new Color(120, 120, 120), new Color(150, 150, 150), new Color(180, 180, 180) };
                    color = smokeColors[random.Next(smokeColors.Length)];
                }

                particles.Add(new Particle(x, y, vx, vy, 0.35f + (float)random.NextDouble() * 0.25f, 
                    3f + (float)random.NextDouble() * 4f, color, true, false));
            }
            // Shockwave ring
            for (int i = 0; i < 8; i++)
            {
                float ringAngle = angle + (i / 8f) * (float)Math.PI * 2;
                float speed = 60f;
                float vx = (float)Math.Cos(ringAngle) * speed;
                float vy = (float)Math.Sin(ringAngle) * speed;
                
                particles.Add(new Particle(x, y, vx, vy, 0.2f, 4f, new Color(255, 200, 100, 200), true, true));
            }
        }
    }

    public void CreateExplosion(float x, float y, string size = "medium")
    {
        int numParticles, numSparks;
        float minSpeed, maxSpeed, minSize, maxSize, minLifetime, maxLifetime;

        switch (size)
        {
            case "small":
                numParticles = 30;
                numSparks = 15;
                minSpeed = 60f; maxSpeed = 180f;
                minSize = 2f; maxSize = 6f;
                minLifetime = 0.3f; maxLifetime = 0.7f;
                break;
            case "large":
                numParticles = 80;
                numSparks = 40;
                minSpeed = 100f; maxSpeed = 300f;
                minSize = 5f; maxSize = 12f;
                minLifetime = 0.6f; maxLifetime = 1.2f;
                break;
            default:  // medium
                numParticles = 50;
                numSparks = 25;
                minSpeed = 80f; maxSpeed = 240f;
                minSize = 3f; maxSize = 9f;
                minLifetime = 0.4f; maxLifetime = 1.0f;
                break;
        }

        // Create bright core flash
        particles.Add(new Particle(x, y, 0, 0, 0.15f, maxSize * 3, Color.White, true, true));
        particles.Add(new Particle(x, y, 0, 0, 0.2f, maxSize * 2.5f, new Color(255, 220, 100), true, true));

        // Create explosion fire particles
        Color[] explosionColors = { 
            new Color(255, 240, 100), new Color(255, 200, 50), new Color(255, 160, 30), 
            new Color(255, 120, 0), new Color(255, 80, 0), new Color(255, 40, 0) 
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

        // Add flying sparks
        Color[] sparkColors = { Color.White, Color.Yellow, new Color(255, 220, 150), new Color(255, 180, 100) };
        for (int i = 0; i < numSparks; i++)
        {
            float angle = (float)(random.NextDouble() * Math.PI * 2);
            float speed = maxSpeed * 0.8f + (float)random.NextDouble() * maxSpeed * 0.4f;
            float vx = (float)Math.Cos(angle) * speed;
            float vy = (float)Math.Sin(angle) * speed;
            Color color = sparkColors[random.Next(sparkColors.Length)];

            particles.Add(new Particle(x, y, vx, vy, 0.3f + (float)random.NextDouble() * 0.3f, 
                1f + (float)random.NextDouble() * 2f, color, true, true, 100f));
        }

        // Add smoke particles
        Color[] smokeColors = { new Color(60, 60, 60), new Color(90, 90, 90), new Color(120, 120, 120), new Color(150, 150, 150) };
        for (int i = 0; i < numParticles / 2; i++)
        {
            float angle = (float)(random.NextDouble() * Math.PI * 2);
            float speed = 30f + (float)random.NextDouble() * 80f;
            float vx = (float)Math.Cos(angle) * speed;
            float vy = (float)Math.Sin(angle) * speed;
            Color color = smokeColors[random.Next(smokeColors.Length)];
            float lifetime = 0.6f + (float)random.NextDouble() * 0.8f;

            particles.Add(new Particle(x, y, vx, vy, lifetime, 5f + (float)random.NextDouble() * 5f, 
                color, true, false, 30f));
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
        // Impact flash
        particles.Add(new Particle(x, y, 0, 0, 0.1f, 6f, Color.White, true, true));
        
        // Sparks flying in all directions
        for (int i = 0; i < 18; i++)
        {
            float angle = (float)(random.NextDouble() * Math.PI * 2);
            float speed = 100f + (float)random.NextDouble() * 100f;
            float vx = (float)Math.Cos(angle) * speed;
            float vy = (float)Math.Sin(angle) * speed;

            Color[] colors = { Color.White, Color.Yellow, new Color(255, 220, 150), new Color(255, 180, 100), new Color(255, 140, 0) };
            Color color = colors[random.Next(colors.Length)];

            particles.Add(new Particle(x, y, vx, vy, 0.25f + (float)random.NextDouble() * 0.25f, 
                1f + (float)random.NextDouble() * 2.5f, color, true, true, 250f));
        }
        
        // Small debris
        for (int i = 0; i < 6; i++)
        {
            float angle = (float)(random.NextDouble() * Math.PI * 2);
            float speed = 50f + (float)random.NextDouble() * 60f;
            float vx = (float)Math.Cos(angle) * speed;
            float vy = (float)Math.Sin(angle) * speed;

            particles.Add(new Particle(x, y, vx, vy, 0.4f + (float)random.NextDouble() * 0.3f, 
                2f + (float)random.NextDouble() * 2f, new Color(100, 100, 100), true, false, 150f));
        }
    }

    public void Clear()
    {
        particles.Clear();
    }
}
