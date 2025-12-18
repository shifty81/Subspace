#!/usr/bin/env python3
"""
Subspace - A Cosmoteer-inspired spaceship building and combat game
"""
import pygame
import sys
from src.game import Game

def main():
    """Main entry point for the game"""
    pygame.init()
    
    # Create game instance
    game = Game()
    
    # Run the game
    game.run()
    
    pygame.quit()
    sys.exit()

if __name__ == "__main__":
    main()
