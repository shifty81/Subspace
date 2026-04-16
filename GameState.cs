using System.Collections.Generic;

namespace Subspace;

/// <summary>
/// Singleton that persists across scene transitions and acts as the authoritative
/// source of truth for the whole game run (ship, crew, inventory, research, etc.).
/// This is also the core of the eventual save-file.
/// </summary>
public class GameState
{
    // ── Singleton ────────────────────────────────────────────────────────────
    private static GameState? _instance;
    public static GameState Instance => _instance ??= new GameState();

    public static void Reset() => _instance = new GameState();

    // ── Ship ─────────────────────────────────────────────────────────────────
    /// <summary>The single player ship — exists for the whole run.</summary>
    public Ship? PlayerShip { get; set; }

    // ── Universe ─────────────────────────────────────────────────────────────
    public SectorMap SectorMap { get; } = new SectorMap();

    // ── Economy / Resources ──────────────────────────────────────────────────
    public Inventory Inventory { get; } = new Inventory();

    // ── Research ─────────────────────────────────────────────────────────────
    public ResearchTree Research { get; } = new ResearchTree();

    // ── Story flags ───────────────────────────────────────────────────────────
    private readonly HashSet<string> _storyFlags = new();

    public void SetFlag(string flag)   => _storyFlags.Add(flag);
    public void ClearFlag(string flag) => _storyFlags.Remove(flag);
    public bool HasFlag(string flag)   => _storyFlags.Contains(flag);

    // ── Run stats ────────────────────────────────────────────────────────────
    public int Wave    { get; set; } = 1;
    public int Score   { get; set; }
    public int Kills   { get; set; }
    public float GameTime { get; set; }

    // ── Constructor ──────────────────────────────────────────────────────────
    private GameState() { }

    /// <summary>
    /// Creates (or resets to) a fresh player ship at the given world position.
    /// Called once during game initialisation.
    /// </summary>
    public Ship InitPlayerShip(float x, float y)
    {
        PlayerShip = new Ship(x, y, 0, isPlayer: true);
        return PlayerShip;
    }
}
