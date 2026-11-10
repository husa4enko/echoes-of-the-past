using System;
using EchoesOfThePast.Art;
using EchoesOfThePast.World;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace EchoesOfThePast.Objects;

/// <summary>
/// A pushable crate. Like Ben, its logical tile changes instantly and only the drawn position slides.
/// </summary>
public class Crate
{
    /// <summary>Seconds to slide one tile. Matches Ben's step so a push looks like one motion.</summary>
    public const float SlideDuration = 0.15f;

    public Point Tile { get; private set; }

    /// <summary>Where the crate started; a crate anywhere else has been moved, which guards find suspicious.</summary>
    public Point Origin { get; }

    public bool Displaced => Tile != Origin;
    public Vector2 WorldPosition { get; private set; }

    private Vector2 _slideFrom;
    private float _progress = 1f;

    public Crate(Point tile)
    {
        Tile = tile;
        Origin = tile;
        WorldPosition = Grid.ToWorld(tile);
        _slideFrom = WorldPosition;
    }

    public void MoveTo(Point tile)
    {
        _slideFrom = WorldPosition;
        Tile = tile;
        _progress = 0f;
    }

    public void Update(float dt)
    {
        if (_progress >= 1f)
            return;

        _progress = MathF.Min(1f, _progress + dt / SlideDuration);
        WorldPosition = Vector2.Lerp(_slideFrom, Grid.ToWorld(Tile), _progress);
    }

    public void Draw(SpriteBatch spriteBatch, Texture2D pixel) =>
        SpriteBank.Draw(spriteBatch, "crate", new Point((int)MathF.Round(WorldPosition.X), (int)MathF.Round(WorldPosition.Y)));
}
