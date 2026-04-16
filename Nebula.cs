using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;

namespace Subspace;

/// <summary>
/// A single nebula cloud
/// </summary>
public class NebulaCloud
{
    public float X { get; set; }
    public float Y { get; set; }
    public float Size { get; set; }
    public Color Color { get; set; }
    public float Rotation { get; set; }
    public float Depth { get; set; }
    public float ParallaxFactor { get; set; }

    public NebulaCloud(float x, float y, float size, Color color, float rotation, float depth, float parallaxFactor)
    {
        X = x;
        Y = y;
        Size = size;
        Color = color;
        Rotation = rotation;
        Depth = depth;
        ParallaxFactor = parallaxFactor;
    }
}

/// <summary>
/// A positioned nebula texture in the world
/// </summary>
public class NebulaTextureCloud
{
    public Texture2D Texture { get; set; }
    public float X { get; set; }
    public float Y { get; set; }
    public float ParallaxFactor { get; set; }
    public float Alpha { get; set; }

    public NebulaTextureCloud(Texture2D texture, float x, float y, float parallaxFactor, float alpha)
    {
        Texture = texture;
        X = x;
        Y = y;
        ParallaxFactor = parallaxFactor;
        Alpha = alpha;
    }
}

/// <summary>
/// Procedural nebula system for space background
/// </summary>
public class NebulaSystem
{
    private List<NebulaCloud> clouds = new List<NebulaCloud>();
    private List<NebulaTextureCloud> textureClouds = new List<NebulaTextureCloud>();
    private Random random = new Random();

    private readonly float worldWidth = Config.SCREEN_WIDTH * 4f;
    private readonly float worldHeight = Config.SCREEN_HEIGHT * 4f;
    
    // Color schemes for different nebula types
    private readonly Color[][] nebulaColorSchemes = 
    {
        // Blue nebula
        new Color[] { new Color(30, 50, 100, 60), new Color(50, 80, 150, 80), new Color(70, 100, 180, 50) },
        // Purple nebula
        new Color[] { new Color(80, 30, 100, 60), new Color(120, 50, 150, 80), new Color(100, 40, 120, 50) },
        // Green nebula
        new Color[] { new Color(20, 80, 50, 60), new Color(30, 120, 80, 80), new Color(40, 100, 60, 50) },
        // Red nebula
        new Color[] { new Color(100, 30, 40, 60), new Color(150, 50, 60, 80), new Color(120, 40, 50, 50) },
    };

    public NebulaSystem()
    {
        GenerateNebulas();
    }

    private void GenerateNebulas()
    {
        // Generate multiple nebula clouds at different depths
        for (int layer = 0; layer < 3; layer++)
        {
            float depth = 0.1f + layer * 0.15f;
            float parallaxFactor = 0.1f + layer * 0.1f;
            int numClouds = 8 - layer * 2; // Fewer clouds in closer layers

            var colorScheme = nebulaColorSchemes[random.Next(nebulaColorSchemes.Length)];

            for (int i = 0; i < numClouds; i++)
            {
                float x = (float)random.NextDouble() * worldWidth;
                float y = (float)random.NextDouble() * worldHeight;
                float size = 200f + (float)random.NextDouble() * 400f + layer * 200f;
                Color color = colorScheme[random.Next(colorScheme.Length)];
                float rotation = (float)random.NextDouble() * (float)Math.PI * 2;

                clouds.Add(new NebulaCloud(x, y, size, color, rotation, depth, parallaxFactor));
            }
        }
    }

    /// <summary>
    /// Load nebula textures and scatter them across the world
    /// </summary>
    public void LoadTextures(Texture2D[] nebulaTextures)
    {
        textureClouds.Clear();
        if (nebulaTextures == null || nebulaTextures.Length == 0)
            return;

        // Place several nebula textures at different positions and parallax depths
        int count = nebulaTextures.Length * 2;
        for (int i = 0; i < count; i++)
        {
            float x = (float)random.NextDouble() * worldWidth;
            float y = (float)random.NextDouble() * worldHeight;
            float parallaxFactor = 0.05f + (float)random.NextDouble() * 0.1f;
            float alpha = 0.25f + (float)random.NextDouble() * 0.25f;
            var texture = nebulaTextures[random.Next(nebulaTextures.Length)];
            textureClouds.Add(new NebulaTextureCloud(texture, x, y, parallaxFactor, alpha));
        }
    }

