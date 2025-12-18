# Changelog

All notable changes to Subspace will be documented in this file.

## [1.0.0] - 2025-12-18

### Initial Release

#### Features
- **Modular Ship System**
  - Grid-based ship construction
  - 7 component types (Core, Engine, Laser, Cannon, Armor, Reactor, Shield)
  - Component-level damage model
  - Dynamic stats calculation

- **Combat System**
  - Real-time space battles
  - Two weapon types (Laser and Cannon)
  - Projectile physics
  - Collision detection
  - Damage propagation

- **Ship Builder**
  - Interactive grid-based interface
  - Add/remove components with mouse
  - Component selection menu
  - Real-time preview
  - Switch between play and build modes

- **AI Opponents**
  - Basic pursuit AI
  - Weapon firing behavior
  - Distance management
  - Target tracking

- **Power Management**
  - Power generation from Cores and Reactors
  - Power consumption by systems
  - Balanced power requirements

- **Physics System**
  - Realistic ship movement
  - Thrust and rotation
  - Velocity and momentum
  - Drag simulation

- **Game Modes**
  - Play Mode (combat)
  - Build Mode (ship design)
  - Pause functionality

- **User Interface**
  - Health display
  - Power status
  - Enemy counter
  - Component selection
  - Controls help

#### Documentation
- Complete game README
- Gameplay guide with strategies
- Open source resources guide
- Quick start guides for multiple engines
- Contributing guidelines
- MIT License

#### Technical
- Python 3.7+ support
- Pygame-based rendering
- Component-based architecture
- Event-driven input system
- Camera system
- Starfield background

### Tested Features
- ✅ Ship movement and rotation
- ✅ Weapon firing and projectiles
- ✅ Component addition/removal
- ✅ Damage system
- ✅ Power management
- ✅ AI behavior
- ✅ Collision detection
- ✅ Build mode interface
- ✅ Game state management
- ✅ Stats calculation

### Known Limitations
- No save/load functionality yet
- Basic AI (single behavior pattern)
- No sound effects or music
- No multiplayer support
- Limited visual effects
- No crew management
- Fixed grid size

### Future Enhancements
See GAMEPLAY.md for planned features including:
- Ship save/load system
- Enhanced visual effects
- Sound and music
- More component types
- Advanced AI behaviors
- Multiplayer support
- Campaign mode
- Ship templates

---

## Version Format

This project uses [Semantic Versioning](https://semver.org/):
- MAJOR.MINOR.PATCH
- MAJOR: Incompatible API changes
- MINOR: New functionality (backwards compatible)
- PATCH: Bug fixes (backwards compatible)

## Categories

- **Added**: New features
- **Changed**: Changes to existing functionality
- **Deprecated**: Soon-to-be removed features
- **Removed**: Removed features
- **Fixed**: Bug fixes
- **Security**: Security fixes
