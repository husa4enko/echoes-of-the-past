using System.Collections.Generic;
using EchoesOfThePast.Art;
using EchoesOfThePast.World;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace EchoesOfThePast.Objects;

/// <summary>
/// A door that is a wall while it's shut and floor while it's open. It is open exactly while its
/// plate is active, and it writes that straight into the <see cref="TileMap"/> so every walkability
/// check treats a closed door like any other wall.
/// </summary>
public class Door
{
    public Point Tile { get; }
    public bool IsOpen { get; private set; }

    /// <summary>Seconds of green glow left after the door swung open.</summary>
    private float _pulse;
    private const float PulseSeconds = 0.6f;

    public Door(Point tile)
    {
        Tile = tile;
    }

    /// <summary>The door is open only while every plate is pressed.</summary>
    public void Update(IEnumerable<PressurePlate> plates, TileMap map)
    {
        bool wasOpen = IsOpen;
        IsOpen = true;
        bool any = false;
        foreach (PressurePlate plate in plates)
        {
            any = true;
            IsOpen &= plate.IsActive;
        }

        IsOpen &= any;
        if (IsOpen && !wasOpen)
            _pulse = PulseSeconds;
        map.SetTile(Tile, IsOpen ? TileType.Floor : TileType.Wall);
    }

    /// <summary>Advances the opening glow.</summary>
    public void Tick(float dt) => _pulse = System.Math.Max(0f, _pulse - dt);

    public void Draw(SpriteBatch spriteBatch, Texture2D pixel)
    {
        Rectangle r = Grid.TileBounds(Tile);
        SpriteBank.Draw(spriteBatch, IsOpen ? "door_open" : "door_closed", r.Location);

        if (_pulse > 0f)
        {
            // A green flash that grows outward from the doorway and fades.
            float t = 1f - _pulse / PulseSeconds;
            int grow = (int)(t * 6);
            var glow = new Rectangle(r.X - grow, r.Y - grow, r.Width + grow * 2, r.Height + grow * 2);
            Color green = new Color(90, 235, 130) * (0.7f * (1f - t));
            spriteBatch.Draw(pixel, new Rectangle(glow.X, glow.Y, glow.Width, 2), green);
            spriteBatch.Draw(pixel, new Rectangle(glow.X, glow.Bottom - 2, glow.Width, 2), green);
            spriteBatch.Draw(pixel, new Rectangle(glow.X, glow.Y, 2, glow.Height), green);
            spriteBatch.Draw(pixel, new Rectangle(glow.Right - 2, glow.Y, 2, glow.Height), green);
            spriteBatch.Draw(pixel, r, new Color(90, 235, 130) * (0.3f * (1f - t)));
        }
    }
}
