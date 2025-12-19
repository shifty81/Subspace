# Legacy Python/Pygame Implementation

⚠️ **This directory contains the legacy Python/Pygame implementation of Subspace.**

## Current Status

**This code is no longer the active development version.**

The project has been migrated to **C#/MonoGame** for better performance, scalability, and cross-platform support. The new implementation is in the root directory of the repository.

## To Play the Current Version

To play the latest MonoGame version of Subspace:

```bash
# From the root directory
cd ..

# Linux/macOS
./launch.sh

# Windows
launch.bat
```

Or build manually:
```bash
dotnet build
dotnet run
```

## Why This Code Is Preserved

This Python/Pygame implementation is kept for:

1. **Reference**: Shows the original game design and mechanics
2. **Documentation**: Demonstrates how features were initially implemented
3. **Learning**: Helpful for understanding Python game development
4. **Comparison**: Can be used to compare with the MonoGame version
5. **History**: Preserves the project's development journey

## Original Features

This implementation includes:
- ✅ Basic ship building with modular components
- ✅ Combat mechanics with lasers and cannons
- ✅ AI-controlled enemy ships
- ✅ Particle effects for weapons and explosions
- ✅ Grid-based ship editor
- ✅ Power management system
- ✅ Parallax starfield background

## If You Want to Run the Legacy Version

**Requirements:**
- Python 3.7+
- Pygame 2.5+

**Installation:**
```bash
cd game
pip install -r requirements.txt
python3 main.py
```

**Note:** This version will not receive new features or updates. All development effort is focused on the MonoGame version.

## Migration Details

For technical details about the migration from Pygame to MonoGame, see:
- [MONOGAME_MIGRATION.md](../MONOGAME_MIGRATION.md) - Detailed migration guide
- [BUILD_INSTRUCTIONS.md](../BUILD_INSTRUCTIONS.md) - How to build the MonoGame version
- [ROADMAP.md](../ROADMAP.md) - Updated with MonoGame status

## Questions?

If you have questions about the migration or the new MonoGame version:
- Open an issue: https://github.com/shifty81/Subspace/issues
- Check the documentation in the root directory
- Read the MonoGame migration guide

---

**Last Updated:** December 19, 2025
**Migration Status:** ✅ Complete
**Active Codebase:** MonoGame (root directory)
