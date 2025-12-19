# Subspace Ship Design Guide
## Cosmoteer-Inspired Design Principles

This guide explains how to build effective ships in Subspace using Cosmoteer-inspired design principles.

## Core Concepts

### Logistics Matter
In Subspace, crew need to move between components to operate them. Efficient ship design minimizes crew travel time and maximizes effectiveness.

### Component Types and Their Roles

#### Essential Components

**Core (Yellow)**
- The heart of your ship
- If destroyed, your ship is destroyed
- Generates power (+50)
- Place in the center, heavily armored

**Reactors (Green)**
- Generate significant power (+100)
- Essential for weapon-heavy ships
- Place near components that need power
- Protect with multiple armor layers

**Engines (Blue)**
- Provide thrust for movement
- Consume power (-10 each)
- More engines = faster ship
- Vulnerable - protect them

**Weapons**
- Laser (Red): Fast firing, low damage, medium range (-15 power)
- Cannon (Orange): Slow firing, high damage, long range (-20 power)
- Place for maximum fields of fire
- Need crew to operate

**Armor (Gray)**
- High health, no special function
- Most cost-effective protection
- Use to create protective layers

**Shields (Cyan)**
- Defensive system (-25 power)
- High power consumption
- Place at front edges

#### Logistics Components

**Crew Quarters (Light Blue)**
- Houses crew members
- Place near where crew is needed
- Reduces response time

**Ammo Factory (Orange/Brown)**
- Produces ammunition for weapons
- Should be adjacent to weapons
- Minimizes DPS loss from crew walking

**Corridor (Gray)**
- Allows fast crew movement
- Crew move at double speed in corridors
- Use to connect sections

**Structure (Dark Gray)**
- Lightweight shape blocks
- Low cost and weight
- Use for non-critical areas
- Good for creating ship shape

**Engine Room (Dark Blue)**
- Provides thrust bonus to adjacent engines
- Place with multiple engines touching it
- Improves maneuverability

## Effective Ship Shapes

### Wedge/Triangle (Recommended for Beginners)
```
      [W]
     [W][W]
    [W][C][W]
   [W][R][R][W]
  [A][E][E][E][A]
```
W = Weapon, C = Core, R = Reactor, E = Engine, A = Armor

**Advantages:**
- Narrow frontal profile minimizes incoming hits
- Weapons fire forward from slanted sides
- Easy to protect core

**Best For:** General combat, kiting enemies

### Box/Wall (Heavy Assault)
```
[A][A][A][A][A]
[A][W][C][W][A]
[A][R][R][R][A]
[A][E][E][E][A]
[A][A][A][A][A]
```

**Advantages:**
- Maximum internal space
- Heavy armor on all sides
- Simple to build

**Disadvantages:**
- Requires massive shielding
- Slow and heavy

**Best For:** Frontal assault, tanking damage

### U-Shape (Abductor)
```
[W][A][A][A][W]
[W][R][C][R][W]
[E][R]   [R][E]
[E][A]   [A][E]
```

**Advantages:**
- Traps smaller ships inside
- Multiple firing angles
- Good for close combat

**Best For:** Capturing/destroying smaller ships

### Modular Design (Advanced)
Create self-contained modules that can function independently:

**Example Module:**
```
[A][A][A]
[A][R][A]  <- Reactor Module
[A][Q][A]  Q = Crew Quarters
```

```
[A][W][A]
[A][F][A]  <- Weapon Module
[A][C][A]  F = Ammo Factory, C = Corridor
```

**Advantages:**
- Damage containment
- Prevents chain reactions
- Redundancy if one module is destroyed

## Optimal Component Placement

### Priority Locations

1. **Deep Internal Center**
   - Core
   - Reactors (most important)
   - Protected by multiple armor layers

2. **Near Power Sources**
   - Weapons
   - Ammo Factories
   - Minimizes power delivery time

