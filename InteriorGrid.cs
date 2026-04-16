using Microsoft.Xna.Framework;
using System.Collections.Generic;

namespace Subspace;

// ── Tile type ────────────────────────────────────────────────────────────────

/// <summary>What a single 96-px grid cell contains.</summary>
public enum InteriorTileType
{
    Empty        = 0,   // vacuum / open space
    Floor        = 1,   // walkable floor
    HullPlating  = 2,   // exterior hull (impassable, takes damage)
    // Furniture / systems (placed on top of a floor cell)
    Door         = 10,
    CommandChair = 11,   // helm / piloting station
    CrewBunk     = 12,
    Workbench    = 13,
    ResearchTerminal = 14,
    PowerConduit = 15,
    Lighting     = 16,
    O2Recycler   = 17,
    KitchenStation = 18,
    CargoCrate   = 19,
    // Ship systems mounted on hull
    Thruster     = 20,   // directional thruster on hull edge
    ShieldGen    = 21,
    WeaponMount  = 22,
    Reactor      = 23,
    MedBay       = 24,
}

// ── Wall edges ───────────────────────────────────────────────────────────────

/// <summary>
/// Which edges of a cell have wall panels.
/// Stored as flags so multiple edges can exist on one cell.
/// </summary>
[System.Flags]
public enum WallEdge
{
    None   = 0,
    North  = 1,
    East   = 2,
    South  = 4,
    West   = 8,
    All    = North | East | South | West,
}

// ── Single tile ───────────────────────────────────────────────────────────────

public class InteriorTile
{
    public InteriorTileType Type { get; set; } = InteriorTileType.Empty;
    public bool HasCeiling { get; set; }
    public WallEdge Walls { get; set; } = WallEdge.None;
    /// <summary>Rotation in 90° steps (0,1,2,3) for directional tiles like Thruster or Door.</summary>
    public int Rotation { get; set; }
    /// <summary>Which room this tile belongs to after a flood-fill (0 = no room).</summary>
    public int RoomId { get; set; }

    public bool IsPassable => Type == InteriorTileType.Floor
                           || Type == InteriorTileType.Door
                           || Type == InteriorTileType.CommandChair
                           || Type == InteriorTileType.CrewBunk
                           || Type == InteriorTileType.Workbench
                           || Type == InteriorTileType.ResearchTerminal
                           || Type == InteriorTileType.KitchenStation
                           || Type == InteriorTileType.MedBay;

    /// <summary>True if a crew member or player can occupy this tile.</summary>
    public bool IsInteractable => Type switch
    {
        InteriorTileType.CommandChair     => true,
        InteriorTileType.Workbench        => true,
        InteriorTileType.ResearchTerminal => true,
        InteriorTileType.KitchenStation   => true,
        InteriorTileType.MedBay           => true,
        InteriorTileType.Door             => true,
        _ => false
    };

    public Color GetDisplayColor() => Type switch
    {
        InteriorTileType.Floor           => new Color(55,  55,  65),
        InteriorTileType.HullPlating     => new Color(80,  90,  100),
        InteriorTileType.Door            => new Color(120, 200, 120),
        InteriorTileType.CommandChair    => new Color(200, 180,  50),
        InteriorTileType.CrewBunk        => new Color(100, 100, 200),
        InteriorTileType.Workbench       => new Color(180, 130,  60),
        InteriorTileType.ResearchTerminal=> new Color( 50, 180, 200),
        InteriorTileType.PowerConduit    => new Color(255, 200,   0),
        InteriorTileType.Lighting        => new Color(255, 255, 200),
        InteriorTileType.O2Recycler      => new Color(100, 220, 140),
        InteriorTileType.KitchenStation  => new Color(220, 140,  80),
        InteriorTileType.CargoCrate      => new Color(160, 120,  80),
        InteriorTileType.Thruster        => new Color( 80, 130, 255),
        InteriorTileType.ShieldGen       => new Color( 80, 160, 255),
        InteriorTileType.WeaponMount     => new Color(220,  60,  60),
        InteriorTileType.Reactor         => new Color( 80, 255, 120),
        InteriorTileType.MedBay          => new Color(220,  80, 220),
        _                                => Color.Transparent
    };
}

// ── Room ─────────────────────────────────────────────────────────────────────

