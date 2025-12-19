using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Subspace;

/// <summary>
/// A spaceship made of modular components
/// </summary>
public class Ship
{
    public float X { get; set; }
    public float Y { get; set; }
    public int ShipId { get; set; }
    public bool IsPlayer { get; set; }
    public float Angle { get; set; }
    public float VX { get; set; }
    public float VY { get; set; }
    public float AngularVelocity { get; set; }

    public List<Component> Components { get; set; } = new List<Component>();
    public int GridWidth { get; set; } = 10;
    public int GridHeight { get; set; } = 10;

    public int TotalHealth { get; private set; }
    public int MaxHealth { get; private set; }
    public int PowerAvailable { get; private set; }
    public int PowerUsed { get; private set; }
    public float TotalThrust { get; private set; }
    
    public CrewManager? CrewManager { get; private set; }

    public Ship? Target { get; set; }
    public string AIState { get; set; } = "idle";

    public Ship(float x, float y, int shipId, bool isPlayer = false)
    {
        X = x;
        Y = y;
        ShipId = shipId;
        IsPlayer = isPlayer;
        Angle = 0f;
        VX = 0f;
        VY = 0f;
        AngularVelocity = 0f;

        // Create default ship layout
        if (isPlayer)
            CreatePlayerShip();
        else
            CreateEnemyShip();

        RecalculateStats();
        
        // Initialize crew
        CrewManager = new CrewManager(this);
        // Start with 5 crew members for player, 3 for enemies
        int crewCount = isPlayer ? 5 : 3;
        CrewManager.AddCrew(crewCount, x, y);
    }

    private void CreatePlayerShip()
    {
        // Core in center
        Components.Add(new Component(ComponentType.CORE, 4, 4));

        // Engines
        Components.Add(new Component(ComponentType.ENGINE, 4, 6));
        Components.Add(new Component(ComponentType.ENGINE, 4, 7));

        // Weapons
        Components.Add(new Component(ComponentType.WEAPON_LASER, 3, 3));
        Components.Add(new Component(ComponentType.WEAPON_LASER, 5, 3));
        Components.Add(new Component(ComponentType.WEAPON_CANNON, 4, 2));

        // Power
        Components.Add(new Component(ComponentType.POWER, 3, 5));
        Components.Add(new Component(ComponentType.POWER, 5, 5));

        // Armor
        Components.Add(new Component(ComponentType.ARMOR, 3, 4));
        Components.Add(new Component(ComponentType.ARMOR, 5, 4));
        Components.Add(new Component(ComponentType.ARMOR, 4, 5));
    }

    private void CreateEnemyShip()
    {
        // Smaller, simpler enemy ship
        Components.Add(new Component(ComponentType.CORE, 4, 4));
        Components.Add(new Component(ComponentType.ENGINE, 4, 6));
        Components.Add(new Component(ComponentType.WEAPON_LASER, 4, 3));
        Components.Add(new Component(ComponentType.POWER, 3, 4));
        Components.Add(new Component(ComponentType.ARMOR, 5, 4));
    }

    private void RecalculateStats()
    {
        TotalHealth = 0;
        MaxHealth = 0;
        PowerAvailable = 0;
        PowerUsed = 0;
        TotalThrust = 0;

        foreach (var comp in Components)
        {
            TotalHealth += comp.Stats.Health;
            MaxHealth += comp.Stats.MaxHealth;
            PowerAvailable += comp.Stats.PowerGeneration;
            PowerUsed += comp.Stats.PowerConsumption;
            TotalThrust += comp.Stats.Thrust;
        }
    }

    public void AddComponent(Component component)
    {
        Components.Add(component);
        RecalculateStats();
    }

    public void RemoveComponent(int gridX, int gridY)
    {
        Components.RemoveAll(c => c.GridX == gridX && c.GridY == gridY);
        RecalculateStats();
    }

    public Component? GetComponentAt(int gridX, int gridY)
    {
        return Components.FirstOrDefault(c => c.GridX == gridX && c.GridY == gridY);
    }