    public void Render(SpriteBatch spriteBatch, Texture2D pixelTexture, float cameraX, float cameraY)
    {
        // Draw texture-based nebulae first (deepest layer)
        foreach (var tc in textureClouds)
        {
            float parallaxX = cameraX * tc.ParallaxFactor;
            float parallaxY = cameraY * tc.ParallaxFactor;

            float screenX = ((tc.X - parallaxX) % worldWidth + worldWidth) % worldWidth;
            float screenY = ((tc.Y - parallaxY) % worldHeight + worldHeight) % worldHeight;

            int drawWidth = tc.Texture.Width;
            int drawHeight = tc.Texture.Height;

            if (screenX >= -drawWidth && screenX <= Config.SCREEN_WIDTH &&
                screenY >= -drawHeight && screenY <= Config.SCREEN_HEIGHT)
            {
                spriteBatch.Draw(tc.Texture, new Rectangle((int)screenX, (int)screenY, drawWidth, drawHeight),
                    Color.White * tc.Alpha);
            }
        }

        foreach (var cloud in clouds)
        {
            // Apply parallax
            float parallaxX = cameraX * cloud.ParallaxFactor;
            float parallaxY = cameraY * cloud.ParallaxFactor;

            float screenX = ((cloud.X - parallaxX) % worldWidth + worldWidth) % worldWidth;
            float screenY = ((cloud.Y - parallaxY) % worldHeight + worldHeight) % worldHeight;

            // Only render if visible (with large margin for cloud size)
            if (screenX >= -cloud.Size && screenX <= Config.SCREEN_WIDTH + cloud.Size &&
                screenY >= -cloud.Size && screenY <= Config.SCREEN_HEIGHT + cloud.Size)
            {
                DrawNebulaCloud(spriteBatch, pixelTexture, screenX, screenY, cloud.Size, cloud.Color, cloud.Rotation);
            }
        }
    }

    private void DrawNebulaCloud(SpriteBatch spriteBatch, Texture2D texture, float x, float y, float size, Color color, float rotation)
    {
        // Draw nebula as a soft circular gradient cloud
        int radius = (int)size / 2;
        float centerX = x;
        float centerY = y;

        // Draw multiple layers for soft gradient effect
        for (int layer = 5; layer >= 0; layer--)
        {
            float layerRadius = radius * (layer / 5f) * (layer / 5f); // Quadratic falloff
            float alpha = color.A * (1f - layer / 6f) * 0.3f; // Softer falloff
            Color layerColor = new Color(color.R, color.G, color.B, (int)alpha);

            DrawSoftCircle(spriteBatch, texture, (int)centerX, (int)centerY, (int)layerRadius, layerColor);
        }
    }

    private void DrawSoftCircle(SpriteBatch spriteBatch, Texture2D texture, int centerX, int centerY, int radius, Color color)
    {
        // Only draw if on screen
        if (centerX + radius < 0 || centerX - radius > Config.SCREEN_WIDTH ||
            centerY + radius < 0 || centerY - radius > Config.SCREEN_HEIGHT)
            return;

        // Draw filled circle with soft edges
        for (int y = -radius; y <= radius; y += 2) // Skip pixels for performance
        {
            for (int x = -radius; x <= radius; x += 2)
            {
                float distance = (float)Math.Sqrt(x * x + y * y);
                if (distance <= radius)
                {
                    // Soft edge falloff
                    float edgeFactor = 1f - (distance / radius);
                    edgeFactor = edgeFactor * edgeFactor; // Square for smoother falloff
                    Color pixelColor = color * edgeFactor;

                    int drawX = centerX + x;
                    int drawY = centerY + y;

                    if (drawX >= 0 && drawX < Config.SCREEN_WIDTH &&
                        drawY >= 0 && drawY < Config.SCREEN_HEIGHT)
                    {
                        spriteBatch.Draw(texture, new Rectangle(drawX, drawY, 2, 2), pixelColor);
                    }
                }
            }
        }
    }
}
