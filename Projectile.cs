using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;

namespace Subspace;

/// <summary>
/// A projectile fired from a weapon
/// </summary>
public class Projectile
{
    public float X { get; set; }
    public float Y { get; set; }
    public float Angle { get; set; }
    public float Speed { get; set; }
    public int Damage { get; set; }
    public string ProjectileType { get; set; }
    public int OwnerId { get; set; }
    public float Lifetime { get; set; }
    public bool Alive { get; set; }

    private float vx;
    private float vy;
    private Color color;
    private int size;

    public Projectile(float x, float y, float angle, float speed, int damage, string projectileType, int ownerId)
    {
        X = x;
        Y = y;
        Angle = angle;
        Speed = speed;
        Damage = damage;
        ProjectileType = projectileType;
        OwnerId = ownerId;
        Lifetime = 3.0f;  // seconds
        Alive = true;

        // Calculate velocity
        vx = (float)Math.Cos(angle) * speed;
        vy = (float)Math.Sin(angle) * speed;

        // Visual properties
        if (projectileType == "laser")
        {
            color = new Color(255, 50, 50);
            size = 3;
        }
        else  // cannon
        {
            color = new Color(255, 200, 0);
            size = 5;
        }
    }

    public void Update(float dt)
    {
        if (!Alive)
            return;

        X += vx * dt;
        Y += vy * dt;
        Lifetime -= dt;

        if (Lifetime <= 0)
            Alive = false;
    }

    public void Render(SpriteBatch spriteBatch, Texture2D pixelTexture, float cameraX, float cameraY)
    {
        if (!Alive)
            return;

        int screenX = (int)(X - cameraX);
        int screenY = (int)(Y - cameraY);

        // Draw projectile with enhanced trail effect
        if (ProjectileType == "laser")
        {
            // Laser beam effect with glow
            int trailLength = 20;
            int endX = screenX - (int)(Math.Cos(Angle) * trailLength);
            int endY = screenY - (int)(Math.Sin(Angle) * trailLength);
            
            // Outer glow
            DrawLine(spriteBatch, pixelTexture, endX, endY, screenX, screenY, 5, color * 0.3f);
            // Middle beam
            DrawLine(spriteBatch, pixelTexture, endX, endY, screenX, screenY, 3, color * 0.7f);
            // Core beam
            DrawLine(spriteBatch, pixelTexture, endX, endY, screenX, screenY, 1, Color.White);
            
            // Bright head
            DrawCircle(spriteBatch, pixelTexture, screenX, screenY, size + 2, color * 0.5f);
            DrawCircle(spriteBatch, pixelTexture, screenX, screenY, size, color);
            DrawCircle(spriteBatch, pixelTexture, screenX, screenY, size / 2, Color.White);
        }
        else
        {
            // Cannon projectile with glow
            DrawCircle(spriteBatch, pixelTexture, screenX, screenY, size + 3, color * 0.4f);
            DrawCircle(spriteBatch, pixelTexture, screenX, screenY, size, color);
            DrawCircle(spriteBatch, pixelTexture, screenX, screenY, size - 2, Color.Yellow);
            DrawCircle(spriteBatch, pixelTexture, screenX, screenY, size / 2, Color.White);
        }
    }

    public bool CheckCollision(Rectangle rect)
    {
        return rect.Contains((int)X, (int)Y);
    }

    private void DrawCircle(SpriteBatch spriteBatch, Texture2D texture, int centerX, int centerY, int radius, Color color)
    {
        for (int y = -radius; y <= radius; y++)
        {
            for (int x = -radius; x <= radius; x++)
            {
                if (x * x + y * y <= radius * radius)
                {
                    spriteBatch.Draw(texture, new Rectangle(centerX + x, centerY + y, 1, 1), color);
                }
            }
        }
    }

    private void DrawLine(SpriteBatch spriteBatch, Texture2D texture, int x1, int y1, int x2, int y2, int thickness, Color color)
    {
        int dx = x2 - x1;
        int dy = y2 - y1;
        float distance = (float)Math.Sqrt(dx * dx + dy * dy);
        float angle = (float)Math.Atan2(dy, dx);

        spriteBatch.Draw(texture,
            new Rectangle(x1, y1, (int)distance, thickness),
            null,
            color,
            angle,
            Vector2.Zero,
            SpriteEffects.None,
            0);
    }
}
