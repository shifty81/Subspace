using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;

namespace Subspace;

/// <summary>
/// Context passed to ShipInteriorScene when entering from SpaceScene.
/// </summary>
public class InteriorContext
{
    public Ship Ship { get; set; }
    public InteriorGrid Grid { get; set; }
    public InteriorContext(Ship ship, InteriorGrid grid) { Ship = ship; Grid = grid; }
}

/// <summary>
/// Sub-modes inside the interior scene.
/// </summary>
public enum InteriorMode { Walking, Building }

/// <summary>
/// The ship interior scene: 96-px tile grid, player character, crew, build mode.
/// Signals exit via <see cref="ExitRequested"/> so Game1 doesn't need to pass
/// a back-reference to itself.
/// </summary>
public class ShipInteriorScene : IScene
{
    // ── Public exit signal ────────────────────────────────────────────────────
    /// <summary>Set to true for one frame when the player exits to space.</summary>
    public bool ExitRequested { get; private set; }

    // ── Dependencies ─────────────────────────────────────────────────────────
    private readonly SceneManager _scenes;

    // ── Ship / Grid data ──────────────────────────────────────────────────────
    private Ship? _ship;
    private InteriorGrid? _grid;

    // ── Player character ──────────────────────────────────────────────────────
    private PlayerCharacter? _player;
    private bool _pilotingRequested;

    // ── Camera (interior world-space pixels) ──────────────────────────────────
    private float _camX, _camY;
    private const float CAMERA_SPEED = 8f;   // lerp factor

    // ── Input ─────────────────────────────────────────────────────────────────
    private KeyboardState _prevKeys;

    // ── Mode ──────────────────────────────────────────────────────────────────
    private InteriorMode _mode = InteriorMode.Walking;
    private InteriorTileType _buildSelectedType = InteriorTileType.Floor;

    // ── HUD / UI helpers ─────────────────────────────────────────────────────
    private PixelFont? _font;
    private Texture2D? _pixel;
    private float _gameTime;

    // ── Notification banner ───────────────────────────────────────────────────
    private string _notification = "";
    private float  _notifyTimer;
    private const float NOTIFY_DURATION = 3f;

    // ── Build palette ─────────────────────────────────────────────────────────
    private static readonly InteriorTileType[] _buildPalette =
    {
        InteriorTileType.Floor,
        InteriorTileType.HullPlating,
        InteriorTileType.Door,
        InteriorTileType.CommandChair,
        InteriorTileType.CrewBunk,
        InteriorTileType.Workbench,
        InteriorTileType.ResearchTerminal,
        InteriorTileType.Thruster,
        InteriorTileType.Reactor,
        InteriorTileType.MedBay,
    };

    // ── Constructor ───────────────────────────────────────────────────────────

    public ShipInteriorScene(SceneManager scenes)
    {
        _scenes = scenes;
    }

    // ── IScene ────────────────────────────────────────────────────────────────

    public void Enter(object? context = null)
    {
        _gameTime = 0f;
        _pilotingRequested = false;
        ExitRequested = false;

        if (context is InteriorContext ctx)
        {
            _ship = ctx.Ship;
            _grid = ctx.Grid;
        }
        else
        {
            // Fallback: create a default grid
            _grid = InteriorGrid.CreateStarterShip();
        }

        // Spawn player at the helm tile (centre of grid)
        int startGx = _grid.Width  / 2;
        int startGy = _grid.Height / 2;
        float startX = (startGx + 0.5f) * Config.INTERIOR_GRID_SIZE;
        float startY = (startGy + 0.5f) * Config.INTERIOR_GRID_SIZE;

        // Find the CommandChair and start there if possible
        for (int gy = 0; gy < _grid.Height; gy++)
            for (int gx = 0; gx < _grid.Width; gx++)
            {
                var t = _grid.Get(gx, gy);
                if (t?.Type == InteriorTileType.CommandChair)
                {
                    startX = (gx + 0.5f) * Config.INTERIOR_GRID_SIZE;
                    startY = (gy + 0.5f) * Config.INTERIOR_GRID_SIZE;
                }
            }

        _player = new PlayerCharacter(startX, startY);
        _player.SetGrid(_grid);

        _camX = startX - Config.SCREEN_WIDTH  / 2f;
        _camY = startY - Config.SCREEN_HEIGHT / 2f;

        Notify("Press I/Tab to exit interior.  E to interact.  B to build.");
    }

