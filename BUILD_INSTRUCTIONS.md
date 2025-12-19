# Build Instructions for Subspace (MonoGame)

## Prerequisites

### Required Software

1. **.NET SDK 6.0 or later**
   - Download: https://dotnet.microsoft.com/download
   - Verify installation: `dotnet --version`

2. **MonoGame** (installed automatically via NuGet)
   - No manual installation required
   - Included as a package reference in the project

### Optional Tools

- **Visual Studio 2022** (Windows) - Full IDE with MonoGame templates
- **Visual Studio Code** - Lightweight cross-platform editor
- **JetBrains Rider** - Premium C# IDE with excellent MonoGame support
- **MonoGame Content Builder (MGCB) Editor** - For managing game assets

## Quick Start

### Option 1: Using Launcher Scripts (Easiest)

**Linux/macOS:**
```bash
chmod +x launch.sh  # Make executable (first time only)
./launch.sh
```

**Windows:**
```cmd
launch.bat
```

### Option 2: Using .NET CLI

```bash
# Clone the repository (if not already done)
git clone https://github.com/shifty81/Subspace.git
cd Subspace

# Restore dependencies
dotnet restore

# Build the project
dotnet build

# Run the game
dotnet run
```

### Option 3: Using Visual Studio

1. Open `Subspace.sln` (or `Subspace.csproj`)
2. Press F5 to build and run

## Build Configurations

### Debug Build (Default)

Used for development with debugging symbols:

```bash
dotnet build
```

### Release Build

Optimized for performance:

```bash
dotnet build -c Release
```

### Clean Build

Remove all build artifacts and rebuild:

```bash
dotnet clean
dotnet build
```

## Publishing the Game

Create a self-contained executable for distribution:

### Windows (64-bit)

```bash
dotnet publish -c Release -r win-x64 --self-contained true
```

Output: `bin/Release/net9.0/win-x64/publish/`

### Linux (64-bit)

```bash
dotnet publish -c Release -r linux-x64 --self-contained true
```

Output: `bin/Release/net9.0/linux-x64/publish/`

### macOS (64-bit)

```bash
dotnet publish -c Release -r osx-x64 --self-contained true
```

Output: `bin/Release/net9.0/osx-x64/publish/`

### All Platforms

To create builds for all platforms:

```bash
# Windows
dotnet publish -c Release -r win-x64 --self-contained true -o dist/windows

# Linux
dotnet publish -c Release -r linux-x64 --self-contained true -o dist/linux

# macOS
dotnet publish -c Release -r osx-x64 --self-contained true -o dist/macos
```

## Troubleshooting

### Build Errors

**Error: SDK not found**
```
Solution: Install .NET SDK from https://dotnet.microsoft.com/download
```

**Error: MonoGame.Framework.DesktopGL not found**
```bash
Solution: Restore NuGet packages
dotnet restore
```

**Error: Cannot find Content.mgcb**
```
Solution: The Content.mgcb file should be in the Content/ directory
This is created automatically by the MonoGame template
```

### Runtime Errors

**Error: Missing SDL2 libraries (Linux)**
```bash
# Ubuntu/Debian
sudo apt-get install libsdl2-dev

# Fedora
sudo dnf install SDL2-devel

# Arch
sudo pacman -S sdl2
```

**Error: Game window doesn't appear**
```
Solution: Make sure you have graphics drivers installed
Check if your system supports OpenGL 3.0+
```

**Error: Game crashes on startup**
```
Solution: Try running from command line to see error messages
dotnet run
```

## Performance Tips

### For Development

- Use Debug configuration for better error messages
- Use Visual Studio debugger for step-by-step debugging
- Profile with dotnet-trace for performance analysis

### For Distribution

- Always use Release configuration
- Use self-contained publishing for easier distribution
- Test on target platforms before releasing
- Consider using single-file publishing:
  ```bash
  dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true
  ```

## Project Structure

```
Subspace/
├── bin/              # Build output (gitignored)
├── obj/              # Intermediate build files (gitignored)
├── Content/          # Game assets (textures, sounds, fonts)
│   └── Content.mgcb  # Content pipeline configuration
├── Components.cs     # Ship component system
├── Config.cs         # Game configuration
├── Game1.cs          # Main game class
├── Particles.cs      # Particle effects
├── Projectile.cs     # Projectile system
├── Program.cs        # Entry point
├── Ship.cs           # Ship logic
├── Starfield.cs      # Background rendering
├── Subspace.csproj   # Project file
├── app.manifest      # Windows manifest
├── Icon.ico          # Windows icon
└── Icon.bmp          # Game icon
```

## Adding New Features

### Adding a New Class

1. Create a new `.cs` file in the root directory
2. Use the `Subspace` namespace
3. The build system will automatically include it

### Adding Assets (Textures, Fonts, etc.)

1. Install MGCB Editor:
   ```bash
   dotnet tool install -g dotnet-mgcb-editor
   mgcb-editor-linux  # or mgcb-editor-windows / mgcb-editor-mac
   ```

2. Open `Content/Content.mgcb`
3. Add your assets
4. Build the content
5. Load in code:
   ```csharp
   var texture = Content.Load<Texture2D>("TextureName");
   var font = Content.Load<SpriteFont>("FontName");
   ```

## CI/CD Integration

### GitHub Actions Example

```yaml
name: Build

on: [push, pull_request]

jobs:
  build:
    runs-on: ubuntu-latest
    steps:
    - uses: actions/checkout@v2
    - name: Setup .NET
      uses: actions/setup-dotnet@v1
      with:
        dotnet-version: 9.0.x
    - name: Restore dependencies
      run: dotnet restore
    - name: Build
      run: dotnet build --no-restore
    - name: Test
      run: dotnet test --no-build --verbosity normal
```

## Getting Help

- **MonoGame Community**: https://community.monogame.net/
- **GitHub Issues**: https://github.com/shifty81/Subspace/issues
- **Discord**: (Add server link if available)
- **Documentation**: See MONOGAME_MIGRATION.md for more details

## Next Steps

After building successfully:

1. Read [GAMEPLAY.md](GAMEPLAY.md) for game controls and mechanics
2. Check [ROADMAP.md](ROADMAP.md) for planned features
3. See [CONTRIBUTING.md](CONTRIBUTING.md) if you want to contribute
4. Review [MONOGAME_MIGRATION.md](MONOGAME_MIGRATION.md) for technical details
