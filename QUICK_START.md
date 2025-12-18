# Quick Start Guide: Building Your Cosmoteer-Inspired Game

This guide will help you get started building a spaceship building and combat game inspired by Cosmoteer.

## Choose Your Path

### Path 1: Godot Engine (Recommended for Beginners)

**Step 1: Install Godot**
```bash
# Download from https://godotengine.org/download
# Or use package manager:
# Ubuntu/Debian:
sudo apt install godot

# macOS (Homebrew):
brew install godot

# Windows: Download from website
```

**Step 2: Create Your Project**
1. Open Godot
2. Click "New Project"
3. Choose a project name and location
4. Select "Renderer: Forward+" (or Mobile for lower-end devices)
5. Click "Create & Edit"

**Step 3: Set Up for 2D**
1. In the top menu, select "2D"
2. Create your first scene: Scene → New Scene
3. Choose "Node2D" as root node
4. Save the scene

**Step 4: Create Your First Ship**
```gdscript
# Ship.gd
extends RigidBody2D

@export var thrust_power = 500.0
@export var rotation_power = 3.0

func _physics_process(delta):
    # Handle movement
    if Input.is_action_pressed("ui_up"):
        apply_central_force(transform.x * thrust_power)
    
    # Handle rotation
    if Input.is_action_pressed("ui_left"):
        apply_torque(-rotation_power)
    if Input.is_action_pressed("ui_right"):
        apply_torque(rotation_power)
```

**Step 5: Add a Sprite**
1. Add a Sprite2D node as child of your ship
2. Create or import a simple ship image
3. Assign it to the Sprite2D texture

**Step 6: Add Collision**
1. Add a CollisionShape2D node as child of your ship
2. Create a new shape (Rectangle or Capsule)
3. Adjust size to match your sprite

**Next Steps**:
- Add weapons (Area2D for firing zones)
- Create projectiles (Area2D with movement)
- Build a component system (separate scenes for each part)
- Add a ship builder UI

---

### Path 2: Unity (For Those Familiar with C#)

**Step 1: Install Unity Hub**
- Download from https://unity.com/download
- Install Unity Hub
- Install Unity Editor (LTS version recommended)

**Step 2: Create Project**
1. Open Unity Hub
2. Click "New Project"
3. Select "2D Core" template
4. Name your project and click "Create"

**Step 3: Create Your First Ship**
```csharp
// Ship.cs
using UnityEngine;

public class Ship : MonoBehaviour
{
    public float thrustPower = 5f;
    public float rotationSpeed = 180f;
    
    private Rigidbody2D rb;
    
    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
    }
    
    void FixedUpdate()
    {
        // Forward thrust
        if (Input.GetKey(KeyCode.W))
        {
            rb.AddForce(transform.up * thrustPower);
        }
        
        // Rotation
        float rotation = 0f;
        if (Input.GetKey(KeyCode.A))
            rotation = rotationSpeed;
        if (Input.GetKey(KeyCode.D))
            rotation = -rotationSpeed;
        
        rb.MoveRotation(rb.rotation + rotation * Time.fixedDeltaTime);
    }
}
```

**Step 4: Set Up the Ship GameObject**
1. Create Empty GameObject (right-click in Hierarchy → Create Empty)
2. Name it "Ship"
3. Add Rigidbody2D component
4. Add SpriteRenderer component and assign a sprite
5. Add PolygonCollider2D or BoxCollider2D
6. Add the Ship.cs script

**Next Steps**:
- Create weapon prefabs
- Build projectile system with object pooling
- Design modular component system
- Create ship builder UI with Unity UI

---

### Path 3: Custom with Rust + Bevy (For Advanced Developers)

**Step 1: Install Rust**
```bash
curl --proto '=https' --tlsv1.2 -sSf https://sh.rustup.rs | sh
```

**Step 2: Create Project**
```bash
cargo new cosmoteer_clone
cd cosmoteer_clone
```

**Step 3: Add Bevy to Cargo.toml**
```toml
[dependencies]
bevy = "0.12"
```

**Step 4: Basic Setup**
```rust
// main.rs
use bevy::prelude::*;

fn main() {
    App::new()
        .add_plugins(DefaultPlugins)
        .add_systems(Startup, setup)
        .add_systems(Update, ship_movement)
        .run();
}

#[derive(Component)]
struct Ship {
    thrust_power: f32,
    rotation_speed: f32,
}

fn setup(mut commands: Commands, asset_server: Res<AssetServer>) {
    commands.spawn(Camera2dBundle::default());
    
    commands.spawn((
        SpriteBundle {
            texture: asset_server.load("ship.png"),
            ..default()
        },
        Ship {
            thrust_power: 500.0,
            rotation_speed: 3.0,
        },
    ));
}

fn ship_movement(
    keyboard: Res<Input<KeyCode>>,
    mut query: Query<(&Ship, &mut Transform)>,
    time: Res<Time>,
) {
    for (ship, mut transform) in query.iter_mut() {
        if keyboard.pressed(KeyCode::W) {
            let forward = transform.rotation * Vec3::Y;
            transform.translation += forward * ship.thrust_power * time.delta_seconds();
        }
        
        if keyboard.pressed(KeyCode::A) {
            transform.rotate_z(ship.rotation_speed * time.delta_seconds());
        }
        if keyboard.pressed(KeyCode::D) {
            transform.rotate_z(-ship.rotation_speed * time.delta_seconds());
        }
    }
}
```

