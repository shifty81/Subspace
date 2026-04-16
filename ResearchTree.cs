using System.Collections.Generic;

namespace Subspace;

/// <summary>
/// A single node in the research tree.
/// Unlocked = player has completed it; available = prerequisites met.
/// </summary>
public class ResearchNode
{
    public string Id { get; }
    public string DisplayName { get; }
    public string Description { get; }
    /// <summary>Research-points cost to unlock.</summary>
    public int Cost { get; }
    public bool Unlocked { get; set; }
    public List<string> Prerequisites { get; } = new();

    public ResearchNode(string id, string displayName, string description, int cost, params string[] prerequisites)
    {
        Id = id;
        DisplayName = displayName;
        Description = description;
        Cost = cost;
        foreach (var p in prerequisites)
            Prerequisites.Add(p);
    }

    public bool IsAvailable(ResearchTree tree) =>
        !Unlocked && Prerequisites.TrueForAll(p => tree.IsUnlocked(p));
}

public class ResearchTree
{
    private readonly Dictionary<string, ResearchNode> _nodes = new();
    public float AccumulatedPoints { get; set; }

    public static class Ids
    {
        // Tier 0 (always available)
        public const string BASIC_HULL          = "basic_hull";
        public const string BASIC_ENGINES       = "basic_engines";
        // Tier 1
        public const string IMPROVED_ENGINES    = "improved_engines";
        public const string BETTER_ARMOR        = "better_armor";
        public const string SHIELD_TECH         = "shield_tech";
        // Tier 2
        public const string ADVANCED_WEAPONS    = "advanced_weapons";
        public const string LIFE_SUPPORT        = "life_support";
        public const string DOCKING_TECH        = "docking_tech";
        // Tier 3
        public const string PLANETARY_LANDING   = "planetary_landing";
        public const string BOARDING_TUBES      = "boarding_tubes";
    }

    public ResearchTree()
    {
        Add(Ids.BASIC_HULL,       "Basic Hull",         "Unlock hull plating for ship construction.", 0);
        Add(Ids.BASIC_ENGINES,    "Basic Engines",      "Standard thruster technology.",               0);
        Add(Ids.IMPROVED_ENGINES, "Improved Engines",   "+25 % thrust from engine components.",        150, Ids.BASIC_ENGINES);
        Add(Ids.BETTER_ARMOR,     "Reinforced Armor",   "+50 % armor HP.",                             200, Ids.BASIC_HULL);
        Add(Ids.SHIELD_TECH,      "Shield Technology",  "Unlock shield generators.",                   250, Ids.BASIC_HULL);
        Add(Ids.ADVANCED_WEAPONS, "Advanced Weapons",   "Unlock missile launchers.",                   300, Ids.SHIELD_TECH);
        Add(Ids.LIFE_SUPPORT,     "Life Support",       "Unlock O₂ recyclers and water recyclers.",   200, Ids.BASIC_HULL);
        Add(Ids.DOCKING_TECH,     "Docking Technology", "Allows docking with space stations.",         350, Ids.LIFE_SUPPORT);
        Add(Ids.PLANETARY_LANDING,"Planetary Landing",  "Allows landing on planet surfaces.",          400, Ids.DOCKING_TECH);
        Add(Ids.BOARDING_TUBES,   "Boarding Tubes",     "Forcibly dock and board enemy ships.",        500, Ids.DOCKING_TECH, Ids.ADVANCED_WEAPONS);

        // Tier 0 unlocked by default
        _nodes[Ids.BASIC_HULL].Unlocked = true;
        _nodes[Ids.BASIC_ENGINES].Unlocked = true;
    }

    private void Add(string id, string name, string desc, int cost, params string[] prereqs)
        => _nodes[id] = new ResearchNode(id, name, desc, cost, prereqs);

    public bool IsUnlocked(string id) => _nodes.TryGetValue(id, out var n) && n.Unlocked;

    public IEnumerable<ResearchNode> AllNodes => _nodes.Values;

    /// <summary>
    /// Spends accumulated points to unlock a node.
    /// Returns true on success.
    /// </summary>
    public bool TryUnlock(string id)
    {
        if (!_nodes.TryGetValue(id, out var node)) return false;
        if (!node.IsAvailable(this)) return false;
        if (AccumulatedPoints < node.Cost) return false;

        AccumulatedPoints -= node.Cost;
        node.Unlocked = true;
        return true;
    }

    /// <summary>Adds research points (from crew working at terminals, etc.).</summary>
    public void AddPoints(float pts) => AccumulatedPoints += pts;
}
