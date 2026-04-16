using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;

namespace Subspace;

/// <summary>
/// Manages the active scene and cross-fade transitions between scenes.
/// Only one scene is active at a time; the simulation still runs in the
/// Game1 Update loop regardless of which scene is showing.
/// </summary>
public class SceneManager
{
    // ── Active scene ─────────────────────────────────────────────────────────
    private IScene? _current;
    private IScene? _next;
    private object? _nextContext;

    // ── Fade transition ───────────────────────────────────────────────────────
    private float _fadeAlpha;   // 0 = transparent, 1 = opaque black
    private bool  _fadingOut;
    private bool  _fadingIn;
    private const float FADE_SPEED = 3f;   // alpha units per second

    private Texture2D? _pixel;

    public IScene? Current => _current;

    /// <summary>
    /// Switch to the supplied scene immediately (no fade).
    /// Call this for the very first scene only.
    /// </summary>
    public void SetImmediate(IScene scene, object? context = null)
    {
        _current?.Exit();
        _current = scene;
        _current.Enter(context);
        _fadeAlpha = 0f;
        _fadingOut = false;
        _fadingIn  = false;
    }

    /// <summary>
    /// Begin a cross-fade transition to the supplied scene.
    /// The new scene's Enter() is called at the mid-point (full black).
    /// </summary>
    public void TransitionTo(IScene scene, object? context = null)
    {
        if (_fadingOut) return;   // already in-transition; ignore
        _next        = scene;
        _nextContext = context;
        _fadingOut   = true;
    }

    public void Initialize(Texture2D pixel) => _pixel = pixel;

    public void Update(float dt)
    {
        if (_fadingOut)
        {
            _fadeAlpha += FADE_SPEED * dt;
            if (_fadeAlpha >= 1f)
            {
                _fadeAlpha = 1f;
                _fadingOut = false;
                _fadingIn  = true;

                // Swap scene at mid-point (full black)
                _current?.Exit();
                _current = _next;
                _current?.Enter(_nextContext);
                _next        = null;
                _nextContext = null;
            }
        }
        else if (_fadingIn)
        {
            _fadeAlpha -= FADE_SPEED * dt;
            if (_fadeAlpha <= 0f)
            {
                _fadeAlpha = 0f;
                _fadingIn  = false;
            }
        }
    }

    public void DrawWorld(SpriteBatch sb, Texture2D pixel, float gameTime)
        => _current?.DrawWorld(sb, pixel, gameTime);

    public void DrawUI(SpriteBatch sb, Texture2D pixel, float gameTime)
    {
        _current?.DrawUI(sb, pixel, gameTime);

        // Fade overlay — always on top
        if (_pixel != null && _fadeAlpha > 0f)
        {
            sb.Draw(_pixel,
                new Rectangle(0, 0, Config.SCREEN_WIDTH, Config.SCREEN_HEIGHT),
                Color.Black * _fadeAlpha);
        }
    }

    // Route Update to the active scene
    public void UpdateScene(float dt) => _current?.Update(dt);
}
