using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;

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
    public const string CREW_QUARTERS = "crew_quarters";
    public const string AMMO_FACTORY = "ammo_factory";
    public const string CORRIDOR = "corridor";
    public const string STRUCTURE = "structure";
    public const string ENGINE_ROOM = "engine_room";
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
            Subspace.ComponentType.CREW_QUARTERS => new ComponentStats(
                "Crew Quarters", 60, 60, 0, 0, 0f, new Color(180, 180, 255)),
            Subspace.ComponentType.AMMO_FACTORY => new ComponentStats(
                "Ammo Factory", 70, 70, 20, 0, 0f, new Color(200, 150, 50)),
            Subspace.ComponentType.CORRIDOR => new ComponentStats(
                "Corridor", 30, 30, 0, 0, 0f, new Color(100, 100, 100)),
            Subspace.ComponentType.STRUCTURE => new ComponentStats(
                "Structure", 40, 40, 0, 0, 0f, new Color(80, 80, 80)),
            Subspace.ComponentType.ENGINE_ROOM => new ComponentStats(
                "Engine Room", 80, 80, 15, 0, 0f, new Color(50, 100, 200)),
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

    private float GetHealthPercent()
    {
        return Stats.MaxHealth > 0 ? (float)Stats.Health / Stats.MaxHealth : 1.0f;
    }

    public void Render(SpriteBatch spriteBatch, Texture2D pixelTexture, int x, int y, int gridSize, Dictionary<string, Texture2D>? componentTextures = null)
    {
        // Get health percentage for damage indication
        float healthPercent = GetHealthPercent();
        
        // Try to use texture if available
        if (componentTextures != null && componentTextures.TryGetValue(ComponentType, out Texture2D? texture))
        {
            // Draw component texture
            Rectangle destRect = new Rectangle(x, y, gridSize - 2, gridSize - 2);
            
            // Damage indicator (darken based on health)
            Color tintColor = new Color(
                (int)(255 * (0.3f + 0.7f * healthPercent)),
                (int)(255 * (0.3f + 0.7f * healthPercent)),
                (int)(255 * (0.3f + 0.7f * healthPercent))
            );
            
            spriteBatch.Draw(texture, destRect, tintColor);
            return;
        }
        
        // Fallback to original rendering if texture not available
        // Base color
        Color baseColor = Stats.Color;
        
        // Draw component with gradient effect (darker at edges, lighter in center)
        Rectangle rect = new Rectangle(x, y, gridSize - 2, gridSize - 2);
        
        // Draw shadow/depth effect
        Rectangle shadowRect = new Rectangle(x + 2, y + 2, gridSize - 2, gridSize - 2);
        spriteBatch.Draw(pixelTexture, shadowRect, Color.Black * 0.3f);
        
        // Draw gradient layers for depth
        for (int layer = 3; layer >= 0; layer--)
        {
            float layerFactor = layer / 3f;
            Color layerColor = new Color(
                (int)(baseColor.R * (0.3f + 0.7f * healthPercent) * (0.6f + 0.4f * layerFactor)),
                (int)(baseColor.G * (0.3f + 0.7f * healthPercent) * (0.6f + 0.4f * layerFactor)),
                (int)(baseColor.B * (0.3f + 0.7f * healthPercent) * (0.6f + 0.4f * layerFactor))
            );
            
            Rectangle layerRect = new Rectangle(
                x + layer, 
                y + layer, 
                gridSize - 2 - layer * 2, 
                gridSize - 2 - layer * 2
            );
            spriteBatch.Draw(pixelTexture, layerRect, layerColor);
        }
        
        // Draw glowing border for active components
        Color borderColor = healthPercent > 0.7f ? new Color(255, 255, 255, 200) : 
                           healthPercent > 0.3f ? new Color(255, 200, 100, 180) : 
                           new Color(255, 100, 100, 160);
        DrawRectangle(spriteBatch, pixelTexture, rect, borderColor, 1);
        
        // Add inner highlight for depth
        Rectangle highlightRect = new Rectangle(x + 3, y + 3, gridSize - 8, gridSize - 8);
        DrawRectangle(spriteBatch, pixelTexture, highlightRect, Color.White * 0.2f, 1);

        // Draw type indicator with glow
        int centerX = x + gridSize / 2;
        int centerY = y + gridSize / 2;

        if (ComponentType == Subspace.ComponentType.CORE)
        {
            // Core with glow effect
            DrawCircle(spriteBatch, pixelTexture, centerX, centerY, 10, Color.Yellow * 0.3f);
            DrawCircle(spriteBatch, pixelTexture, centerX, centerY, 8, Color.Yellow);
            DrawCircle(spriteBatch, pixelTexture, centerX, centerY, 4, Color.White);
        }
        else if (ComponentType == Subspace.ComponentType.ENGINE)
        {
            // Engine with thrust indicator
            DrawTriangle(spriteBatch, pixelTexture, centerX, centerY, 10, new Color(100, 150, 255));
            DrawTriangle(spriteBatch, pixelTexture, centerX, centerY, 7, Color.White);
        }
        else if (ComponentType == Subspace.ComponentType.WEAPON_LASER)
        {
            // Laser weapon with red glow
            DrawCircle(spriteBatch, pixelTexture, centerX, centerY, 6, Color.Red * 0.4f);
            DrawCircle(spriteBatch, pixelTexture, centerX, centerY, 4, Color.Red);
            DrawLine(spriteBatch, pixelTexture, centerX, centerY, centerX, centerY - 12, 3, Color.Red * 0.5f);
            DrawLine(spriteBatch, pixelTexture, centerX, centerY, centerX, centerY - 10, 2, Color.Red);
        }
        else if (ComponentType == Subspace.ComponentType.WEAPON_CANNON)
        {
            // Cannon weapon with orange glow
            DrawCircle(spriteBatch, pixelTexture, centerX, centerY, 7, Color.Orange * 0.4f);
            DrawCircle(spriteBatch, pixelTexture, centerX, centerY, 5, Color.Orange);
            DrawRectangle(spriteBatch, pixelTexture, new Rectangle(centerX - 2, centerY - 14, 4, 12), Color.Orange * 0.6f, 1);
            DrawRectangle(spriteBatch, pixelTexture, new Rectangle(centerX - 1, centerY - 13, 2, 11), Color.Yellow, 1);
        }
        else if (ComponentType == Subspace.ComponentType.POWER)
        {
            // Reactor with energy glow
            DrawCircle(spriteBatch, pixelTexture, centerX, centerY, 9, Color.Green * 0.4f);
            DrawCircle(spriteBatch, pixelTexture, centerX, centerY, 6, Color.Green);
            DrawCircle(spriteBatch, pixelTexture, centerX, centerY, 3, Color.White);
            // Energy bolts
            DrawLine(spriteBatch, pixelTexture, centerX - 7, centerY, centerX - 3, centerY, 1, Color.LimeGreen);
            DrawLine(spriteBatch, pixelTexture, centerX + 3, centerY, centerX + 7, centerY, 1, Color.LimeGreen);
        }
        else if (ComponentType == Subspace.ComponentType.SHIELD)
        {
            // Shield with cyan glow
            DrawCircle(spriteBatch, pixelTexture, centerX, centerY, 10, Color.Cyan * 0.3f);
            DrawCircle(spriteBatch, pixelTexture, centerX, centerY, 7, Color.Cyan * 0.6f);
            DrawCircle(spriteBatch, pixelTexture, centerX, centerY, 5, Color.White);
        }
        else if (ComponentType == Subspace.ComponentType.ARMOR)
        {
            // Armor plates pattern
            DrawRectangle(spriteBatch, pixelTexture, new Rectangle(centerX - 6, centerY - 6, 12, 12), Color.Gray, 2);
            DrawRectangle(spriteBatch, pixelTexture, new Rectangle(centerX - 4, centerY - 4, 8, 8), Color.White * 0.5f, 1);
        }
        else if (ComponentType == Subspace.ComponentType.CREW_QUARTERS)
        {
            // Crew quarters with beds
            DrawRectangle(spriteBatch, pixelTexture, new Rectangle(centerX - 8, centerY - 4, 6, 3), Color.White, 1);
            DrawRectangle(spriteBatch, pixelTexture, new Rectangle(centerX + 2, centerY - 4, 6, 3), Color.White, 1);
            DrawRectangle(spriteBatch, pixelTexture, new Rectangle(centerX - 8, centerY + 2, 6, 3), Color.White, 1);
            DrawRectangle(spriteBatch, pixelTexture, new Rectangle(centerX + 2, centerY + 2, 6, 3), Color.White, 1);
        }
        else if (ComponentType == Subspace.ComponentType.AMMO_FACTORY)
        {
            // Ammo factory with crate symbol
            DrawRectangle(spriteBatch, pixelTexture, new Rectangle(centerX - 6, centerY - 6, 12, 12), Color.Orange, 2);
            DrawLine(spriteBatch, pixelTexture, centerX - 6, centerY, centerX + 6, centerY, 1, Color.Yellow);
            DrawLine(spriteBatch, pixelTexture, centerX, centerY - 6, centerX, centerY + 6, 1, Color.Yellow);
        }
        else if (ComponentType == Subspace.ComponentType.CORRIDOR)
        {
            // Corridor - simple arrows showing flow
            DrawLine(spriteBatch, pixelTexture, centerX - 8, centerY, centerX + 8, centerY, 2, Color.White * 0.6f);
            DrawLine(spriteBatch, pixelTexture, centerX + 4, centerY - 3, centerX + 8, centerY, 1, Color.White);
            DrawLine(spriteBatch, pixelTexture, centerX + 4, centerY + 3, centerX + 8, centerY, 1, Color.White);
        }
        else if (ComponentType == Subspace.ComponentType.STRUCTURE)
        {
            // Structure - simple frame
            DrawRectangle(spriteBatch, pixelTexture, new Rectangle(centerX - 8, centerY - 8, 16, 16), Color.Gray * 0.5f, 1);
            DrawLine(spriteBatch, pixelTexture, centerX - 8, centerY - 8, centerX + 8, centerY + 8, 1, Color.Gray * 0.5f);
            DrawLine(spriteBatch, pixelTexture, centerX + 8, centerY - 8, centerX - 8, centerY + 8, 1, Color.Gray * 0.5f);
        }
        else if (ComponentType == Subspace.ComponentType.ENGINE_ROOM)
        {
            // Engine room with gear symbol
            DrawCircle(spriteBatch, pixelTexture, centerX, centerY, 8, Color.Blue * 0.4f);
            DrawCircle(spriteBatch, pixelTexture, centerX, centerY, 6, new Color(50, 100, 200));
            // Gear teeth
            for (int i = 0; i < 8; i++)
            {
                float angle = i * (float)Math.PI / 4;
                int x1 = centerX + (int)(Math.Cos(angle) * 6);
                int y1 = centerY + (int)(Math.Sin(angle) * 6);
                int x2 = centerX + (int)(Math.Cos(angle) * 9);
                int y2 = centerY + (int)(Math.Sin(angle) * 9);
                DrawLine(spriteBatch, pixelTexture, x1, y1, x2, y2, 2, Color.White);
            }
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
