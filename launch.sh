#!/bin/bash
# Subspace Game Launcher
# Streamlined script to launch the game with automatic dependency installation

set -e  # Exit on error

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
NC='\033[0m' # No Color

echo "======================================"
echo "   Subspace Game Launcher"
echo "======================================"
echo ""

# Check if Python 3 is installed
if ! command -v python3 &> /dev/null; then
    echo -e "${RED}Error: Python 3 is not installed.${NC}"
    echo "Please install Python 3.7+ and try again."
    exit 1
fi

# Check Python version
PYTHON_VERSION=$(python3 -c 'import sys; print(".".join(map(str, sys.version_info[:2])))')
echo -e "${GREEN}✓${NC} Python $PYTHON_VERSION detected"

# Check if pygame is installed
if ! python3 -c "import pygame" 2>/dev/null; then
    echo -e "${YELLOW}⚠${NC} Pygame not found. Installing dependencies..."
    pip install -r game/requirements.txt
    echo -e "${GREEN}✓${NC} Dependencies installed successfully"
else
    echo -e "${GREEN}✓${NC} Pygame is installed"
fi

echo ""
echo "Starting Subspace..."
echo "======================================"
echo ""

# Change to game directory and run the game
cd game
python3 main.py