**Next Steps**:
- Add Rapier2D for physics
- Create ECS components for ship parts
- Build weapon systems
- Add sprite rendering for parts

---

## Core Features to Implement

### 1. Basic Ship Movement ✓
Start with simple movement and rotation (examples above)

### 2. Modular Ship System
```
Ship Structure:
- Ship (root entity)
  ├── Core (required part)
  ├── Engine Parts (provide thrust)
  ├── Weapon Parts (provide firepower)
  ├── Armor Parts (provide protection)
  └── Utility Parts (shields, storage, etc.)
```

**Implementation Strategy**:
- Each part is a component/entity
- Parts communicate through events or direct references
- Grid-based positioning system
- Connection validation (parts must be adjacent)

### 3. Combat System
**Basic Projectile System**:
```
1. Weapon detects target in range
2. Weapon fires projectile
3. Projectile travels in direction
4. On collision, apply damage
5. Damaged part reduces functionality or is destroyed
```

**Damage Propagation**:
- Hits destroy/damage specific parts
- Some parts are critical (core, certain systems)
- Visual feedback for damage

### 4. Ship Building Interface
**Features Needed**:
- Part palette (available parts)
- Grid for placing parts
- Rotation controls
- Cost/resource display
- Validation (ships must be connected, have required parts)
- Save/load designs

### 5. Basic AI
**Simple AI Behaviors**:
```
1. Move toward player
2. Maintain distance (based on weapons)
3. Fire when in range
4. Avoid obstacles
5. Retreat when heavily damaged
```

---

## Development Workflow

### Day 1-2: Setup and Basic Movement
- [ ] Install chosen engine/framework
- [ ] Create project
- [ ] Implement basic ship movement
- [ ] Add rotation controls
- [ ] Test and refine feel

### Day 3-5: Weapons and Combat
- [ ] Create projectile system
- [ ] Add a basic weapon
- [ ] Implement collision and damage
- [ ] Create visual feedback
- [ ] Add multiple weapon types

### Week 2: Modular Ship System
- [ ] Design component architecture
- [ ] Implement grid system
- [ ] Create 3-5 ship parts
- [ ] Test part connections
- [ ] Add part properties (thrust, power, etc.)

### Week 3: Ship Builder
- [ ] Create UI layout
- [ ] Implement part placement
- [ ] Add rotation controls
- [ ] Implement save/load
- [ ] Test with various designs

### Week 4: AI and Content
- [ ] Implement basic AI
- [ ] Create multiple ship designs
- [ ] Add visual polish
- [ ] Balance gameplay
- [ ] Create test scenarios

---

## Testing Your Game

### Performance Testing
- Test with 10+ ships on screen
- Monitor frame rate
- Profile hot paths
- Optimize as needed

### Gameplay Testing
- Is ship building intuitive?
- Is combat fun?
- Is difficulty balanced?
- Are controls responsive?

### Playtesting Checklist
- [ ] Can build basic ship in < 2 minutes
- [ ] Combat feels fair and engaging
- [ ] Ships feel different based on design
- [ ] UI is clear and understandable
- [ ] No major bugs or crashes

---

## Common Pitfalls to Avoid

1. **Over-engineering Early**: Start simple, add complexity later
2. **Poor Performance**: Profile and optimize collision detection early
3. **Complex UI Too Soon**: Get core gameplay working first
4. **Ignoring Game Feel**: Polish movement and combat feel
5. **No Playtesting**: Get feedback early and often
6. **Scope Creep**: Resist adding features before core is solid

---

## Resources

- **Main Resource Guide**: See [OPEN_SOURCE_RESOURCES.md](OPEN_SOURCE_RESOURCES.md)
- **Godot Docs**: https://docs.godotengine.org/
- **Unity Learn**: https://learn.unity.com/
- **Bevy Book**: https://bevyengine.org/learn/book/introduction/
- **Game Dev Communities**: r/gamedev, various Discord servers

---

## Getting Help

When you get stuck:
1. Check official documentation
2. Search Stack Overflow
3. Ask in engine-specific communities
4. Join game dev Discord servers
5. Check YouTube tutorials

Remember: Every game developer gets stuck. Don't be afraid to ask for help!

---

## Next Steps

1. Choose your engine (Godot recommended for beginners)
2. Complete Day 1-2 goals (basic movement)
3. Share your progress in game dev communities
4. Iterate based on feedback
5. Have fun! 🚀

Good luck with your game development journey!
