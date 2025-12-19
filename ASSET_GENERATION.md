# Asset Generation Guide

This document explains how the sci-fi assets for Subspace were generated based on Cosmoteer's visual style.

## Overview

All visual assets (component sprites and GUI elements) were generated using Python scripts with the Pillow (PIL) library to create Cosmoteer-inspired sci-fi graphics.

## Asset Categories

### Component Sprites (64x64px)

Located in `Content/Sprites/component_*.png`

**Visual Features:**
- Metallic panels with gradient shading
- Rounded corners (4px radius)
- Technical greebles (small industrial details)
- Corner bolts/rivets for industrial aesthetic
- Inner detail borders
- Color-coded by function

**Component Types:**

1. **Core** (`component_core.png`) - Yellow/Gold
   - Central glowing energy core
   - Radiating energy conduits (8 points)
   - Containment ring
   - Power output indicators

2. **Engine** (`component_engine.png`) - Blue
   - Thruster nozzle design
   - Glow effect at exhaust
   - Cooling vents
   - Power conduits

3. **Weapon Laser** (`component_weapon_laser.png`) - Red
   - Focusing crystal array
   - Laser emitter housing
   - Energy capacitor banks
   - Power feed lines

4. **Weapon Cannon** (`component_weapon_cannon.png`) - Orange/Tan
   - Heavy barrel with reinforcement bands
   - Ammunition magazine
   - Loading indicators
   - Recoil dampeners

5. **Armor** (`component_armor.png`) - Gray
   - Plating pattern (3x3 grid)
   - Rivets at intersections
   - Depth highlights

6. **Reactor/Power** (`component_power.png`) - Green
   - Glowing reactor core
   - Containment field ring
   - Power transfer coils (3 points)
   - Power output indicators

7. **Shield** (`component_shield.png`) - Cyan
   - Shield emitter core
   - Projection field rings (3 layers with transparency)
   - Field projector nodes at corners

8. **Crew Quarters** (`component_crew_quarters.png`) - Light Blue
   - Habitat module grid (2x2)
   - Viewport windows
   - Life support indicator

9. **Ammo Factory** (`component_ammo_factory.png`) - Orange/Brown
   - Fabrication chamber
   - Fabrication array (3x3 grid)
   - Ammunition output symbols
   - Conveyor system

10. **Corridor** (`component_corridor.png`) - Gray
    - Floor panel pattern
    - Direction arrows
    - Side guide rails

11. **Structure** (`component_structure.png`) - Dark Gray
    - Framework beams (3x3 grid)
    - Cross braces at intersections

12. **Engine Room** (`component_engine_room.png`) - Dark Blue
    - Engine mounting hardpoints (4 corners)
    - Power distribution conduits
    - Support brackets

### GUI Elements

Located in `Content/Sprites/ui_*.png`

#### Component Selection Buttons (80x80px)

- Two states per component: normal and selected
- Selected state: bright blue background (60, 100, 160)
- Normal state: dark gray background (40, 45, 55)
- Keybind indicator at bottom (1-7 keys)
- Component icon in center
- Rounded corners and gradients

Files:
- `ui_button_[type].png` - Normal state
- `ui_button_[type]_selected.png` - Selected state

Types: armor, engine, laser, cannon, core, power, shield

#### Panel Backgrounds

- `ui_panel_dark.png` (300x200px) - Dark background (20, 25, 30)
- `ui_panel_light.png` (300x200px) - Light background (50, 60, 70)
- `ui_panel_info.png` (300x150px) - Info panel (40, 60, 80)

All panels feature:
- Rounded corners (6px radius)
- Semi-transparent background (alpha 220)
- Dual border system (outer and inner detail)

#### Keybind Indicators

Sizes: small (24x24), medium (32x32), large (48x48)

Files:
- `ui_key_wasd.png` - WASD movement keys
- `ui_key_arrows.png` - Arrow keys (↑←↓→)
- `ui_key_space.png` - Space bar (large size)
- `ui_key_b.png` - Build mode toggle
- `ui_key_p.png` - Pause
- `ui_key_r.png` - Reset
- `ui_key_esc.png` - Escape/Exit

Visual style:
- Keyboard key aesthetic
- Gradient shading (top lighter)
- Rounded corners (3px radius)
- Highlight line at top
- Centered text

#### Mode Indicators (100x40px)

- `ui_mode_build_active.png` - Build mode active (blue glow)
- `ui_mode_build_inactive.png` - Build mode inactive
- `ui_mode_play_active.png` - Play mode active (blue glow)
- `ui_mode_play_inactive.png` - Play mode inactive

Active state features:
- Bright blue background (60, 100, 160)
- Additional glow border layer
- Base color: dark gray (30, 35, 40) for inactive

#### Health/Power Bar Frames

- `ui_bar_health.png` (200x20px)
- `ui_bar_power.png` (200x20px)
- `ui_bar_small.png` (100x16px)

Features:
- Semi-transparent background
- Dual border system
- Rounded corners (3px radius)

## Color Scheme

Based on Cosmoteer's design philosophy:

- **Blue** - Energy, shields, electronics, technology
- **Red** - Weapons, danger, offensive systems
- **Green** - Life support, crew areas, eco-friendly components
- **Yellow/Gold** - Reactors, power, caution/warning
- **Gray** - Armor, structure, corridors
- **Cyan** - Shields, defensive systems
- **Orange/Brown** - Ammunition, manufacturing, heavy weapons

## Generation Scripts

### Component Sprites

Script: `/tmp/generate_cosmoteer_style_assets.py`

Key functions:
- `draw_rounded_rectangle()` - Rounded corner drawing
- `create_metallic_base()` - Base panel with gradient
- `add_corner_bolts()` - Industrial rivets/bolts
- `add_panel_lines()` - Panel separation lines
- Individual `create_[type]_sprite()` functions for each component

### GUI Elements

Script: `/tmp/generate_gui_assets.py`

Key functions:
- `create_button_base()` - Button with state support
- `create_component_button()` - Component selection buttons
- `create_panel_background()` - Panel backgrounds
- `create_keybind_indicator()` - Keyboard key indicators
- `create_mode_indicator()` - Mode toggle buttons
- `create_health_bar_frame()` - Bar frames

## Regenerating Assets

To regenerate all assets:

```bash
# Install Pillow if not already installed
pip install Pillow

# Generate component sprites
python3 /tmp/generate_cosmoteer_style_assets.py

# Generate GUI elements
python3 /tmp/generate_gui_assets.py

# Rebuild MonoGame content
dotnet build
```

## Integration

Assets are integrated into the game through:

1. **Content.mgcb** - MonoGame content pipeline configuration
   - All sprites registered with TextureProcessor
   - PremultiplyAlpha enabled for proper blending

2. **Game1.cs** - LoadContent()
   - Component textures loaded into Dictionary
   - Passed to rendering methods

3. **Components.cs** - Render()
   - Checks for texture availability
   - Falls back to procedural rendering if texture missing
   - Applies health-based tinting

4. **Ship.cs** - Render()
   - Passes component textures to component rendering
   - Maintains backward compatibility

## Design References

Primary reference: [Cosmoteer Wiki](https://cosmoteer.wiki.gg/)

Visual principles:
- Grid-based modular design
- Clear visual hierarchy
- Color-coding for quick identification
- Industrial/military aesthetic
- Glowing effects for active systems
- Technical greebles for detail
- Semi-transparent UI elements

## Future Enhancements

Potential improvements:
- Animation frames for active components
- More detailed textures at higher resolutions
- Additional component variants
- Custom color schemes/skins
- Dynamic glow effects
- Damage visualization overlays