    public void Exit() { }

    public void Update(float dt)
    {
        _gameTime += dt;

        if (_notifyTimer > 0) _notifyTimer -= dt;

        KeyboardState keys = Keyboard.GetState();

        // ── Exit interior ────────────────────────────────────────────────────
        bool exitKey = WasJustPressed(Keys.I, keys) || WasJustPressed(Keys.Tab, keys);
        if (exitKey || _pilotingRequested)
        {
            ExitRequested = true;
            _prevKeys = keys;
            return;
        }

        // ── Toggle build mode ────────────────────────────────────────────────
        if (WasJustPressed(Keys.B, keys))
        {
            _mode = _mode == InteriorMode.Walking ? InteriorMode.Building : InteriorMode.Walking;
            Notify(_mode == InteriorMode.Building ? "BUILD MODE — LClick:Place  RClick:Remove  1-0:Select" : "WALK MODE");
        }

        if (_mode == InteriorMode.Building)
        {
            UpdateBuildMode(keys);
        }
        else
        {
            // ── Player movement ──────────────────────────────────────────────
            if (_player != null)
            {
                var interacted = _player.Update(dt, keys, _prevKeys);
                if (_player.PilotingModeRequested)
                {
                    Notify("Engaging helm… returning to space view.");
                    _pilotingRequested = true;
                }
                else if (interacted.HasValue)
                {
                    Notify(GetInteractMessage(interacted.Value));
                }
            }
        }

        // ── Crew simulation (needs tick) ─────────────────────────────────────
        if (_ship?.CrewManager != null)
            _ship.CrewManager.Update(dt);

        // ── Camera smooth-follow player ───────────────────────────────────────
        if (_player != null)
        {
            float targetCamX = _player.X - Config.SCREEN_WIDTH  / 2f;
            float targetCamY = _player.Y - Config.SCREEN_HEIGHT / 2f;
            _camX += (targetCamX - _camX) * CAMERA_SPEED * dt;
            _camY += (targetCamY - _camY) * CAMERA_SPEED * dt;
        }

        _prevKeys = keys;
    }

    private void UpdateBuildMode(KeyboardState keys)
    {
        // 1-0 hotkeys to select tile type
        Keys[] numRow = { Keys.D1, Keys.D2, Keys.D3, Keys.D4, Keys.D5, Keys.D6, Keys.D7, Keys.D8, Keys.D9, Keys.D0 };
        for (int i = 0; i < numRow.Length && i < _buildPalette.Length; i++)
            if (WasJustPressed(numRow[i], keys))
                _buildSelectedType = _buildPalette[i];

        // Mouse click
        var mouse = Mouse.GetState();
        int worldX = (int)(mouse.X + _camX);
        int worldY = (int)(mouse.Y + _camY);
        int gx     = worldX / Config.INTERIOR_GRID_SIZE;
        int gy     = worldY / Config.INTERIOR_GRID_SIZE;

        if (mouse.LeftButton == ButtonState.Pressed)
            _grid?.Set(gx, gy, _buildSelectedType);
        else if (mouse.RightButton == ButtonState.Pressed)
            _grid?.Remove(gx, gy);
    }

    private bool WasJustPressed(Keys key, KeyboardState current)
        => current.IsKeyDown(key) && !_prevKeys.IsKeyDown(key);

    private static string GetInteractMessage(InteriorTileType type) => type switch
    {
        InteriorTileType.CommandChair     => "Sitting at helm…",
        InteriorTileType.Workbench        => "Using workbench.",
        InteriorTileType.ResearchTerminal => "Accessing research terminal.",
        InteriorTileType.KitchenStation   => "Preparing food.",
        InteriorTileType.MedBay           => "Medical bay: treating crew.",
        InteriorTileType.Door             => "Door toggled.",
        _                                 => ""
    };

