using System;
using EchoesOfThePast.Art;
using EchoesOfThePast.World;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace EchoesOfThePast.Entities;

/// <summary>
/// An echo of Ben that retraces his path a fixed number of steps behind him. It never reads input
/// or checks walls: every tile it visits was already a legal step for Ben.
/// </summary>
public class Phantom
{
    public const int MinDelay = 1;
    public const int MaxDelay = 10;
    public const int DefaultDelay = 3;

    /// <summary>How many of Ben's steps the echo trails behind (kept between <see cref="MinDelay"/> and <see cref="MaxDelay"/>).</summary>
    public int Delay
    {
        get => _delay;
        set => _delay = Math.Clamp(value, MinDelay, MaxDelay);
    }

    private int _delay;

    /// <summary>Seconds to cross one tile. Matches Ben's so the gap stays constant.</summary>
    public float StepDuration { get; set; } = 0.15f;

    /// <summary>Tile the echo occupies (already the destination while a step is in progress).</summary>
    public Point GridPosition { get; private set; }

    /// <summary>Pixel position used for drawing (top-left of the sprite).</summary>
    public Vector2 WorldPosition { get; private set; }

    public Direction Facing { get; private set; } = Direction.Down;
    public bool IsMoving { get; private set; }

    private readonly MovementHistory _history;
    private Point _stepOrigin;
    private float _stepProgress;

    // Set when Ben completes a step; the echo only moves while this is set.
    private bool _unlocked;

    public Phantom(Point spawnTile, MovementHistory history, int delay = DefaultDelay)
    {
        _history = history;
        Delay = delay;
        GridPosition = spawnTile;
        _stepOrigin = spawnTile;
        WorldPosition = Grid.ToWorld(spawnTile);
    }

    /// <summary>Ben finished a step: the echo may now replay whatever is more than Delay steps old.</summary>
    public void OnBenStepCompleted() => _unlocked = true;

    /// <summary>
    /// Replays the next step right now, at the moment Ben's step lands, so the echo is already on its new tile when
    /// everything else is judged. Any step still sliding is snapped to its end first; <see cref="Update"/> then animates.
    /// </summary>
    public void StepNow(Func<Point, Direction, bool> tryEnter)
    {
        if (!_unlocked)
            return;

        if (IsMoving)
            SnapToTile();

        ReplayNext(tryEnter);
    }

    /// <summary>Put the echo back on a tile with no step in progress. Pair with clearing the history.</summary>
    public void Reset(Point tile)
    {
        GridPosition = tile;
        _stepOrigin = tile;
        WorldPosition = Grid.ToWorld(tile);
        IsMoving = false;
        _stepProgress = 0f;
        _unlocked = false;
    }

    /// <summary>True if the echo is on this tile, or still leaving it mid-step.</summary>
    public bool Occupies(Point tile) => GridPosition == tile || (IsMoving && _stepOrigin == tile);

    /// <param name="tryEnter">
    /// Asked before each replayed step with the target tile and direction; false makes the echo wait
    /// (Ben in the way, a crate that can't be pushed). It may push a crate as a side effect.
    /// </param>
    public void Update(GameTime gameTime, Func<Point, Direction, bool> tryEnter)
    {
        float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;

        if (IsMoving)
        {
            _stepProgress += dt / StepDuration;
            if (_stepProgress < 1f)
            {
                WorldPosition = Vector2.Lerp(Grid.ToWorld(_stepOrigin), Grid.ToWorld(GridPosition), _stepProgress);
                return;
            }

            // Arrived: snap, then fall through so consecutive replayed steps chain without a pause.
            SnapToTile();
        }

        ReplayNext(tryEnter);
    }

    private void SnapToTile()
    {
        IsMoving = false;
        _stepProgress = 0f;
        WorldPosition = Grid.ToWorld(GridPosition);
    }

    /// <summary>Replays the oldest recorded step if Ben is more than Delay steps ahead and the way is clear; otherwise waits for his next step.</summary>
    private void ReplayNext(Func<Point, Direction, bool> tryEnter)
    {
        if (!_unlocked)
            return;

        if (_history.Count <= Delay)
        {
            _unlocked = false;
            return;
        }

        if (_history.TryPeek(out Point next))
        {
            Direction direction = DirectionTo(next);
            if (tryEnter(next, direction))
            {
                _history.TryDequeue(out _);
                BeginStep(next, direction);
            }
        }
    }

    private Direction DirectionTo(Point target)
    {
        Point delta = target - GridPosition;
        return delta switch
        {
            { Y: < 0 } => Direction.Up,
            { Y: > 0 } => Direction.Down,
            { X: < 0 } => Direction.Left,
            _ => Direction.Right,
        };
    }

    private void BeginStep(Point target, Direction direction)
    {
        Facing = direction;

        _stepOrigin = GridPosition;
        GridPosition = target;
        _stepProgress = 0f;
        IsMoving = true;
    }

    public void Draw(SpriteBatch spriteBatch, Texture2D pixel) =>
        SpriteBank.DrawCharacter(
            spriteBatch, CharacterKind.Phantom, Facing,
            new Point((int)MathF.Round(WorldPosition.X), (int)MathF.Round(WorldPosition.Y)), opacity: 0.9f);
}
