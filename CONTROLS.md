# Subspace Controls Guide

## Game Controls

### Play Mode

#### Movement
- **W** or **↑** - Apply thrust (move forward)
- **A** or **←** - Rotate left
- **D** or **→** - Rotate right
- **S** or **↓** - (No backwards thrust - this is space!)

#### Combat
- **SPACE** - Fire all weapons
  - Lasers: Fast firing, lower damage
  - Cannons: Slower firing, higher damage

#### Mode Switching
- **B** - Toggle Build Mode
- **P** - Pause/Unpause game
- **R** - Reset game (restart with new enemies)
- **ESC** - Exit game

### Build Mode

#### Component Selection
- **1** - Select Armor
- **2** - Select Engine
- **3** - Select Laser Weapon
- **4** - Select Cannon Weapon
- **5** - Select Power Reactor
- **6** - Select Shield Generator

#### Building
- **Left Click** - Add selected component to ship
- **Right Click** - Remove component from ship
- **B** - Return to Play Mode

## Component Types

### Core (Yellow) ⚠️
- **Critical**: If destroyed, ship is destroyed
- **Power**: Generates 50 energy
- **Health**: 200 HP
- **Note**: Cannot be removed, every ship needs one

### Engine (Blue)
- **Function**: Provides thrust for movement
- **Power Cost**: 10 energy
- **Thrust**: 200 force
- **Health**: 50 HP
- **Tip**: Add multiple engines for faster ships

### Laser Weapon (Red)
- **Function**: Fast firing energy weapon
- **Power Cost**: 15 energy
- **Damage**: 10 per shot
- **Fire Rate**: 2 shots per second
- **Health**: 40 HP
- **Range**: Medium

### Cannon Weapon (Yellow-Brown)
- **Function**: Slower, more powerful projectile weapon
- **Power Cost**: 20 energy
- **Damage**: 25 per shot
- **Fire Rate**: ~0.7 shots per second
- **Health**: 60 HP
- **Range**: Medium

### Armor (Gray)
- **Function**: Provides protection and structural support
- **Power Cost**: 0 energy
- **Health**: 150 HP
- **Tip**: Use to protect vital components

### Power Reactor (Green)
- **Function**: Generates energy for other components
- **Power Generation**: 100 energy
- **Health**: 80 HP
- **Note**: Need enough to power all active systems

### Shield Generator (Light Blue)
- **Function**: Defensive system (visual only in current version)
- **Power Cost**: 25 energy
- **Health**: 30 HP
- **Note**: Future feature - currently decorative

## Gameplay Tips

### Combat Strategy
1. **Positioning**: Keep moving to avoid enemy fire
2. **Rotation**: Face enemies to use your weapons effectively
3. **Distance**: Maintain optimal range (~300 units)
4. **Power**: Watch your power usage - too many active components drain energy

### Ship Building
1. **Start Simple**: Begin with Core, Engine, Weapon, Power
2. **Balance**: Power generation ≥ Power consumption
3. **Protection**: Surround vital components with armor
4. **Symmetry**: Balanced designs are more stable (though not enforced)
5. **Specialization**: 
   - Fast ship: Multiple engines, light weapons
   - Tank: Heavy armor, power reactors
   - Gunship: Multiple weapons, lots of power

### Power Management
- **Available Power** = Sum of all reactor generation
- **Used Power** = Sum of all component consumption
- If Used > Available: Some systems may not function
- **Formula**: Core (50) + Reactors (100 each) ≥ Engines (10) + Weapons (15-20) + Shields (25)

## HUD Information

### Top Left
- **Mode**: Current game mode (PLAY/BUILD)
- **Health**: Current/Max ship health
- **Power**: Available power after consumption

### Bottom
- **Controls**: Quick reference of available commands

### Build Mode
- **Selected Component**: Shows currently selected component type
- **Instructions**: How to place/remove components

## Debug Commands

Currently no debug commands are available. To reset the game:
- Press **R** to restart with fresh enemies
- Or restart the application

## Multiplayer

Not yet implemented. Single-player only.

## Performance

### Expected Performance
- **Target**: 60 FPS
- **Particles**: Hundreds on screen at once
- **Ships**: Player + 3 enemies (can be modified)

### If Performance is Low
1. Close other applications
2. Update graphics drivers
3. Build in Release mode: `dotnet build -c Release`
4. Check system meets requirements (OpenGL 3.0+)

## Keyboard Layout Notes

### International Keyboards
- Game uses standard QWERTY layout
- If using different layout, arrow keys will always work
- WASD may be in different positions

### Numpad
- Number keys (1-6) must be from top row, not numpad

## Future Controls (Planned)

Features that may be added:
- **Mouse aiming**: Direct weapon targeting
- **Formation commands**: Control friendly ships
- **Tactical pause**: Pause and issue commands
- **Quick save**: Save your ship design
- **Camera zoom**: Zoom in/out for better view

## Troubleshooting

### Keys Not Responding
- Make sure game window has focus (click on it)
- Check keyboard language settings
- Try arrow keys instead of WASD

### Can't Place Components
- Make sure you're in Build Mode (press B)
- Check if grid position is already occupied
- Verify you're clicking on your ship's grid

### Ship Won't Move
- Check power: Power Available ≥ Power Used
- Make sure you have engines
- Engines need power to work

### Weapons Won't Fire
- Need power for weapons
- Weapons must be intact (not destroyed)
- Wait for cooldown between shots

## Getting Help

- **Documentation**: See [GAMEPLAY.md](GAMEPLAY.md) for strategy
- **Building**: See [BUILD_INSTRUCTIONS.md](BUILD_INSTRUCTIONS.md)
- **Issues**: https://github.com/shifty81/Subspace/issues

---

**Controls Version:** 1.0 (MonoGame)  
**Last Updated:** December 19, 2025
