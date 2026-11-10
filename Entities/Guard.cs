using System;
using System.Collections.Generic;
using EchoesOfThePast.Art;
using EchoesOfThePast.UI;
using EchoesOfThePast.World;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace EchoesOfThePast.Entities;

/// <summary>
/// A sentry with a cone of vision straight ahead of it (a Guard, or the Kidnapper). Walls and crates
/// block its line of sight. It may turn a quarter or half turn every few of Ben's steps, or patrol back
/// and forth along a line, one tile every few of Ben's steps. Ben standing in its sight gets him caught; the
/// Phantom standing in it only distracts it (the guard stares at the Phantom and stops turning).
/// </summary>
public class Guard
{
    public CharacterKind Kind { get; }
    public Point Tile { get; private set; }
    public Direction Facing { get; private set; }

    /// <summary>Ben's steps between turns; 0 means the guard never turns by itself.</summary>
    public int TurnEvery { get; }

    /// <summary>Clockwise quarter turns per turn: 1 = 90 degrees, 2 = 180, 3 = 90 counter-clockwise.</summary>
    public int Quarters { get; }

    /// <summary>True while the guard is staring at the Phantom.</summary>
    public bool Distracted { get; private set; }

    /// <summary>Ben's steps a sentry stands staring at a crate that was moved before it carries on.</summary>
    public const int SuspicionSteps = 3;

    /// <summary>True while the sentry is staring at a crate that was moved (shown as an orange "!").</summary>
    public bool Suspicious => _suspicion > 0;

    private int _suspicion;
    private readonly HashSet<Point> _investigated = new();

    private int _stepsSinceTurn;

    private readonly Point[] _route;
    private readonly bool _loop;
    private readonly int _moveEvery;
    private int _routeIndex;
    private int _routeDirection = 1;
    private int _stepsSinceMove;
    private int _blockedSteps;

    /// <summary>Ben's steps a patrolling sentry waits at a blocked tile before it turns round and walks its route the other way.</summary>
    public const int BlockedPatience = 2;

    public Guard(Point tile, Direction facing, int turnEvery = 0, int quarters = 1)
    {
        Kind = CharacterKind.Guard;
        Tile = tile;
        Facing = facing;
        TurnEvery = turnEvery;
        Quarters = quarters;
    }

    /// <summary>A patrolling sentry: walks the route (round it, or to its end and back), one tile every <paramref name="moveEvery"/> of Ben's steps, looking where it walks.</summary>
    public Guard(CharacterKind kind, Point[] route, int moveEvery, bool loop = false)
    {
        _loop = loop;
        Kind = kind;
        _route = route;
        _moveEvery = moveEvery;
        Tile = route[0];
        Facing = route.Length > 1 ? DirectionBetween(route[0], route[1]) : Direction.Down;
    }

    private static Direction DirectionBetween(Point from, Point to)
    {
        Point delta = to - from;
        return Math.Abs(delta.X) >= Math.Abs(delta.Y)
            ? (delta.X < 0 ? Direction.Left : Direction.Right)
            : (delta.Y < 0 ? Direction.Up : Direction.Down);
    }

    /// <summary>Steps Ben still has to take before the next turn (0 for a guard that never turns).</summary>
    public int StepsUntilTurn => TurnEvery > 0 ? TurnEvery - _stepsSinceTurn : 0;

    /// <summary>
    /// Ben took a step: count it, and turn or walk on when it's time (not while distracted).
    /// <paramref name="canEnter"/> says whether a patrolling sentry may step onto a tile; if not it waits.
    /// </summary>
    public void OnBenStep(Func<Point, bool> canEnter)
    {
        if (Distracted)
            return;

        if (_suspicion > 0)
        {
            _suspicion--;
            return;
        }

        if (TurnEvery > 0)
        {
            _stepsSinceTurn++;
            if (_stepsSinceTurn >= TurnEvery)
            {
                _stepsSinceTurn = 0;
                Facing = Turned(Facing, Quarters);
            }
        }

        if (_route == null || _route.Length < 2)
            return;

        _stepsSinceMove++;
        if (_stepsSinceMove < _moveEvery)
            return;

        int next = NextIndex();

        // Turn toward the way it is going even when blocked (it stares at the crate in its path).
        Facing = DirectionBetween(Tile, _route[next]);
        if (!canEnter(_route[next]))
        {
            // Blocked: wait a moment, then walk the route the other way so it never freezes for good.
            if (++_blockedSteps >= BlockedPatience)
            {
                _blockedSteps = 0;
                _routeDirection = -_routeDirection;
            }

            return;
        }

        Tile = _route[next];
        _routeIndex = next;
        _stepsSinceMove = 0;
        _blockedSteps = 0;
    }

    /// <summary>The route index it walks to next: round a loop, or to the end of the route and back.</summary>
    private int NextIndex()
    {
        int next = _routeIndex + _routeDirection;
        if (_loop)
            return (next + _route.Length) % _route.Length;

        if (next < 0 || next >= _route.Length)
        {
            _routeDirection = -_routeDirection;
            next = _routeIndex + _routeDirection;
        }

        return next;
    }

