@echo off
REM Subspace Game Launcher for Windows (MonoGame)
REM Streamlined script to build and launch the game

echo ======================================
echo    Subspace Game Launcher (MonoGame)
echo ======================================
echo.

REM Check if .NET SDK is installed
dotnet --version >nul 2>&1
if errorlevel 1 (
    echo [ERROR] .NET SDK is not installed or not in PATH.
    echo Please install .NET SDK 6.0+ from https://dotnet.microsoft.com/download
    pause
    exit /b 1
)

echo [OK] .NET SDK detected

echo.
echo Building Subspace...
echo ======================================
echo.

REM Build the project
dotnet build --nologo -v quiet
if errorlevel 1 (
    echo [ERROR] Build failed. Please check for errors above.
    pause
    exit /b 1
)

echo [OK] Build successful

echo.
echo Starting Subspace...
echo ======================================
echo.
echo Controls: WASD/Arrows to move, Space to fire, B for build mode
echo Press ESC to exit the game
echo.

REM Run the game
dotnet run --no-build
