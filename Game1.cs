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
    private List<Missile> _missiles = new List<Missile>();
    private List<Asteroid> _asteroids = new List<Asteroid>();

    // Floating critical-hit labels (world-space)
    private record struct CritLabel(float X, float Y, float Timer);
    private List<CritLabel> _critLabels = new();
    private const float CRIT_LABEL_DURATION = 1.2f;

    // Ship builder state
    private string _builderSelectedType = ComponentType.ARMOR;

    // Mouse control state
    private Ship? _selectedShip = null;
    private Vector2? _playerMoveTarget = null;
    private Ship? _playerCombatTarget = null;

    // Autopilot constants
    private const float AUTOPILOT_ARRIVAL_THRESHOLD = 30f;
    private const float AUTOPILOT_ANGLE_THRESHOLD = 0.15f;

    // Wave / score
    private int _wave = 1;
    private int _score = 0;
    private int _kills = 0;
    private bool _gameOver = false;
    private bool _waveClearPending = false;
    private float _waveClearTimer = 0f;
    private const float WAVE_CLEAR_DELAY = 2.5f;

    // Auto-fire at combat target
    private float _autoFireTimer = 0f;
    private const float AUTO_FIRE_INTERVAL = 0.35f;

    // Enemy fire timers (keyed by ShipId) — frame-rate independent
    private Dictionary<int, float> _enemyFireTimers = new Dictionary<int, float>();
    private const float ENEMY_FIRE_INTERVAL = 2.4f;   // seconds between enemy fire attempts

    // Directional damage indicators (screen-edge flash when player is hit)
    private record struct DamageIndicator(float Angle, float Timer);
    private List<DamageIndicator> _damageIndicators = new();
    private const float DAMAGE_INDICATOR_DURATION = 0.7f;

    // Minimap
    private const int MINIMAP_SIZE = 150;
    private const float MINIMAP_WORLD_RADIUS = 1200f;

    // ── Scene management (interior / station / planet overlay) ───────────────
    private SceneManager _sceneManager = new SceneManager();
    private ShipInteriorScene? _interiorScene;
    private InteriorGrid? _shipInteriorGrid;
    private bool _isInInterior = false;

    // Shared UI padding (pixels)
    private const int UI_PAD = 6;

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
        // Reset wave / score / game state
        _wave = 1;
        _score = 0;
        _kills = 0;
        _gameOver = false;
        _waveClearPending = false;
        _waveClearTimer = 0f;
        _autoFireTimer = 0f;
        _playerMoveTarget = null;
        _playerCombatTarget = null;
        _selectedShip = null;
        _projectiles.Clear();
        _missiles.Clear();
        _enemyFireTimers.Clear();
        _damageIndicators.Clear();
        _critLabels.Clear();

        // Create player ship
        _player = new Ship(Config.SCREEN_WIDTH / 2f, Config.SCREEN_HEIGHT / 2f, 0, true);

        // Sync with GameState
        GameState.Instance.PlayerShip = _player;

        // Scatter asteroids around the arena (persistent across waves)
        _asteroids.Clear();
        SpawnAsteroids();

        // Spawn initial wave of enemies
        _enemies.Clear();
        SpawnWave();
    }

    /// <summary>
    /// Switch to the ship interior scene.  Called when the player presses I.
    /// </summary>
    private void EnterInterior()
    {
        if (_interiorScene == null || _shipInteriorGrid == null) return;
        _isInInterior = true;

        var ctx = new InteriorContext(_player ?? GameState.Instance.PlayerShip!, _shipInteriorGrid);
        _sceneManager.SetImmediate(_interiorScene, ctx);
    }

    private void SpawnAsteroids()
    {
        // 12–15 asteroids scattered in a ring around the player start
        int count = 12 + _random.Next(4);
        float cx = _player?.X ?? Config.SCREEN_WIDTH / 2f;
        float cy = _player?.Y ?? Config.SCREEN_HEIGHT / 2f;

        for (int i = 0; i < count; i++)
        {
            float angle  = _random.NextSingle() * MathF.Tau;
            float dist   = 300f + _random.NextSingle() * 900f;
            float x = cx + MathF.Cos(angle) * dist;
            float y = cy + MathF.Sin(angle) * dist;
            int radius = 20 + _random.Next(35);   // 20–54 px radius
            _asteroids.Add(new Asteroid(x, y, radius, _random));
        }
    }

    private void SpawnWave()
    {
        // Wave 1 → 3 enemies, each subsequent wave adds 1 more
        int count = 2 + _wave;

        // Spawn radius grows slightly each wave so enemies aren't on top of the player
        float spawnRadius = 400f + _wave * 80f;

        int nextId = (_enemies.Count > 0 ? _enemies.Max(e => e.ShipId) : 0) + 1;

        for (int i = 0; i < count; i++)
        {
            float angle = _random.NextSingle() * MathF.Tau;
            float dist  = spawnRadius + _random.NextSingle() * 200f;

            float cx = _player?.X ?? Config.SCREEN_WIDTH / 2f;
            float cy = _player?.Y ?? Config.SCREEN_HEIGHT / 2f;
            float x  = cx + MathF.Cos(angle) * dist;
            float y  = cy + MathF.Sin(angle) * dist;

            EnemyType type = PickEnemyType(_wave);
            var enemy = new Ship(x, y, nextId++, false, type);

            if (_enemyShipSprites.Count > 0)
                enemy.PrerenderedTexture = _enemyShipSprites[_random.Next(_enemyShipSprites.Count)];

            _enemies.Add(enemy);
        }
    }

    /// <summary>
    /// Wave-scaled enemy type selection.
    /// Early waves are mostly Scouts; later waves introduce more Gunships and Support ships.
    /// </summary>
    private EnemyType PickEnemyType(int wave)
    {
        int roll = _random.Next(100);
        if (wave <= 2)
            return roll < 80 ? EnemyType.Scout : EnemyType.Gunship;
        if (wave <= 4)
            return roll < 50 ? EnemyType.Scout : (roll < 80 ? EnemyType.Gunship : EnemyType.Support);
        return roll < 25 ? EnemyType.Scout : (roll < 60 ? EnemyType.Gunship : EnemyType.Support);
    }

    private static string EnemyTypeName(EnemyType t) => t switch
    {
        EnemyType.Gunship => "GUNSHIP",
        EnemyType.Support => "SUPPORT",
        _                 => "SCOUT"
    };

    /// <summary>
    /// Replaces the player ship's components with a named preset layout.
    /// Preserves position, velocity and crew.
    /// </summary>
    private void ApplyShipPreset(string preset)
    {
        if (_player == null) return;
        _player.Components.Clear();

        switch (preset)
        {
            case "assault":
                // Heavy firepower — lasers, cannons, and a missile bay
                _player.Components.Add(new Component(ComponentType.CORE, 4, 4));
                _player.Components.Add(new Component(ComponentType.ENGINE, 4, 6));
                _player.Components.Add(new Component(ComponentType.ENGINE, 4, 7));
                _player.Components.Add(new Component(ComponentType.WEAPON_LASER, 3, 2));
                _player.Components.Add(new Component(ComponentType.WEAPON_LASER, 5, 2));
                _player.Components.Add(new Component(ComponentType.WEAPON_CANNON, 4, 1));
                _player.Components.Add(new Component(ComponentType.WEAPON_MISSILE, 3, 3));
                _player.Components.Add(new Component(ComponentType.WEAPON_MISSILE, 5, 3));
                _player.Components.Add(new Component(ComponentType.POWER, 3, 5));
                _player.Components.Add(new Component(ComponentType.POWER, 5, 5));
                _player.Components.Add(new Component(ComponentType.ARMOR, 4, 5));
                break;

            case "tanker":
                // Tank — heavy shields and armor, modest weapons
                _player.Components.Add(new Component(ComponentType.CORE, 4, 4));
                _player.Components.Add(new Component(ComponentType.ENGINE, 4, 7));
                _player.Components.Add(new Component(ComponentType.WEAPON_CANNON, 4, 2));
                _player.Components.Add(new Component(ComponentType.POWER, 3, 5));
                _player.Components.Add(new Component(ComponentType.POWER, 5, 5));
                _player.Components.Add(new Component(ComponentType.SHIELD, 3, 3));
                _player.Components.Add(new Component(ComponentType.SHIELD, 5, 3));
                _player.Components.Add(new Component(ComponentType.SHIELD, 4, 5));
                _player.Components.Add(new Component(ComponentType.ARMOR, 3, 4));
                _player.Components.Add(new Component(ComponentType.ARMOR, 5, 4));
                _player.Components.Add(new Component(ComponentType.ARMOR, 4, 3));
                _player.Components.Add(new Component(ComponentType.ARMOR, 3, 6));
                _player.Components.Add(new Component(ComponentType.ARMOR, 5, 6));
                break;

            case "speedrun":
                // Speed demon — maximum thrust, light weapons
                _player.Components.Add(new Component(ComponentType.CORE, 4, 4));
                _player.Components.Add(new Component(ComponentType.ENGINE, 3, 5));
                _player.Components.Add(new Component(ComponentType.ENGINE, 5, 5));
                _player.Components.Add(new Component(ComponentType.ENGINE, 3, 6));
                _player.Components.Add(new Component(ComponentType.ENGINE, 5, 6));
                _player.Components.Add(new Component(ComponentType.ENGINE, 4, 7));
                _player.Components.Add(new Component(ComponentType.WEAPON_LASER, 4, 3));
                _player.Components.Add(new Component(ComponentType.POWER, 4, 5));
                break;

            default:
                // Default balanced layout
                _player.Components.Add(new Component(ComponentType.CORE, 4, 4));
                _player.Components.Add(new Component(ComponentType.ENGINE, 4, 6));
                _player.Components.Add(new Component(ComponentType.ENGINE, 4, 7));
                _player.Components.Add(new Component(ComponentType.WEAPON_LASER, 3, 3));
                _player.Components.Add(new Component(ComponentType.WEAPON_LASER, 5, 3));
                _player.Components.Add(new Component(ComponentType.WEAPON_CANNON, 4, 2));
                _player.Components.Add(new Component(ComponentType.POWER, 3, 5));
                _player.Components.Add(new Component(ComponentType.POWER, 5, 5));
                _player.Components.Add(new Component(ComponentType.ARMOR, 3, 4));
                _player.Components.Add(new Component(ComponentType.ARMOR, 5, 4));
                _player.Components.Add(new Component(ComponentType.ARMOR, 4, 5));
                break;
        }

        // Force stat recalculation
        _player.AddComponent(new Component(ComponentType.STRUCTURE, -1, -1)); // dummy to trigger recalc
        _player.RemoveComponent(-1, -1);
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

        // ── Set up SceneManager and interior scene ────────────────────────────
        _sceneManager.Initialize(_pixelTexture);
        _shipInteriorGrid = InteriorGrid.CreateStarterShip();

        _interiorScene = new ShipInteriorScene(_sceneManager);
        _interiorScene.SetResources(_pixelFont, _pixelTexture);

        // Register GameState player ship
        GameState.Instance.PlayerShip = _player;
    }

    protected override void Update(GameTime gameTime)
    {
        float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;

        // ── Interior scene is active ──────────────────────────────────────────
        if (_isInInterior)
        {
            _sceneManager.Update(dt);
            _sceneManager.UpdateScene(dt);

            // Detect when the interior scene signals it wants to return to space
            if (_interiorScene?.ExitRequested == true)
                _isInInterior = false;

            _gameTime += dt;
            base.Update(gameTime);
            return;
        }

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

        // Enter ship interior (I key)
        if (keyboardState.IsKeyDown(Keys.I) && !_previousKeyboardState.IsKeyDown(Keys.I))
            EnterInterior();

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

            // Ship preset templates (F1-F4)
            if (keyboardState.IsKeyDown(Keys.F1) && !_previousKeyboardState.IsKeyDown(Keys.F1))
                ApplyShipPreset("assault");
            else if (keyboardState.IsKeyDown(Keys.F2) && !_previousKeyboardState.IsKeyDown(Keys.F2))
                ApplyShipPreset("tanker");
            else if (keyboardState.IsKeyDown(Keys.F3) && !_previousKeyboardState.IsKeyDown(Keys.F3))
                ApplyShipPreset("speedrun");
            else if (keyboardState.IsKeyDown(Keys.F4) && !_previousKeyboardState.IsKeyDown(Keys.F4))
                ApplyShipPreset("default");

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

        // ── Game over state ─────────────────────────────────────────────────
        if (_gameOver)
            return;  // stop updating; player presses R to restart

        if (_player.IsDestroyed())
        {
            _gameOver = true;
            _particles?.CreateExplosion(_player.X, _player.Y, "large");
            _particles?.CreateExplosion(_player.X, _player.Y, "large");
            return;
        }

        // ── Damage indicator timers ─────────────────────────────────────────
        for (int i = _damageIndicators.Count - 1; i >= 0; i--)
        {
            float newTimer = _damageIndicators[i].Timer - dt;
            if (newTimer <= 0f)
                _damageIndicators.RemoveAt(i);
            else
                _damageIndicators[i] = _damageIndicators[i] with { Timer = newTimer };
        }

        // ── Critical-hit label timers ───────────────────────────────────────
        for (int i = _critLabels.Count - 1; i >= 0; i--)
        {
            float newTimer = _critLabels[i].Timer - dt;
            if (newTimer <= 0f)
                _critLabels.RemoveAt(i);
            else
                _critLabels[i] = _critLabels[i] with { Timer = newTimer };
        }

        // ── Wave clear ──────────────────────────────────────────────────────
        if (_waveClearPending)
        {
            _waveClearTimer -= dt;
            if (_waveClearTimer <= 0f)
            {
                _wave++;
                _waveClearPending = false;
                SpawnWave();
            }
            // During wave-clear delay, still let the player move around
        }
        else if (_enemies.Count == 0)
        {
            _score += 500 * _wave;  // wave-clear bonus
            _waveClearPending = true;
            _waveClearTimer = WAVE_CLEAR_DELAY;
        }

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

            if (dist < AUTOPILOT_ARRIVAL_THRESHOLD)
            {
                _playerMoveTarget = null;
            }
            else
            {
                float targetAngle = MathF.Atan2(dy, dx);
                float angleDiff = targetAngle - _player.Angle;
                angleDiff = ((angleDiff + MathF.PI) % MathF.Tau) - MathF.PI;

                if (MathF.Abs(angleDiff) > AUTOPILOT_ANGLE_THRESHOLD)
                    _player.Rotate(angleDiff > 0 ? 1 : -1, dt);
                else
                    _player.ApplyThrust(dt, _particles);
            }
        }

        // Auto-fire: if a combat target is locked, fire toward it automatically
        if (_playerCombatTarget != null && !_playerCombatTarget.IsDestroyed())
        {
            _autoFireTimer -= dt;
            if (_autoFireTimer <= 0f)
            {
                _autoFireTimer = AUTO_FIRE_INTERVAL;
                var autos = _player.FireWeaponsAtTarget(new Vector2(_playerCombatTarget.X, _playerCombatTarget.Y), _playerCombatTarget);
                AddProjectilesAndMissiles(autos, false);
            }
        }

        if (keyboardState.IsKeyDown(Keys.Space))
        {
            // Manual fire: toward combat target if set, otherwise straight ahead
            List<Projectile> fired;
            if (_playerCombatTarget != null && !_playerCombatTarget.IsDestroyed())
                fired = _player.FireWeaponsAtTarget(new Vector2(_playerCombatTarget.X, _playerCombatTarget.Y), _playerCombatTarget);
            else
                fired = _player.FireWeapons();

            AddProjectilesAndMissiles(fired, false);
        }

        // Update player
        _player.Update(dt, null, _particles);

        // Update enemies
        foreach (var enemy in _enemies.ToList())
        {
            enemy.Update(dt, _player, _particles);

            // Enemy AI firing — frame-rate independent timer
            if (!_enemyFireTimers.TryGetValue(enemy.ShipId, out float fireTimer))
                fireTimer = ENEMY_FIRE_INTERVAL * (float)_random.NextDouble(); // stagger initial shots

            fireTimer -= dt;
            if (fireTimer <= 0f)
            {
                fireTimer = ENEMY_FIRE_INTERVAL;
                var fired = enemy.FireWeapons();
                AddProjectilesAndMissiles(fired, false);
            }
            _enemyFireTimers[enemy.ShipId] = fireTimer;
        }

        // Update asteroids + check ship-asteroid collisions
        foreach (var ast in _asteroids)
        {
            ast.Update(dt);

            // Gentle push + damage if a ship runs into an asteroid
            void CheckShipAsteroid(Ship ship, int contactDamage)
            {
                var bounds = ship.GetBounds();
                float shipRadius = (bounds.Width + bounds.Height) / 4f;
                if (ast.OverlapCircle(ship.X, ship.Y, shipRadius))
                {
                    // Push ship away
                    float nx = ship.X - ast.X;
                    float ny = ship.Y - ast.Y;
                    float len = MathF.Sqrt(nx * nx + ny * ny);
                    if (len > 0.01f) { nx /= len; ny /= len; }
                    ship.VX += nx * 120f;
                    ship.VY += ny * 120f;
                    ship.TakeDamage(contactDamage, ast.X, ast.Y);
                    _particles?.CreateDamageSparks(ship.X, ship.Y);
                }
            }

            CheckShipAsteroid(_player, 2);
            foreach (var enemy in _enemies)
                CheckShipAsteroid(enemy, 2);
        }

        // Update projectiles
        foreach (var proj in _projectiles.ToList())
        {
            proj.Update(dt);
            if (!proj.Alive)
                _projectiles.Remove(proj);
        }

        // Update homing missiles
        foreach (var missile in _missiles.ToList())
        {
            missile.Update(dt);
            if (!missile.Alive)
                _missiles.Remove(missile);
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
                _enemyFireTimers.Remove(enemy.ShipId);
                if (_playerCombatTarget == enemy) _playerCombatTarget = null;
                if (_selectedShip == enemy) _selectedShip = null;
                _kills++;
                _score += 100 * _wave;
            }
        }

        // Update camera: centre on player, accounting for zoom level
        UpdateCamera();
    }

    /// <summary>
    /// Sorts new projectiles into either the regular list or the missile list,
    /// and creates the appropriate particle effect for each.
    /// </summary>
    private void AddProjectilesAndMissiles(List<Projectile> fired, bool suppressParticles)
    {
        foreach (var proj in fired)
        {
            if (proj is Missile missile)
            {
                _missiles.Add(missile);
                if (!suppressParticles)
                    _particles?.CreateMissileLaunch(missile.X, missile.Y, missile.Angle);
            }
            else
            {
                _projectiles.Add(proj);
                if (!suppressParticles)
                    _particles?.CreateWeaponFireEffect(proj.X, proj.Y, proj.Angle, proj.ProjectileType);
            }
        }
    }

    private void UpdateCamera()
    {
        if (_player == null) return;
        _cameraX = _player.X - Config.SCREEN_WIDTH  / (2f * _cameraZoom);
        _cameraY = _player.Y - Config.SCREEN_HEIGHT / (2f * _cameraZoom);
    }

    private void CheckCollisions()
    {
        if (_player == null)
            return;

        // Helper to apply a projectile hit and handle crit/shield feedback
        void ApplyHit(Ship ship, Projectile proj)
        {
            ship.TakeDamage(proj.Damage, proj.X, proj.Y);
            if (ship.LastHitWasShielded)
                _particles?.CreateShieldImpact(proj.X, proj.Y);
            else if (ship.LastHitWasCritical)
            {
                _particles?.CreateCriticalHit(proj.X, proj.Y);
                _critLabels.Add(new CritLabel(proj.X, proj.Y - 20, CRIT_LABEL_DURATION));
            }
            else
                _particles?.CreateDamageSparks(proj.X, proj.Y);
        }

        // ── Regular projectiles ─────────────────────────────────────────────
        foreach (var proj in _projectiles.ToList())
        {
            if (!proj.Alive)
                continue;

            // Check collision with asteroids first
            foreach (var ast in _asteroids)
            {
                if (ast.ContainsPoint(proj.X, proj.Y))
                {
                    bool destroyed = ast.TakeDamage(proj.Damage * 2);
                    _particles?.CreateDamageSparks(proj.X, proj.Y);
                    if (destroyed)
                        _particles?.CreateExplosion(ast.X, ast.Y, "medium");
                    proj.Alive = false;
                    break;
                }
            }
            if (!proj.Alive) continue;

            // Check collision with player
            if (proj.OwnerId != _player.ShipId)
            {
                var playerBounds = _player.GetBounds();
                if (proj.CheckCollision(playerBounds))
                {
                    ApplyHit(_player, proj);
                    float hitAngle = MathF.Atan2(proj.Y - _player.Y, proj.X - _player.X);
                    _damageIndicators.Add(new DamageIndicator(hitAngle, DAMAGE_INDICATOR_DURATION));
                    proj.Alive = false;
                }
            }
            if (!proj.Alive) continue;

            // Check collision with enemies
            foreach (var enemy in _enemies)
            {
                if (proj.OwnerId != enemy.ShipId)
                {
                    var enemyBounds = enemy.GetBounds();
                    if (proj.CheckCollision(enemyBounds))
                    {
                        ApplyHit(enemy, proj);
                        proj.Alive = false;
                        break;
                    }
                }
            }
        }

        // ── Missiles ────────────────────────────────────────────────────────
        foreach (var missile in _missiles.ToList())
        {
            if (!missile.Alive) continue;

            // Asteroid collision
            foreach (var ast in _asteroids)
            {
                if (ast.ContainsPoint(missile.X, missile.Y))
                {
                    _particles?.CreateExplosion(missile.X, missile.Y, "small");
                    missile.Alive = false;
                    break;
                }
            }
            if (!missile.Alive) continue;

            // Player hit
            if (missile.OwnerId != _player.ShipId)
            {
                if (_player.GetBounds().Contains((int)missile.X, (int)missile.Y))
                {
                    ApplyHit(_player, missile);
                    _particles?.CreateExplosion(missile.X, missile.Y, "medium");
                    float hitAngle = MathF.Atan2(missile.Y - _player.Y, missile.X - _player.X);
                    _damageIndicators.Add(new DamageIndicator(hitAngle, DAMAGE_INDICATOR_DURATION));
                    missile.Alive = false;
                }
            }
            if (!missile.Alive) continue;

            // Enemy hit
            foreach (var enemy in _enemies)
            {
                if (missile.OwnerId != enemy.ShipId)
                {
                    if (enemy.GetBounds().Contains((int)missile.X, (int)missile.Y))
                    {
                        ApplyHit(enemy, missile);
                        _particles?.CreateExplosion(missile.X, missile.Y, "medium");
                        missile.Alive = false;
                        break;
                    }
                }
            }
        }

        // Remove destroyed asteroids
        _asteroids.RemoveAll(a => a.IsDestroyed());
    }

    protected override void Draw(GameTime gameTime)
    {
        // ── Interior scene override ───────────────────────────────────────────
        if (_isInInterior)
        {
            GraphicsDevice.Clear(new Color(15, 15, 20));

            _spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend);
            _sceneManager.DrawWorld(_spriteBatch, _pixelTexture, _gameTime);
            _sceneManager.DrawUI(_spriteBatch, _pixelTexture, _gameTime);
            _spriteBatch.End();

            base.Draw(gameTime);
            return;
        }

        // Pre-render every ship's component surface onto its cached RenderTarget2D
        // BEFORE touching the backbuffer.  This keeps all render-target switches
        // out of the main draw pass so the backbuffer is never discarded mid-frame.
        _player?.PreRender(_spriteBatch, _pixelTexture, GraphicsDevice, _gameTime, _componentTextures);
        foreach (var enemy in _enemies)
            enemy.PreRender(_spriteBatch, _pixelTexture, GraphicsDevice, _gameTime, _componentTextures);

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

        // Draw asteroids (behind ships)
        foreach (var ast in _asteroids)
            ast.Render(_spriteBatch, _pixelTexture, _cameraX, _cameraY);

        // Draw player
        if (_player != null)
        {
            _player.Render(_spriteBatch, _pixelTexture, _cameraX, _cameraY, GraphicsDevice, _cameraZoom, _gameTime, _componentTextures);
            
            // Draw selection indicator if selected
            if (_selectedShip == _player)
                DrawSelectionIndicator(_player);
        }

        // Draw enemies
        foreach (var enemy in _enemies)
        {
            enemy.Render(_spriteBatch, _pixelTexture, _cameraX, _cameraY, GraphicsDevice, _cameraZoom, _gameTime, _componentTextures);
            
            // Draw selection indicator if selected
            if (_selectedShip == enemy)
                DrawSelectionIndicator(enemy);
        }

        // Draw projectiles
        foreach (var proj in _projectiles)
            proj.Render(_spriteBatch, _pixelTexture, _cameraX, _cameraY);

        // Draw missiles
        foreach (var missile in _missiles)
            missile.Render(_spriteBatch, _pixelTexture, _cameraX, _cameraY);

        // Draw world-space UI overlays (health bars, move/combat target indicators)
        DrawWorldSpaceUI();

        _spriteBatch.End();

        // Draw UI without zoom transformation
        _spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend);
        DrawUI();

        // Full-screen overlays (drawn on top of everything)
        if (_gameOver)
            DrawGameOverOverlay();
        else if (_waveClearPending)
            DrawWaveClearOverlay();

        _spriteBatch.End();

        base.Draw(gameTime);
    }

    private void DrawUI()
    {
        if (_player == null)
            return;

        const int S = 2;    // font scale
        int lh = _pixelFont.LineHeight(S);

        // ── Top-left status panel ───────────────────────────────────────────
        Color modeColor = _mode == Config.MODE_PLAY ? Color.Cyan : Color.Yellow;

        int extraRows = (_paused ? 1 : 0) + (_selectedShip != null ? 1 : 0);
        int panelH = (6 + extraRows) * (lh + 2) + UI_PAD * 2;   // +1 row for wave/score
        int panelW = 300;

        _spriteBatch.Draw(_pixelTexture, new Rectangle(UI_PAD, UI_PAD, panelW, panelH), Color.Black * 0.78f);
        DrawRectangleBorder(UI_PAD, UI_PAD, panelW, panelH, modeColor * 0.6f);

        int tx = UI_PAD + UI_PAD;
        int ty = UI_PAD + UI_PAD;

        // Mode header
        _pixelFont.DrawString(_spriteBatch, _mode == Config.MODE_PLAY ? "PLAY MODE" : "BUILD MODE", tx, ty, modeColor, S);
        ty += lh + 2;

        // HP bar
        int labelW = _pixelFont.MeasureWidth("HP  ", S);
        int barX = tx + labelW;
        int barW = panelW - labelW - UI_PAD * 2 - 4;
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
            $"CREW {wk}/{tot}  ZOOM {_cameraZoom:F1}x  {_enemies.Count} {(_enemies.Count == 1 ? "enemy" : "enemies")}",
            tx, ty, new Color(170, 170, 170), S);
        ty += lh + 2;

        // Wave / score / kills
        _pixelFont.DrawString(_spriteBatch,
            $"WAVE {_wave}   SCORE {_score}   KILLS {_kills}",
            tx, ty, new Color(200, 200, 100), S);
        ty += lh + 2;

        if (_paused)
        {
            _pixelFont.DrawString(_spriteBatch, "** PAUSED **", tx, ty, Color.Yellow, S);
            ty += lh + 2;
        }

        if (_selectedShip != null)
        {
            string sname = _selectedShip.IsPlayer
                ? "Your Ship"
                : $"{EnemyTypeName(_selectedShip.EnemyType)} #{_selectedShip.ShipId}";
            _pixelFont.DrawString(_spriteBatch,
                $"SEL: {sname}  HP:{_selectedShip.TotalHealth}/{_selectedShip.MaxHealth}",
                tx, ty, Color.Yellow, S);
        }

        // ── Weapon status panel (below main panel) ──────────────────────────
        if (_mode == Config.MODE_PLAY)
            DrawWeaponStatusPanel(panelH);

        // ── Directional damage indicators ───────────────────────────────────
        DrawDamageIndicators();

        // ── Top-right: combat target label ─────────────────────────────────
        if (_playerCombatTarget != null)
        {
            string tgt = $"TARGET {EnemyTypeName(_playerCombatTarget.EnemyType)} #{_playerCombatTarget.ShipId}";
            int tw = _pixelFont.MeasureWidth(tgt, S);
            int rx = Config.SCREEN_WIDTH - tw - UI_PAD * 3;
            _spriteBatch.Draw(_pixelTexture, new Rectangle(rx - UI_PAD, UI_PAD, tw + UI_PAD * 2, lh + UI_PAD), Color.Black * 0.78f);
            DrawRectangleBorder(rx - UI_PAD, UI_PAD, tw + UI_PAD * 2, lh + UI_PAD, Color.Red * 0.6f);
            _pixelFont.DrawString(_spriteBatch, tgt, rx, UI_PAD + UI_PAD / 2, Color.Red, S);
        }

        // Top-right: autopilot label
        if (_playerMoveTarget.HasValue)
        {
            string mv = "AUTOPILOT";
            int mw = _pixelFont.MeasureWidth(mv, S);
            int rx = Config.SCREEN_WIDTH - mw - UI_PAD * 3;
            int ry = UI_PAD + (_playerCombatTarget != null ? lh + UI_PAD * 2 : 0);
            _spriteBatch.Draw(_pixelTexture, new Rectangle(rx - UI_PAD, ry, mw + UI_PAD * 2, lh + UI_PAD), Color.Black * 0.78f);
            DrawRectangleBorder(rx - UI_PAD, ry, mw + UI_PAD * 2, lh + UI_PAD, Color.Cyan * 0.6f);
            _pixelFont.DrawString(_spriteBatch, mv, rx, ry + UI_PAD / 2, Color.Cyan, S);
        }

        // ── Builder panel ───────────────────────────────────────────────────
        if (_mode == Config.MODE_BUILD)
        {
            int by = panelH + UI_PAD * 3;
            int bpW = 560;
            int bpH = lh * 4 + UI_PAD * 2;
            _spriteBatch.Draw(_pixelTexture, new Rectangle(UI_PAD, by, bpW, bpH), Color.Black * 0.78f);
            DrawRectangleBorder(UI_PAD, by, bpW, bpH, Color.Yellow * 0.6f);
            int bx = UI_PAD + UI_PAD; int byt = by + UI_PAD;
            _pixelFont.DrawString(_spriteBatch, $"PLACING: {_builderSelectedType.ToUpper()}", bx, byt, Color.Yellow, S);
            byt += lh + 2;
            _pixelFont.DrawString(_spriteBatch, "1:Armor 2:Engine 3:Laser 4:Cannon 5:Reactor 6:Shield", bx, byt, Color.LightGray, S);
            byt += lh + 2;
            _pixelFont.DrawString(_spriteBatch, "7:Crew 8:Ammo 9:Corridor 0:Structure  [L:Place  R:Remove]", bx, byt, Color.Gray, S);
            byt += lh + 2;
            _pixelFont.DrawString(_spriteBatch, "F1:Assault  F2:Tanker  F3:Speed  F4:Default (preset templates)", bx, byt, new Color(160, 200, 160), S);
        }

        // ── Component status panel (when a ship is selected) ─────────────────
        if (_selectedShip != null)
            DrawComponentStatusPanel();

        // ── Bottom controls bar ─────────────────────────────────────────────
        int cbH = lh + UI_PAD * 2;
        _spriteBatch.Draw(_pixelTexture, new Rectangle(0, Config.SCREEN_HEIGHT - cbH, Config.SCREEN_WIDTH, cbH), Color.Black * 0.85f);
        string ctrlsText = _mode == Config.MODE_BUILD
            ? "WASD:Move  B:Exit-Build  F1-F4:Presets  1-0:Select-Type  L:Place  R:Remove  P:Pause  R:Reset  ESC:Quit"
            : "WASD:Move  Space:Fire  RClick-Enemy:Lock  RClick-Space:Autopilot  Scroll:Zoom  B:Build  I:Interior  P:Pause  R:Reset  ESC:Quit";
        _pixelFont.DrawString(_spriteBatch, ctrlsText,
            UI_PAD, Config.SCREEN_HEIGHT - cbH + UI_PAD, new Color(150, 150, 150), S);

        // ── Minimap ─────────────────────────────────────────────────────────
        DrawMinimap(cbH);
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

        // Floating "CRIT!" labels
        const int CS = 2;
        foreach (var lbl in _critLabels)
        {
            float t = lbl.Timer / CRIT_LABEL_DURATION;
            float alpha = t * t;
            float rise = (1f - t) * 30f;   // floats upward as it fades
            int lx = (int)(lbl.X - _cameraX);
            int ly = (int)(lbl.Y - _cameraY - rise);
            string critText = "CRIT!";
            int tw = _pixelFont.MeasureWidth(critText, CS);
            _pixelFont.DrawString(_spriteBatch, critText, lx - tw / 2, ly, Color.Yellow * alpha, CS);
        }
    }

    private void DrawMinimap(int controlBarH)
    {
        if (_player == null) return;

        int mapX = Config.SCREEN_WIDTH  - MINIMAP_SIZE - UI_PAD;
        int mapY = Config.SCREEN_HEIGHT - controlBarH  - MINIMAP_SIZE - UI_PAD;

        // Background + border
        _spriteBatch.Draw(_pixelTexture, new Rectangle(mapX, mapY, MINIMAP_SIZE, MINIMAP_SIZE), Color.Black * 0.78f);
        DrawRectangleBorder(mapX, mapY, MINIMAP_SIZE, MINIMAP_SIZE, Color.Gray * 0.55f);

        // Label
        _pixelFont.DrawString(_spriteBatch, "MAP", mapX + 4, mapY + 4, new Color(100, 100, 100), 1);

        int cx = mapX + MINIMAP_SIZE / 2;
        int cy = mapY + MINIMAP_SIZE / 2;
        float scale = (MINIMAP_SIZE / 2f) / MINIMAP_WORLD_RADIUS;

        // Player — green cross
        _spriteBatch.Draw(_pixelTexture, new Rectangle(cx - 3, cy - 1, 6, 2), Color.Lime);
        _spriteBatch.Draw(_pixelTexture, new Rectangle(cx - 1, cy - 3, 2, 6), Color.Lime);

        // Enemies — color-coded by type, clamped to map edge
        foreach (var enemy in _enemies)
        {
            int dotX = cx + (int)((enemy.X - _player.X) * scale);
            int dotY = cy + (int)((enemy.Y - _player.Y) * scale);
            dotX = Math.Clamp(dotX, mapX + 3, mapX + MINIMAP_SIZE - 3);
            dotY = Math.Clamp(dotY, mapY + 3, mapY + MINIMAP_SIZE - 3);

            bool isCombatTarget = enemy == _playerCombatTarget;
            Color typeColor = enemy.EnemyType switch
            {
                EnemyType.Gunship => new Color(255, 100, 0),   // orange
                EnemyType.Support => new Color(180, 80, 255),  // purple
                _                 => Color.Red                 // scout
            };
            Color dotColor = isCombatTarget ? Color.OrangeRed : typeColor;
            _spriteBatch.Draw(_pixelTexture, new Rectangle(dotX - 2, dotY - 2, 4, 4), dotColor);
        }

        // Asteroids — grey dots
        foreach (var ast in _asteroids)
        {
            int dotX = cx + (int)((ast.X - _player.X) * scale);
            int dotY = cy + (int)((ast.Y - _player.Y) * scale);
            if (dotX >= mapX + 1 && dotX <= mapX + MINIMAP_SIZE - 1 &&
                dotY >= mapY + 1 && dotY <= mapY + MINIMAP_SIZE - 1)
            {
                _spriteBatch.Draw(_pixelTexture, new Rectangle(dotX - 1, dotY - 1, 3, 3), new Color(120, 110, 100));
            }
        }
    }

    /// <summary>Compact weapon readiness panel drawn below the main status panel.</summary>
    private void DrawWeaponStatusPanel(int mainPanelH)
    {
        if (_player == null) return;
        var ws = _player.GetWeaponSummary();
        if (ws.LaserCount <= 0 && ws.CannonCount <= 0 && ws.MissileCount <= 0) return;

        const int S = 2;
        int lh = _pixelFont.LineHeight(S);

        int rows  = (ws.LaserCount   > 0 ? 1 : 0)
                  + (ws.CannonCount  > 0 ? 1 : 0)
                  + (ws.MissileCount > 0 ? 1 : 0)
                  + 1; // +1 for header
        int panelH = rows * (lh + 2) + UI_PAD * 2;
        int panelW = 200;
        int panelY = UI_PAD + mainPanelH + UI_PAD;

        _spriteBatch.Draw(_pixelTexture, new Rectangle(UI_PAD, panelY, panelW, panelH), Color.Black * 0.78f);
        DrawRectangleBorder(UI_PAD, panelY, panelW, panelH, Color.Gray * 0.5f);

        int tx = UI_PAD + UI_PAD;
        int ty = panelY + UI_PAD;
        int labelW = _pixelFont.MeasureWidth("WEAP ", S);
        int barW   = panelW - labelW - UI_PAD * 2 - 4;

        _pixelFont.DrawString(_spriteBatch, "WEAPONS", tx, ty, Color.LightGray, S);
        ty += lh + 2;

        if (ws.LaserCount > 0)
        {
            _pixelFont.DrawString(_spriteBatch, "LAS", tx, ty, Color.Red, S);
            bool ready = ws.LaserReady >= 0.99f;
            DrawBar(tx + labelW, ty, barW, lh - 2, ws.LaserReady,
                    ready ? Color.Red : new Color(160, 50, 50),
                    new Color(50, 0, 0));
            string label = ready ? $"READY x{ws.LaserCount}" : "...";
            _pixelFont.DrawString(_spriteBatch, label, tx + labelW + 2, ty, Color.White, S);
            ty += lh + 2;
        }

        if (ws.CannonCount > 0)
        {
            _pixelFont.DrawString(_spriteBatch, "CAN", tx, ty, Color.Orange, S);
            bool ready = ws.CannonReady >= 0.99f;
            DrawBar(tx + labelW, ty, barW, lh - 2, ws.CannonReady,
                    ready ? Color.Orange : new Color(140, 70, 20),
                    new Color(50, 20, 0));
            string label = ready ? $"READY x{ws.CannonCount}" : "...";
            _pixelFont.DrawString(_spriteBatch, label, tx + labelW + 2, ty, Color.White, S);
            ty += lh + 2;
        }

        if (ws.MissileCount > 0)
        {
            _pixelFont.DrawString(_spriteBatch, "MSL", tx, ty, new Color(0, 200, 230), S);
            bool ready = ws.MissileReady >= 0.99f;
            DrawBar(tx + labelW, ty, barW, lh - 2, ws.MissileReady,
                    ready ? new Color(0, 200, 230) : new Color(0, 90, 110),
                    new Color(0, 30, 40));
            string label = ready ? $"READY x{ws.MissileCount}" : "...";
            _pixelFont.DrawString(_spriteBatch, label, tx + labelW + 2, ty, Color.White, S);
        }
    }

    /// <summary>
    /// Right-side component status panel shown when a ship is selected.
    /// Lists each component with its name and HP bar.
    /// </summary>
    private void DrawComponentStatusPanel()
    {
        if (_selectedShip == null) return;

        const int S = 1;
        int lh = _pixelFont.LineHeight(S);

        var comps = _selectedShip.Components;
        if (comps.Count == 0) return;

        int panelW = 190;
        int panelH = (comps.Count + 1) * (lh + 2) + UI_PAD * 2;
        int panelX = Config.SCREEN_WIDTH - panelW - UI_PAD;
        // Position below the target label (if visible) or at top-right
        int panelY = UI_PAD + (_playerCombatTarget != null ? _pixelFont.LineHeight(2) + UI_PAD * 2 : 0)
                              + (_playerMoveTarget.HasValue ? _pixelFont.LineHeight(2) + UI_PAD * 2 : 0)
                              + UI_PAD * 4;

        _spriteBatch.Draw(_pixelTexture, new Rectangle(panelX, panelY, panelW, panelH), Color.Black * 0.82f);
        DrawRectangleBorder(panelX, panelY, panelW, panelH, Color.Yellow * 0.55f);

        int tx = panelX + UI_PAD;
        int ty = panelY + UI_PAD;

        string shipLabel = _selectedShip.IsPlayer ? "YOUR SHIP" : $"{EnemyTypeName(_selectedShip.EnemyType)} #{_selectedShip.ShipId}";
        _pixelFont.DrawString(_spriteBatch, shipLabel, tx, ty, Color.Yellow, S);
        ty += lh + 2;

        int nameW  = _pixelFont.MeasureWidth("ENGINE_ROOM ", S);
        int barW   = panelW - nameW - UI_PAD * 2;

        foreach (var comp in comps)
        {
            bool alive = comp.Stats.Health > 0;
            float hpFrac = comp.Stats.MaxHealth > 0 ? (float)comp.Stats.Health / comp.Stats.MaxHealth : 0f;

            Color nameColor = alive ? Color.LightGray : new Color(80, 80, 80);
            string name = comp.Stats.Name.ToUpper();
            _pixelFont.DrawString(_spriteBatch, name, tx, ty, nameColor, S);

            Color barFill = hpFrac > 0.5f ? Color.Green : (hpFrac > 0.25f ? Color.Yellow : Color.Red);
            DrawBar(tx + nameW, ty, barW, lh - 1, hpFrac, alive ? barFill : Color.DarkGray, new Color(20, 20, 20));

            ty += lh + 2;
        }
    }

    /// <summary>
    /// Draws a red bar at the screen edge facing the direction the player was last hit from.
    /// Quadratic alpha fade matches the DAMAGE_INDICATOR_DURATION timer.
    /// </summary>
    private void DrawDamageIndicators()
    {
        const int EDGE_BAR_LONG   = 90;   // px length of the bar along the edge
        const int EDGE_BAR_THICK  = 12;   // px depth into the screen
        const int EDGE_MARGIN     = 2;    // px gap from screen edge

        int W = Config.SCREEN_WIDTH;
        int H = Config.SCREEN_HEIGHT;

        foreach (var ind in _damageIndicators)
        {
            float t = ind.Timer / DAMAGE_INDICATOR_DURATION;
            float alpha = t * t * 0.95f;   // quadratic fade
            Color c = Color.Red * alpha;

            float cos = MathF.Cos(ind.Angle);
            float sin = MathF.Sin(ind.Angle);
            float absCos = MathF.Abs(cos);
            float absSin = MathF.Abs(sin);

            if (absCos > absSin)
            {
                // Left or right screen edge
                int ex = cos > 0 ? W - EDGE_BAR_THICK - EDGE_MARGIN : EDGE_MARGIN;
                // Position along the edge, proportional to the vertical component of hit angle
                int eyCenter = H / 2 + (int)(sin / absCos * (H * 0.3f));
                int ey = Math.Clamp(eyCenter - EDGE_BAR_LONG / 2, EDGE_MARGIN, H - EDGE_BAR_LONG - EDGE_MARGIN);
                _spriteBatch.Draw(_pixelTexture, new Rectangle(ex, ey, EDGE_BAR_THICK, EDGE_BAR_LONG), c);
            }
            else
            {
                // Top or bottom screen edge
                int ey = sin > 0 ? H - EDGE_BAR_THICK - EDGE_MARGIN : EDGE_MARGIN;
                int exCenter = W / 2 + (int)(cos / absSin * (W * 0.3f));
                int ex = Math.Clamp(exCenter - EDGE_BAR_LONG / 2, EDGE_MARGIN, W - EDGE_BAR_LONG - EDGE_MARGIN);
                _spriteBatch.Draw(_pixelTexture, new Rectangle(ex, ey, EDGE_BAR_LONG, EDGE_BAR_THICK), c);
            }
        }
    }

    private void DrawGameOverOverlay()
    {
        // Dim the whole screen
        _spriteBatch.Draw(_pixelTexture,
            new Rectangle(0, 0, Config.SCREEN_WIDTH, Config.SCREEN_HEIGHT),
            Color.Black * 0.65f);

        // Panel
        const int W = 420; const int H = 120;
        int px = (Config.SCREEN_WIDTH  - W) / 2;
        int py = (Config.SCREEN_HEIGHT - H) / 2;
        _spriteBatch.Draw(_pixelTexture, new Rectangle(px, py, W, H), Color.Black * 0.90f);
        DrawRectangleBorder(px, py, W, H, Color.Red * 0.8f);

        const int S = 3;
        int lh = _pixelFont.LineHeight(S);

        // "GAME OVER"
        string t1 = "GAME OVER";
        int tw = _pixelFont.MeasureWidth(t1, S);
        _pixelFont.DrawString(_spriteBatch, t1, px + (W - tw) / 2, py + UI_PAD, Color.Red, S);

        // Stats
        const int S2 = 2;
        int lh2 = _pixelFont.LineHeight(S2);
        string t2 = $"WAVE {_wave}   SCORE {_score}   KILLS {_kills}";
        int tw2 = _pixelFont.MeasureWidth(t2, S2);
        _pixelFont.DrawString(_spriteBatch, t2, px + (W - tw2) / 2, py + UI_PAD + lh + 4, Color.White, S2);

        string t3 = "Press R to restart";
        int tw3 = _pixelFont.MeasureWidth(t3, S2);
        float blink = (MathF.Sin(_gameTime * 4f) + 1f) / 2f;
        _pixelFont.DrawString(_spriteBatch, t3, px + (W - tw3) / 2, py + UI_PAD + lh + 4 + lh2 + 4, Color.Yellow * (0.5f + blink * 0.5f), S2);
    }

    private void DrawWaveClearOverlay()
    {
        // Fade out as the timer counts down
        float fade = Math.Clamp(_waveClearTimer / WAVE_CLEAR_DELAY, 0f, 1f);
        if (fade <= 0f) return;

        const int W = 400; const int H = 90;
        int px = (Config.SCREEN_WIDTH  - W) / 2;
        int py = (Config.SCREEN_HEIGHT - H) / 3;
        _spriteBatch.Draw(_pixelTexture, new Rectangle(px, py, W, H), Color.Black * (0.85f * fade));
        DrawRectangleBorder(px, py, W, H, Color.Yellow * (0.8f * fade));

        const int S = 3;
        int lh = _pixelFont.LineHeight(S);

        string t1 = $"WAVE {_wave} COMPLETE!";
        int tw1 = _pixelFont.MeasureWidth(t1, S);
        _pixelFont.DrawString(_spriteBatch, t1, px + (W - tw1) / 2, py + UI_PAD, Color.Yellow * fade, S);

        const int S2 = 2;
        string t2 = $"+{500 * _wave} BONUS  NEXT: WAVE {_wave + 1}  ({_wave + 3} enemies)";
        int tw2 = _pixelFont.MeasureWidth(t2, S2);
        _pixelFont.DrawString(_spriteBatch, t2, px + (W - tw2) / 2, py + UI_PAD + lh + 4, Color.Cyan * fade, S2);
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
