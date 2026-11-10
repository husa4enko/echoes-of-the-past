using System;
using System.Collections.Generic;
using EchoesOfThePast.World;
using Microsoft.Xna.Framework;

namespace EchoesOfThePast.Entities;

/// <summary>
/// The one place that works out what a sentry can see, shared by Guards and the Kidnapper. A T-shaped cone
/// (1 wide at distance 1, 3 wide at distances 2 and 3) points the way it faces; the Kidnapper also watches
/// the four tiles touching its own. Every tile needs an unobstructed line from the sentry, and walls and
/// crates stop it: a blocker's own tile can be seen, nothing behind it can (a blocker straight ahead shades the whole cone behind it).
/// </summary>
public static class Vision
{
    /// <summary>How many tiles ahead a sentry can see.</summary>
    public const int Range = 3;

    public static List<Point> Cone(TileMap map, Point origin, Direction facing, bool surroundings, Func<Point, bool> blocksSight)
    {
        var seen = new List<Point>();
        Point forward = facing.ToOffset();
        var side = new Point(-forward.Y, forward.X);

        for (int distance = 1; distance <= Range; distance++)
        {
            // The ray straight ahead stops at the first wall or crate; nothing further out is lit, on or off the axis.
            Point ahead = origin + new Point(forward.X * distance, forward.Y * distance);
            if (distance > 1)
            {
                Point before = ahead - forward;
                if (!map.IsWalkable(before) || blocksSight(before))
                    break;
            }

            int reach = distance / 2;
            for (int offset = -reach; offset <= reach; offset++)
                TryAdd(seen, map, origin, origin + new Point(forward.X * distance + side.X * offset, forward.Y * distance + side.Y * offset), blocksSight);
        }

        if (surroundings)
        {
            // Touching distance only: the four tiles that share an edge with the sentry's own.
            foreach (Point step in new[] { new Point(0, -1), new Point(0, 1), new Point(-1, 0), new Point(1, 0) })
            {
                Point tile = origin + step;
                if (!seen.Contains(tile))
                    TryAdd(seen, map, origin, tile, blocksSight);
            }
        }

        return seen;
    }

    private static void TryAdd(List<Point> seen, TileMap map, Point origin, Point tile, Func<Point, bool> blocksSight)
    {
        // Hard clamp: nothing farther than Range tiles away (or off the map) is ever watched, whatever the rays say.
        if (Math.Abs(tile.X - origin.X) > Range || Math.Abs(tile.Y - origin.Y) > Range)
            return;

        if (map.IsWalkable(tile) && HasLineOfSight(origin, tile, blocksSight))
            seen.Add(tile);
    }

    /// <summary>
    /// Walks every tile the straight line between the two tile centres touches (passing exactly through a
    /// corner touches both tiles beside it). Any blocker strictly between them hides the target.
    /// </summary>
    private static bool HasLineOfSight(Point from, Point to, Func<Point, bool> blocksSight)
    {
        int dx = Math.Abs(to.X - from.X), dy = Math.Abs(to.Y - from.Y);
        int stepX = to.X > from.X ? 1 : -1, stepY = to.Y > from.Y ? 1 : -1;
        int x = from.X, y = from.Y;
        int error = dx - dy;
        dx *= 2;
        dy *= 2;

        for (int n = 1 + (dx + dy) / 2; n > 0; n--)
        {
            var tile = new Point(x, y);
            if (tile != from && tile != to && blocksSight(tile))
                return false;

            if (error > 0)
            {
                x += stepX;
                error -= dy;
            }
            else if (error < 0)
            {
                y += stepY;
                error += dx;
            }
            else
            {
                // Exactly through a corner: both tiles beside it count.
                if (blocksSight(new Point(x + stepX, y)) && new Point(x + stepX, y) != to)
                    return false;
                if (blocksSight(new Point(x, y + stepY)) && new Point(x, y + stepY) != to)
                    return false;
                x += stepX;
                y += stepY;
                error += dx - dy;
                n--;
            }
        }

        return true;
    }
}
