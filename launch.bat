@echo off
REM Subspace Game Launcher for Windows
REM Streamlined script to launch the game with automatic dependency installation

echo ======================================
echo    Subspace Game Launcher
echo ======================================
echo.

REM Check if Python is installed
python --version >nul 2>&1
if errorlevel 1 (
    echo [ERROR] Python is not installed or not in PATH.
    echo Please install Python 3.7+ from https://www.python.org/
    pause
    exit /b 1
)

echo [OK] Python detected

REM Check if pygame is installed
python -c "import pygame" >nul 2>&1
if errorlevel 1 (
    echo [INFO] Pygame not found. Installing dependencies...
    pip install -r game\requirements.txt
    if errorlevel 1 (
        echo [ERROR] Failed to install dependencies
        pause
        exit /b 1
    )
    echo [OK] Dependencies installed successfully
) else (
    echo [OK] Pygame is installed
)

echo.
echo Starting Subspace...
echo ======================================
echo.

REM Change to game directory and run the game
cd game
python main.py
