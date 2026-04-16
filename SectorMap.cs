using System.Collections.Generic;

namespace Subspace;

/// <summary>
/// What kind of place a sector node represents on the sector map.
/// </summary>
public enum SectorNodeType
{
    Empty,
    AsteroidBelt,
    Planet,
    Station,
    Nebula,
    DerelictShip,
}

/// <summary>
/// Biome of a planet node (only meaningful when NodeType == Planet).
/// </summary>
public enum PlanetBiome { Temperate, Arctic, Desert, Jungle, Toxic }

/// <summary>A single point-of-interest within a sector.</summary>
public class SectorNode
{
    public SectorNodeType NodeType { get; }
    public string Name { get; }
    public float X { get; }   // world-space position within sector
    public float Y { get; }
    public PlanetBiome Biome { get; }
    public int ThreatLevel { get; }

    public SectorNode(SectorNodeType type, string name, float x, float y,
                      PlanetBiome biome = PlanetBiome.Temperate, int threatLevel = 1)
    {
        NodeType = type;
        Name = name;
        X = x;
        Y = y;
        Biome = biome;
        ThreatLevel = threatLevel;
    }
}

/// <summary>A procedurally generated sector (chunk of the universe).</summary>
public class Sector
{
    public int ChunkX { get; }
    public int ChunkY { get; }
    public int ThreatLevel { get; }
    public List<SectorNode> Nodes { get; } = new();

    public Sector(int chunkX, int chunkY, int threatLevel)
    {
        ChunkX = chunkX;
        ChunkY = chunkY;
        ThreatLevel = threatLevel;
    }

    public string DisplayName => $"Sector ({ChunkX},{ChunkY})";
}

/// <summary>
/// Procedurally generates and caches sectors on demand as the player explores.
/// Sectors keyed by (chunkX, chunkY) — each chunk is a 2000×2000 world-unit area.
/// </summary>
public class SectorMap
{
    public const float SECTOR_SIZE = 2000f;
    private readonly Dictionary<(int, int), Sector> _sectors = new();
    private readonly System.Random _rand;

    public Sector CurrentSector { get; private set; }
    public int CurrentChunkX { get; private set; }
    public int CurrentChunkY { get; private set; }

    public SectorMap(int seed = 12345)
    {
        _rand = new System.Random(seed);
        CurrentChunkX = 0;
        CurrentChunkY = 0;
        CurrentSector = GetOrGenerate(0, 0);
    }

    public Sector GetOrGenerate(int cx, int cy)
    {
        if (!_sectors.TryGetValue((cx, cy), out var sector))
        {
            sector = Generate(cx, cy);
            _sectors[(cx, cy)] = sector;
        }
        return sector;
    }

    public void TravelTo(int cx, int cy)
    {
        CurrentChunkX = cx;
        CurrentChunkY = cy;
        CurrentSector = GetOrGenerate(cx, cy);
    }

    private Sector Generate(int cx, int cy)
    {
        // Threat scales with Manhattan distance from origin
        int dist = System.Math.Abs(cx) + System.Math.Abs(cy);
        int threat = 1 + dist / 2;

        var sector = new Sector(cx, cy, threat);
        // Seed deterministic per-chunk RNG so sectors are reproducible
        var r = new System.Random(cx * 73856093 ^ cy * 19349663 ^ 12345);

        int nodeCount = 3 + r.Next(5);
        for (int i = 0; i < nodeCount; i++)
        {
            float x = (float)(r.NextDouble() * SECTOR_SIZE * 2 - SECTOR_SIZE);
            float y = (float)(r.NextDouble() * SECTOR_SIZE * 2 - SECTOR_SIZE);

            SectorNodeType type = PickNodeType(r, dist);
            string name = GenerateName(r, type);
            PlanetBiome biome = (PlanetBiome)r.Next(5);

            sector.Nodes.Add(new SectorNode(type, name, x, y, biome, threat));
        }

        return sector;
    }

    private static SectorNodeType PickNodeType(System.Random r, int dist)
    {
        int roll = r.Next(100);
        if (dist == 0) return roll < 50 ? SectorNodeType.Planet : SectorNodeType.Station;
        if (roll < 30) return SectorNodeType.Planet;
        if (roll < 50) return SectorNodeType.Station;
        if (roll < 70) return SectorNodeType.AsteroidBelt;
        if (roll < 85) return SectorNodeType.Nebula;
        if (roll < 95) return SectorNodeType.DerelictShip;
        return SectorNodeType.Empty;
    }

    private static readonly string[] _adjectives = { "Red", "Lost", "Iron", "Dark", "Golden", "Frozen", "Crimson", "Silent" };
    private static readonly string[] _nouns      = { "Haven", "Reach", "Drift", "Point", "Bastion", "Shore", "Gate", "Expanse" };

    private static string GenerateName(System.Random r, SectorNodeType type)
    {
        string adj  = _adjectives[r.Next(_adjectives.Length)];
        string noun = _nouns[r.Next(_nouns.Length)];
        return type switch
        {
            SectorNodeType.Station     => $"{adj} Station",
            SectorNodeType.Planet      => $"{adj} {noun}",
            SectorNodeType.AsteroidBelt=> $"{adj} Belt",
            SectorNodeType.DerelictShip=> "Derelict",
            SectorNodeType.Nebula      => $"{adj} Nebula",
            _                          => "Empty Space"
        };
    }
}
