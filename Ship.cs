using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Subspace;

/// <summary>Enemy archetype that determines starting layout and stats.</summary>
public enum EnemyType { Scout, Gunship, Support }

/// <summary>Per-weapon-type readiness summary for the HUD. Ready = 1.0, just fired = 0.0. Count = -1 means no weapons of that type.</summary>
public record struct WeaponSummary(int LaserCount, float LaserReady, int CannonCount, float CannonReady, int MissileCount, float MissileReady);

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

    // Enemy type (only meaningful for non-player ships)
    public EnemyType EnemyType { get; private set; }

    // Optional pre-rendered sprite for enemies (overrides component-based rendering)
    public Texture2D? PrerenderedTexture { get; set; }

    // Cached render target for ship surface (reused across frames)
    private RenderTarget2D? _shipSurface;

    // Shield / damage state
    /// <summary>Set to true for one frame whenever a hit is absorbed by shields.</summary>
    public bool LastHitWasShielded { get; private set; }
    private float _damageCooldown = 0f;
    private const float DAMAGE_COOLDOWN_DURATION = 2.0f;
    private const float SHIELD_REGEN_RATE = 3f;  // HP per second per shield component

    // AI strafing
    private int _strafeDirection = 1;
    private float _strafeTimer = 0f;
    private const float STRAFE_SWITCH_INTERVAL = 2.5f;

    public Ship(float x, float y, int shipId, bool isPlayer = false, EnemyType enemyType = EnemyType.Scout)
    {
        X = x;
        Y = y;
        ShipId = shipId;
        IsPlayer = isPlayer;
        EnemyType = enemyType;
        Angle = 0f;
        VX = 0f;
        VY = 0f;
        AngularVelocity = 0f;

        // Create default ship layout
        if (isPlayer)
            CreatePlayerShip();
        else
            CreateEnemyShip(enemyType);

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

        // Weapons (lasers + cannon + a single missile bay)
        Components.Add(new Component(ComponentType.WEAPON_LASER, 3, 3));
        Components.Add(new Component(ComponentType.WEAPON_LASER, 5, 3));
        Components.Add(new Component(ComponentType.WEAPON_CANNON, 4, 2));
        Components.Add(new Component(ComponentType.WEAPON_MISSILE, 4, 3));

        // Power
        Components.Add(new Component(ComponentType.POWER, 3, 5));
        Components.Add(new Component(ComponentType.POWER, 5, 5));

        // Armor
        Components.Add(new Component(ComponentType.ARMOR, 3, 4));
        Components.Add(new Component(ComponentType.ARMOR, 5, 4));
        Components.Add(new Component(ComponentType.ARMOR, 4, 5));
    }

    private void CreateEnemyShip(EnemyType type = EnemyType.Scout)
    {
        switch (type)
        {
            case EnemyType.Gunship: CreateGunship(); break;
            case EnemyType.Support: CreateSupport(); break;
            default:                CreateScout();   break;
        }
    }

    /// <summary>Fast, lightly-armed interceptor — 3 engines, single laser.</summary>
    private void CreateScout()
    {
        Components.Add(new Component(ComponentType.CORE, 4, 4));
        Components.Add(new Component(ComponentType.ENGINE, 3, 6));
        Components.Add(new Component(ComponentType.ENGINE, 5, 6));
        Components.Add(new Component(ComponentType.ENGINE, 4, 7));
        Components.Add(new Component(ComponentType.WEAPON_LASER, 4, 3));
        Components.Add(new Component(ComponentType.POWER, 4, 5));
    }

    /// <summary>Heavily armoured with dual cannons — slow but lethal.</summary>
    private void CreateGunship()
    {
        Components.Add(new Component(ComponentType.CORE, 4, 4));
        Components.Add(new Component(ComponentType.ENGINE, 4, 7));
        Components.Add(new Component(ComponentType.WEAPON_CANNON, 3, 2));
        Components.Add(new Component(ComponentType.WEAPON_CANNON, 5, 2));
        Components.Add(new Component(ComponentType.WEAPON_LASER, 4, 3));
        Components.Add(new Component(ComponentType.POWER, 3, 5));
        Components.Add(new Component(ComponentType.POWER, 5, 5));
        Components.Add(new Component(ComponentType.ARMOR, 3, 3));
        Components.Add(new Component(ComponentType.ARMOR, 5, 3));
        Components.Add(new Component(ComponentType.ARMOR, 3, 4));
        Components.Add(new Component(ComponentType.ARMOR, 5, 4));
    }

    /// <summary>Shield-heavy support vessel — resilient but offensively modest.</summary>
    private void CreateSupport()
    {
        Components.Add(new Component(ComponentType.CORE, 4, 4));
        Components.Add(new Component(ComponentType.ENGINE, 4, 6));
        Components.Add(new Component(ComponentType.WEAPON_LASER, 4, 3));
        Components.Add(new Component(ComponentType.POWER, 3, 4));
        Components.Add(new Component(ComponentType.POWER, 5, 4));
        Components.Add(new Component(ComponentType.SHIELD, 3, 3));
        Components.Add(new Component(ComponentType.SHIELD, 5, 3));
        Components.Add(new Component(ComponentType.SHIELD, 4, 5));
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

    /// <summary>
    /// Returns weapon readiness for the HUD.
    /// Ready value of 1.0 = fully charged, 0.0 = just fired.
    /// Count of -1 means no weapons of that type are alive.
    /// </summary>
    public WeaponSummary GetWeaponSummary()
    {
        const float LASER_MAX_CD   = 0.5f;
        const float CANNON_MAX_CD  = 1.5f;
        const float MISSILE_MAX_CD = 3.0f;

        var lasers   = Components.Where(c => c.ComponentType == ComponentType.WEAPON_LASER   && c.Stats.Health > 0).ToList();
        var cannons  = Components.Where(c => c.ComponentType == ComponentType.WEAPON_CANNON  && c.Stats.Health > 0).ToList();
        var missiles = Components.Where(c => c.ComponentType == ComponentType.WEAPON_MISSILE && c.Stats.Health > 0).ToList();

        float laserReady   = lasers.Count   > 0 ? (float)lasers.Average(c   => Math.Clamp(1f - c.Cooldown / LASER_MAX_CD,   0f, 1f)) : -1f;
        float cannonReady  = cannons.Count  > 0 ? (float)cannons.Average(c  => Math.Clamp(1f - c.Cooldown / CANNON_MAX_CD,  0f, 1f)) : -1f;
        float missileReady = missiles.Count > 0 ? (float)missiles.Average(c => Math.Clamp(1f - c.Cooldown / MISSILE_MAX_CD, 0f, 1f)) : -1f;

        return new WeaponSummary(lasers.Count, laserReady, cannons.Count, cannonReady, missiles.Count, missileReady);
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

    public void Update(float dt, Ship? target = null, ParticleSystem? particles = null)
    {
        // Reset per-frame flags
        LastHitWasShielded = false;

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

        // Shield recharge (only when not recently hit)
        _damageCooldown -= dt;
        if (_damageCooldown <= 0f)
        {
            foreach (var shield in Components)
            {
                if (shield.ComponentType == ComponentType.SHIELD &&
                    shield.Stats.Health > 0 &&
                    shield.Stats.Health < shield.Stats.MaxHealth)
                {
                    shield.Stats.Health = Math.Min(
                        shield.Stats.MaxHealth,
                        shield.Stats.Health + (int)(SHIELD_REGEN_RATE * dt));
                }
            }
        }

        // Damage smoke: heavily-damaged components emit occasional smoke
        if (particles != null)
        {
            foreach (var comp in Components)
            {
                float hp = (float)comp.Stats.Health / Math.Max(1, comp.Stats.MaxHealth);
                if (hp < 0.4f && hp > 0f)
                {
                    // Probability scales with damage severity (~1 puff/sec at 20% HP)
                    float chance = (0.4f - hp) * 2f * dt;
                    if (Random.Shared.NextSingle() < chance)
                    {
                        float localX = (comp.GridX - GridWidth / 2f) * Config.GRID_SIZE;
                        float localY = (comp.GridY - GridHeight / 2f) * Config.GRID_SIZE;
                        float cosA = MathF.Cos(Angle), sinA = MathF.Sin(Angle);
                        float wx = X + localX * cosA - localY * sinA;
                        float wy = Y + localX * sinA + localY * cosA;
                        particles.CreateDamageSmoke(wx, wy);
                    }
                }
            }
        }

        // AI strafe timer
        _strafeTimer -= dt;
        if (_strafeTimer <= 0f)
        {
            _strafeTimer = STRAFE_SWITCH_INTERVAL;
            _strafeDirection = -_strafeDirection;
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

        float dx = Target.X - X;
        float dy = Target.Y - Y;
        float distance = MathF.Sqrt(dx * dx + dy * dy);
        if (distance < 1f) return;

        float targetAngle = MathF.Atan2(dy, dx);

        // Rotate towards target
        float angleDiff = targetAngle - Angle;
        angleDiff = ((angleDiff + MathF.PI) % MathF.Tau) - MathF.PI;

        float rotationSpeed = 2.5f;
        AngularVelocity = MathF.Abs(angleDiff) > 0.05f
            ? rotationSpeed * (angleDiff > 0 ? 1 : -1)
            : 0f;

        // Flee when below 25% health
        float hpPct = MaxHealth > 0 ? (float)TotalHealth / MaxHealth : 1f;
        if (hpPct < 0.25f)
        {
            // Flee away from target
            if (MathF.Abs(angleDiff) < 1.0f)
                ApplyThrust(dt);
            return;
        }

        const float OPTIMAL_DISTANCE = 280f;
        const float TOO_CLOSE        = 180f;

        if (distance > OPTIMAL_DISTANCE + 60f)
        {
            // Too far — close in while roughly facing the target
            if (MathF.Abs(angleDiff) < 0.6f)
                ApplyThrust(dt);
        }
        else if (distance < TOO_CLOSE)
        {
            // Too close — back away
            ApplyReverseThrust(dt);
        }
        else
        {
            // In combat range — strafe perpendicular to the target direction
            if (MathF.Abs(angleDiff) < 0.4f)
            {
                float perpAngle = targetAngle + MathF.PI / 2f * _strafeDirection;
                float thrustForce = TotalThrust * dt * 0.6f;
                VX += MathF.Cos(perpAngle) * thrustForce;
                VY += MathF.Sin(perpAngle) * thrustForce;
            }
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
                    
                    particles.CreateEngineThrust(engineX, engineY, Angle, TotalThrust / 1000f);
                }
            }
        }
    }

    public void ApplyReverseThrust(float dt, ParticleSystem? particles = null)
    {
        if (TotalThrust > 0 && PowerAvailable >= PowerUsed)
        {
            // Apply 70% thrust in reverse direction
            float thrustForce = TotalThrust * dt * 0.7f;
            VX -= (float)Math.Cos(Angle) * thrustForce;
            VY -= (float)Math.Sin(Angle) * thrustForce;
            
            // Create reverse thrust particles from front of ship
            if (particles != null)
            {
                var engines = Components.Where(c => c.ComponentType == ComponentType.ENGINE && c.Stats.Health > 0);
                foreach (var engine in engines)
                {
                    // Calculate position in front of ship for reverse thrust
                    float localX = (engine.GridX - GridWidth / 2f) * Config.GRID_SIZE;
                    float localY = (engine.GridY - GridHeight / 2f) * Config.GRID_SIZE;
                    
                    float cosAngle = (float)Math.Cos(-Angle);
                    float sinAngle = (float)Math.Sin(-Angle);
                    float rotatedX = localX * cosAngle - localY * sinAngle;
                    float rotatedY = localX * sinAngle + localY * cosAngle;
                    
                    float engineX = X + rotatedX;
                    float engineY = Y + rotatedY;
                    
                    // Particles go forward (opposite of ship movement)
                    particles.CreateEngineThrust(engineX, engineY, Angle + (float)Math.PI, TotalThrust / 1500f);
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

                float localX = (comp.GridX - GridWidth / 2f) * Config.GRID_SIZE;
                float localY = (comp.GridY - GridHeight / 2f) * Config.GRID_SIZE;
                float rotatedX = localX * (float)Math.Cos(Angle) - localY * (float)Math.Sin(Angle);
                float rotatedY = localX * (float)Math.Sin(Angle) + localY * (float)Math.Cos(Angle);
                float spawnX = X + rotatedX;
                float spawnY = Y + rotatedY;

                if (comp.ComponentType == ComponentType.WEAPON_MISSILE)
                {
                    // Unguided forward missile when no target
                    var missile = new Missile(spawnX, spawnY, Angle, 260f, 60, ShipId, Target);
                    projectiles.Add(missile);
                }
                else
                {
                    string projType = comp.ComponentType == ComponentType.WEAPON_LASER ? "laser" : "cannon";
                    int damage = projType == "laser" ? 10 : 25;
                    float speed = projType == "laser" ? 500f : 350f;
                    projectiles.Add(new Projectile(spawnX, spawnY, Angle, speed, damage, projType, ShipId));
                }
            }
        }

        return projectiles;
    }

    public List<Projectile> FireWeaponsAtTarget(Vector2 targetPosition, Ship? targetShip = null)
    {
        var projectiles = new List<Projectile>();

        foreach (var comp in Components)
        {
            if (comp.CanFire() && PowerAvailable >= PowerUsed)
            {
                comp.Fire();

                float localX = (comp.GridX - GridWidth / 2f) * Config.GRID_SIZE;
                float localY = (comp.GridY - GridHeight / 2f) * Config.GRID_SIZE;
                float rotatedX = localX * (float)Math.Cos(Angle) - localY * (float)Math.Sin(Angle);
                float rotatedY = localX * (float)Math.Sin(Angle) + localY * (float)Math.Cos(Angle);
                float spawnX = X + rotatedX;
                float spawnY = Y + rotatedY;

                if (comp.ComponentType == ComponentType.WEAPON_MISSILE)
                {
                    // Guided missile toward the target ship
                    float dx0 = targetPosition.X - spawnX;
                    float dy0 = targetPosition.Y - spawnY;
                    float launchAngle = MathF.Atan2(dy0, dx0);
                    var missile = new Missile(spawnX, spawnY, launchAngle, 260f, 60, ShipId, targetShip);
                    projectiles.Add(missile);
                }
                else
                {
                    float dx = targetPosition.X - spawnX;
                    float dy = targetPosition.Y - spawnY;
                    float targetAngle = (float)Math.Atan2(dy, dx);
                    string projType = comp.ComponentType == ComponentType.WEAPON_LASER ? "laser" : "cannon";
                    int damage = projType == "laser" ? 10 : 25;
                    float speed = projType == "laser" ? 500f : 350f;
                    projectiles.Add(new Projectile(spawnX, spawnY, targetAngle, speed, damage, projType, ShipId));
                }
            }
        }

        return projectiles;
    }

    /// <summary>Set to true for one frame whenever the last hit was a critical (landed on CORE or POWER).</summary>
    public bool LastHitWasCritical { get; private set; }

    public void TakeDamage(int damage, float hitX, float hitY)
    {
        // Reset per-frame flags
        LastHitWasCritical = false;

        // Reset damage cooldown (blocks shield regen)
        _damageCooldown = DAMAGE_COOLDOWN_DURATION;

        // Shield absorption: each functional shield reduces damage by 20%, capped at 70%
        int shieldCount = Components.Count(c =>
            c.ComponentType == ComponentType.SHIELD && c.Stats.Health > 0);
        if (shieldCount > 0)
        {
            float absorption = Math.Min(0.70f, shieldCount * 0.20f);
            damage = Math.Max(1, (int)(damage * (1f - absorption)));
            LastHitWasShielded = true;
        }

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
            // Critical hit: landing on CORE or POWER deals double damage
            if (comp.ComponentType == ComponentType.CORE || comp.ComponentType == ComponentType.POWER)
            {
                damage = (int)(damage * 2.0f);
                LastHitWasCritical = true;
            }

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

    /// <summary>
    /// Pre-renders the ship's components onto <see cref="_shipSurface"/> using a temporary
    /// render target.  Must be called for every ship <b>before</b> the main draw pass begins
    /// so that the render-target switch never clears the backbuffer mid-frame.
    /// </summary>
    public void PreRender(SpriteBatch spriteBatch, Texture2D pixelTexture, GraphicsDevice graphicsDevice, float gameTime, Dictionary<string, Texture2D>? componentTextures = null)
    {
        // Ships that use a pre-assigned sprite don't need a render target.
        if (PrerenderedTexture != null) return;

        int shipWidth  = GridWidth  * Config.GRID_SIZE;
        int shipHeight = GridHeight * Config.GRID_SIZE;

        // Reuse or recreate the cached render target when size changes.
        if (_shipSurface == null || _shipSurface.IsDisposed ||
            _shipSurface.Width != shipWidth || _shipSurface.Height != shipHeight)
        {
            _shipSurface?.Dispose();
            _shipSurface = new RenderTarget2D(graphicsDevice, shipWidth, shipHeight);
        }

        graphicsDevice.SetRenderTarget(_shipSurface);
        graphicsDevice.Clear(Color.Transparent);

        spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend);
        foreach (var comp in Components)
        {
            int compX = comp.GridX * Config.GRID_SIZE;
            int compY = comp.GridY * Config.GRID_SIZE;
            comp.Render(spriteBatch, pixelTexture, compX, compY, Config.GRID_SIZE, gameTime, componentTextures);
        }
        spriteBatch.End();

        // Restore the backbuffer so the caller can proceed with normal drawing.
        graphicsDevice.SetRenderTarget(null);
    }

    /// <summary>
    /// Draws the ship onto the currently active <paramref name="spriteBatch"/>.
    /// <see cref="PreRender"/> must have been called this frame before the main
    /// draw pass starts — this method no longer switches render targets.
    /// </summary>
    public void Render(SpriteBatch spriteBatch, Texture2D pixelTexture, float cameraX, float cameraY, GraphicsDevice graphicsDevice, float zoom, float gameTime, Dictionary<string, Texture2D>? componentTextures = null)
    {
        int screenX = (int)(X - cameraX);
        int screenY = (int)(Y - cameraY);

        // If a pre-rendered sprite is assigned (e.g. for enemy ships), draw it directly.
        if (PrerenderedTexture != null)
        {
            spriteBatch.Draw(
                PrerenderedTexture,
                new Vector2(screenX, screenY),
                null,
                Color.White,
                -Angle,
                new Vector2(PrerenderedTexture.Width / 2f, PrerenderedTexture.Height / 2f),
                1.0f,
                SpriteEffects.None,
                0
            );
            return;
        }

        // _shipSurface is populated by PreRender(); skip silently if not ready.
        if (_shipSurface == null || _shipSurface.IsDisposed) return;

        int shipWidth  = _shipSurface.Width;
        int shipHeight = _shipSurface.Height;

        // Draw the rotated ship surface into the active spriteBatch.
        spriteBatch.Draw(
            _shipSurface,
            new Vector2(screenX, screenY),
            null,
            Color.White,
            -Angle,  // MonoGame rotates clockwise, so negate
            new Vector2(shipWidth / 2f, shipHeight / 2f),
            1.0f,
            SpriteEffects.None,
            0
        );

        // Draw crew members on top of the ship.
        CrewManager?.Render(spriteBatch, pixelTexture, cameraX, cameraY);
    }
}
