using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;

namespace Subspace;

/// <summary>
/// The player-controlled character that walks around the ship interior (and planet surface).
/// Uses the same CrewMember data model for stats/needs but has direct WASD input.
/// </summary>
public class PlayerCharacter
{
    // ── World-space position ─────────────────────────────────────────────────
    public float X { get; set; }
    public float Y { get; set; }

    // ── State ────────────────────────────────────────────────────────────────
    public bool IsAtHelm         { get; private set; }
    public bool PilotingModeRequested { get; private set; }   // set for one frame when helm activated

    // ── Underlying crew data ─────────────────────────────────────────────────
    public CrewMember CrewData { get; }

    // ── Walk speed ───────────────────────────────────────────────────────────
    private const float WALK_SPEED = 120f;   // pixels/sec in interior view

    // ── Facing direction (for rendering) ─────────────────────────────────────
    private int _facingX = 0;   // -1 left, 1 right
    private int _facingY = 1;   // -1 up,   1 down

    // ── Grid reference for collision ──────────────────────────────────────────
    private InteriorGrid? _grid;

    public PlayerCharacter(float startX, float startY)
    {
        X = startX;
        Y = startY;
        // Id 0 is always the player
        CrewData = new CrewMember(0, startX, startY, "Captain");
        CrewData.CrewColor = Color.Lime;
    }

    /// <summary>Provide a grid reference so the character can do tile-based collision.</summary>
    public void SetGrid(InteriorGrid grid) => _grid = grid;

    // ── Update ────────────────────────────────────────────────────────────────

    /// <summary>
    /// Process WASD movement and E interaction.
    /// Returns a non-null tile type if the player just interacted with a tile.
    /// </summary>
    public InteriorTileType? Update(float dt, KeyboardState keys, KeyboardState prevKeys)
    {
        PilotingModeRequested = false;
        IsAtHelm = false;

        float mx = 0, my = 0;
        if (keys.IsKeyDown(Keys.A) || keys.IsKeyDown(Keys.Left))  mx = -1;
        if (keys.IsKeyDown(Keys.D) || keys.IsKeyDown(Keys.Right)) mx =  1;
        if (keys.IsKeyDown(Keys.W) || keys.IsKeyDown(Keys.Up))    my = -1;
        if (keys.IsKeyDown(Keys.S) || keys.IsKeyDown(Keys.Down))  my =  1;

        // Normalise diagonal
        float len = MathF.Sqrt(mx * mx + my * my);
        if (len > 0.01f) { mx /= len; my /= len; }

        if (mx != 0 || my != 0) { _facingX = (int)MathF.Sign(mx); _facingY = (int)MathF.Sign(my); }

        float nx = X + mx * WALK_SPEED * dt;
        float ny = Y + my * WALK_SPEED * dt;

        // Tile-based collision: only allow movement into passable tiles
        if (_grid != null)
        {
            int cell = Config.INTERIOR_GRID_SIZE;
            // Attempt X movement
            int tgx = (int)Math.Floor(nx / cell);
            int tgy = (int)Math.Floor(Y  / cell);
            var tileX = _grid.Get(tgx, tgy);
            if (tileX != null && tileX.IsPassable) X = nx;

            // Attempt Y movement
            tgx = (int)Math.Floor(X  / cell);
            tgy = (int)Math.Floor(ny / cell);
            var tileY = _grid.Get(tgx, tgy);
            if (tileY != null && tileY.IsPassable) Y = ny;
        }
        else
        {
            X = nx;
            Y = ny;
        }

        // Sync crew data position
        CrewData.X = X;
        CrewData.Y = Y;

        // ── Interact (E key) ─────────────────────────────────────────────────
        bool eJustPressed = keys.IsKeyDown(Keys.E) && !prevKeys.IsKeyDown(Keys.E);
        if (eJustPressed && _grid != null)
        {
            // Interact with tile player is standing on, or the one they face
            int igx = (int)Math.Floor(X / Config.INTERIOR_GRID_SIZE);
            int igy = (int)Math.Floor(Y / Config.INTERIOR_GRID_SIZE);
            var standing = _grid.Get(igx, igy);

            if (standing != null && standing.IsInteractable)
                return Interact(standing.Type);

            // Try facing direction
            var facing = _grid.Get(igx + _facingX, igy + _facingY);
            if (facing != null && facing.IsInteractable)
                return Interact(facing.Type);
        }

        return null;
    }

    private InteriorTileType Interact(InteriorTileType type)
    {
        if (type == InteriorTileType.CommandChair)
        {
            IsAtHelm              = true;
            PilotingModeRequested = true;
        }
        return type;
    }

    // ── Render (interior view) ────────────────────────────────────────────────

    public void Render(SpriteBatch sb, Texture2D pixel, float camX, float camY, float gameTime)
    {
        int sx = (int)(X - camX);
        int sy = (int)(Y - camY);

        // Shadow
        sb.Draw(pixel, new Rectangle(sx - 5, sy + 4, 10, 4), Color.Black * 0.3f);

        // Legs (animated)
        float t = gameTime * 8f;
        int legOff = (int)(MathF.Sin(t) * 3f);
        sb.Draw(pixel, new Rectangle(sx - 5, sy - 2, 4, 8 + legOff), Color.DarkGreen);
        sb.Draw(pixel, new Rectangle(sx + 1, sy - 2, 4, 8 - legOff), Color.DarkGreen);

        // Body
        sb.Draw(pixel, new Rectangle(sx - 6, sy - 18, 12, 16), Color.Lime);

        // Head
        sb.Draw(pixel, new Rectangle(sx - 5, sy - 28, 10, 10), Color.LimeGreen);

        // Eyes
        sb.Draw(pixel, new Rectangle(sx - 3, sy - 25, 2, 2), Color.White);
        sb.Draw(pixel, new Rectangle(sx + 1, sy - 25, 2, 2), Color.White);

        // "E to interact" prompt when near something
        if (_grid != null)
        {
            int igx = (int)Math.Floor(X / Config.INTERIOR_GRID_SIZE);
            int igy = (int)Math.Floor(Y / Config.INTERIOR_GRID_SIZE);
            bool nearInteractable = CheckNearInteractable(igx, igy);
            if (nearInteractable)
            {
                // Draw a small "E" indicator above the head
                float pulse = (MathF.Sin(gameTime * 4f) + 1f) * 0.5f;
                sb.Draw(pixel, new Rectangle(sx - 12, sy - 40, 24, 2), Color.Cyan * pulse);
            }
        }
    }

    private bool CheckNearInteractable(int gx, int gy)
    {
        if (_grid == null) return false;
        for (int dy = -1; dy <= 1; dy++)
            for (int dx = -1; dx <= 1; dx++)
            {
                var t = _grid.Get(gx + dx, gy + dy);
                if (t != null && t.IsInteractable) return true;
            }
        return false;
    }
}
