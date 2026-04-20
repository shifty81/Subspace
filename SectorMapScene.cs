using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;

namespace Subspace;

/// <summary>
/// Full-screen galaxy-map overlay.  Opens from space view with the M key and
/// lets the player view and travel to neighbouring sectors.
///
/// Signals exit via <see cref="ExitRequested"/> (same pattern as ShipInteriorScene).
/// When <see cref="TravelRequested"/> is true, <see cref="TravelDestination"/> holds
/// the (chunkX, chunkY) of the sector the player chose to travel to.
/// </summary>
public class SectorMapScene : IScene
{
    // ── Exit / travel signals ─────────────────────────────────────────────────
    public bool ExitRequested   { get; private set; }
    public bool TravelRequested { get; private set; }
    public (int cx, int cy) TravelDestination { get; private set; }

    // ── UI resources ──────────────────────────────────────────────────────────
    private PixelFont? _font;
    private Texture2D? _pixel;

    // ── View state ────────────────────────────────────────────────────────────
    private int _cursorCx;
    private int _cursorCy;

    // Half-width of the visible grid on each axis (renders -HALF..+HALF around origin chunk)
    private const int HALF = 2;   // 5×5 visible grid
    private const int CELL = 110; // pixels per cell
    private const int ICON_R = 28; // icon circle radius

    // Centre of the map area on screen
    private int _mapCx;
    private int _mapCy;

    // Input guard
    private KeyboardState _prevKeys;

    // ── Constructor ───────────────────────────────────────────────────────────

    public SectorMapScene() { }

    public void SetResources(PixelFont font, Texture2D pixel)
    {
        _font  = font;
        _pixel = pixel;
    }

    // ── IScene ────────────────────────────────────────────────────────────────

    public void Enter(object? context = null)
    {
        ExitRequested   = false;
        TravelRequested = false;

        var gs = GameState.Instance;
        _cursorCx = gs.SectorMap.CurrentChunkX;
        _cursorCy = gs.SectorMap.CurrentChunkY;

        _mapCx = Config.SCREEN_WIDTH  / 2;
        _mapCy = Config.SCREEN_HEIGHT / 2;

        _prevKeys = Keyboard.GetState();
    }

    public void Exit() { }

    public void Update(float dt)
    {
        var keys = Keyboard.GetState();

        bool Just(Keys k) => keys.IsKeyDown(k) && !_prevKeys.IsKeyDown(k);

        // Close map
        if (Just(Keys.Escape) || Just(Keys.M))
        {
            ExitRequested = true;
            _prevKeys = keys;
            return;
        }

        var gs = GameState.Instance;

        // Move cursor
        if (Just(Keys.Left)  || Just(Keys.A)) _cursorCx--;
        if (Just(Keys.Right) || Just(Keys.D)) _cursorCx++;
        if (Just(Keys.Up)    || Just(Keys.W)) _cursorCy--;
        if (Just(Keys.Down)  || Just(Keys.S)) _cursorCy++;

        // Travel to cursor sector (only if it's a different sector)
        if (Just(Keys.Enter) || Just(Keys.Space))
        {
            if (_cursorCx != gs.SectorMap.CurrentChunkX ||
                _cursorCy != gs.SectorMap.CurrentChunkY)
            {
                TravelDestination = (_cursorCx, _cursorCy);
                TravelRequested   = true;
            }
            ExitRequested = true;
        }

        _prevKeys = keys;
    }

    public void DrawWorld(SpriteBatch sb, Texture2D pixel, float gameTime) { }