    private void Notify(string msg) { _notification = msg; _notifyTimer = NOTIFY_DURATION; }

    // ── Draw (World) ──────────────────────────────────────────────────────────

    public void DrawWorld(SpriteBatch sb, Texture2D pixel, float gameTime)
    {
        _pixel = pixel;
        if (_grid == null) return;

        int cell = Config.INTERIOR_GRID_SIZE;
        int camGx0 = Math.Max(0, (int)(_camX / cell) - 1);
        int camGy0 = Math.Max(0, (int)(_camY / cell) - 1);
        int camGx1 = Math.Min(_grid.Width  - 1, camGx0 + Config.SCREEN_WIDTH  / cell + 3);
        int camGy1 = Math.Min(_grid.Height - 1, camGy0 + Config.SCREEN_HEIGHT / cell + 3);

        // ── Floor + Hull tiles ──────────────────────────────────────────────
        for (int gy = camGy0; gy <= camGy1; gy++)
        {
            for (int gx = camGx0; gx <= camGx1; gx++)
            {
                var tile = _grid.Get(gx, gy);
                if (tile == null || tile.Type == InteriorTileType.Empty) continue;

                int sx = (int)(gx * cell - _camX);
                int sy = (int)(gy * cell - _camY);

                Color baseColor = tile.GetDisplayColor();

                // Draw tile body (inset by 1px for grout line look)
                sb.Draw(pixel, new Rectangle(sx + 1, sy + 1, cell - 2, cell - 2), baseColor);

                // Subtle checkerboard detail on floor tiles
                if (tile.Type == InteriorTileType.Floor && ((gx + gy) % 2 == 0))
                    sb.Draw(pixel, new Rectangle(sx + 1, sy + 1, cell - 2, cell - 2), Color.White * 0.04f);

                // Thruster flame (animated)
                if (tile.Type == InteriorTileType.Thruster)
                {
                    float flicker = (MathF.Sin(gameTime * 20f + gx * 1.3f) + 1f) * 0.5f;
                    sb.Draw(pixel, new Rectangle(sx + cell / 4, sy + cell - 12, cell / 2, 10 + (int)(flicker * 8)), Color.OrangeRed * (0.6f + flicker * 0.4f));
                }

                // Door: draw a lighter stripe across centre
                if (tile.Type == InteriorTileType.Door)
                {
                    sb.Draw(pixel, new Rectangle(sx + 4, sy + cell / 2 - 2, cell - 8, 4), Color.White * 0.6f);
                }

                // Command chair: draw a small star icon
                if (tile.Type == InteriorTileType.CommandChair)
                {
                    int cx = sx + cell / 2, cy = sy + cell / 2;
                    sb.Draw(pixel, new Rectangle(cx - 6, cy - 2, 12, 4), Color.Gold);
                    sb.Draw(pixel, new Rectangle(cx - 2, cy - 6, 4, 12), Color.Gold);
                }
            }
        }

        // ── Wall edges ──────────────────────────────────────────────────────
        for (int gy = camGy0; gy <= camGy1; gy++)
        {
            for (int gx = camGx0; gx <= camGx1; gx++)
            {
                var tile = _grid.Get(gx, gy);
                if (tile == null || tile.Walls == WallEdge.None) continue;

                int sx = (int)(gx * cell - _camX);
                int sy = (int)(gy * cell - _camY);
                const int W = 4;
                Color wallCol = new Color(160, 170, 180);

                if ((tile.Walls & WallEdge.North) != 0)
                    sb.Draw(pixel, new Rectangle(sx, sy, cell, W), wallCol);
                if ((tile.Walls & WallEdge.South) != 0)
                    sb.Draw(pixel, new Rectangle(sx, sy + cell - W, cell, W), wallCol);
                if ((tile.Walls & WallEdge.West) != 0)
                    sb.Draw(pixel, new Rectangle(sx, sy, W, cell), wallCol);
                if ((tile.Walls & WallEdge.East) != 0)
                    sb.Draw(pixel, new Rectangle(sx + cell - W, sy, W, cell), wallCol);
            }
        }

        // ── Grid overlay in build mode ───────────────────────────────────────
        if (_mode == InteriorMode.Building)
        {
            for (int gy = camGy0; gy <= camGy1 + 1; gy++)
            {
                int sy = (int)(gy * cell - _camY);
                sb.Draw(pixel, new Rectangle(0, sy, Config.SCREEN_WIDTH, 1), new Color(60, 70, 80) * 0.4f);
            }
            for (int gx = camGx0; gx <= camGx1 + 1; gx++)
            {
                int sx = (int)(gx * cell - _camX);
                sb.Draw(pixel, new Rectangle(sx, 0, 1, Config.SCREEN_HEIGHT), new Color(60, 70, 80) * 0.4f);
            }

            // Hover highlight
            var mouse = Mouse.GetState();
            int hgx = (int)((mouse.X + _camX) / cell);
            int hgy = (int)((mouse.Y + _camY) / cell);
            int hsx = (int)(hgx * cell - _camX);
            int hsy = (int)(hgy * cell - _camY);
            DrawBorder(sb, pixel, hsx, hsy, cell, cell, Color.Cyan * 0.7f);
        }

        // ── Crew dots ────────────────────────────────────────────────────────
        if (_ship?.CrewManager != null)
        {
            foreach (var cm in _ship.CrewManager.All)
                cm.RenderInterior(sb, pixel, _camX, _camY);
        }

        // ── Player ───────────────────────────────────────────────────────────
        _player?.Render(sb, pixel, _camX, _camY, gameTime);
    }

