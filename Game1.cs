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

    // Game state
    private string _mode = Config.MODE_PLAY;
    private bool _paused = false;
    private float _gameTime = 0f;

    // Camera
    private float _cameraX = 0f;
    private float _cameraY = 0f;

    // Game objects
    private Starfield? _starfield;
    private NebulaSystem? _nebulas;
    private ParticleSystem? _particles;
    private Ship? _player;
    private List<Ship> _enemies = new List<Ship>();
    private List<Projectile> _projectiles = new List<Projectile>();

    // Ship builder state
    private string _builderSelectedType = ComponentType.ARMOR;

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

            // Handle mouse clicks
            if (mouseState.LeftButton == ButtonState.Pressed && _previousMouseState.LeftButton == ButtonState.Released)
                HandleBuilderClick(mouseState.Position, true);
            else if (mouseState.RightButton == ButtonState.Pressed && _previousMouseState.RightButton == ButtonState.Released)
                HandleBuilderClick(mouseState.Position, false);
        }

        _previousKeyboardState = keyboardState;
        _previousMouseState = mouseState;
    }

    private void HandleBuilderClick(Point position, bool leftClick)
    {
        if (_player == null)
            return;

        // Convert screen position to world position
        float worldX = position.X + _cameraX;
        float worldY = position.Y + _cameraY;

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

        if (keyboardState.IsKeyDown(Keys.A) || keyboardState.IsKeyDown(Keys.Left))
            _player.Rotate(-1, dt);

        if (keyboardState.IsKeyDown(Keys.D) || keyboardState.IsKeyDown(Keys.Right))
            _player.Rotate(1, dt);

        if (keyboardState.IsKeyDown(Keys.Space))
        {
            var projectiles = _player.FireWeapons();
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

        _spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend);

        // Draw starfield (deepest layer)
        _starfield?.Render(_spriteBatch, _pixelTexture, _cameraX, _cameraY, _gameTime);

        // Draw nebulas (middle background layer)
        _nebulas?.Render(_spriteBatch, _pixelTexture, _cameraX, _cameraY);

        // Draw particles (background layer)
        _particles?.Render(_spriteBatch, _pixelTexture, _cameraX, _cameraY, Config.SCREEN_WIDTH, Config.SCREEN_HEIGHT);

        // Draw player
        if (_player != null)
            _player.Render(_spriteBatch, _pixelTexture, _shipRenderTarget, _cameraX, _cameraY, GraphicsDevice);

        // Draw enemies
        foreach (var enemy in _enemies)
            enemy.Render(_spriteBatch, _pixelTexture, _shipRenderTarget, _cameraX, _cameraY, GraphicsDevice);

        // Draw projectiles
        foreach (var proj in _projectiles)
            proj.Render(_spriteBatch, _pixelTexture, _cameraX, _cameraY);

        // Draw UI
        DrawUI();

        _spriteBatch.End();

        base.Draw(gameTime);
    }

    private void DrawUI()
    {
        // Draw basic UI elements
        if (_player == null)
            return;

        // Mode indicator
        string modeText = _mode == Config.MODE_PLAY ? "PLAY MODE" : "BUILD MODE";
        DrawText(modeText, 10, 10, Color.White);

        if (_paused)
            DrawText("PAUSED", Config.SCREEN_WIDTH / 2 - 40, 10, Color.Yellow);

        // Player stats
        DrawText($"Health: {_player.TotalHealth}/{_player.MaxHealth}", 10, 40, Color.Green);
        DrawText($"Power: {_player.PowerAvailable - _player.PowerUsed}/{_player.PowerAvailable}", 10, 60, Color.Cyan);

        // Builder mode UI
        if (_mode == Config.MODE_BUILD)
        {
            DrawText("Selected: " + _builderSelectedType, 10, 90, Color.Yellow);
            DrawText("1-6: Select component | Left Click: Add | Right Click: Remove", 10, 110, Color.White);
        }

        // Controls
        DrawText("Controls: WASD/Arrows: Move | Space: Fire | B: Build Mode | P: Pause | R: Reset | ESC: Exit", 
            10, Config.SCREEN_HEIGHT - 30, Color.Gray);
    }

    private void DrawText(string text, int x, int y, Color color)
    {
        // Simple pixel-based text drawing (very basic)
        // In a real game, you would use SpriteFont
        // For now, we'll just draw a colored rectangle as placeholder
        int width = text.Length * 8;
        int height = 16;
        _spriteBatch.Draw(_pixelTexture, new Rectangle(x, y, width, height), color * 0.3f);
    }
}