    public void Update(float dt, Ship? target = null)
    {
        // Update all components
        foreach (var comp in Components)
            comp.Update(dt);

        // Update crew
        CrewManager?.Update(dt);

        // AI control
        if (!IsPlayer && target != null)
        {
            Target = target;
            UpdateAI(dt);
        }

        // Apply drag
        VX *= Config.DRAG;
        VY *= Config.DRAG;
        AngularVelocity *= Config.DRAG;

        // Limit velocity
        float speed = (float)Math.Sqrt(VX * VX + VY * VY);
        if (speed > Config.MAX_VELOCITY)
        {
            VX = (VX / speed) * Config.MAX_VELOCITY;
            VY = (VY / speed) * Config.MAX_VELOCITY;
        }

        // Update position
        X += VX * dt;
        Y += VY * dt;
        Angle += AngularVelocity * dt;

        // Keep angle in range
        Angle = Angle % (float)(2 * Math.PI);

        // Recalculate stats
        RecalculateStats();
    }

    private void UpdateAI(float dt)
    {
        if (Target == null)
            return;

        // Calculate direction to target
        float dx = Target.X - X;
        float dy = Target.Y - Y;
        float distance = (float)Math.Sqrt(dx * dx + dy * dy);

        if (distance < 10)
            return;

        float targetAngle = (float)Math.Atan2(dy, dx);

        // Rotate towards target
        float angleDiff = targetAngle - Angle;
        // Normalize angle difference to [-pi, pi]
        while (angleDiff > Math.PI)
            angleDiff -= (float)(2 * Math.PI);
        while (angleDiff < -Math.PI)
            angleDiff += (float)(2 * Math.PI);

        // Apply rotation
        float rotationSpeed = 2.0f;
        if (Math.Abs(angleDiff) > 0.1f)
            AngularVelocity = rotationSpeed * (angleDiff > 0 ? 1 : -1);
        else
            AngularVelocity = 0;

        // Move forward if facing target
        if (Math.Abs(angleDiff) < 0.5f)
        {
            // Keep distance
            float optimalDistance = 300f;
            if (distance > optimalDistance)
                ApplyThrust(dt);
        }
    }

    public void ApplyThrust(float dt, ParticleSystem? particles = null)
    {
        if (TotalThrust > 0 && PowerAvailable >= PowerUsed)
        {
            float thrustForce = TotalThrust * dt;
            VX += (float)Math.Cos(Angle) * thrustForce;
            VY += (float)Math.Sin(Angle) * thrustForce;
            
            // Create engine thrust particles
            if (particles != null)
            {
                var engines = Components.Where(c => c.ComponentType == ComponentType.ENGINE && c.Stats.Health > 0);
                foreach (var engine in engines)
                {
                    // Calculate engine position in world space
                    float localX = (engine.GridX - GridWidth / 2f) * Config.GRID_SIZE;
                    float localY = (engine.GridY - GridHeight / 2f) * Config.GRID_SIZE;
                    
                    float cosAngle = (float)Math.Cos(-Angle);
                    float sinAngle = (float)Math.Sin(-Angle);
                    float rotatedX = localX * cosAngle - localY * sinAngle;
                    float rotatedY = localX * sinAngle + localY * cosAngle;
                    
                    float engineX = X + rotatedX;
                    float engineY = Y + rotatedY;
                    
                    particles.CreateEngineTrust(engineX, engineY, Angle, TotalThrust / 1000f);
                }
            }
        }
    }

    public void Rotate(int direction, float dt)
    {
        float rotationSpeed = 3.0f;
        AngularVelocity += direction * rotationSpeed * dt;
    }

    public List<Projectile> FireWeapons()
    {
        var projectiles = new List<Projectile>();

        foreach (var comp in Components)
        {
            if (comp.CanFire() && PowerAvailable >= PowerUsed)
            {
                comp.Fire();

                // Calculate projectile spawn position (in world space)
                float localX = (comp.GridX - GridWidth / 2f) * Config.GRID_SIZE;
                float localY = (comp.GridY - GridHeight / 2f) * Config.GRID_SIZE;

                // Rotate by ship angle
                float rotatedX = localX * (float)Math.Cos(Angle) - localY * (float)Math.Sin(Angle);
                float rotatedY = localX * (float)Math.Sin(Angle) + localY * (float)Math.Cos(Angle);

                float spawnX = X + rotatedX;
                float spawnY = Y + rotatedY;

                // Create projectile
                string projType = comp.ComponentType == ComponentType.WEAPON_LASER ? "laser" : "cannon";
                int damage = projType == "laser" ? 10 : 25;
                float speed = projType == "laser" ? 500f : 350f;

                var projectile = new Projectile(spawnX, spawnY, Angle, speed, damage, projType, ShipId);
                projectiles.Add(projectile);
            }
        }

        return projectiles;
    }