    public void DrawUI(SpriteBatch sb, Texture2D pixel, float gameTime)
    {
        if (_font == null || _pixel == null) return;

        var gs = GameState.Instance;

        // ── Full-screen dark overlay ──────────────────────────────────────────
        sb.Draw(_pixel,
            new Rectangle(0, 0, Config.SCREEN_WIDTH, Config.SCREEN_HEIGHT),
            Color.Black * 0.88f);

        // ── Title ─────────────────────────────────────────────────────────────
        const int TS = 3;
        string title = "GALAXY MAP";
        int tw = _font.MeasureWidth(title, TS);
        _font.DrawString(sb, title, (Config.SCREEN_WIDTH - tw) / 2, 14, Color.Cyan, TS);

        // ── Sector grid ───────────────────────────────────────────────────────
        int originCx = gs.SectorMap.CurrentChunkX;
        int originCy = gs.SectorMap.CurrentChunkY;

        for (int dcy = -HALF; dcy <= HALF; dcy++)
        {
            for (int dcx = -HALF; dcx <= HALF; dcx++)
            {
                int cx  = originCx + dcx;
                int cy  = originCy + dcy;

                int sx  = _mapCx + dcx * CELL;
                int sy  = _mapCy + dcy * CELL;

                bool isCurrent = cx == originCx && cy == originCy;
                bool isCursor  = cx == _cursorCx && cy == _cursorCy;

                DrawSectorCell(sb, pixel, gameTime,
                    gs.SectorMap.GetOrGenerate(cx, cy),
                    sx, sy, isCurrent, isCursor);
            }
        }

        // ── Instructions bar ──────────────────────────────────────────────────
        const int IS = 2;
        string inst = "WASD/Arrows:Move Cursor   Enter/Space:Travel to Sector   M/Esc:Close";
        int iw = _font.MeasureWidth(inst, IS);
        _font.DrawString(sb, inst,
            (Config.SCREEN_WIDTH - iw) / 2,
            Config.SCREEN_HEIGHT - _font.LineHeight(IS) - 10,
            new Color(140, 140, 140), IS);

        // ── Cursor sector info ────────────────────────────────────────────────
        DrawCursorInfo(sb, pixel, gs);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private void DrawSectorCell(SpriteBatch sb, Texture2D pixel, float gameTime,
                                Sector sector, int cx, int cy,
                                bool isCurrent, bool isCursor)
    {
        if (_font == null || _pixel == null) return;

        // Cell background
        Color bgCol = isCurrent ? new Color(0, 40, 60) : new Color(10, 10, 20);
        int half = CELL / 2 - 4;
        sb.Draw(pixel, new Rectangle(cx - half, cy - half, half * 2, half * 2), bgCol);

        // Border
        float pulse = isCursor ? (MathF.Sin(gameTime * 5f) * 0.3f + 0.7f) : 0.35f;
        Color borderCol = isCursor  ? Color.Yellow  * pulse
                        : isCurrent ? Color.Cyan    * 0.7f
                        :             Color.Gray    * 0.35f;
        DrawRect(sb, pixel, cx - half, cy - half, half * 2, half * 2, borderCol);

        // Node icons (up to 5)
        var nodes = sector.Nodes;
        int iconCount = Math.Min(nodes.Count, 5);
        for (int i = 0; i < iconCount; i++)
        {
            float angle = MathF.Tau * i / Math.Max(1, iconCount);
            int nx = cx + (int)(MathF.Cos(angle) * 22);
            int ny = cy + (int)(MathF.Sin(angle) * 22);
            DrawNodeIcon(sb, pixel, nodes[i].NodeType, nx, ny);
        }

        // "HERE" marker for current sector
        if (isCurrent)
        {
            const int SS = 1;
            string here = "HERE";
            int hw = _font.MeasureWidth(here, SS);
            _font.DrawString(sb, here, cx - hw / 2, cy - 8, Color.Cyan, SS);
        }

        // Threat level badge (bottom-right of cell)
        int tl = sector.ThreatLevel;
        Color threatCol = tl <= 2 ? Color.LimeGreen : (tl <= 4 ? Color.Yellow : Color.Red);
        _font.DrawString(sb, $"T{tl}", cx + half - 16, cy + half - 10, threatCol, 1);
    }

    private void DrawNodeIcon(SpriteBatch sb, Texture2D pixel, SectorNodeType type, int x, int y)
    {
        Color col = type switch
        {
            SectorNodeType.Planet      => new Color(80, 180, 80),
            SectorNodeType.Station     => new Color(100, 160, 220),
            SectorNodeType.AsteroidBelt=> new Color(180, 140, 80),
            SectorNodeType.Nebula      => new Color(180, 80, 200),
            SectorNodeType.DerelictShip=> new Color(160, 80, 80),
            _                          => Color.Gray,
        };
        // Draw a 4×4 colored square icon
        sb.Draw(pixel, new Rectangle(x - 2, y - 2, 4, 4), col);
    }

    private void DrawCursorInfo(SpriteBatch sb, Texture2D pixel, GameState gs)
    {
        if (_font == null) return;

        var sector = gs.SectorMap.GetOrGenerate(_cursorCx, _cursorCy);
        bool isCurrentSector = _cursorCx == gs.SectorMap.CurrentChunkX &&
                               _cursorCy == gs.SectorMap.CurrentChunkY;

        const int PW = 280;
        const int S  = 2;
        int lh = _font.LineHeight(S);
        int linesNeeded = 3 + Math.Min(sector.Nodes.Count, 7);
        int PH = linesNeeded * (lh + 2) + 12;

        int px = 12;
        int py = Config.SCREEN_HEIGHT / 2 - PH / 2;

        sb.Draw(pixel, new Rectangle(px, py, PW, PH), Color.Black * 0.85f);
        DrawRect(sb, pixel, px, py, PW, PH, Color.Cyan * 0.5f);

        int tx = px + 6;
        int ty = py + 6;

        string sectorLabel = $"{sector.DisplayName}";
        _font.DrawString(sb, sectorLabel, tx, ty, Color.Cyan, S);
        ty += lh + 2;

        Color threatCol = sector.ThreatLevel <= 2 ? Color.LimeGreen
                        : sector.ThreatLevel <= 4 ? Color.Yellow : Color.Red;
        _font.DrawString(sb, $"Threat: {sector.ThreatLevel}", tx, ty, threatCol, S);
        ty += lh + 2;

        if (isCurrentSector)
        {
            _font.DrawString(sb, "[Current Location]", tx, ty, Color.Cyan, S);
            ty += lh + 2;
        }
        else
        {
            _font.DrawString(sb, "Press Enter to travel here", tx, ty, Color.Yellow, S);
            ty += lh + 2;
        }

        foreach (var node in sector.Nodes)
        {
            if (ty > py + PH - lh - 4) break;
            Color nodeCol = node.NodeType switch
            {
                SectorNodeType.Planet      => new Color(80, 200, 80),
                SectorNodeType.Station     => new Color(100, 170, 230),
                SectorNodeType.AsteroidBelt=> new Color(200, 160, 80),
                SectorNodeType.Nebula      => new Color(200, 100, 220),
                SectorNodeType.DerelictShip=> new Color(200, 100, 100),
                _                          => Color.Gray,
            };
            _font.DrawString(sb, $"  {node.NodeType,-12} {node.Name}", tx, ty, nodeCol, S);
            ty += lh + 2;
        }
    }

    private static void DrawRect(SpriteBatch sb, Texture2D pixel,
                                 int x, int y, int w, int h, Color color)
    {
        sb.Draw(pixel, new Rectangle(x,         y,         w, 1),     color);
        sb.Draw(pixel, new Rectangle(x,         y + h - 1, w, 1),     color);
        sb.Draw(pixel, new Rectangle(x,         y,         1, h),     color);
        sb.Draw(pixel, new Rectangle(x + w - 1, y,         1, h),     color);
    }
}
