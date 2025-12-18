"""Projectile system"""
import pygame
import math
from typing import Optional

class Projectile:
    """A projectile fired from a weapon"""
    
    def __init__(self, x: float, y: float, angle: float, speed: float, 
                 damage: int, projectile_type: str, owner_id: int):
        self.x = x
        self.y = y
        self.angle = angle
        self.speed = speed
        self.damage = damage
        self.projectile_type = projectile_type
        self.owner_id = owner_id
        self.lifetime = 3.0  # seconds
        self.alive = True
        
        # Calculate velocity
        self.vx = math.cos(angle) * speed
        self.vy = math.sin(angle) * speed
        
        # Visual properties
        if projectile_type == "laser":
            self.color = (255, 50, 50)
            self.size = 3
        else:  # cannon
            self.color = (255, 200, 0)
            self.size = 5
    
    def update(self, dt: float):
        """Update projectile position"""
        if not self.alive:
            return
        
        self.x += self.vx * dt
        self.y += self.vy * dt
        self.lifetime -= dt
        
        if self.lifetime <= 0:
            self.alive = False
    
    def render(self, surface: pygame.Surface, camera_x: float, camera_y: float):
        """Render the projectile"""
        if not self.alive:
            return
        
        screen_x = int(self.x - camera_x)
        screen_y = int(self.y - camera_y)
        
        # Draw projectile with trail effect
        if self.projectile_type == "laser":
            # Laser beam effect
            trail_length = 15
            end_x = screen_x - int(math.cos(self.angle) * trail_length)
            end_y = screen_y - int(math.sin(self.angle) * trail_length)
            pygame.draw.line(surface, self.color, (end_x, end_y), (screen_x, screen_y), 2)
            pygame.draw.circle(surface, (255, 255, 255), (screen_x, screen_y), self.size)
        else:
            # Cannon projectile
            pygame.draw.circle(surface, self.color, (screen_x, screen_y), self.size)
            pygame.draw.circle(surface, (255, 255, 0), (screen_x, screen_y), self.size // 2)
    
    def check_collision(self, rect: pygame.Rect) -> bool:
        """Check if projectile collides with a rectangle"""
        return rect.collidepoint(int(self.x), int(self.y))
