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
    private SpriteBatch _spriteBatch;
    private Texture2D _pixelTexture;
    
    // Component textures
    private Dictionary<string, Texture2D> _componentTextures = new Dictionary<string, Texture2D>();

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
    private bool _mouseTargetingMode = false;
    private Vector2? _mouseTargetPosition = null;

    // UI
    private SpriteFont? _font;

    // Input state
    private KeyboardState _previousKeyboardState;
    private MouseState _previousMouseState;

    private Random _random = new Random();
    private RenderTarget2D? _shipRenderTarget;

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

            _enemies.Add(new Ship(x, y, i + 1, false));
        }

        _projectiles.Clear();
    }

    protected override void LoadContent()
    {
        _spriteBatch = new SpriteBatch(GraphicsDevice);
        
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

        // Toggle targeting mode with T key
        if (keyboardState.IsKeyDown(Keys.T) && !_previousKeyboardState.IsKeyDown(Keys.T))
            _mouseTargetingMode = !_mouseTargetingMode;

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
            // Handle mouse clicks in play mode
            if (mouseState.LeftButton == ButtonState.Pressed && _previousMouseState.LeftButton == ButtonState.Released)
                HandlePlayModeClick(mouseState.Position, true);
            else if (mouseState.RightButton == ButtonState.Pressed && _previousMouseState.RightButton == ButtonState.Released)
                HandlePlayModeClick(mouseState.Position, false);
            
            // Update mouse target position for weapon aiming
            if (_mouseTargetingMode)
            {
                float worldX = (mouseState.Position.X / _cameraZoom) + _cameraX;
                float worldY = (mouseState.Position.Y / _cameraZoom) + _cameraY;
                _mouseTargetPosition = new Vector2(worldX, worldY);
            }
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

            // Check if clicking on player ship
            var playerBounds = _player.GetBounds();
            if (playerBounds.Contains(new Point((int)worldX, (int)worldY)))
            {
                _selectedShip = _player;
                return;
            }

            // Check if clicking on enemy ship
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
            // Right click - set target for selected ship or enable targeting mode
            if (_selectedShip != null && _selectedShip.IsPlayer)
            {
                _mouseTargetingMode = true;
                _mouseTargetPosition = new Vector2(worldX, worldY);
            }
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

        // Handle player input
        KeyboardState keyboardState = Keyboard.GetState();

        if (keyboardState.IsKeyDown(Keys.W) || keyboardState.IsKeyDown(Keys.Up))
            _player.ApplyThrust(dt, _particles);

        // Add reverse thrust with S key
        if (keyboardState.IsKeyDown(Keys.S) || keyboardState.IsKeyDown(Keys.Down))
            _player.ApplyReverseThrust(dt, _particles);

        if (keyboardState.IsKeyDown(Keys.A) || keyboardState.IsKeyDown(Keys.Left))
            _player.Rotate(-1, dt);

        if (keyboardState.IsKeyDown(Keys.D) || keyboardState.IsKeyDown(Keys.Right))
            _player.Rotate(1, dt);

        if (keyboardState.IsKeyDown(Keys.Space))
        {
            // Fire weapons with mouse targeting if enabled
            var projectiles = _mouseTargetingMode && _mouseTargetPosition.HasValue
                ? _player.FireWeaponsAtTarget(_mouseTargetPosition.Value)
                : _player.FireWeapons();
            _projectiles.AddRange(projectiles);
            // Create muzzle flash particles
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

        // Remove destroyed enemies
        foreach (var enemy in _enemies.ToList())
        {
            if (enemy.IsDestroyed())
            {
                _particles?.CreateExplosion(enemy.X, enemy.Y, "large");
                _enemies.Remove(enemy);
            }
        }

        // Update camera to follow player
        _cameraX = _player.X - Config.SCREEN_WIDTH / 2f;
        _cameraY = _player.Y - Config.SCREEN_HEIGHT / 2f;
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
            _player.Render(_spriteBatch, _pixelTexture, _shipRenderTarget, _cameraX, _cameraY, GraphicsDevice, _componentTextures);
            
            // Draw selection indicator if selected
            if (_selectedShip == _player)
                DrawSelectionIndicator(_player);
        }

        // Draw enemies
        foreach (var enemy in _enemies)
        {
            enemy.Render(_spriteBatch, _pixelTexture, _shipRenderTarget, _cameraX, _cameraY, GraphicsDevice, _componentTextures);
            
            // Draw selection indicator if selected
            if (_selectedShip == enemy)
                DrawSelectionIndicator(enemy);
        }

        // Draw projectiles
        foreach (var proj in _projectiles)
            proj.Render(_spriteBatch, _pixelTexture, _cameraX, _cameraY);

        // Draw mouse targeting reticle
        if (_mouseTargetingMode && _mouseTargetPosition.HasValue)
            DrawTargetingReticle(_mouseTargetPosition.Value);

        _spriteBatch.End();

        // Draw UI without zoom transformation
        _spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend);
        DrawUI();
        _spriteBatch.End();

        base.Draw(gameTime);
    }

    private void DrawUI()
    {
        // Draw basic UI elements with better visibility
        if (_player == null)
            return;

        // Draw semi-transparent background panels for UI
        _spriteBatch.Draw(_pixelTexture, new Rectangle(5, 5, 400, 160), Color.Black * 0.7f);

        // Mode indicator
        string modeText = _mode == Config.MODE_PLAY ? "PLAY MODE" : "BUILD MODE";
        DrawText(modeText, 10, 10, Color.White, large: true);

        if (_paused)
        {
            _spriteBatch.Draw(_pixelTexture, new Rectangle(Config.SCREEN_WIDTH / 2 - 80, 5, 160, 30), Color.Black * 0.7f);
            DrawText("PAUSED", Config.SCREEN_WIDTH / 2 - 40, 10, Color.Yellow, large: true);
        }

        // Player stats with bars
        DrawText($"Health: {_player.TotalHealth}/{_player.MaxHealth}", 10, 40, Color.White);
        DrawBar(150, 42, 200, 12, (float)_player.TotalHealth / _player.MaxHealth, Color.Red, Color.DarkRed);

        DrawText($"Power: {_player.PowerAvailable - _player.PowerUsed}/{_player.PowerAvailable}", 10, 60, Color.White);
        DrawBar(150, 62, 200, 12, (float)(Math.Max(0, _player.PowerAvailable - _player.PowerUsed)) / Math.Max(1, _player.PowerAvailable), Color.Cyan, Color.DarkCyan);

        DrawText($"Crew: {_player.CrewManager?.GetWorkingCrew()}/{_player.CrewManager?.GetTotalCrew()} Working", 10, 80, Color.White);

        // Zoom level indicator
        DrawText($"Zoom: {_cameraZoom:F1}x (Mouse Wheel)", 10, 100, Color.Gray);

        // Mouse targeting mode indicator
        if (_mouseTargetingMode)
            DrawText("TARGETING MODE (T to toggle)", 10, 120, Color.Orange);

        // Selected ship info
        if (_selectedShip != null)
        {
            string shipName = _selectedShip.IsPlayer ? "Your Ship" : $"Enemy Ship #{_selectedShip.ShipId}";
            DrawText($"Selected: {shipName}", 10, 140, Color.Yellow);
        }

        // Builder mode UI
        if (_mode == Config.MODE_BUILD)
        {
            _spriteBatch.Draw(_pixelTexture, new Rectangle(5, 170, 600, 60), Color.Black * 0.7f);
            DrawText("Selected: " + _builderSelectedType, 10, 175, Color.Yellow);
            DrawText("1-9,0: Select component | Left Click: Add | Right Click: Remove", 10, 195, Color.White);
            DrawText("7:Quarters 8:Ammo 9:Corridor 0:Structure", 10, 210, Color.Gray);
        }

        // Controls at bottom
        _spriteBatch.Draw(_pixelTexture, new Rectangle(5, Config.SCREEN_HEIGHT - 55, Config.SCREEN_WIDTH - 10, 50), Color.Black * 0.7f);
        DrawText("WASD/Arrows: Move/Rotate | S: Reverse | Space: Fire | T: Target Mode", 
            10, Config.SCREEN_HEIGHT - 50, Color.Gray);
        DrawText("Mouse: Click ships to select | Right-click: Target | Wheel: Zoom | B: Build | P: Pause | R: Reset | ESC: Exit", 
            10, Config.SCREEN_HEIGHT - 30, Color.Gray);
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

    private void DrawText(string text, int x, int y, Color color, bool large = false)
    {
        // Simple pixel-based text drawing with better visibility
        int charWidth = large ? 10 : 8;
        int charHeight = large ? 20 : 16;
        
        // Draw text shadow for better readability
        int width = text.Length * charWidth;
        _spriteBatch.Draw(_pixelTexture, new Rectangle(x + 1, y + 1, width, charHeight), Color.Black * 0.5f);
        
        // Draw text background
        _spriteBatch.Draw(_pixelTexture, new Rectangle(x, y, width, charHeight), color * 0.4f);
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

    private void DrawTargetingReticle(Vector2 worldPosition)
    {
        int screenX = (int)((worldPosition.X - _cameraX));
        int screenY = (int)((worldPosition.Y - _cameraY));

        // Draw crosshair
        int size = 20;
        int gap = 8;
        int thickness = 2;

        Color reticleColor = Color.Red * 0.8f;

        // Horizontal lines
        _spriteBatch.Draw(_pixelTexture, new Rectangle(screenX - size, screenY - thickness / 2, size - gap, thickness), reticleColor);
        _spriteBatch.Draw(_pixelTexture, new Rectangle(screenX + gap, screenY - thickness / 2, size - gap, thickness), reticleColor);

        // Vertical lines
        _spriteBatch.Draw(_pixelTexture, new Rectangle(screenX - thickness / 2, screenY - size, thickness, size - gap), reticleColor);
        _spriteBatch.Draw(_pixelTexture, new Rectangle(screenX - thickness / 2, screenY + gap, thickness, size - gap), reticleColor);

        // Center dot
        _spriteBatch.Draw(_pixelTexture, new Rectangle(screenX - 2, screenY - 2, 4, 4), Color.Red);

        // Outer circle
        float pulse = (float)Math.Sin(_gameTime * 6) * 0.2f + 0.8f;
        DrawCircleOutline(screenX, screenY, 30, Color.Red * pulse);
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
