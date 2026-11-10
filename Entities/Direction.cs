using Microsoft.Xna.Framework;

namespace EchoesOfThePast.Entities;

public enum Direction
{
    Up,
    Down,
    Left,
    Right,
}

public static class DirectionExtensions
{
    public static Direction Opposite(this Direction direction) => direction switch
    {
        Direction.Up => Direction.Down,
        Direction.Down => Direction.Up,
        Direction.Left => Direction.Right,
        _ => Direction.Left,
    };

    /// <summary>One-tile offset in grid space (Y grows downward).</summary>
    public static Point ToOffset(this Direction direction) => direction switch
    {
        Direction.Up => new Point(0, -1),
        Direction.Down => new Point(0, 1),
        Direction.Left => new Point(-1, 0),
        Direction.Right => new Point(1, 0),
        _ => Point.Zero,
    };
}
