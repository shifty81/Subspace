using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;

namespace Subspace;

/// <summary>
/// A homing missile that steers toward its target each frame.
/// Inherits from Projectile but overrides Update to apply a turn rate.
/// </summary>
public class Missile : Projectile
{
    // World-space target position; null once the target is destroyed or lost.
    public Ship? Target { get; set; }

    private float _speed;
    private const float TURN_RATE = 2.8f;   // radians per second

    public Missile(float x, float y, float angle, float speed, int damage, int ownerId, Ship? target)
        : base(x, y, angle, speed, damage, "missile", ownerId)
    {
        Target = target;
        _speed = speed;
        Lifetime = 5.0f;   // missiles have a longer lifetime
    }

    public new void Update(float dt)
    {
        if (!Alive) return;

        // Home toward target if it still exists and isn't destroyed
        if (Target != null && !Target.IsDestroyed())
        {
            float dx = Target.X - X;
            float dy = Target.Y - Y;
            float targetAngle = MathF.Atan2(dy, dx);

            // Compute shortest angular distance
            float diff = targetAngle - Angle;
            diff = ((diff + MathF.PI) % MathF.Tau) - MathF.PI;

            float turn = Math.Clamp(diff, -TURN_RATE * dt, TURN_RATE * dt);
            Angle += turn;
        }

        // Re-derive velocity from current angle every frame
        float vx = MathF.Cos(Angle) * _speed;
        float vy = MathF.Sin(Angle) * _speed;

        X += vx * dt;
        Y += vy * dt;

        Lifetime -= dt;
        if (Lifetime <= 0f)
            Alive = false;
    }

    public new void Render(SpriteBatch spriteBatch, Texture2D pixelTexture, float cameraX, float cameraY)
    {
        if (!Alive) return;

        int screenX = (int)(X - cameraX);
        int screenY = (int)(Y - cameraY);

        // Cyan exhaust trail
        int trailLen = 18;
        float trailAlpha = Math.Clamp(Lifetime / 5.0f, 0.1f, 1.0f);
        int ex = screenX - (int)(MathF.Cos(Angle) * trailLen);
        int ey = screenY - (int)(MathF.Sin(Angle) * trailLen);
        DrawLine(spriteBatch, pixelTexture, ex, ey, screenX, screenY, 4, new Color(0, 200, 255) * (trailAlpha * 0.25f));
        DrawLine(spriteBatch, pixelTexture, ex, ey, screenX, screenY, 2, new Color(100, 240, 255) * (trailAlpha * 0.5f));

        // White missile body — a short line along the heading
        int bx = screenX + (int)(MathF.Cos(Angle) * 8);
        int by = screenY + (int)(MathF.Sin(Angle) * 8);
        DrawLine(spriteBatch, pixelTexture, screenX - (int)(MathF.Cos(Angle) * 5),
                                             screenY - (int)(MathF.Sin(Angle) * 5),
                                             bx, by, 3, Color.White);

        // Bright cyan tip
        DrawCircle(spriteBatch, pixelTexture, bx, by, 3, new Color(0, 220, 255));
        DrawCircle(spriteBatch, pixelTexture, bx, by, 1, Color.White);
    }

    private static void DrawCircle(SpriteBatch sb, Texture2D tex, int cx, int cy, int r, Color c)
    {
        for (int dy = -r; dy <= r; dy++)
            for (int dx = -r; dx <= r; dx++)
                if (dx * dx + dy * dy <= r * r)
                    sb.Draw(tex, new Rectangle(cx + dx, cy + dy, 1, 1), c);
    }

    private static void DrawLine(SpriteBatch sb, Texture2D tex, int x1, int y1, int x2, int y2, int thickness, Color c)
    {
        int ddx = x2 - x1, ddy = y2 - y1;
        float dist = MathF.Sqrt(ddx * ddx + ddy * ddy);
        float angle = MathF.Atan2(ddy, ddx);
        sb.Draw(tex, new Rectangle(x1, y1, (int)dist, thickness),
                null, c, angle, Vector2.Zero, SpriteEffects.None, 0);
    }
}
