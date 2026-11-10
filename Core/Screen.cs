using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace EchoesOfThePast.Core;

/// <summary>One full-window screen (a menu, the game). The <see cref="ScreenManager"/> runs exactly one at a time.</summary>
public abstract class Screen
{
    protected Screen(GameContext context)
    {
        Context = context;
    }

    protected GameContext Context { get; }

    /// <summary>True while the mouse should be hidden and confined to the window (steering Ben).</summary>
    public virtual bool CapturesMouse => false;

    /// <summary>Called each time the screen becomes the current one.</summary>
    public virtual void OnShown()
    {
    }

    public abstract void Update(GameTime gameTime, InputState input);

    /// <summary>Draws in world pixels; the game has already applied the view transform.</summary>
    public abstract void Draw(SpriteBatch spriteBatch, Texture2D pixel);
}
