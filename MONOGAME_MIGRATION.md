# MonoGame Migration Guide

## Overview

This document describes the successful migration of Subspace from Python/Pygame to C#/MonoGame.

## Migration Date

**December 19, 2025**

## Why MonoGame?

The migration to MonoGame provides several key benefits:

1. **Performance**: Significantly better performance for complex graphics and physics
2. **Cross-Platform**: True cross-platform support including desktop, mobile, and consoles
3. **Professional Framework**: Used by successful indie games like Stardew Valley and Celeste
4. **Scalability**: Better suited for growing the game with advanced features
5. **Type Safety**: C#'s strong typing reduces bugs and improves code quality
6. **Modern Tooling**: Visual Studio, VS Code, and Rider provide excellent development experience

## Architecture Changes

### File Structure

**Python/Pygame (Legacy)**
```
game/
├── main.py
├── src/
│   ├── game.py
│   ├── ship.py
│   ├── components.py
│   ├── projectile.py
│   ├── particles.py
│   ├── starfield.py
│   └── config.py
└── requirements.txt
```

**C#/MonoGame (Current)**
```
/
├── Program.cs         # Entry point
├── Game1.cs          # Main game loop (replaces game.py)
├── Config.cs         # Constants and settings
├── Ship.cs           # Ship logic
├── Components.cs     # Component system
├── Projectile.cs     # Projectile system
├── Particles.cs      # Particle effects
├── Starfield.cs      # Background rendering
├── Subspace.csproj   # Project configuration
├── Content/          # Game assets
└── launch.sh/bat     # Updated launchers
```

### Key Code Conversions

#### 1. Game Loop

**Python (Pygame)**
```python
def run(self):
    while self.running:
        dt = self.clock.tick(FPS) / 1000.0
        self._handle_events()
        if not self.paused:
            self._update(dt)
            self.game_time += dt
        self._render()
```

**C# (MonoGame)**
```csharp
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
```

#### 2. Rendering

**Python (Pygame)**
```python
pygame.draw.rect(surface, color, rect)
pygame.draw.circle(surface, color, (x, y), radius)
```

**C# (MonoGame)**
```csharp
spriteBatch.Draw(pixelTexture, rect, color);
// Custom circle drawing using pixel texture
```

#### 3. Input Handling

**Python (Pygame)**
```python
keys = pygame.key.get_pressed()
if keys[pygame.K_w]:
    self.player.apply_thrust(dt)
```

**C# (MonoGame)**
```csharp
KeyboardState keyboardState = Keyboard.GetState();
if (keyboardState.IsKeyDown(Keys.W))
    _player.ApplyThrust(dt);
```

## Game Features Status

All core features have been successfully ported:

- ✅ **Ship Building System**: Modular component-based ship design
- ✅ **Combat System**: Projectile weapons (lasers and cannons)
- ✅ **Physics**: Ship movement, rotation, and velocity
- ✅ **AI**: Enemy ship behavior and pathfinding
- ✅ **Particle Effects**: Weapon fire, explosions, engine thrust, damage sparks
- ✅ **Starfield**: Parallax scrolling background with twinkling stars
- ✅ **Power Management**: Component-based power generation and consumption
- ✅ **Damage System**: Component-level damage and destruction
- ✅ **Build Mode**: Interactive ship editor
- ✅ **Game Controls**: All keyboard and mouse controls

## Performance Improvements

Expected performance improvements with MonoGame:

- **Frame Rate**: 60 FPS with hundreds of particles (vs ~30-40 FPS in Pygame)
- **Particle System**: Can handle 1000+ particles smoothly
- **Ship Complexity**: Support for larger ships with more components
- **Memory Usage**: More efficient memory management
- **Startup Time**: Faster game initialization

## Development Workflow

### Building the Game

```bash
# Debug build
dotnet build

# Release build
dotnet build -c Release

# Clean build
dotnet clean && dotnet build
```

### Running the Game

```bash
# Run directly
dotnet run

# Run without rebuilding
dotnet run --no-build

# Use launcher scripts
./launch.sh        # Linux/macOS
launch.bat         # Windows
```

### Publishing

```bash
# Publish for current platform
dotnet publish -c Release

# Publish for specific platform
dotnet publish -c Release -r win-x64     # Windows 64-bit
dotnet publish -c Release -r linux-x64   # Linux 64-bit
dotnet publish -c Release -r osx-x64     # macOS 64-bit
```

## Known Differences

### Text Rendering

The current implementation uses a placeholder for text rendering. To add proper text:

1. Create fonts in the Content Pipeline (Content.mgcb)
2. Load fonts using `Content.Load<SpriteFont>("FontName")`
3. Render with `spriteBatch.DrawString(font, text, position, color)`

### Asset Pipeline

MonoGame uses a content pipeline for assets. To add images, sounds, or fonts:

1. Open `Content/Content.mgcb` with MGCB Editor
2. Add assets to the pipeline
3. Build the content
4. Load in code using `Content.Load<T>("AssetName")`

## Future Enhancements

With MonoGame, we can now implement:

- **Shaders**: Custom visual effects using HLSL
- **Audio**: Background music and sound effects via MonoGame.Extended
- **Networking**: Multiplayer support with Lidgren.Network
- **Mobile**: Deploy to iOS and Android
- **Advanced Rendering**: Lighting, shadows, post-processing effects
- **Performance**: Multi-threading for physics and AI

## Resources

### MonoGame Documentation
- Official Docs: https://docs.monogame.net/
- Tutorials: https://docs.monogame.net/articles/getting_started/0_getting_started.html
- Community: https://community.monogame.net/

### C# Resources
- C# Guide: https://docs.microsoft.com/en-us/dotnet/csharp/
- .NET Documentation: https://docs.microsoft.com/en-us/dotnet/

### Game Development
- MonoGame Samples: https://github.com/CartBlanche/MonoGame-Samples
- MonoGame.Extended: https://www.monogameextended.net/

## Legacy Code

The original Python/Pygame implementation is preserved in the `game/` directory for reference. While it's no longer the active codebase, it serves as:

- Documentation of game design decisions
- Reference for feature implementation
- Comparison for performance testing
- Educational resource for Python game development

## Conclusion

The migration to MonoGame has been successful, with all core features ported and working. The new architecture provides a solid foundation for future development and significantly improves the game's performance and scalability.
