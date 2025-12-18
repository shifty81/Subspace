# Subspace - Cosmoteer-Inspired Space Combat Game

A fully playable spaceship building and combat game inspired by Cosmoteer, built with Python and Pygame.

## Features

- **Modular Ship Design**: Build ships with different components (engines, weapons, armor, reactors)
- **Real-Time Combat**: Engage in space battles with laser and cannon weapons
- **Component-Based Damage**: Individual ship parts can be destroyed
- **Ship Builder**: Design your ship with an intuitive grid-based interface
- **AI Opponents**: Fight against AI-controlled enemy ships
- **Power Management**: Balance power generation and consumption
- **Physics-Based Movement**: Realistic ship movement with thrust and inertia

## Installation

### Requirements
- Python 3.7+
- Pygame

### Install Dependencies
```bash
pip install pygame
```

## How to Play

### Starting the Game
```bash
cd game
python3 main.py
```

### Controls

#### Combat Mode (Play Mode)
- **W / Up Arrow**: Thrust forward
- **A / Left Arrow**: Rotate left
- **D / Right Arrow**: Rotate right
- **Space**: Fire weapons
- **B**: Enter Build Mode
- **P**: Pause game
- **R**: Reset game
- **ESC**: Quit

#### Build Mode
- **Left Click**: Place selected component
- **Right Click**: Remove component
- **1**: Select Armor
- **2**: Select Engine
- **3**: Select Laser Weapon
- **4**: Select Cannon Weapon
- **5**: Select Reactor (Power Generator)
- **6**: Select Shield
- **B**: Return to Play Mode

## Game Mechanics

### Ship Components

- **Core** (Yellow): The heart of your ship. If destroyed, the ship is destroyed.
- **Engine** (Blue): Provides thrust. More engines = faster ship.
- **Laser Weapon** (Red): Fast-firing, low damage weapon.
- **Cannon Weapon** (Yellow): Slow-firing, high damage weapon.
- **Armor** (Gray): Durable component that protects other parts.
- **Reactor** (Green): Generates power for weapons and systems.
- **Shield** (Light Blue): High power consumption, provides protection.

### Power System
- Each component consumes or generates power
- If power consumption exceeds generation, some systems won't work properly
- Reactors and Cores generate power
- Weapons and shields consume power

### Combat
- Weapons automatically fire in the direction your ship is facing
- Projectiles damage individual components they hit
- Destroy all enemy ships to spawn the next wave
- Don't let your core get destroyed!

### Ship Building
- Press **B** to enter Build Mode
- Components must be placed on the grid
- You can add/remove components to customize your ship
- Strategic placement matters - protect your core!

## Strategy Tips

1. **Protect Your Core**: Surround it with armor
2. **Balance Your Design**: Need engines for speed, weapons for offense, and power for both
3. **Power Management**: Make sure you have enough reactors
4. **Weapon Placement**: Place weapons on the front for better firing angles
5. **Engine Placement**: Putting engines at the back provides better thrust

## Development

### Project Structure
```
game/
├── main.py              # Entry point
├── src/
│   ├── __init__.py
│   ├── game.py          # Main game loop and logic
│   ├── ship.py          # Ship class with component system
│   ├── components.py    # Component definitions
│   ├── projectile.py    # Projectile system
│   └── config.py        # Game configuration
├── assets/              # Game assets (sprites, sounds)
└── README.md           # This file
```

### Technical Details

The game uses:
- **Entity-Component System**: Ships are composed of modular components
- **Grid-Based Building**: Components snap to a grid for easy placement
- **Physics Simulation**: Simple physics for movement and projectiles
- **Component Damage**: Individual parts can be destroyed independently
- **AI System**: Basic AI for enemy ships (pursuit and combat)

### Extending the Game

Want to add features? Here are some ideas:
- Add more component types (shields with active defense, repair systems)
- Implement a campaign mode with progression
- Add multiplayer support
- Create a ship save/load system
- Add visual effects (explosions, engine trails)
- Implement more sophisticated AI behaviors
- Add sound effects and music

## License

This game is open source. Feel free to modify and extend it!

## Credits

Inspired by **Cosmoteer** by Walternate Realities.

Built with Python and Pygame.
