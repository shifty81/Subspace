using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;

namespace Subspace;

/// <summary>
/// Stats for a ship component
/// </summary>
public class ComponentStats
{
    public string Name { get; set; } = "";
    public int Health { get; set; }
    public int MaxHealth { get; set; }
    public int PowerConsumption { get; set; }
    public int PowerGeneration { get; set; }
    public float Thrust { get; set; }
    public Color Color { get; set; }

    public ComponentStats(string name, int health, int maxHealth, int powerConsumption,
                         int powerGeneration = 0, float thrust = 0f, Color? color = null)
    {
        Name = name;
        Health = health;
        MaxHealth = maxHealth;
        PowerConsumption = powerConsumption;
        PowerGeneration = powerGeneration;
        Thrust = thrust;
        Color = color ?? new Color(100, 100, 100);
    }
}

/// <summary>
/// Component type definitions
/// </summary>
public static class ComponentType
{
    public const string CORE = "core";
    public const string ENGINE = "engine";
    public const string WEAPON_LASER = "weapon_laser";
    public const string WEAPON_CANNON = "weapon_cannon";
    public const string ARMOR = "armor";
    public const string POWER = "power";
    public const string SHIELD = "shield";
}

/// <summary>
/// Base class for ship components
/// </summary>
public class Component
{
    public string ComponentType { get; set; }
    public int GridX { get; set; }
    public int GridY { get; set; }
    public ComponentStats Stats { get; set; }
    public float Cooldown { get; set; }
    public float Rotation { get; set; }

    public Component(string componentType, int gridX, int gridY)
    {
        ComponentType = componentType;
        GridX = gridX;
        GridY = gridY;
        Stats = GetStats(componentType);
        Cooldown = 0f;
        Rotation = 0f;
    }

    private ComponentStats GetStats(string componentType)
    {
        return componentType switch
        {
            Subspace.ComponentType.CORE => new ComponentStats(
                "Core", 200, 200, 0, 50, 0f, new Color(255, 200, 0)),
            Subspace.ComponentType.ENGINE => new ComponentStats(
                "Engine", 50, 50, 10, 0, 200f, new Color(0, 150, 255)),
            Subspace.ComponentType.WEAPON_LASER => new ComponentStats(
                "Laser", 40, 40, 15, 0, 0f, new Color(255, 0, 0)),
            Subspace.ComponentType.WEAPON_CANNON => new ComponentStats(
                "Cannon", 60, 60, 20, 0, 0f, new Color(150, 150, 0)),
            Subspace.ComponentType.ARMOR => new ComponentStats(
                "Armor", 150, 150, 0, 0, 0f, new Color(150, 150, 150)),
            Subspace.ComponentType.POWER => new ComponentStats(
                "Reactor", 80, 80, 0, 100, 0f, new Color(0, 255, 100)),
            Subspace.ComponentType.SHIELD => new ComponentStats(
                "Shield", 30, 30, 25, 0, 0f, new Color(100, 200, 255)),
            _ => new ComponentStats("Unknown", 50, 50, 0)
        };
    }

    public bool TakeDamage(int damage)
    {
        Stats.Health -= damage;
        if (Stats.Health <= 0)
        {
            Stats.Health = 0;
            return true;
        }
        return false;
    }

    public void Update(float dt)
    {
        if (Cooldown > 0)
            Cooldown -= dt;
    }

    public bool CanFire()
    {
        return (ComponentType == Subspace.ComponentType.WEAPON_LASER || 
                ComponentType == Subspace.ComponentType.WEAPON_CANNON) &&
               Cooldown <= 0 && Stats.Health > 0;
    }

    public void Fire()
    {
        if (ComponentType == Subspace.ComponentType.WEAPON_LASER)
            Cooldown = 0.5f;  // 2 shots per second
        else if (ComponentType == Subspace.ComponentType.WEAPON_CANNON)
            Cooldown = 1.5f;  // Slower but more powerful
    }

    public void Render(SpriteBatch spriteBatch, Texture2D pixelTexture, int x, int y, int gridSize)
    {
        // Base color
        Color color = Stats.Color;

        // Damage indicator (darken based on health)
        float healthPercent = (float)Stats.Health / Stats.MaxHealth;
        color = new Color(
            (int)(color.R * (0.3f + 0.7f * healthPercent)),
            (int)(color.G * (0.3f + 0.7f * healthPercent)),
            (int)(color.B * (0.3f + 0.7f * healthPercent))
        );

        // Draw component background
        Rectangle rect = new Rectangle(x, y, gridSize - 2, gridSize - 2);
        spriteBatch.Draw(pixelTexture, rect, color);

        // Draw border
        DrawRectangle(spriteBatch, pixelTexture, rect, Color.White, 1);

        // Draw type indicator
        int centerX = x + gridSize / 2;
        int centerY = y + gridSize / 2;

        if (ComponentType == Subspace.ComponentType.CORE)
        {
            DrawCircle(spriteBatch, pixelTexture, centerX, centerY, 8, Color.Yellow);
        }
        else if (ComponentType == Subspace.ComponentType.ENGINE)
        {
            DrawTriangle(spriteBatch, pixelTexture, centerX, centerY, 8, Color.White);
        }
        else if (ComponentType == Subspace.ComponentType.WEAPON_LASER || 
                 ComponentType == Subspace.ComponentType.WEAPON_CANNON)
        {
            DrawCircle(spriteBatch, pixelTexture, centerX, centerY, 4, Color.Red);
            DrawLine(spriteBatch, pixelTexture, centerX, centerY, centerX, centerY - 10, 2, Color.Red);
        }
    }

    private void DrawRectangle(SpriteBatch spriteBatch, Texture2D texture, Rectangle rect, Color color, int thickness)
    {
        // Top
        spriteBatch.Draw(texture, new Rectangle(rect.X, rect.Y, rect.Width, thickness), color);
        // Bottom
        spriteBatch.Draw(texture, new Rectangle(rect.X, rect.Y + rect.Height - thickness, rect.Width, thickness), color);
        // Left
        spriteBatch.Draw(texture, new Rectangle(rect.X, rect.Y, thickness, rect.Height), color);
        // Right
        spriteBatch.Draw(texture, new Rectangle(rect.X + rect.Width - thickness, rect.Y, thickness, rect.Height), color);
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

    private void DrawTriangle(SpriteBatch spriteBatch, Texture2D texture, int centerX, int centerY, int size, Color color)
    {
        // Simple triangle approximation
        for (int y = 0; y < size; y++)
        {
            int width = (size - y) / 2;
            for (int x = -width; x <= width; x++)
            {
                spriteBatch.Draw(texture, new Rectangle(centerX + x, centerY - size / 2 + y, 1, 1), color);
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
