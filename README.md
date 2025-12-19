# Subspace
A Cosmoteer-inspired spaceship building and combat game built with MonoGame

## Play the Game!

🎮 **The game is fully playable!** You can launch the game using our streamlined launcher scripts.

### Quick Launch (Recommended)

**Linux/macOS:**
```bash
./launch.sh
```

**Windows:**
```bash
launch.bat
```

The launcher scripts will automatically:
- Check for .NET SDK installation
- Build the MonoGame project
- Launch the game

### Manual Launch

You can also build and run the game manually:

```bash
# Build the project
dotnet build

# Run the game
dotnet run
```

### Requirements

- **.NET SDK 6.0 or later** - [Download here](https://dotnet.microsoft.com/download)
- **MonoGame** (installed automatically via NuGet)

## Features

- **Modular Ship Design** - Build custom ships with engines, weapons, armor, and reactors
- **Real-Time Combat** - Fast-paced space battles with projectile weapons
- **Ship Builder** - Design and modify your ship with an intuitive interface
- **Component Damage** - Individual ship parts can be targeted and destroyed
- **AI Opponents** - Battle against intelligent enemy ships
- **Power Management** - Balance energy generation and consumption

## Resources

For open source tools and libraries that can help extend this game or build similar projects, see:
- [OPEN_SOURCE_RESOURCES.md](OPEN_SOURCE_RESOURCES.md) - Comprehensive guide to game development tools
- [QUICK_START.md](QUICK_START.md) - Quick start guides for various game engines

## Screenshots

The game features:
- Grid-based ship building with multiple component types
- Real-time space combat with lasers and cannons
- AI-controlled enemy ships
- Component-level damage system
- Power management mechanics

## Built With

- C# / .NET 9.0
- MonoGame 3.8 (Cross-platform game framework)

## 🎉 Recent Update: MonoGame Migration

**December 19, 2025** - Subspace has been successfully migrated from Python/Pygame to C#/MonoGame!

**Benefits:**
- ⚡ **Better Performance** - Smoother gameplay with more particles and effects
- 🚀 **Cross-Platform** - Deploy to Windows, Linux, macOS, mobile, and consoles
- 🎮 **Professional Framework** - Same technology used in Stardew Valley and Celeste
- 📈 **Scalability** - Ready for advanced features and larger ships

The legacy Python/Pygame code is preserved in the `game/` directory for reference.

## Documentation

### Getting Started
- **[BUILD_INSTRUCTIONS.md](BUILD_INSTRUCTIONS.md)** - How to build and run the game
- **[GAMEPLAY.md](GAMEPLAY.md)** - Complete gameplay guide with strategies

### Development
- **[MONOGAME_MIGRATION.md](MONOGAME_MIGRATION.md)** - Technical details of the migration
- **[ROADMAP.md](ROADMAP.md)** - Future development plans (updated for MonoGame)
- **[CONTRIBUTING.md](CONTRIBUTING.md)** - How to contribute to the project

### Resources
- **[OPEN_SOURCE_RESOURCES.md](OPEN_SOURCE_RESOURCES.md)** - Guide to game dev tools and libraries
- **[QUICK_START.md](QUICK_START.md)** - Quick start guides for various engines

### Legacy
- **[game/LEGACY_NOTICE.md](game/LEGACY_NOTICE.md)** - About the legacy Python/Pygame version

## License

MIT License - see [LICENSE](LICENSE) file for details
