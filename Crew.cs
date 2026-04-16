using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Subspace;

// ── Needs & skill definitions ─────────────────────────────────────────────────

/// <summary>Job categories for crew work-priority system (RimWorld-style).</summary>
public enum JobCategory
{
    Firefighting = 0,
    Medicine     = 1,
    Engineering  = 2,
    Research     = 3,
    Cooking      = 4,
    Hauling      = 5,
    Cleaning     = 6,
    Defence      = 7,
}

/// <summary>Skill areas for a crew member.</summary>
public class CrewSkills
{
    public int Combat      { get; set; } = 1;
    public int Engineering { get; set; } = 1;
    public int Medicine    { get; set; } = 1;
    public int Research    { get; set; } = 1;
    public int Crafting    { get; set; } = 1;
    public int Social      { get; set; } = 1;

    public int Get(JobCategory job) => job switch
    {
        JobCategory.Medicine    => Medicine,
        JobCategory.Engineering => Engineering,
        JobCategory.Research    => Research,
        JobCategory.Defence     => Combat,
        _                       => 1,
    };
}

/// <summary>Emotional state severity levels.</summary>
public enum MoodLevel { Fine, Unhappy, Breakdown }

// ── CrewMember ────────────────────────────────────────────────────────────────

/// <summary>
/// Represents a single crew member.
/// Expanded with RimWorld-style needs (hunger, rest, mood) and work priorities.
/// </summary>
public class CrewMember
{
    // ── Identity ──────────────────────────────────────────────────────────────
    public int Id { get; set; }
    public string Name { get; set; }
    public Color CrewColor { get; set; }
    public bool IsPrisoner { get; set; }

    // ── Spatial (world-space pixels) ─────────────────────────────────────────
    public float X { get; set; }
    public float Y { get; set; }

    // ── Legacy ship-component assignment ─────────────────────────────────────
    public Component? AssignedComponent { get; set; }
    public Component? TargetComponent   { get; set; }
    public float WorkProgress { get; set; }
    public string State { get; set; } = "idle"; // idle, walking, working

    // ── Vitals ────────────────────────────────────────────────────────────────
    public int   Health  { get; set; } = 100;
    public int   MaxHealth { get; set; } = 100;

    /// <summary>0 = starving, 1 = full.  Drains at ~0.03/min.</summary>
    public float Hunger  { get; set; } = 1f;

    /// <summary>0 = exhausted, 1 = rested.  Drains at ~0.02/min when awake.</summary>
    public float Rest    { get; set; } = 1f;

    /// <summary>0 = breakdown, 1 = ecstatic.</summary>
    public float Mood    { get; set; } = 0.8f;

    public MoodLevel MoodLevel => Mood switch
    {
        < 0.2f => MoodLevel.Breakdown,
        < 0.5f => MoodLevel.Unhappy,
        _      => MoodLevel.Fine,
    };

    // ── Work priorities ───────────────────────────────────────────────────────
    /// <summary>Lower index = higher priority.  Initialised to default order.</summary>
    public List<JobCategory> WorkPriorities { get; set; } = Enum.GetValues<JobCategory>().ToList();

    // ── Skills ────────────────────────────────────────────────────────────────
    public CrewSkills Skills { get; } = new();

    // ── Work speed modifier (affected by mood) ────────────────────────────────
    public float WorkSpeedFactor => MoodLevel switch
    {
        MoodLevel.Breakdown => 0.3f,
        MoodLevel.Unhappy   => 0.7f,
        _                   => 1.0f,
    };

    // ── Constants ─────────────────────────────────────────────────────────────
    private const float HUNGER_DRAIN_PER_SEC = 0.03f / 60f;   // ~0.03 per minute
    private const float REST_DRAIN_PER_SEC   = 0.02f / 60f;   // ~0.02 per minute
    private const float MOVE_SPEED           = 80f;            // pixels/sec

    // ── Constructor ───────────────────────────────────────────────────────────

    public CrewMember(int id, float x, float y, string? name = null)
    {
        Id = id;
        X  = x;
        Y  = y;
        Name = name ?? GenerateName(id);

        var rand = new Random(id * 31337 + 7);
        CrewColor = new Color(150 + rand.Next(100), 150 + rand.Next(100), 150 + rand.Next(100));
        Hunger = 0.7f + (float)rand.NextDouble() * 0.3f;
        Rest   = 0.6f + (float)rand.NextDouble() * 0.4f;

        // Randomise skill spread
        Skills.Combat      = 1 + rand.Next(5);
        Skills.Engineering = 1 + rand.Next(5);
        Skills.Medicine    = 1 + rand.Next(5);
        Skills.Research    = 1 + rand.Next(5);
        Skills.Crafting    = 1 + rand.Next(5);
        Skills.Social      = 1 + rand.Next(5);
    }

    private static readonly string[] _firstNames = { "Ark", "Brix", "Cass", "Del", "Emi", "Finn", "Gia", "Hal", "Iris", "Jett" };
    private static readonly string[] _lastNames  = { "Nova", "Vance", "Orr", "Drake", "Solari", "Quinn", "Rho", "Stax", "Wen", "Zane" };

    private static string GenerateName(int seed)
    {
        var r = new Random(seed + 999);
        return _firstNames[r.Next(_firstNames.Length)] + " " + _lastNames[r.Next(_lastNames.Length)];
    }

    // ── Update ────────────────────────────────────────────────────────────────

