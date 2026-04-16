using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Subspace;

public class Game1 : Game
{
    private GraphicsDeviceManager _graphics;
    private SpriteBatch _spriteBatch = null!;
    private Texture2D _pixelTexture = null!;
    
    // Component textures
    private Dictionary<string, Texture2D> _componentTextures = new Dictionary<string, Texture2D>();

    // Enemy ship sprites (pre-rendered)
    private List<Texture2D> _enemyShipSprites = new List<Texture2D>();

    // Nebula textures
    private Texture2D[] _nebulaTextures = Array.Empty<Texture2D>();

    // Game state
    private string _mode = Config.MODE_PLAY;
    private bool _paused = false;
    private float _gameTime = 0f;

    // Camera
    private float _cameraX = 0f;
    private float _cameraY = 0f;
    private float _cameraZoom = 1.5f; // Start with 1.5x zoom to make ships more visible

    // Game objects
    private Starfield? _starfield;
    private NebulaSystem? _nebulas;
    private ParticleSystem? _particles;
    private Ship? _player;
    private List<Ship> _enemies = new List<Ship>();
    private List<Projectile> _projectiles = new List<Projectile>();

    // Ship builder state
    private string _builderSelectedType = ComponentType.ARMOR;

    // Mouse control state
    private Ship? _selectedShip = null;
    private Vector2? _playerMoveTarget = null;
    private Ship? _playerCombatTarget = null;

    // UI
    private PixelFont _pixelFont = null!;

    // Input state
    private KeyboardState _previousKeyboardState;
    private MouseState _previousMouseState;

    private Random _random = new Random();

    public Game1()
    {
        _graphics = new GraphicsDeviceManager(this);
        Content.RootDirectory = "Content";
        IsMouseVisible = true;

        // Set window size
        _graphics.PreferredBackBufferWidth = Config.SCREEN_WIDTH;
        _graphics.PreferredBackBufferHeight = Config.SCREEN_HEIGHT;
    }

    protected override void Initialize()
    {
        Window.Title = "Subspace - Cosmoteer-Inspired Space Combat";

        // Create pixel texture
        _pixelTexture = new Texture2D(GraphicsDevice, 1, 1);
        _pixelTexture.SetData(new[] { Color.White });

        // Initialize game objects
        _starfield = new Starfield();
        _nebulas = new NebulaSystem();
        _particles = new ParticleSystem();

        InitGame();

        base.Initialize();
    }

    private void InitGame()
    {
        // Create player ship
        _player = new Ship(Config.SCREEN_WIDTH / 2f, Config.SCREEN_HEIGHT / 2f, 0, true);

        // Create enemy ships
        _enemies.Clear();
        for (int i = 0; i < 3; i++)
        {
            float x = _random.Next(100, Config.SCREEN_WIDTH - 100);
            float y = _random.Next(100, Config.SCREEN_HEIGHT - 100);

            // Make sure enemies don't spawn too close to player
            while (Math.Sqrt(Math.Pow(x - _player.X, 2) + Math.Pow(y - _player.Y, 2)) < 300)
            {
                x = _random.Next(100, Config.SCREEN_WIDTH - 100);
                y = _random.Next(100, Config.SCREEN_HEIGHT - 100);
            }

            var enemy = new Ship(x, y, i + 1, false);

            // Assign a random pre-rendered sprite if available
            if (_enemyShipSprites.Count > 0)
                enemy.PrerenderedTexture = _enemyShipSprites[_random.Next(_enemyShipSprites.Count)];

            _enemies.Add(enemy);
        }

        _projectiles.Clear();
    }