3. **Front Edges**
   - Shields
   - Place behind thin armor layer
   - First line of defense

4. **External Placement**
   - Engines
   - Far from center of mass improves turning
   - But more vulnerable

5. **Adjacent to Usage**
   - Ammo Factories next to weapons
   - Crew Quarters near reactors/weapons
   - Reduces crew walk time

## Design Checklist

### Power Balance
- [ ] Count power generation (Reactors + Core)
- [ ] Count power consumption (Engines + Weapons + Shields)
- [ ] Ensure generation > consumption
- [ ] Add extra reactors for safety margin

### Protection
- [ ] Core surrounded by at least 2 layers of armor
- [ ] Reactors have 1-2 layers of armor
- [ ] Front-facing shields or heavy armor
- [ ] Critical path has structure/armor

### Logistics
- [ ] Crew quarters near busy areas
- [ ] Corridors connect major sections
- [ ] Ammo factories adjacent to weapons
- [ ] No long walks between components

### Mobility
- [ ] Enough engines for desired speed
- [ ] Engines balanced around ship
- [ ] Engine rooms adjacent to engines
- [ ] Consider turning speed vs. armor

## Example Ships

### Starter Combat Ship
```
    [A][W][A]
    [W][C][W]
[A][R][R][R][A]
[A][E][E][E][A]
    [A][A][A]
```

- 3 Weapons (2 Lasers, 1 Cannon)
- 3 Reactors (sufficient power)
- 3 Engines (good mobility)
- Core protected by reactors and armor
- Total Power: +200 (3 reactors + core)
- Total Consumption: -75 (3 engines + 3 weapons)
- Net Power: +125 (good margin)

### Advanced Wedge Fighter
```
        [S]
       [W][W]
      [W][C][W]
     [A][R][R][A]
    [A][Q][F][Q][A]
   [A][E][ER][E][A]
  [A][E]    [E][A]
 [A][A]      [A][A]
```

S = Shield, Q = Crew Quarters, F = Ammo Factory, ER = Engine Room

- Narrow front for minimal target area
- Shield at front for first defense
- Core deep inside with reactor protection
- Crew quarters near reactors and weapons
- Ammo factory directly behind front weapons
- Engine room with multiple adjacent engines
- Heavy armor on outer layers

## Tips for Success

1. **Start Simple** - Build basic shapes first, optimize later
2. **Test Often** - Enter play mode to test ship performance
3. **Balance is Key** - Don't over-build weapons without power
4. **Armor is Cheap** - Use it liberally for protection
5. **Think Modular** - Build in sections that can survive independently
6. **Crew Efficiency** - Keep crew paths short
7. **Protect Power** - Losing reactors = losing the fight
8. **Consider Role** - Design ship for specific combat style

## Common Mistakes

❌ **Too Many Weapons, Not Enough Power**
- Weapons won't fire if power insufficient
- Add more reactors

❌ **Exposed Core**
- Core is instant-kill
- Always surround with armor

❌ **All Components Clustered**
- One explosion can chain-react
- Separate critical systems

❌ **Crew Quarters Far Away**
- Long crew travel = slow response
- Place quarters centrally

❌ **No Ammo Factories**
- Weapons will run slower
- Place factories near weapons

## Advanced Techniques

### Compartmentalization
Separate your ship into independent modules with corridors between them. If one module explodes, others remain functional.

### Redundancy
Have backup systems:
- Multiple reactors
- Backup engines
- Distributed crew quarters

### Optimal Crew Paths
Use corridors to create "highways" for crew to move quickly between critical systems.

### Engine Placement Leverage
Place engines far from center of mass to improve turning speed, but protect them with armor.

## Conclusion

Effective ship design in Subspace is about balancing offense, defense, power, and logistics. Experiment with different shapes and configurations to find what works for your play style. Remember: the best ship is one that survives and completes its mission!

Happy building, Commander! 🚀
