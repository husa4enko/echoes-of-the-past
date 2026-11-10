using System;
using Microsoft.Xna.Framework;

namespace EchoesOfThePast.World;

/// <summary>
/// Grid constants and conversions between tile coordinates and world (pixel) space.
/// </summary>
public static class Grid
{
    public const int TileSize = 16;

    /// <summary>Top-left pixel of the given tile.</summary>
    public static Vector2 ToWorld(Point tile) => new(tile.X * TileSize, tile.Y * TileSize);

    /// <summary>Tile containing the given pixel position.</summary>
    public static Point ToTile(Vector2 world) =>
        new((int)MathF.Floor(world.X / TileSize), (int)MathF.Floor(world.Y / TileSize));

    public static Rectangle TileBounds(Point tile) =>
        new(tile.X * TileSize, tile.Y * TileSize, TileSize, TileSize);
}