    protected override void LoadContent()
    {
        _spriteBatch = new SpriteBatch(GraphicsDevice);
        _pixelFont = new PixelFont(_pixelTexture);
        
        // Load component textures
        try
        {
            _componentTextures[ComponentType.CORE] = Content.Load<Texture2D>("Sprites/component_core");
            _componentTextures[ComponentType.ENGINE] = Content.Load<Texture2D>("Sprites/component_engine");
            _componentTextures[ComponentType.WEAPON_LASER] = Content.Load<Texture2D>("Sprites/component_weapon_laser");
            _componentTextures[ComponentType.WEAPON_CANNON] = Content.Load<Texture2D>("Sprites/component_weapon_cannon");
            _componentTextures[ComponentType.ARMOR] = Content.Load<Texture2D>("Sprites/component_armor");
            _componentTextures[ComponentType.POWER] = Content.Load<Texture2D>("Sprites/component_power");
            _componentTextures[ComponentType.SHIELD] = Content.Load<Texture2D>("Sprites/component_shield");
            _componentTextures[ComponentType.CREW_QUARTERS] = Content.Load<Texture2D>("Sprites/component_crew_quarters");
            _componentTextures[ComponentType.AMMO_FACTORY] = Content.Load<Texture2D>("Sprites/component_ammo_factory");
            _componentTextures[ComponentType.CORRIDOR] = Content.Load<Texture2D>("Sprites/component_corridor");
            _componentTextures[ComponentType.STRUCTURE] = Content.Load<Texture2D>("Sprites/component_structure");
            _componentTextures[ComponentType.ENGINE_ROOM] = Content.Load<Texture2D>("Sprites/component_engine_room");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Warning: Could not load component textures: {ex.Message}");
            Console.WriteLine("Falling back to simple rendering.");
        }

        // Load nebula background textures
        try
        {
            _nebulaTextures = new[]
            {
                Content.Load<Texture2D>("Sprites/Background/Nebula1"),
                Content.Load<Texture2D>("Sprites/Background/Nebula2"),
                Content.Load<Texture2D>("Sprites/Background/Nebula3"),
            };
            _nebulas?.LoadTextures(_nebulaTextures);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Warning: Could not load nebula textures: {ex.Message}");
        }

        // Load enemy ship sprites
        try
        {
            for (int i = 1; i <= 13; i++)
            {
                _enemyShipSprites.Add(Content.Load<Texture2D>($"Sprites/EnemyShips/{i}"));
                _enemyShipSprites.Add(Content.Load<Texture2D>($"Sprites/EnemyShips/{i}B"));
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Warning: Could not load enemy ship sprites: {ex.Message}");
        }

        // Note: In a real game, you would load a font from Content pipeline
        // For now, we'll skip text rendering or use a basic approach
    }

    protected override void Update(GameTime gameTime)
    {
        float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;

        HandleInput();

        if (!_paused)
        {
            UpdateGame(dt);
            _gameTime += dt;
        }

        base.Update(gameTime);
    }

    private void HandleInput()
    {
        KeyboardState keyboardState = Keyboard.GetState();
        MouseState mouseState = Mouse.GetState();

        // Check for exit
        if (keyboardState.IsKeyDown(Keys.Escape))
            Exit();

        // Check for pause
        if (keyboardState.IsKeyDown(Keys.P) && !_previousKeyboardState.IsKeyDown(Keys.P))
            _paused = !_paused;

        // Check for mode toggle
        if (keyboardState.IsKeyDown(Keys.B) && !_previousKeyboardState.IsKeyDown(Keys.B))
            _mode = _mode == Config.MODE_PLAY ? Config.MODE_BUILD : Config.MODE_PLAY;

        // Check for reset
        if (keyboardState.IsKeyDown(Keys.R) && !_previousKeyboardState.IsKeyDown(Keys.R))
            InitGame();

        // Camera zoom with mouse wheel
        int scrollDelta = mouseState.ScrollWheelValue - _previousMouseState.ScrollWheelValue;
        if (scrollDelta != 0)
        {
            _cameraZoom += scrollDelta * 0.001f;
            _cameraZoom = Math.Clamp(_cameraZoom, 0.5f, 3.0f);
        }

        // Toggle targeting mode with T key — removed; right-click sets targets directly

        // Builder controls
        if (_mode == Config.MODE_BUILD)
        {
            if (keyboardState.IsKeyDown(Keys.D1) && !_previousKeyboardState.IsKeyDown(Keys.D1))
                _builderSelectedType = ComponentType.ARMOR;
            else if (keyboardState.IsKeyDown(Keys.D2) && !_previousKeyboardState.IsKeyDown(Keys.D2))
                _builderSelectedType = ComponentType.ENGINE;
            else if (keyboardState.IsKeyDown(Keys.D3) && !_previousKeyboardState.IsKeyDown(Keys.D3))
                _builderSelectedType = ComponentType.WEAPON_LASER;
            else if (keyboardState.IsKeyDown(Keys.D4) && !_previousKeyboardState.IsKeyDown(Keys.D4))
                _builderSelectedType = ComponentType.WEAPON_CANNON;
            else if (keyboardState.IsKeyDown(Keys.D5) && !_previousKeyboardState.IsKeyDown(Keys.D5))
                _builderSelectedType = ComponentType.POWER;
            else if (keyboardState.IsKeyDown(Keys.D6) && !_previousKeyboardState.IsKeyDown(Keys.D6))
                _builderSelectedType = ComponentType.SHIELD;
            else if (keyboardState.IsKeyDown(Keys.D7) && !_previousKeyboardState.IsKeyDown(Keys.D7))
                _builderSelectedType = ComponentType.CREW_QUARTERS;
            else if (keyboardState.IsKeyDown(Keys.D8) && !_previousKeyboardState.IsKeyDown(Keys.D8))
                _builderSelectedType = ComponentType.AMMO_FACTORY;
            else if (keyboardState.IsKeyDown(Keys.D9) && !_previousKeyboardState.IsKeyDown(Keys.D9))
                _builderSelectedType = ComponentType.CORRIDOR;
            else if (keyboardState.IsKeyDown(Keys.D0) && !_previousKeyboardState.IsKeyDown(Keys.D0))
                _builderSelectedType = ComponentType.STRUCTURE;

            // Handle mouse clicks in build mode
            if (mouseState.LeftButton == ButtonState.Pressed && _previousMouseState.LeftButton == ButtonState.Released)
                HandleBuilderClick(mouseState.Position, true);
            else if (mouseState.RightButton == ButtonState.Pressed && _previousMouseState.RightButton == ButtonState.Released)
                HandleBuilderClick(mouseState.Position, false);
        }
        else if (_mode == Config.MODE_PLAY)
        {
            if (mouseState.LeftButton == ButtonState.Pressed && _previousMouseState.LeftButton == ButtonState.Released)
                HandlePlayModeClick(mouseState.Position, true);
            else if (mouseState.RightButton == ButtonState.Pressed && _previousMouseState.RightButton == ButtonState.Released)
                HandlePlayModeClick(mouseState.Position, false);
        }

        _previousKeyboardState = keyboardState;
        _previousMouseState = mouseState;
    }

    private void HandleBuilderClick(Point position, bool leftClick)
    {
        if (_player == null)
            return;

        // Convert screen position to world position (accounting for zoom)
        float worldX = (position.X / _cameraZoom) + _cameraX;
        float worldY = (position.Y / _cameraZoom) + _cameraY;

        // Convert to ship local space
        float localX = worldX - _player.X;
        float localY = worldY - _player.Y;

        // Convert to grid coordinates
        int gridX = (int)((localX / Config.GRID_SIZE) + _player.GridWidth / 2f);
        int gridY = (int)((localY / Config.GRID_SIZE) + _player.GridHeight / 2f);

        // Check if within bounds
        if (gridX >= 0 && gridX < _player.GridWidth && gridY >= 0 && gridY < _player.GridHeight)
        {
            if (leftClick)
            {
                // Add component
                var existing = _player.GetComponentAt(gridX, gridY);
                if (existing == null)
                {
                    var comp = new Component(_builderSelectedType, gridX, gridY);
                    _player.AddComponent(comp);
                }
            }
            else
            {
                // Remove component
                _player.RemoveComponent(gridX, gridY);
            }
        }
    }

    private void HandlePlayModeClick(Point position, bool leftClick)
    {
        if (_player == null)
            return;

        // Convert screen position to world position (accounting for zoom)
        float worldX = (position.X / _cameraZoom) + _cameraX;
        float worldY = (position.Y / _cameraZoom) + _cameraY;

        if (leftClick)
        {
            // Select ship at clicked position
            _selectedShip = null;

            var playerBounds = _player.GetBounds();
            if (playerBounds.Contains(new Point((int)worldX, (int)worldY)))
            {
                _selectedShip = _player;
                return;
            }

            foreach (var enemy in _enemies)
            {
                var enemyBounds = enemy.GetBounds();
                if (enemyBounds.Contains(new Point((int)worldX, (int)worldY)))
                {
                    _selectedShip = enemy;
                    return;
                }
            }
        }
        else
        {
            // Right click: if an enemy is under the cursor set it as combat target;
            // otherwise send the player ship to that world position (autopilot).
            foreach (var enemy in _enemies)
            {
                var enemyBounds = enemy.GetBounds();
                if (enemyBounds.Contains(new Point((int)worldX, (int)worldY)))
                {
                    _playerCombatTarget = enemy;
                    _playerMoveTarget = null;
                    return;
                }
            }

            // No enemy hit — set autopilot move target
            _playerMoveTarget = new Vector2(worldX, worldY);
            _playerCombatTarget = null;
        }
    }

    private void UpdateGame(float dt)
    {
        // Always update particles
        _particles?.Update(dt);

        if (_mode == Config.MODE_PLAY)
            UpdatePlayMode(dt);
    }

    private void UpdatePlayMode(float dt)
    {
        if (_player == null)
            return;

        // Handle player manual input
        KeyboardState keyboardState = Keyboard.GetState();

        bool manualControl =
            keyboardState.IsKeyDown(Keys.W) || keyboardState.IsKeyDown(Keys.Up) ||
            keyboardState.IsKeyDown(Keys.S) || keyboardState.IsKeyDown(Keys.Down) ||
            keyboardState.IsKeyDown(Keys.A) || keyboardState.IsKeyDown(Keys.Left) ||
            keyboardState.IsKeyDown(Keys.D) || keyboardState.IsKeyDown(Keys.Right);

        // Manual keys cancel autopilot
        if (manualControl)
            _playerMoveTarget = null;

        if (keyboardState.IsKeyDown(Keys.W) || keyboardState.IsKeyDown(Keys.Up))
            _player.ApplyThrust(dt, _particles);

        if (keyboardState.IsKeyDown(Keys.S) || keyboardState.IsKeyDown(Keys.Down))
            _player.ApplyReverseThrust(dt, _particles);

        if (keyboardState.IsKeyDown(Keys.A) || keyboardState.IsKeyDown(Keys.Left))
            _player.Rotate(-1, dt);

        if (keyboardState.IsKeyDown(Keys.D) || keyboardState.IsKeyDown(Keys.Right))
            _player.Rotate(1, dt);

        // Autopilot: steer and thrust toward the move target
        if (_playerMoveTarget.HasValue)
        {
            float dx = _playerMoveTarget.Value.X - _player.X;
            float dy = _playerMoveTarget.Value.Y - _player.Y;
            float dist = MathF.Sqrt(dx * dx + dy * dy);

            if (dist < 30f)
            {
                _playerMoveTarget = null;
            }
            else
            {
                float targetAngle = MathF.Atan2(dy, dx);
                float angleDiff = targetAngle - _player.Angle;
                while (angleDiff > MathF.PI) angleDiff -= 2 * MathF.PI;
                while (angleDiff < -MathF.PI) angleDiff += 2 * MathF.PI;

                if (MathF.Abs(angleDiff) > 0.15f)
                    _player.Rotate(angleDiff > 0 ? 1 : -1, dt);
                else
                    _player.ApplyThrust(dt, _particles);
            }
        }

        if (keyboardState.IsKeyDown(Keys.Space))
        {
            // Fire toward combat target if one is set, otherwise fire forward
            List<Projectile> projectiles;
            if (_playerCombatTarget != null && !_playerCombatTarget.IsDestroyed())
                projectiles = _player.FireWeaponsAtTarget(new Vector2(_playerCombatTarget.X, _playerCombatTarget.Y));
            else
                projectiles = _player.FireWeapons();

            _projectiles.AddRange(projectiles);
            foreach (var proj in projectiles)
                _particles?.CreateWeaponFireEffect(proj.X, proj.Y, proj.Angle, proj.ProjectileType);
        }

        // Update player
        _player.Update(dt);

        // Update enemies
        foreach (var enemy in _enemies.ToList())
        {
            enemy.Update(dt, _player);

            // Enemy AI firing
            if (_random.NextDouble() < 0.02)  // 2% chance per frame
            {
                var projectiles = enemy.FireWeapons();
                _projectiles.AddRange(projectiles);
                foreach (var proj in projectiles)
                    _particles?.CreateWeaponFireEffect(proj.X, proj.Y, proj.Angle, proj.ProjectileType);
            }
        }

        // Update projectiles
        foreach (var proj in _projectiles.ToList())
        {
            proj.Update(dt);
            if (!proj.Alive)
                _projectiles.Remove(proj);
        }

        // Check collisions
        CheckCollisions();

        // Remove destroyed enemies; clear stale references
        foreach (var enemy in _enemies.ToList())
        {
            if (enemy.IsDestroyed())
            {
                _particles?.CreateExplosion(enemy.X, enemy.Y, "large");
                _enemies.Remove(enemy);
                if (_playerCombatTarget == enemy) _playerCombatTarget = null;
                if (_selectedShip == enemy) _selectedShip = null;
            }
        }

        // Update camera: centre on player, accounting for zoom level
        _cameraX = _player.X - Config.SCREEN_WIDTH / (2f * _cameraZoom);
        _cameraY = _player.Y - Config.SCREEN_HEIGHT / (2f * _cameraZoom);
    }

    private void CheckCollisions()
    {
        if (_player == null)
            return;

        foreach (var proj in _projectiles.ToList())
        {
            if (!proj.Alive)
                continue;

            // Check collision with player
            if (proj.OwnerId != _player.ShipId)
            {
                var playerBounds = _player.GetBounds();
                if (proj.CheckCollision(playerBounds))
                {
                    _player.TakeDamage(proj.Damage, proj.X, proj.Y);
                    _particles?.CreateDamageSparks(proj.X, proj.Y);
                    proj.Alive = false;
                }
            }

            // Check collision with enemies
            foreach (var enemy in _enemies)
            {
                if (proj.OwnerId != enemy.ShipId)
                {
                    var enemyBounds = enemy.GetBounds();
                    if (proj.CheckCollision(enemyBounds))
                    {
                        enemy.TakeDamage(proj.Damage, proj.X, proj.Y);
                        _particles?.CreateDamageSparks(proj.X, proj.Y);
                        proj.Alive = false;
                        break;
                    }
                }
            }
        }
    }

    protected override void Draw(GameTime gameTime)
    {
        GraphicsDevice.Clear(Color.Black);

        // Create a transformation matrix for the zoom
        Matrix transformMatrix = Matrix.CreateScale(_cameraZoom);
        
        _spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, null, null, null, null, transformMatrix);

        // Draw starfield (deepest layer)
        _starfield?.Render(_spriteBatch, _pixelTexture, _cameraX, _cameraY, _gameTime);

        // Draw nebulas (middle background layer)
        _nebulas?.Render(_spriteBatch, _pixelTexture, _cameraX, _cameraY);

        // Draw particles (background layer)
        _particles?.Render(_spriteBatch, _pixelTexture, _cameraX, _cameraY, Config.SCREEN_WIDTH, Config.SCREEN_HEIGHT);

        // Draw player
        if (_player != null)
        {
            _player.Render(_spriteBatch, _pixelTexture, _cameraX, _cameraY, GraphicsDevice, _cameraZoom, _componentTextures);
            
            // Draw selection indicator if selected
            if (_selectedShip == _player)
                DrawSelectionIndicator(_player);
        }

        // Draw enemies
        foreach (var enemy in _enemies)
        {
            enemy.Render(_spriteBatch, _pixelTexture, _cameraX, _cameraY, GraphicsDevice, _cameraZoom, _componentTextures);
            
            // Draw selection indicator if selected
            if (_selectedShip == enemy)
                DrawSelectionIndicator(enemy);
        }

        // Draw projectiles
        foreach (var proj in _projectiles)
            proj.Render(_spriteBatch, _pixelTexture, _cameraX, _cameraY);

        // Draw world-space UI overlays (health bars, move/combat target indicators)
        DrawWorldSpaceUI();

        _spriteBatch.End();

        // Draw UI without zoom transformation
        _spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend);
        DrawUI();
        _spriteBatch.End();