/// <summary>
/// A connected set of floor tiles sharing a ceiling (found by flood-fill).
/// Tracks simple atmospherics so life-support gameplay can be layered in later.
/// </summary>
public class Room
{
    public int Id { get; }
    public List<(int gx, int gy)> Tiles { get; } = new();
    public float O2Level     { get; set; } = 1f;   // 0–1
    public float Temperature { get; set; } = 20f;  // °C

    public Room(int id) => Id = id;
}

// ── Grid ──────────────────────────────────────────────────────────────────────

/// <summary>
/// The 96-px interior tile grid for a single ship (or planet base).
/// Supports runtime editing (build mode) and room flood-fill.
/// </summary>
public class InteriorGrid
{
    public int Width  { get; }
    public int Height { get; }
    public int CellPx => Config.INTERIOR_GRID_SIZE;

    private readonly InteriorTile[,] _tiles;
    private List<Room> _rooms = new();

    public InteriorGrid(int width, int height)
    {
        Width  = width;
        Height = height;
        _tiles = new InteriorTile[width, height];
        for (int y = 0; y < height; y++)
            for (int x = 0; x < width; x++)
                _tiles[x, y] = new InteriorTile();
    }

    // ── Accessors ─────────────────────────────────────────────────────────────

    public InteriorTile? Get(int gx, int gy)
    {
        if (gx < 0 || gx >= Width || gy < 0 || gy >= Height) return null;
        return _tiles[gx, gy];
    }

    public void Set(int gx, int gy, InteriorTileType type, bool addCeiling = true)
    {
        if (gx < 0 || gx >= Width || gy < 0 || gy >= Height) return;
        _tiles[gx, gy].Type = type;
        if (type == InteriorTileType.Floor || type == InteriorTileType.HullPlating)
            _tiles[gx, gy].HasCeiling = addCeiling;
        RebuildRooms();
        AutoWalls();
    }

    public void Remove(int gx, int gy)
    {
        if (gx < 0 || gx >= Width || gy < 0 || gy >= Height) return;
        _tiles[gx, gy].Type       = InteriorTileType.Empty;
        _tiles[gx, gy].HasCeiling = false;
        _tiles[gx, gy].Walls      = WallEdge.None;
        RebuildRooms();
        AutoWalls();
    }

    // ── Auto-wall placement ───────────────────────────────────────────────────

    /// <summary>
    /// Automatically puts a wall edge on the boundary between a solid tile and empty/vacuum.
    /// Interior walls are placed explicitly by the player.
    /// </summary>
    public void AutoWalls()
    {
        for (int gy = 0; gy < Height; gy++)
        {
            for (int gx = 0; gx < Width; gx++)
            {
                var tile = _tiles[gx, gy];
                if (tile.Type == InteriorTileType.Empty) { tile.Walls = WallEdge.None; continue; }

                WallEdge edges = WallEdge.None;
                if (IsSolid(gx, gy - 1)) { /* neighbour is solid — no wall on that edge */ }
                else edges |= WallEdge.North;

                if (IsSolid(gx + 1, gy)) { }
                else edges |= WallEdge.East;

                if (IsSolid(gx, gy + 1)) { }
                else edges |= WallEdge.South;

                if (IsSolid(gx - 1, gy)) { }
                else edges |= WallEdge.West;

                tile.Walls = edges;
            }
        }
    }

    private bool IsSolid(int gx, int gy)
    {
        if (gx < 0 || gx >= Width || gy < 0 || gy >= Height) return false;
        return _tiles[gx, gy].Type != InteriorTileType.Empty;
    }

    // ── Room flood-fill ───────────────────────────────────────────────────────

    public IReadOnlyList<Room> Rooms => _rooms;

