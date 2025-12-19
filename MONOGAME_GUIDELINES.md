# MonoGame Development Guidelines

## Overview

This document provides comprehensive coding guidelines and best practices for developing games with MonoGame, specifically tailored for the Subspace project. These guidelines are based on official MonoGame documentation, community best practices, and established game development patterns.

---

## Table of Contents

1. [Project Structure](#project-structure)
2. [Coding Conventions](#coding-conventions)
3. [Game Architecture](#game-architecture)
4. [Content Pipeline and Asset Management](#content-pipeline-and-asset-management)
5. [Rendering Best Practices](#rendering-best-practices)
6. [Memory Management and Performance](#memory-management-and-performance)
7. [Input Handling](#input-handling)
8. [Cross-Platform Development](#cross-platform-development)
9. [Common Patterns](#common-patterns)
10. [Testing and Debugging](#testing-and-debugging)
11. [Resources](#resources)

---

## Project Structure

### Recommended File Organization

```
YourGame/
├── *.cs                      # Game logic classes
├── YourGame.csproj           # Project file
├── Program.cs                # Entry point with error handling
├── Game1.cs                  # Main game class
├── Content/                  # Content pipeline directory
│   ├── Content.mgcb          # Content project file
│   ├── Textures/             # Texture assets
│   ├── Fonts/                # SpriteFont files
│   ├── Audio/                # Sound effects and music
│   └── Shaders/              # Custom effect files
├── Systems/                  # Game systems (optional)
│   ├── Physics/
│   ├── AI/
│   └── UI/
└── Components/               # Game components (if using ECS)
```

### Separation of Concerns

- **Game1.cs**: Main game loop, initialization, and high-level coordination
- **Separate files**: One class per file for game objects, systems, and components
- **Config/Constants**: Centralize all constants in a dedicated Config class
- **Utilities**: Create separate utility classes for common operations

---

## Coding Conventions

### C# Naming Conventions

Follow standard .NET naming conventions:

```csharp
// PascalCase for classes, methods, properties, and public fields
public class Ship { }
public void Update(float deltaTime) { }
public int Health { get; set; }

// camelCase for private fields and local variables
private float _velocityX;
private SpriteBatch _spriteBatch;
float deltaTime = 0.016f;

// UPPER_CASE for constants
public const int MAX_HEALTH = 100;
public const string MODE_PLAY = "play";
```

### Code Organization

```csharp
// Order of class members:
public class Ship
{
    // 1. Constants
    private const float MAX_SPEED = 300f;
    
    // 2. Fields (private)
    private float _x;
    private float _y;
    
    // 3. Properties (public)
    public float X { get; set; }
    public float Y { get; set; }
    
    // 4. Constructor(s)
    public Ship(float x, float y) { }
    
    // 5. Public methods
    public void Update(float dt) { }
    public void Render(SpriteBatch spriteBatch) { }
    
    // 6. Private methods
    private void UpdateVelocity(float dt) { }
}
```

### Documentation

Use XML documentation comments for public APIs:

```csharp
/// <summary>
/// Represents a spaceship with modular components.
/// </summary>
public class Ship
{
    /// <summary>
    /// Updates the ship's position and physics.
    /// </summary>
    /// <param name="deltaTime">Time elapsed since last frame in seconds</param>
    public void Update(float deltaTime) { }
}
```

---

## Game Architecture

### The Game Loop

MonoGame provides a structured game loop:

```csharp
public class Game1 : Game
{
    // Initialize is called once after construction
    protected override void Initialize()
    {
        // Initialize game objects, systems, and state
        base.Initialize();
    }
    
    // LoadContent is called once after Initialize
    protected override void LoadContent()
    {
        // Load textures, fonts, sounds from Content Pipeline
        _spriteBatch = new SpriteBatch(GraphicsDevice);
        _texture = Content.Load<Texture2D>("MyTexture");
    }
    
    // Update is called every frame (default 60 FPS)
    protected override void Update(GameTime gameTime)
    {
        float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;
        
        // 1. Handle input
        HandleInput();
        
        // 2. Update game state
        UpdateGameLogic(dt);
        
        // 3. Check win/loss conditions
        CheckGameState();
        
        base.Update(gameTime);
    }
    
    // Draw is called every frame after Update
    protected override void Draw(GameTime gameTime)
    {
        GraphicsDevice.Clear(Color.Black);
        
        _spriteBatch.Begin();
        // Draw game objects
        _spriteBatch.End();
        
        base.Draw(gameTime);
    }
}
```

### Key Principles

1. **Keep Update and Draw separate**: Never mix game logic in Draw
2. **Use delta time**: Always multiply velocities by `deltaTime` for frame-rate independence
3. **Avoid allocations in loops**: Pre-allocate collections and reuse them
4. **Fail fast**: Use early returns and guard clauses

---

## Content Pipeline and Asset Management

### Always Use the Content Pipeline

The Content Pipeline preprocesses assets into optimized .xnb files:

**Benefits:**
- Platform-optimized formats
- Faster loading times
- Better memory usage
- GPU compatibility

### Adding Assets

1. Open Content.mgcb with MGCB Editor
2. Add your assets (Right-click → Add Existing Item)
3. Set appropriate properties (compression, format)
4. Build the content project

### Loading Assets

```csharp
// In LoadContent()
_playerTexture = Content.Load<Texture2D>("Textures/Player");
_font = Content.Load<SpriteFont>("Fonts/Arial");
_explosionSound = Content.Load<SoundEffect>("Audio/Explosion");
```

### Asset Management Best Practices

```csharp
public class AssetManager
{
    private ContentManager _content;
    private Dictionary<string, Texture2D> _textureCache;
    
    public AssetManager(ContentManager content)
    {
        _content = content;
        _textureCache = new Dictionary<string, Texture2D>();
    }
    
    public Texture2D GetTexture(string name)
    {
        // Cache textures to avoid multiple loads
        if (!_textureCache.ContainsKey(name))
        {
            _textureCache[name] = _content.Load<Texture2D>(name);
        }
        return _textureCache[name];
    }
    
    public void UnloadAssets()
    {
        // Dispose resources when changing scenes
        foreach (var texture in _textureCache.Values)
        {
            texture?.Dispose();
        }
        _textureCache.Clear();
    }
}
```

### Important Notes

- **Platform-specific builds**: .xnb files are not cross-platform; rebuild for each target
- **Dispose unused assets**: Prevent memory leaks by disposing Texture2D and other resources
- **Use texture atlases**: Combine sprites into spritesheets for better performance
- **Never load in Update/Draw**: Load all content during initialization or scene transitions

---

## Rendering Best Practices

### SpriteBatch Fundamentals

SpriteBatch batches draw calls for efficient 2D rendering:

```csharp
protected override void Draw(GameTime gameTime)
{
    GraphicsDevice.Clear(Color.Black);
    
    // Begin batch
    _spriteBatch.Begin(
        SpriteSortMode.Deferred,  // Default, fastest
        BlendState.AlphaBlend,    // Standard alpha blending
        SamplerState.PointClamp,  // Pixel-perfect rendering
        null, null, null, 
        Matrix.CreateTranslation(-_cameraX, -_cameraY, 0)  // Camera transform
    );
    
    // Draw calls
    _spriteBatch.Draw(_texture, position, Color.White);
    
    // End batch (submits to GPU)
    _spriteBatch.End();
}
```

### Minimize State Changes

```csharp
// BAD: Multiple Begin/End pairs hurt performance
_spriteBatch.Begin();
_spriteBatch.Draw(_texture1, pos1, Color.White);
_spriteBatch.End();

_spriteBatch.Begin();
_spriteBatch.Draw(_texture2, pos2, Color.White);
_spriteBatch.End();

// GOOD: Batch draws together
_spriteBatch.Begin();
_spriteBatch.Draw(_texture1, pos1, Color.White);
_spriteBatch.Draw(_texture2, pos2, Color.White);
_spriteBatch.End();
```

### Use Source Rectangles

Draw sprites from texture atlases using source rectangles:

```csharp
// Define sprite regions in the atlas
Rectangle playerIdleFrame = new Rectangle(0, 0, 32, 32);
Rectangle playerRunFrame = new Rectangle(32, 0, 32, 32);

// Draw specific frame
_spriteBatch.Draw(
    _spriteSheet,        // Full texture atlas
    position,            // Destination position
    playerIdleFrame,     // Source rectangle
    Color.White          // Tint color
);
```

### Drawing Order

Organize draw calls to minimize overdraw:

```csharp
_spriteBatch.Begin();

// 1. Background layers (furthest)
DrawStarfield();

// 2. Game objects
DrawEnemies();
DrawPlayer();
DrawProjectiles();

// 3. Effects (particles, explosions)
DrawParticles();

// 4. UI (closest, always on top)
DrawHealthBar();
DrawScore();

_spriteBatch.End();
```

### Custom Pixel Drawing

For procedural graphics (current Subspace approach):

```csharp
// Create a 1x1 white pixel texture once
_pixelTexture = new Texture2D(GraphicsDevice, 1, 1);
_pixelTexture.SetData(new[] { Color.White });

// Draw rectangles by scaling
_spriteBatch.Draw(
    _pixelTexture,
    new Rectangle(x, y, width, height),
    color  // Color tints the white pixel
);
```

---

## Memory Management and Performance

### Garbage Collection Awareness

C# uses automatic garbage collection, but frequent allocations cause performance hiccups.

#### Minimize Allocations in Update/Draw

```csharp
// BAD: Creates new objects every frame
protected override void Update(GameTime gameTime)
{
    var enemies = new List<Enemy>();  // ❌ Allocates every frame
    var velocity = new Vector2(1, 0);  // ❌ Allocates every frame
}

// GOOD: Reuse objects
private List<Enemy> _enemies = new List<Enemy>();
private Vector2 _velocity;

protected override void Update(GameTime gameTime)
{
    _enemies.Clear();  // ✅ Reuse list
    _velocity.X = 1;   // ✅ Modify existing struct
}
```

#### Avoid LINQ in Hot Paths

```csharp
// BAD: LINQ allocates enumerators
var activeEnemies = _enemies.Where(e => e.Active).ToList();

// GOOD: Manual loop
for (int i = 0; i < _enemies.Count; i++)
{
    if (_enemies[i].Active)
    {
        // Process enemy
    }
}
```

### Object Pooling

Pool frequently created/destroyed objects:

```csharp
public class ProjectilePool
{
    private List<Projectile> _pool;
    private int _nextAvailable = 0;
    
    public ProjectilePool(int capacity)
    {
        _pool = new List<Projectile>(capacity);
        for (int i = 0; i < capacity; i++)
        {
            _pool.Add(new Projectile());
        }
    }
    
    public Projectile Get()
    {
        for (int i = 0; i < _pool.Count; i++)
        {
            int index = (_nextAvailable + i) % _pool.Count;
            if (!_pool[index].Active)
            {
                _nextAvailable = (index + 1) % _pool.Count;
                _pool[index].Active = true;
                return _pool[index];
            }
        }
        return null;  // Pool exhausted
    }
    
    public void Return(Projectile projectile)
    {
        projectile.Reset();
        projectile.Active = false;
    }
}
```

### When to Use Pooling

**Good candidates:**
- Projectiles/bullets
- Particles
- Enemy spawns
- Temporary visual effects

**Poor candidates:**
- Long-lived objects (player, managers)
- Objects with complex state
- Rarely created objects

### Structs vs Classes

```csharp
// Use struct for small, value-type data (allocated on stack)
public struct Particle
{
    public Vector2 Position;
    public Color Color;
    public float Lifetime;
}

// Use class for complex objects with identity
public class Ship
{
    public List<Component> Components { get; set; }
    public void Update(float dt) { }
}
```

---

## Input Handling

### Keyboard Input

```csharp
private KeyboardState _previousKeyboardState;
private KeyboardState _currentKeyboardState;

protected override void Update(GameTime gameTime)
{
    _currentKeyboardState = Keyboard.GetState();
    
    // Continuous input (held keys)
    if (_currentKeyboardState.IsKeyDown(Keys.W))
    {
        _player.MoveForward();
    }
    
    // Single press (detect key down transition)
    if (_currentKeyboardState.IsKeyDown(Keys.Space) && 
        !_previousKeyboardState.IsKeyDown(Keys.Space))
    {
        _player.Fire();
    }
    
    _previousKeyboardState = _currentKeyboardState;
}
```

### Mouse Input

```csharp
private MouseState _previousMouseState;
private MouseState _currentMouseState;

protected override void Update(GameTime gameTime)
{
    _currentMouseState = Mouse.GetState();
    
    // Mouse position
    int mouseX = _currentMouseState.X;
    int mouseY = _currentMouseState.Y;
    
    // Click detection
    if (_currentMouseState.LeftButton == ButtonState.Pressed &&
        _previousMouseState.LeftButton == ButtonState.Released)
    {
        HandleClick(mouseX, mouseY);
    }
    
    _previousMouseState = _currentMouseState;
}
```

### Gamepad Support

```csharp
private GamePadState _gamePadState;

protected override void Update(GameTime gameTime)
{
    _gamePadState = GamePad.GetState(PlayerIndex.One);
    
    if (_gamePadState.IsConnected)
    {
        // Thumbstick input
        Vector2 leftStick = _gamePadState.ThumbSticks.Left;
        _player.Move(leftStick.X, leftStick.Y);
        
        // Button input
        if (_gamePadState.IsButtonDown(Buttons.A))
        {
            _player.Jump();
        }
    }
}
```

---

## Cross-Platform Development

### Test on All Target Platforms

- **Windows**: Primary development platform
- **Linux**: Test with DesktopGL
- **macOS**: Different graphics drivers, test thoroughly
- **Mobile**: Different input paradigms and performance

### Platform-Specific Code

```csharp
#if WINDOWS
    // Windows-specific code
#elif LINUX
    // Linux-specific code
#elif OSX
    // macOS-specific code
#elif ANDROID || IOS
    // Mobile-specific code
#endif
```

### Resolution Independence

```csharp
// Support multiple resolutions
_graphics.PreferredBackBufferWidth = 1920;
_graphics.PreferredBackBufferHeight = 1080;
_graphics.IsFullScreen = false;
_graphics.ApplyChanges();

// Scale UI elements relative to screen size
float scaleX = GraphicsDevice.Viewport.Width / 1920f;
float scaleY = GraphicsDevice.Viewport.Height / 1080f;
```

---

## Common Patterns

### Game State Management

```csharp
public enum GameState
{
    MainMenu,
    Playing,
    Paused,
    GameOver
}

public class Game1 : Game
{
    private GameState _state = GameState.MainMenu;
    
    protected override void Update(GameTime gameTime)
    {
        switch (_state)
        {
            case GameState.MainMenu:
                UpdateMainMenu();
                break;
            case GameState.Playing:
                UpdateGameplay(gameTime);
                break;
            case GameState.Paused:
                UpdatePauseMenu();
                break;
            case GameState.GameOver:
                UpdateGameOver();
                break;
        }
    }
}
```

### Component Pattern

```csharp
public class GameObject
{
    public Vector2 Position { get; set; }
    public List<Component> Components { get; private set; }
    
    public GameObject()
    {
        Components = new List<Component>();
    }
    
    public T GetComponent<T>() where T : Component
    {
        return Components.OfType<T>().FirstOrDefault();
    }
    
    public void AddComponent(Component component)
    {
        component.Parent = this;
        Components.Add(component);
    }
    
    public void Update(float dt)
    {
        foreach (var component in Components)
        {
            component.Update(dt);
        }
    }
}

public abstract class Component
{
    public GameObject Parent { get; set; }
    public abstract void Update(float dt);
}
```

### Singleton Pattern (Use Sparingly)

```csharp
public class AudioManager
{
    private static AudioManager _instance;
    public static AudioManager Instance
    {
        get
        {
            if (_instance == null)
                _instance = new AudioManager();
            return _instance;
        }
    }
    
    private AudioManager() { }
    
    public void PlaySound(string soundName) { }
}
```

---

## Testing and Debugging

### Exception Handling

```csharp
// Program.cs with comprehensive error handling
try
{
    using var game = new Game1();
    game.Run();
}
catch (NoSuitableGraphicsDeviceException ex)
{
    Console.Error.WriteLine("Graphics device error: " + ex.Message);
    LogCrash(ex);
    Environment.Exit(1);
}
catch (Exception ex)
{
    Console.Error.WriteLine("Unexpected error: " + ex.Message);
    LogCrash(ex);
    Environment.Exit(1);
}
```

### Debug Visualization

```csharp
#if DEBUG
private void DrawDebugInfo()
{
    _spriteBatch.DrawString(_debugFont, 
        $"FPS: {_fps:F1}\n" +
        $"Objects: {_gameObjects.Count}\n" +
        $"Memory: {GC.GetTotalMemory(false) / 1024 / 1024}MB",
        new Vector2(10, 10), 
        Color.Yellow);
}
#endif
```

### Performance Profiling

```csharp
// Measure frame time
private float _totalTime;
private int _frameCount;
private float _fps;

protected override void Update(GameTime gameTime)
{
    _totalTime += (float)gameTime.ElapsedGameTime.TotalSeconds;
    _frameCount++;
    
    if (_totalTime >= 1.0f)
    {
        _fps = _frameCount / _totalTime;
        _frameCount = 0;
        _totalTime = 0;
    }
}
```

---

## Resources

### Official Documentation

- **MonoGame Documentation**: https://docs.monogame.net/
- **API Reference**: https://docs.monogame.net/api/
- **Tutorials**: https://docs.monogame.net/articles/tutorials.html
- **Community Forums**: https://community.monogame.net/

### Recommended Books

- **MonoGame Mastery** by Louis Salin and Gabriel Fenton
- **Learning C# by Developing Games with Unity** (C# fundamentals)
- **Game Programming Patterns** by Robert Nystrom

### Community Resources

- **MonoGame.Extended**: https://www.monogameextended.net/ (Useful extensions)
- **MonoGame Samples**: https://github.com/CartBlanche/MonoGame-Samples
- **RB Whitaker's Tutorials**: http://rbwhitaker.wikidot.com/monogame-tutorials

### Tools

- **MGCB Editor**: Content pipeline tool (built-in)
- **Aseprite**: Sprite creation and animation
- **Tiled**: 2D level editor with MonoGame support
- **Visual Studio / VS Code / Rider**: IDEs with excellent C# support

---

## Conclusion

Following these guidelines will help you write clean, performant, and maintainable MonoGame code. Remember:

1. **Profile before optimizing**: Measure what's actually slow
2. **Keep it simple**: Start with basic patterns, add complexity only when needed
3. **Test on target platforms**: Don't assume cross-platform compatibility
4. **Document your code**: Help future you and other developers
5. **Learn from the community**: MonoGame has a wealth of shared knowledge

Happy game development! 🎮
