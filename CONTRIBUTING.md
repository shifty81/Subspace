# Contributing to Subspace

Thank you for your interest in contributing to Subspace! This document provides guidelines and information for contributors.

## Ways to Contribute

### 1. Report Bugs
- Use GitHub Issues
- Provide clear reproduction steps
- Include system information (OS, Python version, Pygame version)
- Describe expected vs actual behavior

### 2. Suggest Features
- Check existing issues first
- Describe the feature clearly
- Explain use cases
- Consider implementation complexity

### 3. Improve Documentation
- Fix typos and unclear sections
- Add examples
- Improve code comments
- Create tutorials

### 4. Submit Code
- Follow the code style guidelines below
- Write clear commit messages
- Test your changes thoroughly
- Update documentation if needed

## Getting Started

### Setup Development Environment

```bash
# Clone the repository
git clone https://github.com/shifty81/Subspace.git
cd Subspace

# Install dependencies
cd game
pip install -r requirements.txt

# Run the game
python3 main.py

# Run tests
python3 -m pytest  # (when tests are added)
```

### Project Structure

```
Subspace/
├── game/               # Main game directory
│   ├── main.py        # Entry point
│   ├── src/           # Source code
│   │   ├── game.py    # Game loop and main logic
│   │   ├── ship.py    # Ship class and physics
│   │   ├── components.py  # Component definitions
│   │   ├── projectile.py  # Projectile system
│   │   └── config.py  # Configuration constants
│   ├── assets/        # Game assets (sprites, sounds)
│   └── README.md      # Game documentation
├── OPEN_SOURCE_RESOURCES.md  # Resource guide
├── QUICK_START.md     # Quick start guide
├── GAMEPLAY.md        # Gameplay documentation
└── README.md          # Main README
```

## Code Style Guidelines

### Python Code Style

Follow PEP 8 with these specifics:

```python
# Use 4 spaces for indentation
def example_function():
    pass

# Use descriptive variable names
player_health = 100  # Good
ph = 100            # Bad

# Add docstrings to classes and functions
def calculate_damage(base_damage: int, armor: int) -> int:
    """
    Calculate final damage after armor reduction.
    
    Args:
        base_damage: The initial damage value
        armor: The armor value to reduce damage
        
    Returns:
        Final damage after armor reduction
    """
    return max(0, base_damage - armor)

# Use type hints where possible
def create_ship(x: float, y: float, is_player: bool) -> Ship:
    return Ship(x, y, is_player)

# Keep functions focused and small
# Break large functions into smaller ones
```

### Comments

```python
# Good comments explain WHY, not WHAT
# Bad: Increment x by 1
x += 1

# Good: Track frame count for animation timing
frame_count += 1

# Use comments for complex logic
# Calculate angle difference and normalize to [-pi, pi]
# This ensures we always rotate the shortest direction
angle_diff = target_angle - current_angle
while angle_diff > math.pi:
    angle_diff -= 2 * math.pi
```

### File Organization

```python
# Import order:
# 1. Standard library
import math
import sys

# 2. Third-party libraries
import pygame

# 3. Local modules
from .config import *
from .ship import Ship
```

## Feature Development

### Adding New Component Types

1. Add type constant in `components.py`:
```python
class ComponentType:
    NEW_TYPE = "new_type"
```

2. Add stats in `Component._get_stats()`:
```python
ComponentType.NEW_TYPE: ComponentStats(
    name="New Type",
    health=100,
    max_health=100,
    power_consumption=10,
    color=(R, G, B)
)
```

3. Add rendering logic in `Component.render()` if needed

4. Update builder UI in `game.py`

5. Test thoroughly!

### Adding New Features

1. **Create a branch**
   ```bash
   git checkout -b feature/new-feature-name
   ```

2. **Implement the feature**
   - Write clean, documented code
   - Follow existing patterns
   - Keep changes focused

3. **Test your changes**
   - Manual testing
   - Add automated tests if possible
   - Test edge cases

4. **Update documentation**
   - Update README if needed
   - Add to CHANGELOG
   - Document new features in GAMEPLAY.md

5. **Commit and push**
   ```bash
   git add .
   git commit -m "Add feature: brief description"
   git push origin feature/new-feature-name
   ```

6. **Create Pull Request**
   - Describe what you changed
   - Explain why
   - Link related issues
   - Add screenshots/videos if applicable

## Testing Guidelines

### Manual Testing Checklist

- [ ] Game starts without errors
- [ ] Player ship moves correctly
- [ ] Weapons fire and hit targets
- [ ] Build mode works properly
- [ ] Components can be added/removed
- [ ] Power system works correctly
- [ ] Damage is applied properly
- [ ] Game doesn't crash
- [ ] Performance is acceptable

### Automated Testing (Future)

When adding tests:
- Use pytest
- Test individual functions
- Mock complex dependencies
- Aim for >70% coverage

## Common Development Tasks

### Adding a New Weapon Type

1. Define in `ComponentType`
2. Add stats with damage parameters
3. Create projectile type in `projectile.py`
4. Add firing logic in `Ship.fire_weapons()`
5. Add to builder menu

### Modifying Game Balance

Edit values in these locations:
- Component health: `components.py` → `ComponentStats`
- Weapon damage: `ship.py` → `fire_weapons()`
- Projectile speed: `ship.py` → `fire_weapons()`
- Power values: `components.py` → `ComponentStats`
- Ship physics: `config.py` → constants

### Adding Visual Effects

1. Create effect class in new `effects.py`
2. Trigger effects from appropriate systems
3. Update and render in main game loop
4. Keep performance in mind

## Performance Guidelines

- Profile before optimizing
- Use object pooling for projectiles
- Minimize drawing operations
- Cache calculations when possible
- Consider spatial partitioning for large numbers of objects

## Documentation Standards

### Code Documentation

```python
class NewClass:
    """Brief description of the class.
    
    More detailed description if needed.
    Explain key concepts and usage.
    """
    
    def __init__(self, param: type):
        """Initialize the class.
        
        Args:
            param: Description of parameter
        """
        pass
```

### README Documentation

- Keep it concise
- Use clear headings
- Include code examples
- Add screenshots/GIFs when possible

## Community Guidelines

### Be Respectful
- Be kind and courteous
- Respect different opinions
- Provide constructive feedback
- Help newcomers

### Communication
- Use clear, concise language
- Stay on topic
- Ask questions if unsure
- Share knowledge

## Questions?

- Open an issue for questions
- Tag with "question" label
- Provide context
- Be patient waiting for response

## Recognition

Contributors will be:
- Listed in CONTRIBUTORS.md
- Credited in release notes
- Thanked in the community

## License

By contributing, you agree that your contributions will be licensed under the MIT License.

---

Thank you for helping make Subspace better! 🚀
