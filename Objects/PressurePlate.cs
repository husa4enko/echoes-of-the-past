using EchoesOfThePast.Art;
using EchoesOfThePast.Entities;
using EchoesOfThePast.UI;
using EchoesOfThePast.World;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace EchoesOfThePast.Objects;

/// <summary>
/// A floor switch. A round button answers to characters only: it is active while Ben or the Phantom is
/// standing on it (Ben counts from the moment he arrives until his next step lands elsewhere). A square plate answers to crates only: it is active while a crate sits on it.
/// </summary>
public class PressurePlate
{
    public Point Tile { get; }
    public bool Square { get; }

    /// <summary>An "E" switch: once a crate has pushed it down it stays active for good, even if the crate is moved off.</summary>
    public bool Latching { get; }

    public bool IsActive { get; private set; }

    public PressurePlate(Point tile, bool square = false, bool latching = false)
    {
        Tile = tile;
        Square = square;
        Latching = latching;
    }

    public void Update(Player ben, Phantom phantom, CrateField crates)
    {
        bool pressed = Square ? crates.CrateAt(Tile) != null : ben.SettledTile == Tile || phantom.GridPosition == Tile;
        IsActive = pressed || (Latching && IsActive);
    }

    public void Draw(SpriteBatch spriteBatch, Texture2D pixel)
    {
        if (!Latching)
        {
            SpriteBank.Draw(spriteBatch, (Square ? "plate_" : "button_") + (IsActive ? "on" : "off"), Grid.TileBounds(Tile).Location);
            return;
        }

        // The "E" switch: a square plate with an E on it that glows green once a crate has pressed it.
        Rectangle r = Grid.TileBounds(Tile);
        SpriteBank.Draw(spriteBatch, IsActive ? "plate_on" : "plate_off", r.Location);

        var letterAt = new Point(r.X + 5, r.Y + 4);
        if (IsActive)
        {
            Color green = new(90, 235, 130);
            spriteBatch.Draw(pixel, new Rectangle(r.X + 2, r.Y + 2, r.Width - 4, r.Height - 4), green * 0.45f);
            spriteBatch.Draw(pixel, new Rectangle(r.X + 1, r.Y + 1, r.Width - 2, 1), green);
            spriteBatch.Draw(pixel, new Rectangle(r.X + 1, r.Bottom - 2, r.Width - 2, 1), green);
            spriteBatch.Draw(pixel, new Rectangle(r.X + 1, r.Y + 1, 1, r.Height - 2), green);
            spriteBatch.Draw(pixel, new Rectangle(r.Right - 2, r.Y + 1, 1, r.Height - 2), green);
            PixelText.Draw(spriteBatch, pixel, "E", letterAt, 1, new Color(14, 60, 30));
        }
        else
        {
            PixelText.Draw(spriteBatch, pixel, "E", letterAt + new Point(1, 1), 1, new Color(250, 226, 120) * 0.5f);
            PixelText.Draw(spriteBatch, pixel, "E", letterAt, 1, new Color(60, 46, 20));
        }
    }
}