    /// <summary>
    /// Flood-fills the grid to identify enclosed rooms.
    /// A "room" is a set of connected passable tiles that all have a ceiling.
    /// Preserves each room's existing O2/temperature data when re-running.
    /// </summary>
    public void RebuildRooms()
    {
        // Snapshot old room data (keyed by canonical first tile) to preserve atmo values
        var oldAtmo = new Dictionary<(int, int), (float o2, float temp)>();
        foreach (var r in _rooms)
        {
            if (r.Tiles.Count > 0)
                oldAtmo[r.Tiles[0]] = (r.O2Level, r.Temperature);
        }

        _rooms = new List<Room>();
        var visited = new bool[Width, Height];
        int nextId = 1;

        for (int gy = 0; gy < Height; gy++)
        {
            for (int gx = 0; gx < Width; gx++)
            {
                if (visited[gx, gy]) continue;
                var tile = _tiles[gx, gy];
                if (!tile.IsPassable || !tile.HasCeiling) continue;

                // BFS
                var room = new Room(nextId++);
                var queue = new Queue<(int, int)>();
                queue.Enqueue((gx, gy));
                visited[gx, gy] = true;

                while (queue.Count > 0)
                {
                    var (cx, cy) = queue.Dequeue();
                    room.Tiles.Add((cx, cy));
                    _tiles[cx, cy].RoomId = room.Id;

                    void TryVisit(int nx, int ny)
                    {
                        if (nx < 0 || nx >= Width || ny < 0 || ny >= Height) return;
                        if (visited[nx, ny]) return;
                        var n = _tiles[nx, ny];
                        if (!n.IsPassable || !n.HasCeiling) return;
                        visited[nx, ny] = true;
                        queue.Enqueue((nx, ny));
                    }
                    TryVisit(cx - 1, cy);
                    TryVisit(cx + 1, cy);
                    TryVisit(cx, cy - 1);
                    TryVisit(cx, cy + 1);
                }

                // Restore atmospherics if the room has a predecessor
                if (room.Tiles.Count > 0 && oldAtmo.TryGetValue(room.Tiles[0], out var atmo))
                {
                    room.O2Level     = atmo.o2;
                    room.Temperature = atmo.temp;
                }

                _rooms.Add(room);
            }
        }

        // Clear room IDs for non-room tiles
        for (int gy = 0; gy < Height; gy++)
            for (int gx = 0; gx < Width; gx++)
                if (!_tiles[gx, gy].IsPassable || !_tiles[gx, gy].HasCeiling)
                    _tiles[gx, gy].RoomId = 0;
    }

    // ── Factory: default starter ship layout ─────────────────────────────────

    /// <summary>
    /// Creates a compact 14×14 starter ship interior at the grid centre.
    /// </summary>
    public static InteriorGrid CreateStarterShip()
    {
        var grid = new InteriorGrid(30, 30);
        int ox = 8, oy = 8;   // top-left of ship

        // Hull outline (14 wide × 12 tall)
        for (int y = 0; y < 12; y++)
            for (int x = 0; x < 14; x++)
            {
                bool isEdge = x == 0 || x == 13 || y == 0 || y == 11;
                var t = grid._tiles[ox + x, oy + y];
                t.Type       = isEdge ? InteriorTileType.HullPlating : InteriorTileType.Floor;
                t.HasCeiling = true;
            }

        // Command chair (helm) near nose
        grid._tiles[ox + 6, oy + 1].Type = InteriorTileType.CommandChair;

        // Reactor
        grid._tiles[ox + 3,  oy + 8].Type = InteriorTileType.Reactor;
        grid._tiles[ox + 10, oy + 8].Type = InteriorTileType.Reactor;

        // Crew bunks
        grid._tiles[ox + 2, oy + 3].Type = InteriorTileType.CrewBunk;
        grid._tiles[ox + 2, oy + 4].Type = InteriorTileType.CrewBunk;
        grid._tiles[ox + 11, oy + 3].Type = InteriorTileType.CrewBunk;
        grid._tiles[ox + 11, oy + 4].Type = InteriorTileType.CrewBunk;

        // Workbench
        grid._tiles[ox + 6, oy + 5].Type = InteriorTileType.Workbench;

        // Research terminal
        grid._tiles[ox + 7, oy + 5].Type = InteriorTileType.ResearchTerminal;

        // Thrusters on bottom hull row
        grid._tiles[ox + 3, oy + 11].Type = InteriorTileType.Thruster;
        grid._tiles[ox + 6, oy + 11].Type = InteriorTileType.Thruster;
        grid._tiles[ox + 7, oy + 11].Type = InteriorTileType.Thruster;
        grid._tiles[ox + 10, oy + 11].Type = InteriorTileType.Thruster;

        // Doors on the midship divider
        grid._tiles[ox + 6, oy + 6].Type = InteriorTileType.Door;
        grid._tiles[ox + 7, oy + 6].Type = InteriorTileType.Door;

        grid.AutoWalls();
        grid.RebuildRooms();
        return grid;
    }
}