    public void Update(float dt, Ship ship)
    {
        // Needs decay
        Hunger = Math.Max(0f, Hunger - HUNGER_DRAIN_PER_SEC * dt);
        Rest   = Math.Max(0f, Rest   - REST_DRAIN_PER_SEC   * dt);

        // Mood: pushed by hunger / rest
        float moodTarget = (Hunger * 0.5f + Rest * 0.5f);
        Mood = Math.Clamp(Mood + (moodTarget - Mood) * 0.1f * dt, 0f, 1f);

        // Legacy component assignment movement
        if (TargetComponent != null && AssignedComponent != TargetComponent)
            MoveToComponent(dt, ship, TargetComponent);
        else if (AssignedComponent != null)
        {
            State = "working";
            WorkProgress += dt * WorkSpeedFactor;
        }
        else
            State = "idle";
    }

    private void MoveToComponent(float dt, Ship ship, Component target)
    {
        State = "walking";
        float targetX = ship.X + (target.GridX - ship.GridWidth  / 2f) * Config.GRID_SIZE;
        float targetY = ship.Y + (target.GridY - ship.GridHeight / 2f) * Config.GRID_SIZE;
        float dx = targetX - X, dy = targetY - Y;
        float dist = MathF.Sqrt(dx * dx + dy * dy);

        if (dist < 5f)
        {
            X = targetX; Y = targetY;
            AssignedComponent = target;
            TargetComponent   = null;
            State = "working";
        }
        else
        {
            X += dx / dist * MOVE_SPEED * dt;
            Y += dy / dist * MOVE_SPEED * dt;
        }
    }

    public void AssignTo(Component component)
    {
        TargetComponent = component;
        WorkProgress    = 0f;
    }

    // ── Render (space view — small dot on ship) ───────────────────────────────

    public void Render(SpriteBatch spriteBatch, Texture2D pixelTexture, float cameraX, float cameraY)
    {
        int sx = (int)(X - cameraX);
        int sy = (int)(Y - cameraY);

        int radius = 3;
        for (int dy = -radius; dy <= radius; dy++)
            for (int dx = -radius; dx <= radius; dx++)
                if (dx * dx + dy * dy <= radius * radius)
                    spriteBatch.Draw(pixelTexture, new Rectangle(sx + dx, sy + dy, 1, 1), CrewColor);

        Color stateColor = State switch { "working" => Color.Green, "walking" => Color.Yellow, _ => Color.Gray };
        spriteBatch.Draw(pixelTexture, new Rectangle(sx - 1, sy - 6, 2, 2), stateColor);
    }

    // ── Interior render (96-px view) ─────────────────────────────────────────

    /// <summary>
    /// Draws the crew member as a character sprite in the ship interior scene.
    /// <paramref name="camX"/> and <paramref name="camY"/> are interior camera offsets (pixels).
    /// </summary>
    public void RenderInterior(SpriteBatch sb, Texture2D pixel, float camX, float camY, bool isPlayer = false)
    {
        int sx = (int)(X - camX);
        int sy = (int)(Y - camY);

        // Body (rectangle)
        sb.Draw(pixel, new Rectangle(sx - 6, sy - 14, 12, 18), isPlayer ? Color.Lime : CrewColor);

        // Head
        sb.Draw(pixel, new Rectangle(sx - 4, sy - 22, 8, 8), isPlayer ? Color.LimeGreen : CrewColor);

        // Mood indicator dot above head
        Color moodCol = MoodLevel switch
        {
            MoodLevel.Fine    => Color.Green,
            MoodLevel.Unhappy => Color.Yellow,
            _                 => Color.Red,
        };
        sb.Draw(pixel, new Rectangle(sx - 2, sy - 28, 4, 4), moodCol);

        // Prisoner marker
        if (IsPrisoner)
            sb.Draw(pixel, new Rectangle(sx - 4, sy - 32, 8, 2), Color.Orange);
    }
}

/// <summary>
/// Manages all crew on a ship.
/// Expanded: exposes the crew list, adds a crew-by-name factory and
/// helper accessors used by the interior scene.
/// </summary>
public class CrewManager
{
    private readonly List<CrewMember> crew = new();
    private int nextCrewId = 0;
    private Ship ship;

    public IReadOnlyList<CrewMember> All => crew;

    public CrewManager(Ship ship)
    {
        this.ship = ship;
    }

    public CrewMember AddSingle(float shipX, float shipY, string? name = null)
    {
        var cm = new CrewMember(nextCrewId++, shipX, shipY, name);
        crew.Add(cm);
        return cm;
    }

    public void AddCrew(int count, float shipX, float shipY)
    {
        for (int i = 0; i < count; i++)
            AddSingle(shipX, shipY);
    }

    public void Update(float dt)
    {
        foreach (var crewMember in crew)
            crewMember.Update(dt, ship);

        AssignIdleCrew();
    }

    private void AssignIdleCrew()
    {
        var idleCrew = crew.Where(c => c.State == "idle" && c.AssignedComponent == null).ToList();

        foreach (var component in ship.Components)
        {
            if (component.ComponentType == ComponentType.POWER ||
                component.ComponentType == ComponentType.WEAPON_LASER ||
                component.ComponentType == ComponentType.WEAPON_CANNON ||
                component.ComponentType == ComponentType.ENGINE)
            {
                int assigned = crew.Count(c => c.AssignedComponent == component);
                int required = component.ComponentType == ComponentType.POWER ? 2 : 1;

                if (assigned < required && idleCrew.Count > 0)
                {
                    var crewToAssign = idleCrew[0];
                    crewToAssign.AssignTo(component);
                    idleCrew.RemoveAt(0);
                }
            }
        }
    }

    public void Render(SpriteBatch spriteBatch, Texture2D pixelTexture, float cameraX, float cameraY)
    {
        foreach (var crewMember in crew)
            crewMember.Render(spriteBatch, pixelTexture, cameraX, cameraY);
    }

    public int GetTotalCrew()   => crew.Count;
    public int GetIdleCrew()    => crew.Count(c => c.State == "idle");
    public int GetWorkingCrew() => crew.Count(c => c.State == "working");
}