        base.Draw(gameTime);
    }

    private void DrawUI()
    {
        if (_player == null)
            return;

        const int S = 2;    // font scale
        const int PAD = 6;
        int lh = _pixelFont.LineHeight(S);

        // ── Top-left status panel ───────────────────────────────────────────
        Color modeColor = _mode == Config.MODE_PLAY ? Color.Cyan : Color.Yellow;

        int extraRows = (_paused ? 1 : 0) + (_selectedShip != null ? 1 : 0);
        int panelH = (5 + extraRows) * (lh + 2) + PAD * 2;
        int panelW = 270;

        _spriteBatch.Draw(_pixelTexture, new Rectangle(PAD, PAD, panelW, panelH), Color.Black * 0.78f);
        DrawRectangleBorder(PAD, PAD, panelW, panelH, modeColor * 0.6f);

        int tx = PAD + PAD;
        int ty = PAD + PAD;

        // Mode header
        _pixelFont.DrawString(_spriteBatch, _mode == Config.MODE_PLAY ? "PLAY MODE" : "BUILD MODE", tx, ty, modeColor, S);
        ty += lh + 2;

        // HP bar
        int labelW = _pixelFont.MeasureWidth("HP  ", S);
        int barX = tx + labelW;
        int barW = panelW - labelW - PAD * 2 - 4;
        _pixelFont.DrawString(_spriteBatch, "HP ", tx, ty, Color.LightGray, S);
        float hpPct = (float)_player.TotalHealth / Math.Max(1, _player.MaxHealth);
        DrawBar(barX, ty, barW, lh - 2, hpPct, Color.Red, new Color(80, 0, 0));
        _pixelFont.DrawString(_spriteBatch, $"{_player.TotalHealth}/{_player.MaxHealth}", barX + 2, ty, Color.White, S);
        ty += lh + 2;

        // Power bar
        int freePwr = _player.PowerAvailable - _player.PowerUsed;
        _pixelFont.DrawString(_spriteBatch, "PWR", tx, ty, Color.LightGray, S);
        float pwrPct = (float)Math.Max(0, freePwr) / Math.Max(1, _player.PowerAvailable);
        DrawBar(barX, ty, barW, lh - 2, pwrPct, new Color(0, 210, 255), new Color(0, 50, 80));
        _pixelFont.DrawString(_spriteBatch, $"{freePwr}/{_player.PowerAvailable}", barX + 2, ty, Color.White, S);
        ty += lh + 2;

        // Speed bar
        float spd = MathF.Sqrt(_player.VX * _player.VX + _player.VY * _player.VY);
        _pixelFont.DrawString(_spriteBatch, "SPD", tx, ty, Color.LightGray, S);
        DrawBar(barX, ty, barW, lh - 2, spd / Config.MAX_VELOCITY, new Color(80, 220, 80), new Color(10, 50, 10));
        _pixelFont.DrawString(_spriteBatch, $"{(int)spd}", barX + 2, ty, Color.White, S);
        ty += lh + 2;

        // Crew / zoom / enemy count
        int wk = _player.CrewManager?.GetWorkingCrew() ?? 0;
        int tot = _player.CrewManager?.GetTotalCrew() ?? 0;
        _pixelFont.DrawString(_spriteBatch,
            $"CREW {wk}/{tot}  ZOOM {_cameraZoom:F1}x  {_enemies.Count} enemy",
            tx, ty, new Color(170, 170, 170), S);
        ty += lh + 2;

        if (_paused)
        {
            _pixelFont.DrawString(_spriteBatch, "** PAUSED **", tx, ty, Color.Yellow, S);
            ty += lh + 2;
        }

        if (_selectedShip != null)
        {
            string sname = _selectedShip.IsPlayer ? "Your Ship" : $"Enemy #{_selectedShip.ShipId}";
            _pixelFont.DrawString(_spriteBatch,
                $"SEL: {sname}  HP:{_selectedShip.TotalHealth}/{_selectedShip.MaxHealth}",
                tx, ty, Color.Yellow, S);
        }

        // ── Top-right: combat target label ─────────────────────────────────
        if (_playerCombatTarget != null)
        {
            string tgt = $"TARGET Enemy #{_playerCombatTarget.ShipId}";
            int tw = _pixelFont.MeasureWidth(tgt, S);
            int rx = Config.SCREEN_WIDTH - tw - PAD * 3;
            _spriteBatch.Draw(_pixelTexture, new Rectangle(rx - PAD, PAD, tw + PAD * 2, lh + PAD), Color.Black * 0.78f);
            DrawRectangleBorder(rx - PAD, PAD, tw + PAD * 2, lh + PAD, Color.Red * 0.6f);
            _pixelFont.DrawString(_spriteBatch, tgt, rx, PAD + PAD / 2, Color.Red, S);
        }

        // Top-right: autopilot label
        if (_playerMoveTarget.HasValue)
        {
            string mv = "AUTOPILOT";
            int mw = _pixelFont.MeasureWidth(mv, S);
            int rx = Config.SCREEN_WIDTH - mw - PAD * 3;
            int ry = PAD + (_playerCombatTarget != null ? lh + PAD * 2 : 0);
            _spriteBatch.Draw(_pixelTexture, new Rectangle(rx - PAD, ry, mw + PAD * 2, lh + PAD), Color.Black * 0.78f);
            DrawRectangleBorder(rx - PAD, ry, mw + PAD * 2, lh + PAD, Color.Cyan * 0.6f);
            _pixelFont.DrawString(_spriteBatch, mv, rx, ry + PAD / 2, Color.Cyan, S);
        }

        // ── Builder panel ───────────────────────────────────────────────────
        if (_mode == Config.MODE_BUILD)
        {
            int by = panelH + PAD * 3;
            int bpW = 500;
            int bpH = lh * 3 + PAD * 2;
            _spriteBatch.Draw(_pixelTexture, new Rectangle(PAD, by, bpW, bpH), Color.Black * 0.78f);
            DrawRectangleBorder(PAD, by, bpW, bpH, Color.Yellow * 0.6f);
            int bx = PAD + PAD; int byt = by + PAD;
            _pixelFont.DrawString(_spriteBatch, $"PLACING: {_builderSelectedType.ToUpper()}", bx, byt, Color.Yellow, S);
            byt += lh + 2;
            _pixelFont.DrawString(_spriteBatch, "1:Armor 2:Engine 3:Laser 4:Cannon 5:Reactor 6:Shield", bx, byt, Color.LightGray, S);
            byt += lh + 2;
            _pixelFont.DrawString(_spriteBatch, "7:Crew 8:Ammo 9:Corridor 0:Structure  [L:Place  R:Remove]", bx, byt, Color.Gray, S);
        }

        // ── Bottom controls bar ─────────────────────────────────────────────
        int cbH = lh + PAD * 2;
        _spriteBatch.Draw(_pixelTexture, new Rectangle(0, Config.SCREEN_HEIGHT - cbH, Config.SCREEN_WIDTH, cbH), Color.Black * 0.85f);
        _pixelFont.DrawString(_spriteBatch,
            "WASD:Move  Space:Fire  LClick:Select  RClick:Target/Move  Scroll:Zoom  B:Build  P:Pause  R:Reset  ESC:Quit",
            PAD, Config.SCREEN_HEIGHT - cbH + PAD, new Color(150, 150, 150), S);
    }

    /// <summary>Draws overlays that live in world (zoom-transformed) space.</summary>
    private void DrawWorldSpaceUI()
    {
        // Enemy health bars above each enemy ship
        foreach (var enemy in _enemies)
        {
            var bounds = enemy.GetBounds();
            int sx = (int)(bounds.X - _cameraX);
            int sy = (int)(bounds.Y - _cameraY) - 8;
            int bw = Math.Max(10, bounds.Width);
            if (enemy.MaxHealth > 0)
                DrawBar(sx, sy, bw, 4, (float)enemy.TotalHealth / enemy.MaxHealth, Color.Red, new Color(80, 0, 0));
        }

        // Autopilot move-target indicator
        if (_playerMoveTarget.HasValue && _player != null)
        {
            int px = (int)(_player.X - _cameraX);
            int py = (int)(_player.Y - _cameraY);
            int tx2 = (int)(_playerMoveTarget.Value.X - _cameraX);
            int ty2 = (int)(_playerMoveTarget.Value.Y - _cameraY);
            DrawLine(px, py, tx2, ty2, Color.Cyan * 0.35f);
            float pulse = MathF.Sin(_gameTime * 4f) * 0.3f + 0.7f;
            DrawCircleOutline(tx2, ty2, 14, Color.Cyan * pulse);
        }

        // Combat-target ring
        if (_playerCombatTarget != null)
        {
            int tx2 = (int)(_playerCombatTarget.X - _cameraX);
            int ty2 = (int)(_playerCombatTarget.Y - _cameraY);
            float pulse = MathF.Sin(_gameTime * 6f) * 0.3f + 0.7f;
            DrawCircleOutline(tx2, ty2, 38, Color.Red * pulse);
            DrawCircleOutline(tx2, ty2, 32, Color.OrangeRed * (pulse * 0.5f));
        }
    }

    private void DrawBar(int x, int y, int width, int height, float fillPercent, Color fillColor, Color bgColor)
    {
        // Background
        _spriteBatch.Draw(_pixelTexture, new Rectangle(x, y, width, height), bgColor);
        
        // Fill
        int fillWidth = (int)(width * Math.Clamp(fillPercent, 0f, 1f));
        if (fillWidth > 0)
            _spriteBatch.Draw(_pixelTexture, new Rectangle(x, y, fillWidth, height), fillColor);
        
        // Border
        DrawRectangleBorder(x, y, width, height, Color.White * 0.5f);
    }

    private void DrawRectangleBorder(int x, int y, int width, int height, Color color)
    {
        _spriteBatch.Draw(_pixelTexture, new Rectangle(x, y, width, 1), color); // Top
        _spriteBatch.Draw(_pixelTexture, new Rectangle(x, y + height - 1, width, 1), color); // Bottom
        _spriteBatch.Draw(_pixelTexture, new Rectangle(x, y, 1, height), color); // Left
        _spriteBatch.Draw(_pixelTexture, new Rectangle(x + width - 1, y, 1, height), color); // Right
    }

    private void DrawSelectionIndicator(Ship ship)
    {
        var bounds = ship.GetBounds();
        int screenX = (int)(bounds.X - _cameraX);
        int screenY = (int)(bounds.Y - _cameraY);
        int width = bounds.Width;
        int height = bounds.Height;

        // Pulsing selection box
        float pulse = (float)Math.Sin(_gameTime * 4) * 0.3f + 0.7f;
        Color selectionColor = (ship.IsPlayer ? Color.Cyan : Color.Yellow) * pulse;

        // Draw corner brackets
        int bracketSize = 20;
        int thickness = 3;

        // Top-left
        _spriteBatch.Draw(_pixelTexture, new Rectangle(screenX - 5, screenY - 5, bracketSize, thickness), selectionColor);
        _spriteBatch.Draw(_pixelTexture, new Rectangle(screenX - 5, screenY - 5, thickness, bracketSize), selectionColor);

        // Top-right
        _spriteBatch.Draw(_pixelTexture, new Rectangle(screenX + width - bracketSize + 5, screenY - 5, bracketSize, thickness), selectionColor);
        _spriteBatch.Draw(_pixelTexture, new Rectangle(screenX + width + 2, screenY - 5, thickness, bracketSize), selectionColor);

        // Bottom-left
        _spriteBatch.Draw(_pixelTexture, new Rectangle(screenX - 5, screenY + height + 2, bracketSize, thickness), selectionColor);
        _spriteBatch.Draw(_pixelTexture, new Rectangle(screenX - 5, screenY + height - bracketSize + 5, thickness, bracketSize), selectionColor);

        // Bottom-right
        _spriteBatch.Draw(_pixelTexture, new Rectangle(screenX + width - bracketSize + 5, screenY + height + 2, bracketSize, thickness), selectionColor);
        _spriteBatch.Draw(_pixelTexture, new Rectangle(screenX + width + 2, screenY + height - bracketSize + 5, thickness, bracketSize), selectionColor);
    }

    private void DrawCircleOutline(int centerX, int centerY, int radius, Color color)
    {
        int segments = 32;
        for (int i = 0; i < segments; i++)
        {
            float angle1 = (float)(i * 2 * Math.PI / segments);
            float angle2 = (float)((i + 1) * 2 * Math.PI / segments);

            int x1 = centerX + (int)(Math.Cos(angle1) * radius);
            int y1 = centerY + (int)(Math.Sin(angle1) * radius);
            int x2 = centerX + (int)(Math.Cos(angle2) * radius);
            int y2 = centerY + (int)(Math.Sin(angle2) * radius);

            DrawLine(x1, y1, x2, y2, color);
        }
    }

    private void DrawLine(int x1, int y1, int x2, int y2, Color color)
    {
        int dx = Math.Abs(x2 - x1);
        int dy = Math.Abs(y2 - y1);
        int steps = Math.Max(dx, dy);

        if (steps == 0)
            return;

        float xIncrement = (x2 - x1) / (float)steps;
        float yIncrement = (y2 - y1) / (float)steps;

        float x = x1;
        float y = y1;

        for (int i = 0; i <= steps; i++)
        {
            _spriteBatch.Draw(_pixelTexture, new Rectangle((int)x, (int)y, 2, 2), color);
            x += xIncrement;
            y += yIncrement;
        }
    }
}
