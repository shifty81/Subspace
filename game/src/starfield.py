"""Enhanced starfield with parallax scrolling and nebula effects"""
import pygame
import random
import math
from typing import List, Tuple
from .config import SCREEN_WIDTH, SCREEN_HEIGHT

class Star:
    """A single star in the starfield"""
    def __init__(self, x: float, y: float, size: float, brightness: int, depth: float):
        self.x = x
        self.y = y
        self.size = size
        self.brightness = brightness
        self.depth = depth  # 0.0-1.0, lower = farther away
        self.twinkle_offset = random.random() * 6.28  # Random phase for twinkling

class Starfield:
    """Enhanced starfield with parallax scrolling"""
    
    def __init__(self):
        # Multiple layers for parallax effect
        self.layers: List[List[Star]] = []
        self.num_layers = 3
        
        # Layer properties (depth, density, size_range, brightness_range, parallax_factor)
        self.layer_configs = [
            # Far layer - small, dim, slow parallax
            (0.2, 150, (1, 1), (80, 120), 0.2),
            # Mid layer - medium size and brightness
            (0.5, 100, (1, 2), (120, 180), 0.5),
            # Near layer - larger, brighter, fast parallax
            (0.8, 50, (2, 3), (180, 255), 1.0),
        ]
        
        # Nebula particles for background atmosphere
        self.nebula_particles: List[Tuple[float, float, float, Tuple[int, int, int, int]]] = []
        
        self._generate_starfield()
        self._generate_nebula()
    
    def _generate_starfield(self):
        """Generate stars for all layers"""
        for depth, count, size_range, brightness_range, _ in self.layer_configs:
            layer = []
            for _ in range(count):
                x = random.randint(0, SCREEN_WIDTH * 3)
                y = random.randint(0, SCREEN_HEIGHT * 3)
                size = random.uniform(*size_range)
                brightness = random.randint(*brightness_range)
                star = Star(x, y, size, brightness, depth)
                layer.append(star)
            self.layers.append(layer)
    
    def _generate_nebula(self):
        """Generate nebula particles for atmospheric effect"""
        num_nebula = 20
        colors = [
            (50, 0, 80, 30),    # Purple
            (0, 30, 80, 30),    # Blue
            (80, 0, 40, 30),    # Magenta
            (0, 50, 60, 30),    # Cyan
        ]
        
        for _ in range(num_nebula):
            x = random.randint(0, SCREEN_WIDTH * 4)
            y = random.randint(0, SCREEN_HEIGHT * 4)
            radius = random.randint(80, 200)
            color = random.choice(colors)
            self.nebula_particles.append((x, y, radius, color))
    
    def render(self, screen: pygame.Surface, camera_x: float, camera_y: float, time: float):
        """Render the starfield with parallax scrolling"""
        # Render nebula first (background)
        self._render_nebula(screen, camera_x, camera_y)
        
        # Render star layers with parallax
        for layer_idx, layer in enumerate(self.layers):
            _, _, size_range, _, parallax_factor = self.layer_configs[layer_idx]
            
            for star in layer:
                # Apply parallax based on depth
                parallax_x = camera_x * parallax_factor
                parallax_y = camera_y * parallax_factor
                
                # Calculate screen position with wrapping
                world_width = SCREEN_WIDTH * 3
                world_height = SCREEN_HEIGHT * 3
                
                screen_x = (star.x - parallax_x) % world_width
                screen_y = (star.y - parallax_y) % world_height
                
                # Only draw if on screen (with margin)
                if -50 < screen_x < SCREEN_WIDTH + 50 and -50 < screen_y < SCREEN_HEIGHT + 50:
                    # Add subtle twinkling effect
                    twinkle = math.sin(time * 2 + star.twinkle_offset) * 0.2 + 0.8
                    brightness = int(star.brightness * twinkle)
                    brightness = max(0, min(255, brightness))
                    
                    color = (brightness, brightness, brightness)
                    
                    if star.size > 1:
                        # Draw larger stars as circles
                        pygame.draw.circle(screen, color, (int(screen_x), int(screen_y)), int(star.size))
                    else:
                        # Draw small stars as single pixels
                        if 0 <= screen_x < SCREEN_WIDTH and 0 <= screen_y < SCREEN_HEIGHT:
                            screen.set_at((int(screen_x), int(screen_y)), color)
    
    def _render_nebula(self, screen: pygame.Surface, camera_x: float, camera_y: float):
        """Render nebula clouds in the background"""
        # Create a temporary surface for nebula with alpha
        nebula_surface = pygame.Surface((SCREEN_WIDTH, SCREEN_HEIGHT), pygame.SRCALPHA)
        
        for x, y, radius, color in self.nebula_particles:
            # Very slow parallax for nebula (0.1x camera movement)
            parallax_x = camera_x * 0.1
            parallax_y = camera_y * 0.1
            
            world_width = SCREEN_WIDTH * 4
            world_height = SCREEN_HEIGHT * 4
            
            screen_x = (x - parallax_x) % world_width
            screen_y = (y - parallax_y) % world_height
            
            # Draw nebula as soft gradients
            if -radius < screen_x < SCREEN_WIDTH + radius and -radius < screen_y < SCREEN_HEIGHT + radius:
                # Create radial gradient effect
                steps = 5
                for i in range(steps):
                    r = radius * (1 - i / steps)
                    alpha = int(color[3] * (1 - i / steps))
                    current_color = (color[0], color[1], color[2], alpha)
                    pygame.draw.circle(nebula_surface, current_color, (int(screen_x), int(screen_y)), int(r))
        
        # Blit nebula surface onto main screen
        screen.blit(nebula_surface, (0, 0))
