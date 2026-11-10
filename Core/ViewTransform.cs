using System;
using Microsoft.Xna.Framework;

namespace EchoesOfThePast.Core;

/// <summary>
/// Maps the fixed-size world onto the window: the largest whole-number scale that fits, centred, so every
/// world pixel stays a crisp block of identical screen pixels (letterboxed if needed).
/// </summary>
public readonly struct ViewTransform
{
    public int Scale { get; }
    public Point Offset { get; }

    public ViewTransform(int scale, Point offset)
    {
        Scale = scale;
        Offset = offset;
    }

    public static ViewTransform Fit(Point backBuffer, Point world)
    {
        int scale = Math.Max(1, Math.Min(backBuffer.X / world.X, backBuffer.Y / world.Y));
        return new ViewTransform(scale, new Point(
            (backBuffer.X - world.X * scale) / 2,
            (backBuffer.Y - world.Y * scale) / 2));
    }

    /// <summary>The SpriteBatch transform that draws world pixels at screen scale.</summary>
    public Matrix Matrix => Matrix.CreateScale(Scale) * Matrix.CreateTranslation(Offset.X, Offset.Y, 0);

    /// <summary>A screen position in world pixels; floor keeps the letterbox bars off the buttons.</summary>
    public Point ToWorld(Point screen) => new(
        (int)MathF.Floor((screen.X - Offset.X) / (float)Scale),
        (int)MathF.Floor((screen.Y - Offset.Y) / (float)Scale));
}
