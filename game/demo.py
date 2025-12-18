#!/usr/bin/env python3
"""
Demo script that creates a screenshot of the game
This demonstrates what the game looks like
"""
import os
os.environ['SDL_VIDEODRIVER'] = 'dummy'

import pygame
import math
from src.ship import Ship
from src.projectile import Projectile
from src.config import *

def create_demo_image():
    """Create a demo image showing the game"""
    pygame.init()
    
    # Create surface
    screen = pygame.Surface((SCREEN_WIDTH, SCREEN_HEIGHT))
    font = pygame.font.Font(None, 36)
    small_font = pygame.font.Font(None, 24)
    
    # Fill background
    screen.fill(BLACK)
    
    # Draw starfield
    for i in range(200):
        x = (i * 1234) % SCREEN_WIDTH
        y = (i * 5678) % SCREEN_HEIGHT
        brightness = 100 + (i % 100)
        pygame.draw.circle(screen, (brightness, brightness, brightness), (x, y), 1)
    
    # Create ships
    player = Ship(SCREEN_WIDTH // 2, SCREEN_HEIGHT // 2, 0, is_player=True)
    player.angle = -math.pi / 4  # Angle ship nicely
    
    enemy = Ship(SCREEN_WIDTH // 2 + 250, SCREEN_HEIGHT // 2 - 150, 1, is_player=False)
    enemy.angle = math.pi * 3 / 4
    
    # Create some projectiles
    projectiles = []
    for i in range(5):
        x = SCREEN_WIDTH // 2 + i * 30
        y = SCREEN_HEIGHT // 2 - 50 - i * 20
        angle = -math.pi / 4 + i * 0.1
        proj = Projectile(x, y, angle, 500, 10, "laser", 0)
        projectiles.append(proj)
    
    # Render everything
    camera_x = player.x - SCREEN_WIDTH // 2
    camera_y = player.y - SCREEN_HEIGHT // 2
    
    player.render(screen, camera_x, camera_y)
    enemy.render(screen, camera_x, camera_y)
    
    for proj in projectiles:
        proj.render(screen, camera_x, camera_y)
    
    # Draw title
    title = font.render("SUBSPACE - Cosmoteer-Inspired Space Combat", True, WHITE)
    screen.blit(title, (SCREEN_WIDTH // 2 - title.get_width() // 2, 20))
    
    # Draw UI elements
    ui_texts = [
        "Player Ship (Blue Player)",
        "Enemy Ship (Red Enemy)",
        "",
        "Features:",
        "• Modular ship building",
        "• Component-based damage",
        "• Real-time combat",
        "• Power management",
        "• AI opponents",
    ]
    
    y_pos = 80
    for text in ui_texts:
        rendered = small_font.render(text, True, CYAN if text.startswith("•") else WHITE)
        screen.blit(rendered, (20, y_pos))
        y_pos += 25
    
    # Draw controls
    controls = [
        "Controls:",
        "WASD/Arrows - Move",
        "Space - Fire",
        "B - Build Mode",
        "P - Pause",
    ]
    
    y_pos = SCREEN_HEIGHT - 150
    for text in controls:
        rendered = small_font.render(text, True, YELLOW if text == "Controls:" else GRAY)
        screen.blit(rendered, (20, y_pos))
        y_pos += 25
    
    # Draw stats box
    stats_x = SCREEN_WIDTH - 250
    stats_y = 80
    
    pygame.draw.rect(screen, (30, 30, 30), (stats_x - 10, stats_y - 10, 240, 180))
    pygame.draw.rect(screen, (100, 100, 100), (stats_x - 10, stats_y - 10, 240, 180), 2)
    
    stats = [
        "Player Stats:",
        f"Components: {len(player.components)}",
        f"Health: {player.total_health}",
        f"Power: {player.power_available}",
        f"Thrust: {int(player.total_thrust)}",
        "",
        "Ready to play!",
    ]
    
    for text in stats:
        color = GREEN if text == "Ready to play!" else (WHITE if text == "Player Stats:" else CYAN)
        rendered = small_font.render(text, True, color)
        screen.blit(rendered, (stats_x, stats_y))
        stats_y += 25
    
    # Save image
    pygame.image.save(screen, "/tmp/subspace_demo.png")
    print("✓ Demo image created: /tmp/subspace_demo.png")
    
    return screen

if __name__ == "__main__":
    create_demo_image()
    print("\n✅ Demo complete!")
    print("This shows what the game looks like when running.")
    print("To play the actual game, run: python3 main.py")
