using System;
using System.Collections.Generic;
using EchoesOfThePast.Entities;
using EchoesOfThePast.World;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace EchoesOfThePast.Objects;

/// <summary>
/// All crates in a level and the push rule shared by Ben and the Phantom. Crates are indestructible: they can only be pushed.
/// </summary>
public class CrateField
{
    private readonly List<Crate> _crates = new();

    public IReadOnlyList<Crate> Crates => _crates;

    public void Add(Point tile) => _crates.Add(new Crate(tile));

    public Crate CrateAt(Point tile)
    {
        foreach (Crate crate in _crates)
        {
            if (crate.Tile == tile)
                return crate;
        }

        return null;
    }

    /// <summary>
    /// Asked when something wants to walk onto <paramref name="tile"/> heading <paramref name="direction"/>.
    /// With no crate there it returns true. With a crate it pushes it one tile onward and returns
    /// true, or leaves it alone and returns false if the tile behind it is a wall (a closed door
    /// counts as one), another crate, or blocked according to <paramref name="isBlocked"/>.
    /// </summary>
    public bool TryEnter(TileMap map, Point tile, Direction direction, Func<Point, bool> isBlocked)
    {
        Crate crate = CrateAt(tile);
        if (crate == null)
            return true;

        Point behind = tile + direction.ToOffset();
        if (!map.IsWalkable(behind) || CrateAt(behind) != null || isBlocked(behind))
            return false;

        crate.MoveTo(behind);
        return true;
    }

    public void Update(float dt)
    {
        foreach (Crate crate in _crates)
            crate.Update(dt);
    }

    public void Draw(SpriteBatch spriteBatch, Texture2D pixel)
    {
        foreach (Crate crate in _crates)
            crate.Draw(spriteBatch, pixel);
    }
}
