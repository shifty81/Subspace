"""Ship component definitions"""
import pygame
import math
from dataclasses import dataclass
from typing import Optional, Tuple

@dataclass
class ComponentStats:
    """Stats for a ship component"""
    name: str
    health: int
    max_health: int
    power_consumption: int
    power_generation: int = 0
    thrust: float = 0
    color: Tuple[int, int, int] = (100, 100, 100)
    
class ComponentType:
    """Component type definitions"""
    CORE = "core"
    ENGINE = "engine"
    WEAPON_LASER = "weapon_laser"
    WEAPON_CANNON = "weapon_cannon"
    ARMOR = "armor"
    POWER = "power"
    SHIELD = "shield"

class Component:
    """Base class for ship components"""
    
    def __init__(self, component_type: str, grid_x: int, grid_y: int):
        self.component_type = component_type
        self.grid_x = grid_x
        self.grid_y = grid_y
        self.stats = self._get_stats()
        self.cooldown = 0
        self.rotation = 0  # Component rotation relative to ship
        
    def _get_stats(self) -> ComponentStats:
        """Get stats based on component type"""
        stats_map = {
            ComponentType.CORE: ComponentStats(
                name="Core",
                health=200,
                max_health=200,
                power_consumption=0,
                power_generation=50,
                color=(255, 200, 0)
            ),
            ComponentType.ENGINE: ComponentStats(
                name="Engine",
                health=50,
                max_health=50,
                power_consumption=10,
                thrust=200.0,
                color=(0, 150, 255)
            ),
            ComponentType.WEAPON_LASER: ComponentStats(
                name="Laser",
                health=40,
                max_health=40,
                power_consumption=15,
                color=(255, 0, 0)
            ),
            ComponentType.WEAPON_CANNON: ComponentStats(
                name="Cannon",
                health=60,
                max_health=60,
                power_consumption=20,
                color=(150, 150, 0)
            ),
            ComponentType.ARMOR: ComponentStats(
                name="Armor",
                health=150,
                max_health=150,
                power_consumption=0,
                color=(150, 150, 150)
            ),
            ComponentType.POWER: ComponentStats(
                name="Reactor",
                health=80,
                max_health=80,
                power_consumption=0,
                power_generation=100,
                color=(0, 255, 100)
            ),
            ComponentType.SHIELD: ComponentStats(
                name="Shield",
                health=30,
                max_health=30,
                power_consumption=25,
                color=(100, 200, 255)
            ),
        }
        return stats_map.get(self.component_type, ComponentStats(
            name="Unknown",
            health=50,
            max_health=50,
            power_consumption=0
        ))
    
    def take_damage(self, damage: int) -> bool:
        """Apply damage to component. Returns True if destroyed."""
        self.stats.health -= damage
        if self.stats.health <= 0:
            self.stats.health = 0
            return True
        return False
    
    def update(self, dt: float):
        """Update component state"""
        if self.cooldown > 0:
            self.cooldown -= dt
    
    def can_fire(self) -> bool:
        """Check if weapon component can fire"""
        return (self.component_type in [ComponentType.WEAPON_LASER, ComponentType.WEAPON_CANNON] 
                and self.cooldown <= 0 
                and self.stats.health > 0)
    
    def fire(self):
        """Fire weapon (set cooldown)"""
        if self.component_type == ComponentType.WEAPON_LASER:
            self.cooldown = 0.5  # 2 shots per second
        elif self.component_type == ComponentType.WEAPON_CANNON:
            self.cooldown = 1.5  # Slower but more powerful
    
    def render(self, surface: pygame.Surface, x: int, y: int, grid_size: int):
        """Render the component"""
        # Base color
        color = self.stats.color
        
        # Damage indicator (darken based on health)
        health_percent = self.stats.health / self.stats.max_health
        color = tuple(int(c * (0.3 + 0.7 * health_percent)) for c in color)
        
        # Draw component
        rect = pygame.Rect(x, y, grid_size - 2, grid_size - 2)
        pygame.draw.rect(surface, color, rect)
        pygame.draw.rect(surface, (255, 255, 255), rect, 1)
        
        # Draw type indicator
        center_x = x + grid_size // 2
        center_y = y + grid_size // 2
        
        if self.component_type == ComponentType.CORE:
            pygame.draw.circle(surface, (255, 255, 0), (center_x, center_y), 8)
        elif self.component_type == ComponentType.ENGINE:
            pygame.draw.polygon(surface, (255, 255, 255), [
                (center_x, center_y - 8),
                (center_x - 6, center_y + 8),
                (center_x + 6, center_y + 8)
            ])
        elif self.component_type in [ComponentType.WEAPON_LASER, ComponentType.WEAPON_CANNON]:
            pygame.draw.circle(surface, (255, 0, 0), (center_x, center_y), 4)
            pygame.draw.line(surface, (255, 0, 0), 
                           (center_x, center_y), 
                           (center_x, center_y - 10), 2)
