"""Particle system for visual effects"""
import pygame
import math
import random
from typing import List, Tuple
from dataclasses import dataclass

@dataclass
class Particle:
    """A single particle"""
    x: float
    y: float
    vx: float
    vy: float
    lifetime: float
    max_lifetime: float
    size: float
    color: Tuple[int, int, int]
    fade: bool = True
    shrink: bool = False
    gravity: float = 0.0

class ParticleSystem:
    """Manages all particles in the game"""
    
    # Particle physics constants
    PARTICLE_DRAG = 0.98
    
    def __init__(self):
        self.particles: List[Particle] = []
    
    def update(self, dt: float):
        """Update all particles"""
        # Iterate backwards to safely remove particles
        for i in range(len(self.particles) - 1, -1, -1):
            particle = self.particles[i]
            particle.lifetime -= dt
            
            if particle.lifetime <= 0:
                self.particles.pop(i)
                continue
            
            # Update position
            particle.x += particle.vx * dt
            particle.y += particle.vy * dt
            
            # Apply gravity
            if particle.gravity != 0:
                particle.vy += particle.gravity * dt
            
            # Apply drag
            particle.vx *= self.PARTICLE_DRAG
            particle.vy *= self.PARTICLE_DRAG
    
    def render(self, surface: pygame.Surface, camera_x: float, camera_y: float):
        """Render all particles"""
        for particle in self.particles:
            screen_x = int(particle.x - camera_x)
            screen_y = int(particle.y - camera_y)
            
            # Skip if off screen
            if screen_x < -10 or screen_x > surface.get_width() + 10:
                continue
            if screen_y < -10 or screen_y > surface.get_height() + 10:
                continue
            
            # Calculate alpha based on lifetime
            alpha = 255
            if particle.fade:
                alpha = int(255 * (particle.lifetime / particle.max_lifetime))
                alpha = max(0, min(255, alpha))
            
            # Calculate size
            size = particle.size
            if particle.shrink:
                size = particle.size * (particle.lifetime / particle.max_lifetime)
                size = max(0.5, size)
            
            # Render particle
            color = particle.color
            if alpha < 250 and particle.fade:
                # Use alpha blending for fading particles
                temp_surface = pygame.Surface((int(size * 2 + 2), int(size * 2 + 2)), pygame.SRCALPHA)
                color_with_alpha = (*color, alpha)
                pygame.draw.circle(temp_surface, color_with_alpha, 
                                 (int(size + 1), int(size + 1)), int(size))
                surface.blit(temp_surface, (screen_x - int(size + 1), screen_y - int(size + 1)))
            else:
                # Direct rendering for fully opaque particles (faster)
                pygame.draw.circle(surface, color, (screen_x, screen_y), int(size))
    
    def create_weapon_fire_effect(self, x: float, y: float, angle: float, weapon_type: str):
        """Create particles for weapon firing"""
        if weapon_type == "laser":
            # Laser muzzle flash - bright and quick
            for _ in range(8):
                speed = random.uniform(30, 60)
                spread = random.uniform(-0.3, 0.3)
                particle_angle = angle + spread
                vx = math.cos(particle_angle) * speed
                vy = math.sin(particle_angle) * speed
                
                color = random.choice([
                    (255, 100, 100),
                    (255, 150, 150),
                    (255, 200, 200),
                ])
                
                particle = Particle(
                    x=x,
                    y=y,
                    vx=vx,
                    vy=vy,
                    lifetime=0.2,
                    max_lifetime=0.2,
                    size=random.uniform(2, 4),
                    color=color,
                    fade=True,
                    shrink=True
                )
                self.particles.append(particle)
        
        else:  # cannon
            # Cannon smoke and fire
            for _ in range(15):
                speed = random.uniform(20, 50)
                spread = random.uniform(-0.5, 0.5)
                particle_angle = angle + spread
                vx = math.cos(particle_angle) * speed
                vy = math.sin(particle_angle) * speed
                
                # Mix of fire and smoke colors
                if random.random() < 0.6:
                    color = random.choice([
                        (255, 200, 0),   # Yellow fire
                        (255, 150, 0),   # Orange fire
                        (255, 100, 0),   # Red-orange
                    ])
                else:
                    color = random.choice([
                        (100, 100, 100), # Gray smoke
                        (150, 150, 150), # Light smoke
                    ])
                
                particle = Particle(
                    x=x,
                    y=y,
                    vx=vx,
                    vy=vy,
                    lifetime=random.uniform(0.3, 0.5),
                    max_lifetime=0.5,
                    size=random.uniform(3, 6),
                    color=color,
                    fade=True,
                    shrink=False
                )
                self.particles.append(particle)
    
    def create_explosion(self, x: float, y: float, size: str = "medium"):
        """Create an explosion effect"""
        # Determine explosion parameters based on size
        if size == "small":
            num_particles = 20
            speed_range = (50, 150)
            size_range = (2, 5)
            lifetime_range = (0.3, 0.6)
        elif size == "large":
            num_particles = 50
            speed_range = (80, 250)
            size_range = (4, 10)
            lifetime_range = (0.5, 1.0)
        else:  # medium
            num_particles = 35
            speed_range = (60, 200)
            size_range = (3, 7)
            lifetime_range = (0.4, 0.8)
        
        # Create explosion particles
        for _ in range(num_particles):
            angle = random.uniform(0, math.pi * 2)
            speed = random.uniform(*speed_range)
            vx = math.cos(angle) * speed
            vy = math.sin(angle) * speed
            
            # Mix of fire colors
            color = random.choice([
                (255, 200, 0),   # Yellow
                (255, 150, 0),   # Orange
                (255, 100, 0),   # Red-orange
                (255, 50, 0),    # Red
                (200, 200, 200), # White-hot
            ])
            
            lifetime = random.uniform(*lifetime_range)
            particle = Particle(
                x=x,
                y=y,
                vx=vx,
                vy=vy,
                lifetime=lifetime,
                max_lifetime=lifetime,
                size=random.uniform(*size_range),
                color=color,
                fade=True,
                shrink=True
            )
            self.particles.append(particle)
        
        # Add smoke particles
        for _ in range(num_particles // 2):
            angle = random.uniform(0, math.pi * 2)
            speed = random.uniform(20, 80)
            vx = math.cos(angle) * speed
            vy = math.sin(angle) * speed
            
            color = random.choice([
                (80, 80, 80),
                (100, 100, 100),
                (120, 120, 120),
            ])
            
            lifetime = random.uniform(0.5, 1.2)
            particle = Particle(
                x=x,
                y=y,
                vx=vx,
                vy=vy,
                lifetime=lifetime,
                max_lifetime=lifetime,
                size=random.uniform(4, 8),
                color=color,
                fade=True,
                shrink=False,
                gravity=20  # Smoke rises slowly
            )
            self.particles.append(particle)
    
    def create_engine_thrust(self, x: float, y: float, angle: float, power: float = 1.0):
        """Create engine thrust particles"""
        # Create 2-3 particles per frame when engine is active
        num_particles = random.randint(2, 3)
        
        for _ in range(num_particles):
            # Thrust goes opposite to engine direction
            thrust_angle = angle + math.pi
            spread = random.uniform(-0.2, 0.2)
            particle_angle = thrust_angle + spread
            
            speed = random.uniform(80, 150) * power
            vx = math.cos(particle_angle) * speed
            vy = math.sin(particle_angle) * speed
            
            # Blue engine glow
            color = random.choice([
                (100, 150, 255),
                (150, 200, 255),
                (200, 220, 255),
            ])
            
            particle = Particle(
                x=x,
                y=y,
                vx=vx,
                vy=vy,
                lifetime=random.uniform(0.1, 0.3),
                max_lifetime=0.3,
                size=random.uniform(2, 4),
                color=color,
                fade=True,
                shrink=True
            )
            self.particles.append(particle)
    
    def create_damage_sparks(self, x: float, y: float):
        """Create sparks when component takes damage"""
        for _ in range(10):
            angle = random.uniform(0, math.pi * 2)
            speed = random.uniform(80, 150)
            vx = math.cos(angle) * speed
            vy = math.sin(angle) * speed
            
            color = random.choice([
                (255, 255, 0),   # Yellow sparks
                (255, 200, 0),   # Orange sparks
                (255, 255, 255), # White sparks
            ])
            
            particle = Particle(
                x=x,
                y=y,
                vx=vx,
                vy=vy,
                lifetime=random.uniform(0.2, 0.4),
                max_lifetime=0.4,
                size=random.uniform(1, 3),
                color=color,
                fade=True,
                shrink=True,
                gravity=200  # Sparks fall
            )
            self.particles.append(particle)
    
    def clear(self):
        """Remove all particles"""
        self.particles.clear()
