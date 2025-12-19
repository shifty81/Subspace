"""Main game class"""
import pygame
import math
import random
from typing import List, Optional
from .config import *
from .ship import Ship
from .projectile import Projectile
from .components import Component, ComponentType
from .starfield import Starfield

class Game:
    """Main game class"""
    
    def __init__(self):
        self.screen = pygame.display.set_mode((SCREEN_WIDTH, SCREEN_HEIGHT))
        pygame.display.set_caption("Subspace - Cosmoteer-Inspired Space Combat")
        self.clock = pygame.time.Clock()
        self.running = True
        
        # Game state
        self.mode = MODE_PLAY
        self.paused = False
        self.game_time = 0  # Track time for animations
        
        # Camera
        self.camera_x = 0
        self.camera_y = 0
        
        # Enhanced starfield
        self.starfield = Starfield()
        
        # Game objects
        self.player: Optional[Ship] = None
        self.enemies: List[Ship] = []
        self.projectiles: List[Projectile] = []
        
        # Ship builder state
        self.builder_selected_type = ComponentType.ARMOR
        self.builder_available_types = [
            ComponentType.CORE,
            ComponentType.ENGINE,
            ComponentType.WEAPON_LASER,
            ComponentType.WEAPON_CANNON,
            ComponentType.ARMOR,
            ComponentType.POWER,
            ComponentType.SHIELD,
        ]
        
        # UI
        self.font = pygame.font.Font(None, 24)
        self.small_font = pygame.font.Font(None, 18)
        
        # Initialize game
        self._init_game()
    
    def _init_game(self):
        """Initialize game state"""
        # Create player ship
        self.player = Ship(SCREEN_WIDTH // 2, SCREEN_HEIGHT // 2, 0, is_player=True)
        
        # Create enemy ships
        self.enemies = []
        for i in range(3):
            x = random.randint(100, SCREEN_WIDTH - 100)
            y = random.randint(100, SCREEN_HEIGHT - 100)
            # Make sure enemies don't spawn too close to player
            while math.sqrt((x - self.player.x)**2 + (y - self.player.y)**2) < 300:
                x = random.randint(100, SCREEN_WIDTH - 100)
                y = random.randint(100, SCREEN_HEIGHT - 100)
            self.enemies.append(Ship(x, y, i + 1, is_player=False))
    
    def run(self):
        """Main game loop"""
        while self.running:
            dt = self.clock.tick(FPS) / 1000.0
            
            self._handle_events()
            
            if not self.paused:
                self._update(dt)
                self.game_time += dt
            
            self._render()
        
    def _handle_events(self):
        """Handle pygame events"""
        for event in pygame.event.get():
            if event.type == pygame.QUIT:
                self.running = False
            
            elif event.type == pygame.KEYDOWN:
                if event.key == pygame.K_ESCAPE:
                    self.running = False
                elif event.key == pygame.K_p:
                    self.paused = not self.paused
                elif event.key == pygame.K_b:
                    # Toggle build mode
                    self.mode = MODE_BUILD if self.mode == MODE_PLAY else MODE_PLAY
                elif event.key == pygame.K_r:
                    # Reset game
                    self._init_game()
                
                # Builder controls
                elif self.mode == MODE_BUILD:
                    if event.key == pygame.K_1:
                        self.builder_selected_type = ComponentType.ARMOR
                    elif event.key == pygame.K_2:
                        self.builder_selected_type = ComponentType.ENGINE
                    elif event.key == pygame.K_3:
                        self.builder_selected_type = ComponentType.WEAPON_LASER
                    elif event.key == pygame.K_4:
                        self.builder_selected_type = ComponentType.WEAPON_CANNON
                    elif event.key == pygame.K_5:
                        self.builder_selected_type = ComponentType.POWER
                    elif event.key == pygame.K_6:
                        self.builder_selected_type = ComponentType.SHIELD
            
            elif event.type == pygame.MOUSEBUTTONDOWN and self.mode == MODE_BUILD:
                self._handle_builder_click(event.pos, event.button)
    
    def _handle_builder_click(self, pos, button):
        """Handle mouse clicks in builder mode"""
        if not self.player:
            return
        
        # Convert screen position to world position
        world_x = pos[0] + self.camera_x
        world_y = pos[1] + self.camera_y
        
        # Convert to ship local space
        local_x = world_x - self.player.x
        local_y = world_y - self.player.y
        
        # Convert to grid coordinates
        grid_x = int((local_x / GRID_SIZE) + self.player.grid_width // 2)
        grid_y = int((local_y / GRID_SIZE) + self.player.grid_height // 2)
        
        # Check if within bounds
        if 0 <= grid_x < self.player.grid_width and 0 <= grid_y < self.player.grid_height:
            if button == 1:  # Left click - add component
                existing = self.player.get_component_at(grid_x, grid_y)
                if not existing:
                    comp = Component(self.builder_selected_type, grid_x, grid_y)
                    self.player.add_component(comp)
            elif button == 3:  # Right click - remove component
                self.player.remove_component(grid_x, grid_y)
    
    def _update(self, dt: float):
        """Update game state"""
        if self.mode == MODE_PLAY:
            self._update_play_mode(dt)
        elif self.mode == MODE_BUILD:
            self._update_build_mode(dt)
    
    def _update_play_mode(self, dt: float):
        """Update play mode"""
        if not self.player:
            return
        
        # Handle player input
        keys = pygame.key.get_pressed()
        
        if keys[pygame.K_w] or keys[pygame.K_UP]:
            self.player.apply_thrust(dt)
        
        if keys[pygame.K_a] or keys[pygame.K_LEFT]:
            self.player.rotate(-1, dt)
        
        if keys[pygame.K_d] or keys[pygame.K_RIGHT]:
            self.player.rotate(1, dt)
        
        if keys[pygame.K_SPACE]:
            projectiles = self.player.fire_weapons()
            self.projectiles.extend(projectiles)
        
        # Update player
        self.player.update(dt)
        
        # Update enemies
        for enemy in self.enemies[:]:
            enemy.update(dt, self.player)
            
            # Enemy AI firing
            if random.random() < 0.02:  # 2% chance per frame
                projectiles = enemy.fire_weapons()
                self.projectiles.extend(projectiles)
            
            # Remove destroyed enemies
            if enemy.is_destroyed():
                self.enemies.remove(enemy)
        
        # Update projectiles
        for proj in self.projectiles[:]:
            proj.update(dt)
            
            if not proj.alive:
                self.projectiles.remove(proj)
                continue
            
            # Check collision with player
            if proj.owner_id != self.player.ship_id:
                bounds = self.player.get_bounds()
                if proj.check_collision(bounds):
                    self.player.take_damage(proj.damage, proj.x, proj.y)
                    proj.alive = False
            
            # Check collision with enemies
            for enemy in self.enemies:
                if proj.owner_id != enemy.ship_id:
                    bounds = enemy.get_bounds()
                    if proj.check_collision(bounds):
                        enemy.take_damage(proj.damage, proj.x, proj.y)
                        proj.alive = False
                        break
        
        # Update camera to follow player
        self.camera_x = self.player.x - SCREEN_WIDTH // 2
        self.camera_y = self.player.y - SCREEN_HEIGHT // 2
        
        # Check win/lose conditions
        if self.player.is_destroyed():
            self.paused = True
        elif len(self.enemies) == 0:
            # Spawn more enemies
            for i in range(3):
                x = self.player.x + random.randint(-500, 500)
                y = self.player.y + random.randint(-500, 500)
                enemy_id = max([e.ship_id for e in self.enemies] + [self.player.ship_id]) + 1
                self.enemies.append(Ship(x, y, enemy_id, is_player=False))
    
    def _update_build_mode(self, dt: float):
        """Update build mode"""
        if self.player:
            # Camera follows player in build mode too
            self.camera_x = self.player.x - SCREEN_WIDTH // 2
            self.camera_y = self.player.y - SCREEN_HEIGHT // 2
    
    def _render(self):
        """Render everything"""
        # Clear screen
        self.screen.fill(BLACK)
        
        # Draw enhanced starfield background with parallax
        self.starfield.render(self.screen, self.camera_x, self.camera_y, self.game_time)
        
        # Render game objects
        if self.player:
            self.player.render(self.screen, self.camera_x, self.camera_y)
        
        for enemy in self.enemies:
            enemy.render(self.screen, self.camera_x, self.camera_y)
        
        for proj in self.projectiles:
            proj.render(self.screen, self.camera_x, self.camera_y)
        
        # Draw UI
        self._draw_ui()
        
        # Draw mode-specific overlays
        if self.mode == MODE_BUILD:
            self._draw_builder_ui()
        
        pygame.display.flip()
    
    def _draw_ui(self):
        """Draw main UI"""
        if not self.player:
            return
        
        # Player stats
        y_offset = 10
        stats = [
            f"Health: {self.player.total_health}/{self.player.max_health}",
            f"Power: {self.player.power_available - self.player.power_used}/{self.player.power_available}",
            f"Enemies: {len(self.enemies)}",
            f"Mode: {self.mode.upper()}",
        ]
        
        for stat in stats:
            text = self.font.render(stat, True, WHITE)
            self.screen.blit(text, (10, y_offset))
            y_offset += 30
        
        # Controls
        y_offset = SCREEN_HEIGHT - 100
        controls = [
            "WASD/Arrows: Move/Rotate | SPACE: Fire | B: Build Mode",
            "P: Pause | R: Reset | ESC: Quit"
        ]
        
        for control in controls:
            text = self.small_font.render(control, True, GRAY)
            self.screen.blit(text, (10, y_offset))
            y_offset += 20
        
        # Win/Lose conditions
        if self.paused:
            if self.player.is_destroyed():
                text = self.font.render("GAME OVER! Press R to restart", True, RED)
            else:
                text = self.font.render("PAUSED - Press P to continue", True, YELLOW)
            text_rect = text.get_rect(center=(SCREEN_WIDTH // 2, SCREEN_HEIGHT // 2))
            self.screen.blit(text, text_rect)
    
    def _draw_builder_ui(self):
        """Draw ship builder UI"""
        # Component palette
        palette_x = SCREEN_WIDTH - 200
        palette_y = 10
        
        text = self.font.render("Builder (Press B to exit)", True, WHITE)
        self.screen.blit(text, (palette_x - 50, palette_y))
        palette_y += 40
        
        text = self.small_font.render("Left Click: Add | Right Click: Remove", True, GRAY)
        self.screen.blit(text, (palette_x - 50, palette_y))
        palette_y += 30
        
        # Component types
        type_display = {
            ComponentType.ARMOR: "1: Armor",
            ComponentType.ENGINE: "2: Engine",
            ComponentType.WEAPON_LASER: "3: Laser",
            ComponentType.WEAPON_CANNON: "4: Cannon",
            ComponentType.POWER: "5: Reactor",
            ComponentType.SHIELD: "6: Shield",
        }
        
        for comp_type in self.builder_available_types:
            if comp_type == ComponentType.CORE:
                continue  # Don't allow placing cores
            
            color = WHITE if comp_type == self.builder_selected_type else GRAY
            text = self.small_font.render(type_display.get(comp_type, comp_type), True, color)
            self.screen.blit(text, (palette_x, palette_y))
            palette_y += 25
        
        # Draw grid overlay on player ship
        if self.player:
            for comp in self.player.components:
                world_x = self.player.x + (comp.grid_x - self.player.grid_width // 2) * GRID_SIZE
                world_y = self.player.y + (comp.grid_y - self.player.grid_height // 2) * GRID_SIZE
                
                screen_x = int(world_x - self.camera_x)
                screen_y = int(world_y - self.camera_y)
                
                pygame.draw.rect(self.screen, (255, 255, 255), 
                               (screen_x, screen_y, GRID_SIZE, GRID_SIZE), 1)
            
            # Show build grid
            for gx in range(self.player.grid_width):
                for gy in range(self.player.grid_height):
                    world_x = self.player.x + (gx - self.player.grid_width // 2) * GRID_SIZE
                    world_y = self.player.y + (gy - self.player.grid_height // 2) * GRID_SIZE
                    
                    screen_x = int(world_x - self.camera_x)
                    screen_y = int(world_y - self.camera_y)
                    
                    pygame.draw.rect(self.screen, GRID_COLOR,
                                   (screen_x, screen_y, GRID_SIZE, GRID_SIZE), 1)
