using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;

namespace Subspace;

/// <summary>
/// A rocky asteroid obstacle. Blocks projectiles, damages ships on contact, and can be destroyed.
/// </summary>
public class Asteroid
{
    public float X { get; set; }
    public float Y { get; set; }
    public int Radius { get; }
    public int Health { get; private set; }
    public int MaxHealth { get; }
    public float Angle { get; private set; }

    private readonly float _rotationSpeed;
    private readonly int _seed;   // used to generate a consistent rocky outline

    private const int HEALTH_PER_RADIUS_UNIT = 8;

    public Asteroid(float x, float y, int radius, Random rng)
    {
        X = x;
        Y = y;
        Radius = radius;
        MaxHealth = radius * HEALTH_PER_RADIUS_UNIT;
        Health = MaxHealth;
        Angle = rng.NextSingle() * MathF.Tau;
        _rotationSpeed = (rng.NextSingle() - 0.5f) * 0.4f;   // ±0.2 rad/sec
        _seed = rng.Next();
    }

    public bool IsDestroyed() => Health <= 0;

    /// <summary>Returns true when the asteroid is destroyed by this hit.</summary>
    public bool TakeDamage(int damage)
    {
        Health -= damage;
        if (Health < 0) Health = 0;
        return Health == 0;
    }

    public void Update(float dt)
    {
        Angle += _rotationSpeed * dt;
    }

    /// <summary>True when the given point is inside the asteroid's rocky collision circle.</summary>
    public bool ContainsPoint(float px, float py)
    {
        float dx = px - X;
        float dy = py - Y;
        return dx * dx + dy * dy <= (float)Radius * Radius;
    }

    /// <summary>True when a circle of the given radius overlaps the asteroid.</summary>
    public bool OverlapCircle(float cx, float cy, float circleRadius)
    {
        float dx = cx - X;
        float dy = cy - Y;
        float combinedRadius = circleRadius + Radius;
        return dx * dx + dy * dy < combinedRadius * combinedRadius;
    }

    public void Render(SpriteBatch spriteBatch, Texture2D pixelTexture, float cameraX, float cameraY)
    {
        int screenX = (int)(X - cameraX);
        int screenY = (int)(Y - cameraY);

        float healthPct = (float)Health / MaxHealth;

        // Use the seed to generate a consistent bump pattern (8 bumps)
        const int BUMPS = 8;
        var rng = new Random(_seed);
        float[] bumps = new float[BUMPS];
        for (int i = 0; i < BUMPS; i++)
            bumps[i] = 0.8f + (float)rng.NextDouble() * 0.4f;   // 0.8–1.2 radius multiplier

        // Dark background circle for shadow
        DrawFilledCircle(spriteBatch, pixelTexture, screenX + 3, screenY + 3, Radius, Color.Black * 0.35f);

        // Build the rocky polygon by walking around the perimeter in small angular steps
        int segments = BUMPS * 4;  // 4 steps per bump segment → smooth
        for (int i = 0; i < segments; i++)
        {
            float a1 = Angle + (float)i       / segments * MathF.Tau;
            float a2 = Angle + (float)(i + 1) / segments * MathF.Tau;

            // Interpolate bump factor for the two edges
            float t1 = (float)i       / segments * BUMPS;
            float t2 = (float)(i + 1) / segments * BUMPS;
            int b1 = (int)t1 % BUMPS;
            int b2 = (int)t2 % BUMPS;
            float lerp1 = t1 - (int)t1;
            float lerp2 = t2 - (int)t2;
            float r1 = Radius * Lerp(bumps[b1], bumps[(b1 + 1) % BUMPS], lerp1);
            float r2 = Radius * Lerp(bumps[b2], bumps[(b2 + 1) % BUMPS], lerp2);

            // Rock colour darkens with damage
            Color rockBase = new Color(100, 90, 80);
            Color rockColor = new Color(
                (int)(rockBase.R * (0.4f + 0.6f * healthPct)),
                (int)(rockBase.G * (0.4f + 0.6f * healthPct)),
                (int)(rockBase.B * (0.4f + 0.6f * healthPct)));

            // Draw a thin trapezoid slice from the centre out to the rocky edge
            int ax1 = screenX + (int)(MathF.Cos(a1) * r1);
            int ay1 = screenY + (int)(MathF.Sin(a1) * r1);
            int ax2 = screenX + (int)(MathF.Cos(a2) * r2);
            int ay2 = screenY + (int)(MathF.Sin(a2) * r2);

            // Fill triangle (screenX,screenY) → (ax1,ay1) → (ax2,ay2) with one line per step
            FillTriangle(spriteBatch, pixelTexture,
                screenX, screenY,
                ax1, ay1,
                ax2, ay2,
                rockColor);
        }

        // Surface detail: lighter inner ring
        int innerR = Math.Max(2, (int)(Radius * 0.55f));
        DrawFilledCircle(spriteBatch, pixelTexture, screenX, screenY, innerR,
            new Color(130, 120, 110) * 0.55f);

        // Highlight (top-left crescent)
        DrawFilledCircle(spriteBatch, pixelTexture,
            screenX - Radius / 5, screenY - Radius / 5,
            Math.Max(2, Radius / 4),
            Color.White * 0.12f);

        // HP bar only when damaged
        if (healthPct < 0.99f)
        {
            int barW = Radius * 2;
            int barX = screenX - Radius;
            int barY = screenY - Radius - 6;
            spriteBatch.Draw(pixelTexture, new Rectangle(barX, barY, barW, 3), new Color(80, 0, 0));
            spriteBatch.Draw(pixelTexture, new Rectangle(barX, barY, (int)(barW * healthPct), 3), Color.OrangeRed);
        }
    }

    // ── helpers ─────────────────────────────────────────────────────────────

    private static float Lerp(float a, float b, float t) => a + (b - a) * t;

    private static void DrawFilledCircle(SpriteBatch sb, Texture2D px, int cx, int cy, int r, Color color)
    {
        for (int y = -r; y <= r; y++)
        {
            int halfW = (int)MathF.Sqrt(Math.Max(0f, r * r - y * y));
            sb.Draw(px, new Rectangle(cx - halfW, cy + y, halfW * 2, 1), color);
        }
    }

    private static void FillTriangle(SpriteBatch sb, Texture2D px,
        int x0, int y0, int x1, int y1, int x2, int y2, Color color)
    {
        // Scan-line fill — sort vertices by Y
        if (y0 > y1) { (x0, x1) = (x1, x0); (y0, y1) = (y1, y0); }
        if (y0 > y2) { (x0, x2) = (x2, x0); (y0, y2) = (y2, y0); }
        if (y1 > y2) { (x1, x2) = (x2, x1); (y1, y2) = (y2, y1); }

        int totalHeight = y2 - y0;
        if (totalHeight == 0) return;

        for (int scanY = y0; scanY <= y2; scanY++)
        {
            bool secondHalf = scanY > y1;
            int segmentHeight = secondHalf ? y2 - y1 : y1 - y0;
            if (segmentHeight == 0) continue;

            float alpha = (float)(scanY - y0) / totalHeight;
            float beta  = secondHalf
                ? (float)(scanY - y1) / segmentHeight
                : (float)(scanY - y0) / segmentHeight;

            int lx = x0 + (int)((x2 - x0) * alpha);
            int rx = secondHalf ? x1 + (int)((x2 - x1) * beta) : x0 + (int)((x1 - x0) * beta);

            if (lx > rx) (lx, rx) = (rx, lx);
            int width = rx - lx;
            if (width > 0)
                sb.Draw(px, new Rectangle(lx, scanY, width, 1), color);
        }
    }
}