    /// <summary>The guard stares at the Phantom: it turns toward it and holds that turn.</summary>
    public void Distract(Point target)
    {
        Distracted = true;
        Point delta = target - Tile;
        Facing = Math.Abs(delta.X) >= Math.Abs(delta.Y)
            ? (delta.X < 0 ? Direction.Left : Direction.Right)
            : (delta.Y < 0 ? Direction.Up : Direction.Down);
    }

    public void Calm() => Distracted = false;

    /// <summary>
    /// The sentry sees a crate that has been moved: it turns to it and stares for a few of Ben's steps. It
    /// has then looked at that crate where it lies; it only grows suspicious again if the crate is moved on.
    /// Returns false (and does nothing) if it has already investigated the crate at that tile.
    /// </summary>
    public bool Suspect(Point crateTile)
    {
        if (!_investigated.Add(crateTile))
            return false;

        _suspicion = SuspicionSteps;
        Facing = DirectionBetween(Tile, crateTile);
        return true;
    }

    private static Direction Turned(Direction facing, int quarters)
    {
        // Clockwise order on screen: up, right, down, left.
        Direction[] clockwise = { Direction.Up, Direction.Right, Direction.Down, Direction.Left };
        int at = Array.IndexOf(clockwise, facing);
        return clockwise[((at + quarters) % 4 + 4) % 4];
    }

    /// <summary>Every tile the sentry watches right now (see <see cref="Vision"/>); the Kidnapper also watches the four tiles touching him.</summary>
    public List<Point> WatchedTiles(TileMap map, Func<Point, bool> blocksSight) =>
        Vision.Cone(map, Tile, Facing, Kind == CharacterKind.Kidnapper, blocksSight);

    public void Draw(SpriteBatch spriteBatch, Texture2D pixel, TileMap map, Func<Point, bool> blocksSight)
    {
        Color tint = Kind == CharacterKind.Kidnapper ? new Color(170, 70, 220) : new Color(220, 50, 50);
        DrawVision(spriteBatch, pixel, WatchedTiles(map, blocksSight), tint);
        SpriteBank.DrawCharacter(spriteBatch, Kind, Facing, Grid.TileBounds(Tile).Location);
        DrawFacingMarker(spriteBatch, pixel, tint);
        DrawStatus(spriteBatch, pixel);
    }

    /// <summary>A bright bar on the edge of the guard's tile that it faces, so the cone visibly starts from its front.</summary>
    private void DrawFacingMarker(SpriteBatch spriteBatch, Texture2D pixel, Color tint)
    {
        Rectangle r = Grid.TileBounds(Tile);
        Color c = Color.Lerp(tint, Color.White, 0.55f);
        Rectangle bar = Facing switch
        {
            Direction.Up => new Rectangle(r.X + 4, r.Y, r.Width - 8, 2),
            Direction.Down => new Rectangle(r.X + 4, r.Bottom - 2, r.Width - 8, 2),
            Direction.Left => new Rectangle(r.X, r.Y + 4, 2, r.Height - 8),
            _ => new Rectangle(r.Right - 2, r.Y + 4, 2, r.Height - 8),
        };
        spriteBatch.Draw(pixel, bar, c);
    }

    /// <summary>A coloured wash over every watched tile, with a brighter edge wherever the zone ends.</summary>
    private static void DrawVision(SpriteBatch spriteBatch, Texture2D pixel, List<Point> watched, Color tint)
    {
        var set = new HashSet<Point>(watched);
        Color fill = tint * 0.26f;
        Color edge = Color.Lerp(tint, Color.White, 0.3f) * 0.7f;

        foreach (Point tile in watched)
        {
            Rectangle r = Grid.TileBounds(tile);
            spriteBatch.Draw(pixel, r, fill);

            if (!set.Contains(tile + new Point(0, -1)))
                spriteBatch.Draw(pixel, new Rectangle(r.X, r.Y, r.Width, 1), edge);
            if (!set.Contains(tile + new Point(0, 1)))
                spriteBatch.Draw(pixel, new Rectangle(r.X, r.Bottom - 1, r.Width, 1), edge);
            if (!set.Contains(tile + new Point(-1, 0)))
                spriteBatch.Draw(pixel, new Rectangle(r.X, r.Y, 1, r.Height), edge);
            if (!set.Contains(tile + new Point(1, 0)))
                spriteBatch.Draw(pixel, new Rectangle(r.Right - 1, r.Y, 1, r.Height), edge);
        }
    }

    /// <summary>An "!" while it stares at the Phantom, otherwise one pip per step left before it turns.</summary>
    private void DrawStatus(SpriteBatch spriteBatch, Texture2D pixel)
    {
        Rectangle r = Grid.TileBounds(Tile);

        if (Distracted)
        {
            PixelText.Draw(spriteBatch, pixel, "!", new Point(r.X + 6, r.Y - 8), 1, new Color(255, 230, 120));
            return;
        }

        if (Suspicious)
        {
            PixelText.Draw(spriteBatch, pixel, "!", new Point(r.X + 6, r.Y - 8), 1, new Color(255, 140, 40));
            return;
        }

        for (int i = 0; i < StepsUntilTurn; i++)
            spriteBatch.Draw(pixel, new Rectangle(r.X + 2 + i * 3, r.Y - 2, 2, 2), new Color(240, 220, 140));
    }
}
