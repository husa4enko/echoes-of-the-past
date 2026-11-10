using System;
using System.Collections.Generic;
using EchoesOfThePast.Entities;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace EchoesOfThePast.Art;

public enum CharacterKind
{
    Ben,
    Phantom,
    Guard,
    Kidnapper,
}

/// <summary>
/// Every sprite the game draws, as a 16x16 texture looked up by name. The placeholder pixel art in
/// <see cref="SpriteData"/> is baked into textures at start-up; to use final art, make
/// <see cref="Load"/> read PNGs into the same names (the rest of the game only asks for names).
/// </summary>
public static class SpriteBank
{
    public const int Size = 16;

    private static readonly Dictionary<string, Texture2D> Textures = new();

    public static void Load(GraphicsDevice device)
    {
        foreach ((string name, (string[] rows, Dictionary<char, Color> palette)) in SpriteData.Sprites)
        {
            var colors = new Color[Size * Size];
            for (int y = 0; y < Size; y++)
            {
                for (int x = 0; x < Size; x++)
                {
                    char c = rows[y][x];
                    colors[y * Size + x] = c == '.' ? Color.Transparent : palette[c];
                }
            }

            Textures[name] = Bake(device, colors);
        }

        foreach ((string name, Color[] colors) in SpriteData.Tiles)
            Textures[name] = Bake(device, colors);
    }

    private static Texture2D Bake(GraphicsDevice device, Color[] colors)
    {
        var texture = new Texture2D(device, Size, Size);
        texture.SetData(colors);
        return texture;
    }

    public static Texture2D Get(string name) => Textures[name];

    /// <summary>Draws a sprite with its top-left at <paramref name="origin"/>, <paramref name="scale"/> times larger.</summary>
    public static void Draw(
        SpriteBatch spriteBatch, string name, Point origin, int scale = 1, float opacity = 1f,
        SpriteEffects effects = SpriteEffects.None)
    {
        spriteBatch.Draw(
            Get(name), new Rectangle(origin.X, origin.Y, Size * scale, Size * scale), null,
            Color.White * opacity, 0f, Vector2.Zero, effects, 0f);
    }

    /// <summary>
    /// Draws one of the four characters facing <paramref name="facing"/>. Side-view art faces right
    /// and is mirrored for left.
    /// </summary>
    public static void DrawCharacter(
        SpriteBatch spriteBatch, CharacterKind kind, Direction facing, Point origin, int scale = 1, float opacity = 1f)
    {
        string prefix = kind.ToString().ToLowerInvariant();
        (string suffix, SpriteEffects effects) = facing switch
        {
            Direction.Up => ("up", SpriteEffects.None),
            Direction.Down => ("down", SpriteEffects.None),
            Direction.Left => ("side", SpriteEffects.FlipHorizontally),
            _ => ("side", SpriteEffects.None),
        };

        Draw(spriteBatch, $"{prefix}_{suffix}", origin, scale, opacity, effects);
    }
}
