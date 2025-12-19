using Microsoft.Xna.Framework;

namespace Subspace;

/// <summary>
/// Game configuration and constants
/// </summary>
public static class Config
{
    // Screen settings
    public const int SCREEN_WIDTH = 1280;
    public const int SCREEN_HEIGHT = 720;
    public const int FPS = 60;

    // Colors
    public static readonly Color BLACK = Color.Black;
    public static readonly Color WHITE = Color.White;
    public static readonly Color GRAY = new Color(128, 128, 128);
    public static readonly Color DARK_GRAY = new Color(64, 64, 64);
    public static readonly Color RED = Color.Red;
    public static readonly Color GREEN = Color.Green;
    public static readonly Color BLUE = new Color(0, 100, 255);
    public static readonly Color YELLOW = Color.Yellow;
    public static readonly Color ORANGE = Color.Orange;
    public static readonly Color CYAN = Color.Cyan;

    // Grid settings (for ship building)
    public const int GRID_SIZE = 32;
    public static readonly Color GRID_COLOR = new Color(40, 40, 40);

    // Physics
    public const float MAX_VELOCITY = 300f;
    public const float DRAG = 0.98f;

    // Game modes
    public const string MODE_PLAY = "play";
    public const string MODE_BUILD = "build";
}
