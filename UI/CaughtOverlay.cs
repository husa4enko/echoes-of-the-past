using System;
using EchoesOfThePast.World;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace EchoesOfThePast.UI;

/// <summary>
/// The Game Over sequence: a red flash that builds up, a ring on the tile where Ben was caught and CAUGHT!,
/// then the Game Over menu once the flash is done.
/// </summary>
public sealed class CaughtOverlay
{
    /// <summary>Seconds of red flash before the Game Over menu appears.</summary>
    private const float FlashSeconds = 0.7f;

    private readonly Rectangle _screen;
    private readonly OverlayMenu _menu;
    private float _timer;
    private Point _at;

    public CaughtOverlay(Point worldSize)
    {
        _screen = new Rectangle(0, 0, worldSize.X, worldSize.Y);
        _menu = new OverlayMenu(worldSize.X, worldSize.Y, "CAUGHT!", new Color(214, 96, 90), true, 132,
            "RESTART LEVEL [R]", "LEVEL SELECT [ESC]");
    }

    public void Begin(Point caughtAt)
    {
        _timer = FlashSeconds;
        _at = caughtAt;
    }

    /// <summary>The menu button picked this frame, or -1. Buttons only respond once the flash is over.</summary>
    public int Update(float dt, Point cursor, bool clicked)
    {
        _timer -= dt;
        return _timer <= 0f ? _menu.Update(cursor, clicked) : -1;
    }

    public void Draw(SpriteBatch spriteBatch, Texture2D pixel)
    {
        float progress = 1f - Math.Clamp(_timer / FlashSeconds, 0f, 1f);
        spriteBatch.Draw(pixel, _screen, new Color(170, 20, 20) * (0.55f * MathF.Min(1f, progress * 3f)));

        Rectangle r = Grid.TileBounds(_at);
        Color ring = new Color(255, 230, 120);
        spriteBatch.Draw(pixel, new Rectangle(r.X - 1, r.Y - 1, r.Width + 2, 1), ring);
        spriteBatch.Draw(pixel, new Rectangle(r.X - 1, r.Bottom, r.Width + 2, 1), ring);
        spriteBatch.Draw(pixel, new Rectangle(r.X - 1, r.Y, 1, r.Height), ring);
        spriteBatch.Draw(pixel, new Rectangle(r.Right, r.Y, 1, r.Height), ring);

        if (_timer <= 0f)
        {
            _menu.Draw(spriteBatch, pixel);
            return;
        }

        const string message = "CAUGHT!";
        const int scale = 2;
        Point size = PixelText.Measure(message, scale);
        var origin = new Point((_screen.Width - size.X) / 2, (_screen.Height - size.Y) / 2);
        PixelText.Draw(spriteBatch, pixel, message, origin + new Point(scale, scale), scale, Color.Black);
        PixelText.Draw(spriteBatch, pixel, message, origin, scale, new Color(255, 120, 110));
    }
}
