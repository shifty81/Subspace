using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Subspace;

/// <summary>
/// Contract that every game scene must implement.
/// Scenes are managed by <see cref="SceneManager"/>.
/// </summary>
public interface IScene
{
    /// <summary>Called once when this scene becomes the active scene.</summary>
    void Enter(object? context = null);

    /// <summary>Called once when this scene is replaced by another.</summary>
    void Exit();

    /// <summary>Per-frame logic update.</summary>
    void Update(float dt);

    /// <summary>Per-frame render (world-space spriteBatch already begun with camera transform).</summary>
    void DrawWorld(SpriteBatch spriteBatch, Texture2D pixel, float gameTime);

    /// <summary>Per-frame render for screen-space UI (no camera transform).</summary>
    void DrawUI(SpriteBatch spriteBatch, Texture2D pixel, float gameTime);
}
