using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;

namespace Subspace;

/// <summary>
/// A single star in the starfield
/// </summary>
public class Star
{
    public float X { get; set; }
    public float Y { get; set; }
    public float Size { get; set; }
    public int Brightness { get; set; }
    public float Depth { get; set; }
    public float TwinkleOffset { get; set; }

    public Star(float x, float y, float size, int brightness, float depth)
    {
        X = x;
        Y = y;
        Size = size;
        Brightness = brightness;
        Depth = depth;
        TwinkleOffset = (float)(new Random().NextDouble() * Math.PI * 2);
    }
}

/// <summary>
/// Enhanced starfield with parallax scrolling
/// </summary>
public class Starfield
{
    private const float TWINKLE_SPEED = 2.0f;
    private const float TWINKLE_AMPLITUDE = 0.2f;
    private const float TWINKLE_BASE = 0.8f;

    private List<List<Star>> layers = new List<List<Star>>();
    private Random random = new Random();

    // Layer properties (depth, density, minSize, maxSize, minBrightness, maxBrightness, parallaxFactor)
    private readonly (float depth, int count, float minSize, float maxSize, int minBrightness, int maxBrightness, float parallaxFactor)[] layerConfigs = 
    {
        (0.2f, 150, 1f, 1f, 80, 120, 0.2f),    // Far layer
        (0.5f, 100, 1f, 2f, 120, 180, 0.5f),   // Mid layer
        (0.8f, 50, 2f, 3f, 180, 255, 1.0f),    // Near layer
    };

    public Starfield()
    {
        GenerateStarfield();
    }

    private void GenerateStarfield()
    {
        foreach (var config in layerConfigs)
        {
            var layer = new List<Star>();
            for (int i = 0; i < config.count; i++)
            {
                float x = (float)random.NextDouble() * Config.SCREEN_WIDTH * 3;
                float y = (float)random.NextDouble() * Config.SCREEN_HEIGHT * 3;
                float size = config.minSize + (float)random.NextDouble() * (config.maxSize - config.minSize);
                int brightness = config.minBrightness + random.Next(config.maxBrightness - config.minBrightness);
                var star = new Star(x, y, size, brightness, config.depth);
                layer.Add(star);
            }
            layers.Add(layer);
        }
    }

    public void Render(SpriteBatch spriteBatch, Texture2D pixelTexture, float cameraX, float cameraY, float gameTime)
    {
        for (int layerIdx = 0; layerIdx < layers.Count; layerIdx++)
        {
            var layer = layers[layerIdx];
            var config = layerConfigs[layerIdx];

            foreach (var star in layer)
            {
                // Apply parallax based on depth
                float parallaxX = cameraX * config.parallaxFactor;
                float parallaxY = cameraY * config.parallaxFactor;

                // Calculate screen position with wrapping
                float worldWidth = Config.SCREEN_WIDTH * 3;
                float worldHeight = Config.SCREEN_HEIGHT * 3;

                float screenX = (star.X - parallaxX) % worldWidth;
                float screenY = (star.Y - parallaxY) % worldHeight;

                if (screenX < 0) screenX += worldWidth;
                if (screenY < 0) screenY += worldHeight;

                // Only draw if on screen (with margin)
                if (screenX >= -50 && screenX <= Config.SCREEN_WIDTH + 50 && 
                    screenY >= -50 && screenY <= Config.SCREEN_HEIGHT + 50)
                {
                    // Add subtle twinkling effect
                    float twinkle = (float)Math.Sin(gameTime * TWINKLE_SPEED + star.TwinkleOffset) * TWINKLE_AMPLITUDE + TWINKLE_BASE;
                    int brightness = (int)(star.Brightness * twinkle);
                    brightness = Math.Clamp(brightness, 0, 255);

                    Color color = new Color(brightness, brightness, brightness);

                    if (star.Size > 1)
                    {
                        // Draw larger stars as circles
                        DrawCircle(spriteBatch, pixelTexture, (int)screenX, (int)screenY, (int)star.Size, color);
                    }
                    else
                    {
                        // Draw small stars as single pixels
                        spriteBatch.Draw(pixelTexture, new Rectangle((int)screenX, (int)screenY, 1, 1), color);
                    }
                }
            }
        }
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
}
