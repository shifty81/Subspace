# Gameplay Guide

## What is Subspace?

Subspace is a fully playable spaceship building and combat game inspired by Cosmoteer. Build custom ships from modular components, engage in real-time space battles, and destroy enemy fleets!

## Quick Start

```bash
cd game
pip install pygame
python3 main.py
```

## Game Modes

### Play Mode (Default)
Combat mode where you control your ship and fight enemies.

**Controls:**
- `W` or `↑` - Thrust forward
- `A` or `←` - Rotate left  
- `D` or `→` - Rotate right
- `Space` - Fire all weapons
- `B` - Enter Build Mode
- `P` - Pause
- `R` - Reset game
- `ESC` - Quit

### Build Mode
Design and modify your ship with a grid-based builder.

**Controls:**
- `Left Click` - Place selected component
- `Right Click` - Remove component
- `1-6` - Select component type:
  - `1` - Armor (durable protection)
  - `2` - Engine (provides thrust)
  - `3` - Laser (fast, weak weapon)
  - `4` - Cannon (slow, strong weapon)
  - `5` - Reactor (generates power)
  - `6` - Shield (defensive system)
- `B` - Return to Play Mode

## Core Mechanics

### Modular Ship System

Your ship is composed of individual components arranged on a grid. Each component has unique properties:

#### Core (Yellow)
- The heart of your ship
- **If destroyed, your ship is destroyed**
- Generates moderate power
- Moderate health

#### Engine (Blue)
- Provides thrust for movement
- More engines = faster ship
- Consumes power
- Low health - protect them!

#### Laser Weapon (Red)
- Fast firing rate (2 shots/second)
- Low damage per shot
- Medium range
- Good for sustained damage

#### Cannon Weapon (Orange/Yellow)
- Slow firing rate (0.67 shots/second)
- High damage per shot
- Longer range
- Excellent for finishing blows

#### Armor (Gray)
- High health, no special function
- Perfect for protecting critical components
- No power consumption
- Strategic placement is key

#### Reactor (Green)
- Generates significant power
- Essential for weapon-heavy ships
- Medium health
- Explosion risk if destroyed

#### Shield (Light Blue)
- Defensive system
- High power consumption
- Low health
- Future enhancement: active shielding

### Power Management

Every component either generates or consumes power:

**Power Generators:**
- Core: +50 power
- Reactor: +100 power

**Power Consumers:**
- Engine: -10 power each
- Laser: -15 power each
- Cannon: -20 power each
- Shield: -25 power each

**Important:** If power consumption exceeds generation, systems may malfunction or weapons won't fire!

### Combat System

#### Projectiles
- Projectiles fire in the direction your ship is facing
- Each weapon has a cooldown period
- Projectiles travel at constant speed
- Direct hits damage the component struck

#### Damage Model
- Components take damage individually
- Destroyed components are removed from the ship
- Strategic targeting: aim for engines to slow enemies, or weapons to reduce firepower
- Losing your core = game over

#### AI Behavior
- Enemies pursue the player
- Maintain optimal firing distance
- Rotate to face player
- Fire weapons when in range
- Adapt to damage (reduced thrust if engines destroyed)

## Strategy Tips

### Ship Design

1. **Protect Your Core**
   - Surround core with armor
   - Place core in center of ship
   - Add multiple armor layers

2. **Power Balance**
   - Count your power generators
   - Calculate total consumption
   - Add extra reactors for weapon-heavy designs

3. **Engine Placement**
   - Place at rear for better thrust efficiency
   - Multiple small engines > one large engine (redundancy)
   - Losing one engine won't cripple you

4. **Weapon Layout**
   - Front-facing weapons are most effective
   - Mix lasers (sustained DPS) and cannons (burst damage)
   - More weapons = more power needed

5. **Armor Strategy**
   - Outer layer: armor
   - Middle layer: systems and weapons
   - Inner core: power and core
   - Think "onion layers"

### Combat Tactics

1. **Movement**
   - Keep moving to avoid enemy fire
   - Use rotation to keep weapons aimed
   - Thrust in bursts to conserve momentum

2. **Positioning**
   - Maintain distance from enemies
   - Angle ship to present armored side
   - Retreat if heavily damaged

3. **Target Priority**
   - Enemy weapons (reduce their damage)
   - Enemy engines (slow them down)
   - Enemy core (finish them off)

4. **Power Management**
   - Don't over-build weapons without power
   - Reactors are high-value targets
   - Consider power efficiency in design

## Advanced Techniques

### Kiting
- Maintain distance while firing
- Use superior speed to stay out of range
- Requires good engine-to-weapon ratio

### Jousting
- Quick pass-by attacks
- High speed, angle for maximum damage
- Retreat and repeat

### Tanking
- Heavy armor, close range
- Absorb damage while dealing it
- Requires excellent power generation

### Glass Cannon
- Maximum weapons, minimal defense
- High risk, high reward
- Requires excellent piloting

## Progression

### Wave System
- Defeat all enemies to spawn next wave
- Each wave spawns 3 new enemies
- Enemies have random positions
- Infinite waves for endless gameplay

### Ship Upgrades
- Use Build Mode between waves
- Add more components
- Repair by replacing components
- Experiment with designs

## Tips for New Players

1. **Start Simple**
   - Master basic movement first
   - Learn weapon timing
   - Practice in Build Mode

2. **Test Your Designs**
   - Enter Build Mode (press B)
   - Add/remove components
   - Return to Play Mode to test
   - Iterate!

3. **Watch Your Stats**
   - Health bar (top left)
   - Power generation/consumption
   - Enemy count

4. **Learn from Defeat**
   - Which components were destroyed first?
   - Did you have enough power?
   - Was positioning the issue?

5. **Experiment**
   - Try different ship layouts
   - Find what works for your playstyle
   - There's no "best" design!

## Common Mistakes

1. ❌ **Insufficient Power**
   - Problem: Weapons won't fire
   - Solution: Add reactors

2. ❌ **Exposed Core**
   - Problem: Quick defeats
   - Solution: Armor layers

3. ❌ **All Engines on One Side**
   - Problem: Losing one area cripples movement
   - Solution: Distribute engines

4. ❌ **Too Many Weapons**
   - Problem: Power issues, expensive
   - Solution: Balance weapons with power

5. ❌ **No Armor**
   - Problem: Critical components destroyed easily
   - Solution: Armor everything

## Future Enhancements

Possible additions to the game:

- Ship save/load system
- Multiple ship presets
- Crew management
- Active shield mechanics
- Repair systems
- Resource collection
- Campaign mode
- Multiplayer battles
- More component types
- Visual effects (explosions, trails)
- Sound effects and music
- Damage indicators
- Minimap
- Ship templates

## Troubleshooting

**Game won't start:**
- Ensure Pygame is installed: `pip install pygame`
- Check Python version: `python3 --version` (need 3.7+)

**Performance issues:**
- Reduce number of enemies
- Close other applications
- Check system requirements

**Controls not working:**
- Make sure game window has focus
- Try alternative keys (WASD vs Arrows)

**Can't place components:**
- Ensure you're in Build Mode (press B)
- Check grid boundaries
- Try removing existing component first

## Credits

Inspired by **Cosmoteer** by Walternate Realities

Built with:
- Python 3
- Pygame library
- Love for space games 🚀

## Contributing

Want to improve Subspace? 

- Fix bugs
- Add features
- Create ship presets
- Improve AI
- Add visual effects
- Write documentation

All contributions welcome!

---

**Have fun building and battling in space! 🚀**