    // ── Draw (UI) ─────────────────────────────────────────────────────────────

    public void DrawUI(SpriteBatch sb, Texture2D pixel, float gameTime)
    {
        if (_font == null) return;
        const int S = 2;
        int lh = _font.LineHeight(S);
        int pad = 6;

        // ── Mode banner ──────────────────────────────────────────────────────
        Color modeCol = _mode == InteriorMode.Building ? Color.Yellow : Color.Cyan;
        string modeStr = _mode == InteriorMode.Building
            ? $"BUILD MODE  [{_buildSelectedType}]"
            : "INTERIOR — WALK MODE";
        int mw = _font.MeasureWidth(modeStr, S) + pad * 2;
        int mh = lh + pad * 2;
        sb.Draw(pixel, new Rectangle(pad, pad, mw, mh), Color.Black * 0.8f);
        DrawBorder(sb, pixel, pad, pad, mw, mh, modeCol * 0.7f);
        _font.DrawString(sb, modeStr, pad + pad, pad + pad, modeCol, S);

        // ── Crew vitals panel ─────────────────────────────────────────────────
        if (_ship?.CrewManager != null)
        {
            var crewList = _ship.CrewManager.All;
            int panelH = crewList.Count * (lh + 2) + lh + pad * 2;
            int panelW = 280;
            int px = Config.SCREEN_WIDTH - panelW - pad;
            int py = pad;
            sb.Draw(pixel, new Rectangle(px, py, panelW, panelH), Color.Black * 0.8f);
            DrawBorder(sb, pixel, px, py, panelW, panelH, Color.Gray * 0.5f);
            int ty = py + pad;
            _font.DrawString(sb, "CREW", px + pad, ty, Color.LightGray, S);
            ty += lh + 2;
            foreach (var cm in crewList)
            {
                Color nc = cm.MoodLevel switch
                {
                    MoodLevel.Fine      => Color.LightGray,
                    MoodLevel.Unhappy   => Color.Yellow,
                    _                   => Color.Red,
                };
                string status = $"{cm.Name}  H:{cm.Hunger:P0} R:{cm.Rest:P0}";
                _font.DrawString(sb, status, px + pad, ty, nc, 1);
                ty += lh + 2;
            }
        }

        // ── Player vitals ─────────────────────────────────────────────────────
        if (_player != null)
        {
            var cd = _player.CrewData;
            int pvW = 220, pvH = lh * 3 + pad * 2;
            int pvX = pad, pvY = mh + pad * 2;
            sb.Draw(pixel, new Rectangle(pvX, pvY, pvW, pvH), Color.Black * 0.8f);
            DrawBorder(sb, pixel, pvX, pvY, pvW, pvH, Color.Cyan * 0.5f);
            int ty = pvY + pad;
            _font.DrawString(sb, $"HP   {cd.Health}/{cd.MaxHealth}", pvX + pad, ty, Color.Red, S);    ty += lh + 2;
            _font.DrawString(sb, $"FOOD {cd.Hunger:P0}", pvX + pad, ty, Color.Orange, S); ty += lh + 2;
            _font.DrawString(sb, $"REST {cd.Rest:P0}",   pvX + pad, ty, Color.Cyan, S);
        }

        // ── Build palette ─────────────────────────────────────────────────────
        if (_mode == InteriorMode.Building)
        {
            int bpH = lh * 3 + pad * 2;
            int bpW = 500;
            int bpY = Config.SCREEN_HEIGHT - bpH - pad;
            sb.Draw(pixel, new Rectangle(pad, bpY, bpW, bpH), Color.Black * 0.85f);
            DrawBorder(sb, pixel, pad, bpY, bpW, bpH, Color.Yellow * 0.6f);
            _font.DrawString(sb, "1:Floor 2:Hull 3:Door 4:Helm 5:Bunk 6:Bench 7:Research", pad + pad, bpY + pad, Color.LightGray, S);
            _font.DrawString(sb, "8:Thruster 9:Reactor 0:MedBay   LClick:Place  RClick:Remove", pad + pad, bpY + pad + lh + 2, Color.Gray, S);
            _font.DrawString(sb, $"SELECTED: {_buildSelectedType}", pad + pad, bpY + pad + (lh + 2) * 2, Color.Yellow, S);
        }
        else
        {
            // Controls hint
            int cbH = lh + pad * 2;
            sb.Draw(pixel, new Rectangle(0, Config.SCREEN_HEIGHT - cbH, Config.SCREEN_WIDTH, cbH), Color.Black * 0.8f);
            _font.DrawString(sb, "WASD:Walk  E:Interact  B:Build  I/Tab:Exit-Interior  Helm→Pilot",
                pad, Config.SCREEN_HEIGHT - cbH + pad, new Color(150, 150, 150), S);
        }

        // ── Notification ──────────────────────────────────────────────────────
        if (_notifyTimer > 0f)
        {
            float alpha = Math.Min(1f, _notifyTimer);
            int nw = _font.MeasureWidth(_notification, S) + pad * 2;
            int nh = lh + pad * 2;
            int nx = (Config.SCREEN_WIDTH  - nw) / 2;
            int ny = Config.SCREEN_HEIGHT / 3;
            sb.Draw(pixel, new Rectangle(nx, ny, nw, nh), Color.Black * (0.85f * alpha));
            DrawBorder(sb, pixel, nx, ny, nw, nh, Color.Cyan * (0.7f * alpha));
            _font.DrawString(sb, _notification, nx + pad, ny + pad, Color.Cyan * alpha, S);
        }
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static void DrawBorder(SpriteBatch sb, Texture2D px, int x, int y, int w, int h, Color c)
    {
        sb.Draw(px, new Rectangle(x,         y,         w, 1), c);
        sb.Draw(px, new Rectangle(x,         y + h - 1, w, 1), c);
        sb.Draw(px, new Rectangle(x,         y,         1, h), c);
        sb.Draw(px, new Rectangle(x + w - 1, y,         1, h), c);
    }

    // ── Called by Game1 to give us font/pixel texture ─────────────────────────

    public void SetResources(PixelFont font, Texture2D pixel)
    {
        _font  = font;
        _pixel = pixel;
    }
}
