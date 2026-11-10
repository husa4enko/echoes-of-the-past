using System.Collections.Generic;
using Microsoft.Xna.Framework;

namespace EchoesOfThePast.Entities;

/// <summary>
/// FIFO record of the tiles Ben has stepped onto, oldest first. A <see cref="Phantom"/> drains it
/// from the front, so the queue only ever holds the steps the echo hasn't replayed yet.
/// </summary>
public class MovementHistory
{
    private readonly Queue<Point> _steps = new();

    /// <summary>Steps recorded but not yet replayed.</summary>
    public int Count => _steps.Count;

    /// <summary>Record the destination tile of a successful step.</summary>
    public void Record(Point tile) => _steps.Enqueue(tile);

    public bool TryPeek(out Point tile) => _steps.TryPeek(out tile);

    public bool TryDequeue(out Point tile) => _steps.TryDequeue(out tile);

    public void Clear() => _steps.Clear();
}
