using System;
using EchoesOfThePast.Art;
using EchoesOfThePast.World;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace EchoesOfThePast.Entities;

/// <summary>
/// Ben. Moves one tile at a time with W/A/S/D. His logical position is always a whole tile;
/// the drawn position slides between tiles and lands exactly on the destination.
/// </summary>
public class Player
{
    /// <summary>Seconds to cross one tile.</summary>
    public float StepDuration { get; set; } = 0.15f;

    /// <summary>
    /// The tile Ben occupies. While a step is in progress this is already the destination tile,
    /// so nothing else can claim it.
    /// </summary>
    public Point GridPosition { get; private set; }

    /// <summary>
    /// The tile Ben last arrived on. It only changes when a step completes, so guards, buttons and the
    /// CAUGHT check all see Ben on the same tile for a whole step, at the moment the sentries have moved.
    /// </summary>
    public Point SettledTile { get; private set; }

    /// <summary>Pixel position used for drawing (top-left of the sprite).</summary>
    public Vector2 WorldPosition { get; private set; }

    public Direction Facing { get; private set; } = Direction.Down;
    public bool IsMoving { get; private set; }

    /// <summary>Raised with the tile Ben has just arrived on, once per completed step (walls don't count).</summary>
    public event Action<Point> Stepped;

    private Point _stepOrigin;
    private float _stepProgress;

    public Player(Point spawnTile)
    {
        Teleport(spawnTile);
    }

    /// <summary>True if Ben is on this tile, or still leaving it mid-step.</summary>
    public bool Occupies(Point tile) => GridPosition == tile || (IsMoving && _stepOrigin == tile);

    /// <summary>Instantly place Ben on a tile, cancelling any step in progress.</summary>
    public void Teleport(Point tile)
    {
        GridPosition = tile;
        SettledTile = tile;
        _stepOrigin = tile;
        WorldPosition = Grid.ToWorld(tile);
        IsMoving = false;
        _stepProgress = 0f;
    }

    /// <param name="tryEnter">
    /// Asked after the wall check with the target tile and direction; false blocks the step. It may
    /// have side effects (pushing a crate), so it is only called for a step that is otherwise legal. May be null.
    /// </param>
    public void Update(GameTime gameTime, KeyboardState keyboard, TileMap map, Func<Point, Direction, bool> tryEnter = null)
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

            // Arrived: snap exactly onto the tile, then fall through so a held key continues
            // walking this frame without a pause.
            IsMoving = false;
            _stepProgress = 0f;
            WorldPosition = Grid.ToWorld(GridPosition);
            SettledTile = GridPosition;
            Stepped?.Invoke(GridPosition);
        }

        Direction? input = ReadDirection(keyboard);
        if (input is Direction direction)
            TryStep(direction, map, tryEnter);
    }

    /// <summary>One direction at a time, no diagonals. WASD or the arrow keys; priority: up, down, left, right.</summary>
    private static Direction? ReadDirection(KeyboardState keyboard)
    {
        if (keyboard.IsKeyDown(Keys.W) || keyboard.IsKeyDown(Keys.Up)) return Direction.Up;
        if (keyboard.IsKeyDown(Keys.S) || keyboard.IsKeyDown(Keys.Down)) return Direction.Down;
        if (keyboard.IsKeyDown(Keys.A) || keyboard.IsKeyDown(Keys.Left)) return Direction.Left;
        if (keyboard.IsKeyDown(Keys.D) || keyboard.IsKeyDown(Keys.Right)) return Direction.Right;
        return null;
    }

    private bool TryStep(Direction direction, TileMap map, Func<Point, Direction, bool> tryEnter, bool ignoreStuck = false)
    {
        // Turning toward a wall still updates facing, so Ben can "look" at things he bumps into.
        Facing = direction;

        // Ben is inside a door that has shut (its tile is now a wall): he can't move until it reopens.
        if (!ignoreStuck && !map.IsWalkable(GridPosition))
            return false;

        Point target = GridPosition + direction.ToOffset();
        if (!map.IsWalkable(target) || !(tryEnter?.Invoke(target, direction) ?? true))
            return false;

        _stepOrigin = GridPosition;
        GridPosition = target;
        _stepProgress = 0f;
        IsMoving = true;
        return true;
    }

    /// <summary>
    /// A step Ben didn't ask for, used to move him out of a door that shut on him. Unlike a normal
    /// step it may start from a wall tile. Returns false (and leaves him facing as before) if blocked.
    /// </summary>
    public bool TryEject(Direction direction, TileMap map, Func<Point, Direction, bool> tryEnter)
    {
        if (IsMoving)
            return false;

        Direction previousFacing = Facing;
        if (TryStep(direction, map, tryEnter, ignoreStuck: true))
            return true;

        Facing = previousFacing;
        return false;
    }

    public void Draw(SpriteBatch spriteBatch, Texture2D pixel) =>
        SpriteBank.DrawCharacter(
            spriteBatch, CharacterKind.Ben, Facing,
            new Point((int)MathF.Round(WorldPosition.X), (int)MathF.Round(WorldPosition.Y)));
}
