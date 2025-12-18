"""Ship class with modular components"""
import pygame
import math
import random
from typing import List, Optional, Tuple
from .components import Component, ComponentType
from .projectile import Projectile
from .config import GRID_SIZE, MAX_VELOCITY, DRAG

class Ship:
    """A spaceship made of modular components"""
    
    def __init__(self, x: float, y: float, ship_id: int, is_player: bool = False):
        self.x = x
        self.y = y
        self.ship_id = ship_id
        self.is_player = is_player
        self.angle = 0  # Ship rotation in radians
        self.vx = 0  # Velocity X
        self.vy = 0  # Velocity Y
        self.angular_velocity = 0
        
        # Ship components organized in a grid
        self.components: List[Component] = []
        self.grid_width = 10
        self.grid_height = 10
        
        # Ship stats (calculated from components)
        self.total_health = 0
        self.max_health = 0
        self.power_available = 0
        self.power_used = 0
        self.total_thrust = 0
        
        # AI properties
        self.target: Optional['Ship'] = None
        self.ai_state = "idle"
        
        # Create default ship layout
        if is_player:
            self._create_player_ship()
        else:
            self._create_enemy_ship()
        
        self._recalculate_stats()
    
    def _create_player_ship(self):
        """Create default player ship"""
        # Core in center
        self.components.append(Component(ComponentType.CORE, 4, 4))
        
        # Engines
        self.components.append(Component(ComponentType.ENGINE, 4, 6))
        self.components.append(Component(ComponentType.ENGINE, 4, 7))
        
        # Weapons
        self.components.append(Component(ComponentType.WEAPON_LASER, 3, 3))
        self.components.append(Component(ComponentType.WEAPON_LASER, 5, 3))
        self.components.append(Component(ComponentType.WEAPON_CANNON, 4, 2))
        
        # Power
        self.components.append(Component(ComponentType.POWER, 3, 5))
        self.components.append(Component(ComponentType.POWER, 5, 5))
        
        # Armor
        self.components.append(Component(ComponentType.ARMOR, 3, 4))
        self.components.append(Component(ComponentType.ARMOR, 5, 4))
        self.components.append(Component(ComponentType.ARMOR, 4, 5))
    
    def _create_enemy_ship(self):
        """Create default enemy ship"""
        # Smaller, simpler enemy ship
        self.components.append(Component(ComponentType.CORE, 4, 4))
        self.components.append(Component(ComponentType.ENGINE, 4, 6))
        self.components.append(Component(ComponentType.WEAPON_LASER, 4, 3))
        self.components.append(Component(ComponentType.POWER, 3, 4))
        self.components.append(Component(ComponentType.ARMOR, 5, 4))
    
    def _recalculate_stats(self):
        """Recalculate ship stats from components"""
        self.total_health = 0
        self.max_health = 0
        self.power_available = 0
        self.power_used = 0
        self.total_thrust = 0
        
        for comp in self.components:
            self.total_health += comp.stats.health
            self.max_health += comp.stats.max_health
            self.power_available += comp.stats.power_generation
            self.power_used += comp.stats.power_consumption
            self.total_thrust += comp.stats.thrust
    
    def add_component(self, component: Component):
        """Add a component to the ship"""
        self.components.append(component)
        self._recalculate_stats()
    
    def remove_component(self, grid_x: int, grid_y: int):
        """Remove component at grid position"""
        self.components = [c for c in self.components 
                          if not (c.grid_x == grid_x and c.grid_y == grid_y)]
        self._recalculate_stats()
    
    def get_component_at(self, grid_x: int, grid_y: int) -> Optional[Component]:
        """Get component at grid position"""
        for comp in self.components:
            if comp.grid_x == grid_x and comp.grid_y == grid_y:
                return comp
        return None
    
    def update(self, dt: float, target: Optional['Ship'] = None):
        """Update ship physics and components"""
        # Update all components
        for comp in self.components:
            comp.update(dt)
        
        # AI control
        if not self.is_player and target:
            self.target = target
            self._update_ai(dt)
        
        # Apply drag
        self.vx *= DRAG
        self.vy *= DRAG
        self.angular_velocity *= DRAG
        
        # Limit velocity
        speed = math.sqrt(self.vx**2 + self.vy**2)
        if speed > MAX_VELOCITY:
            self.vx = (self.vx / speed) * MAX_VELOCITY
            self.vy = (self.vy / speed) * MAX_VELOCITY
        
        # Update position
        self.x += self.vx * dt
        self.y += self.vy * dt
        self.angle += self.angular_velocity * dt
        
        # Keep angle in range
        self.angle = self.angle % (2 * math.pi)
        
        # Recalculate stats
        self._recalculate_stats()
    
    def _update_ai(self, dt: float):
        """Simple AI behavior"""
        if not self.target:
            return
        
        # Calculate direction to target
        dx = self.target.x - self.x
        dy = self.target.y - self.y
        distance = math.sqrt(dx**2 + dy**2)
        
        if distance < 10:
            return
        
        target_angle = math.atan2(dy, dx)
        
        # Rotate towards target
        angle_diff = target_angle - self.angle
        # Normalize angle difference to [-pi, pi]
        while angle_diff > math.pi:
            angle_diff -= 2 * math.pi
        while angle_diff < -math.pi:
            angle_diff += 2 * math.pi
        
        # Apply rotation
        rotation_speed = 2.0
        if abs(angle_diff) > 0.1:
            self.angular_velocity = rotation_speed * (1 if angle_diff > 0 else -1)
        else:
            self.angular_velocity = 0
        
        # Move forward if facing target
        if abs(angle_diff) < 0.5:
            # Keep distance
            optimal_distance = 300
            if distance > optimal_distance:
                self.apply_thrust(dt)
    
    def apply_thrust(self, dt: float):
        """Apply thrust in the direction the ship is facing"""
        if self.total_thrust > 0 and self.power_available >= self.power_used:
            thrust_force = self.total_thrust * dt
            self.vx += math.cos(self.angle) * thrust_force
            self.vy += math.sin(self.angle) * thrust_force
    
    def rotate(self, direction: int, dt: float):
        """Rotate the ship (-1 for left, 1 for right)"""
        rotation_speed = 3.0
        self.angular_velocity += direction * rotation_speed * dt
    
    def fire_weapons(self) -> List[Projectile]:
        """Fire all weapons that can fire. Returns list of projectiles."""
        projectiles = []
        
        for comp in self.components:
            if comp.can_fire() and self.power_available >= self.power_used:
                comp.fire()
                
                # Calculate projectile spawn position (in world space)
                local_x = (comp.grid_x - self.grid_width // 2) * GRID_SIZE
                local_y = (comp.grid_y - self.grid_height // 2) * GRID_SIZE
                
                # Rotate by ship angle
                rotated_x = local_x * math.cos(self.angle) - local_y * math.sin(self.angle)
                rotated_y = local_x * math.sin(self.angle) + local_y * math.cos(self.angle)
                
                spawn_x = self.x + rotated_x
                spawn_y = self.y + rotated_y
                
                # Create projectile
                proj_type = "laser" if comp.component_type == ComponentType.WEAPON_LASER else "cannon"
                damage = 10 if proj_type == "laser" else 25
                speed = 500 if proj_type == "laser" else 350
                
                projectile = Projectile(
                    spawn_x, spawn_y, 
                    self.angle,
                    speed, damage, proj_type, 
                    self.ship_id
                )
                projectiles.append(projectile)
        
        return projectiles
    
    def take_damage(self, damage: int, hit_x: float, hit_y: float):
        """Apply damage to the ship at a specific world position"""
        # Convert world position to local grid position
        local_x = hit_x - self.x
        local_y = hit_y - self.y
        
        # Rotate by inverse of ship angle
        angle = -self.angle
        rotated_x = local_x * math.cos(angle) - local_y * math.sin(angle)
        rotated_y = local_x * math.sin(angle) + local_y * math.cos(angle)
        
        # Convert to grid coordinates
        grid_x = int((rotated_x / GRID_SIZE) + self.grid_width // 2)
        grid_y = int((rotated_y / GRID_SIZE) + self.grid_height // 2)
        
        # Find component at position
        comp = self.get_component_at(grid_x, grid_y)
        if comp:
            destroyed = comp.take_damage(damage)
            if destroyed:
                self.components.remove(comp)
        
        self._recalculate_stats()
    
    def is_destroyed(self) -> bool:
        """Check if ship is destroyed (no core)"""
        for comp in self.components:
            if comp.component_type == ComponentType.CORE:
                return False
        return True
    
    def get_bounds(self) -> pygame.Rect:
        """Get bounding rectangle in world space"""
        if not self.components:
            return pygame.Rect(self.x, self.y, 1, 1)
        
        min_x = min(c.grid_x for c in self.components)
        max_x = max(c.grid_x for c in self.components)
        min_y = min(c.grid_y for c in self.components)
        max_y = max(c.grid_y for c in self.components)
        
        width = (max_x - min_x + 1) * GRID_SIZE
        height = (max_y - min_y + 1) * GRID_SIZE
        
        # Approximate center position
        center_offset_x = (min_x + max_x) / 2 - self.grid_width // 2
        center_offset_y = (min_y + max_y) / 2 - self.grid_height // 2
        
        return pygame.Rect(
            self.x + center_offset_x * GRID_SIZE - width // 2,
            self.y + center_offset_y * GRID_SIZE - height // 2,
            width, height
        )
    
    def render(self, surface: pygame.Surface, camera_x: float, camera_y: float):
        """Render the ship"""
        screen_x = int(self.x - camera_x)
        screen_y = int(self.y - camera_y)
        
        # Create a temporary surface for the ship
        ship_width = self.grid_width * GRID_SIZE
        ship_height = self.grid_height * GRID_SIZE
        ship_surface = pygame.Surface((ship_width, ship_height), pygame.SRCALPHA)
        
        # Render components on temporary surface
        for comp in self.components:
            comp_x = comp.grid_x * GRID_SIZE
            comp_y = comp.grid_y * GRID_SIZE
            comp.render(ship_surface, comp_x, comp_y, GRID_SIZE)
        
        # Rotate the ship surface
        rotated = pygame.transform.rotate(ship_surface, -math.degrees(self.angle))
        rotated_rect = rotated.get_rect(center=(screen_x, screen_y))
        
        # Draw on main surface
        surface.blit(rotated, rotated_rect.topleft)
        
        # Draw velocity indicator (debug)
        if False:  # Set to True for debugging
            vel_scale = 0.5
            pygame.draw.line(surface, (0, 255, 0),
                           (screen_x, screen_y),
                           (screen_x + self.vx * vel_scale, screen_y + self.vy * vel_scale), 2)
