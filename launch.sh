#!/bin/bash
# Subspace Game Launcher (MonoGame)
# Streamlined script to build and launch the game

set -e  # Exit on error

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
NC='\033[0m' # No Color

echo "======================================"
echo "   Subspace Game Launcher (MonoGame)"
echo "======================================"
echo ""

# Check if .NET SDK is installed
if ! command -v dotnet &> /dev/null; then
    echo -e "${RED}Error: .NET SDK is not installed.${NC}"
    echo "Please install .NET SDK 6.0+ and try again."
    echo "Download from: https://dotnet.microsoft.com/download"
    exit 1
fi

# Check .NET version
DOTNET_VERSION=$(dotnet --version)
echo -e "${GREEN}✓${NC} .NET SDK $DOTNET_VERSION detected"

echo ""
echo "Building Subspace..."
echo "======================================"
echo ""

# Build the project
if dotnet build --nologo -v quiet; then
    echo -e "${GREEN}✓${NC} Build successful"
    echo ""
    echo "Starting Subspace..."
    echo "======================================"
    echo ""
    echo "Controls: WASD/Arrows to move, Space to fire, B for build mode"
    echo "Press ESC to exit the game"
    echo ""
    
    # Run the game
    dotnet run --no-build
else
    echo -e "${RED}✗${NC} Build failed. Please check for errors above."
    exit 1
fi