    public void TakeDamage(int damage, float hitX, float hitY)
    {
        // Convert world position to local grid position
        float localX = hitX - X;
        float localY = hitY - Y;

        // Rotate by inverse of ship angle
        float angle = -Angle;
        float rotatedX = localX * (float)Math.Cos(angle) - localY * (float)Math.Sin(angle);
        float rotatedY = localX * (float)Math.Sin(angle) + localY * (float)Math.Cos(angle);

        // Convert to grid coordinates
        int gridX = (int)((rotatedX / Config.GRID_SIZE) + GridWidth / 2f);
        int gridY = (int)((rotatedY / Config.GRID_SIZE) + GridHeight / 2f);

        // Find component at position
        var comp = GetComponentAt(gridX, gridY);
        if (comp != null)
        {
            bool destroyed = comp.TakeDamage(damage);
            if (destroyed)
                Components.Remove(comp);
        }

        RecalculateStats();
    }

    public bool IsDestroyed()
    {
        return !Components.Any(c => c.ComponentType == ComponentType.CORE);
    }

    public Rectangle GetBounds()
    {
        if (Components.Count == 0)
            return new Rectangle((int)X, (int)Y, 1, 1);

        int minX = Components.Min(c => c.GridX);
        int maxX = Components.Max(c => c.GridX);
        int minY = Components.Min(c => c.GridY);
        int maxY = Components.Max(c => c.GridY);

        int width = (maxX - minX + 1) * Config.GRID_SIZE;
        int height = (maxY - minY + 1) * Config.GRID_SIZE;

        // Approximate center position
        float centerOffsetX = (minX + maxX) / 2f - GridWidth / 2f;
        float centerOffsetY = (minY + maxY) / 2f - GridHeight / 2f;

        return new Rectangle(
            (int)(X + centerOffsetX * Config.GRID_SIZE - width / 2f),
            (int)(Y + centerOffsetY * Config.GRID_SIZE - height / 2f),
            width, height
        );
    }

    public void Render(SpriteBatch spriteBatch, Texture2D pixelTexture, RenderTarget2D? shipSurface, float cameraX, float cameraY, GraphicsDevice graphicsDevice)
    {
        int screenX = (int)(X - cameraX);
        int screenY = (int)(Y - cameraY);

        // Create a temporary surface for the ship
        int shipWidth = GridWidth * Config.GRID_SIZE;
        int shipHeight = GridHeight * Config.GRID_SIZE;

        if (shipSurface == null)
            shipSurface = new RenderTarget2D(graphicsDevice, shipWidth, shipHeight);

        // End the current spriteBatch before changing render targets
        spriteBatch.End();

        // Render components on temporary surface
        graphicsDevice.SetRenderTarget(shipSurface);
        graphicsDevice.Clear(Color.Transparent);

        spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend);
        foreach (var comp in Components)
        {
            int compX = comp.GridX * Config.GRID_SIZE;
            int compY = comp.GridY * Config.GRID_SIZE;
            comp.Render(spriteBatch, pixelTexture, compX, compY, Config.GRID_SIZE);
        }
        spriteBatch.End();

        // Reset render target
        graphicsDevice.SetRenderTarget(null);

        // Restart spriteBatch for drawing to main surface
        spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend);

        // Draw rotated ship on main surface
        spriteBatch.Draw(
            shipSurface,
            new Vector2(screenX, screenY),
            null,
            Color.White,
            -Angle,  // MonoGame rotates clockwise, so negate
            new Vector2(shipWidth / 2f, shipHeight / 2f),
            1.0f,
            SpriteEffects.None,
            0
        );
        
        // Draw crew members on top of ship
        CrewManager?.Render(spriteBatch, pixelTexture, cameraX, cameraY);
    }
}
