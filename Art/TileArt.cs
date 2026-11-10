using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;

namespace EchoesOfThePast.Art;

/// <summary>How a theme's floor is patterned (every pattern keeps the dark grid line on the tile's top and left).</summary>
internal enum FloorStyle { Flagstone, Checker, Planks, Slab, Diamond }

/// <summary>How a theme's walls are built.</summary>
internal enum WallStyle { Brick, Blocks, Boards, Ornate }

/// <summary>The colours and patterns of one level theme.</summary>
internal sealed record TileTheme(
    string Name,
    FloorStyle Floor, Color FloorBase, Color FloorGrid, Color FloorLight, Color FloorDark, Color FloorCrack,
    WallStyle Wall, Color Mortar, Color[] Brick, Color BrickLit, Color BrickShade, Color WallBase,
    Color Accent);

/// <summary>
/// Procedurally drawn 16x16 floor and wall tiles, one set per theme (each level picks a theme). Each variant is seeded,
/// so the art is identical on every run. Floors keep a dark top/left grid line (so the grid stays readable) and stay
/// mid-toned, so crates, buttons, characters and the red / purple vision tint all stand out on every theme.
/// </summary>
internal static class TileArt
{
    public const int FloorVariants = 8;
    public const int WallVariants = 4;

    private const int S = SpriteBank.Size;

    /// <summary>Theme 0 entrance, 1 twin hall, 2 guard corridor, 3 storeroom, 4 boss arena.</summary>
    public static readonly TileTheme[] Themes =
    {
        new("Entrance",
            FloorStyle.Flagstone, new(96, 88, 82), new(66, 60, 58), new(110, 102, 95), new(84, 77, 72), new(60, 54, 52),
            WallStyle.Brick, new(32, 27, 44), new Color[] { new(88, 79, 110), new(80, 72, 102), new(72, 64, 94) },
            new(108, 98, 132), new(58, 51, 78), new(22, 18, 30), new(214, 184, 96)),

        new("Twin hall",
            FloorStyle.Checker, new(78, 88, 104), new(50, 58, 74), new(92, 104, 122), new(66, 76, 92), new(44, 50, 64),
            WallStyle.Blocks, new(22, 26, 40), new Color[] { new(72, 88, 114), new(64, 80, 106), new(58, 72, 98) },
            new(100, 122, 154), new(40, 52, 78), new(16, 20, 32), new(120, 190, 220)),

        new("Guard corridor",
            FloorStyle.Planks, new(86, 72, 68), new(56, 44, 42), new(102, 86, 80), new(72, 60, 56), new(48, 38, 36),
            WallStyle.Brick, new(36, 20, 22), new Color[] { new(116, 62, 56), new(106, 56, 52), new(96, 50, 48) },
            new(146, 88, 78), new(70, 34, 36), new(30, 16, 18), new(220, 150, 80)),

        new("Storeroom",
            FloorStyle.Slab, new(72, 66, 62), new(46, 42, 40), new(86, 80, 75), new(60, 55, 52), new(40, 36, 34),
            WallStyle.Boards, new(30, 22, 14), new Color[] { new(104, 74, 46), new(96, 68, 42), new(88, 62, 38) },
            new(132, 98, 62), new(62, 42, 26), new(24, 17, 11), new(190, 150, 90)),

        new("Boss arena",
            FloorStyle.Diamond, new(60, 52, 76), new(38, 32, 52), new(76, 66, 98), new(50, 44, 66), new(36, 30, 48),
            WallStyle.Ornate, new(20, 14, 32), new Color[] { new(66, 52, 96), new(60, 46, 90), new(54, 42, 82) },
            new(98, 78, 138), new(40, 30, 64), new(14, 10, 24), new(214, 170, 86)),
    };

    public static Dictionary<string, Color[]> Build()
    {
        var tiles = new Dictionary<string, Color[]>();
        for (int t = 0; t < Themes.Length; t++)
        {
            for (int v = 0; v < FloorVariants; v++)
                tiles[$"t{t}_floor{v}"] = Floor(Themes[t], v);
            for (int v = 0; v < WallVariants; v++)
            {
                tiles[$"t{t}_wall{v}"] = Wall(Themes[t], v, lip: false);
                tiles[$"t{t}_wall_lip{v}"] = Wall(Themes[t], v, lip: true);
            }
        }

        return tiles;
    }

    private static Color[] Floor(TileTheme th, int variant)
    {
        var px = new Color[S * S];
        var rng = new Random(1000 + variant);

        // Checker floors alternate a lighter and a darker tile: variants 0-3 are the light ones, 4-7 the dark ones.
        Color baseColor = th.Floor == FloorStyle.Checker && variant >= 4 ? Shade(th.FloorBase, 0.90f) : th.FloorBase;
        Array.Fill(px, baseColor);

        // Stone shading: lit edge just inside the grid line, shaded bottom/right edge.
        for (int i = 1; i < S; i++)
        {
            px[1 * S + i] = th.FloorLight;
            px[i * S + 1] = th.FloorLight;
            px[(S - 1) * S + i] = th.FloorDark;
            px[i * S + (S - 1)] = th.FloorDark;
        }

        switch (th.Floor)
        {
            case FloorStyle.Planks:
                // Long horizontal boards: a dark seam every five rows, with staggered end joints.
                for (int y = 5; y < S; y += 5)
                    for (int x = 2; x < S - 1; x++)
                        px[y * S + x] = th.FloorDark;
                for (int row = 0; row < 3; row++)
                    Put(px, 3 + (row * 5 + variant * 3) % 10, 2 + row * 5, th.FloorGrid);
                break;

            case FloorStyle.Slab:
                // Big square slabs with a recessed inner panel.
                for (int i = 4; i < S - 3; i++)
                {
                    px[4 * S + i] = th.FloorDark;
                    px[(S - 4) * S + i] = th.FloorLight;
                    px[i * S + 4] = th.FloorDark;
                    px[i * S + (S - 4)] = th.FloorLight;
                }
                break;

            case FloorStyle.Diamond:
                // A small lozenge in the middle of every tile.
                for (int d = 0; d <= 3; d++)
                {
                    Put(px, 8 + d, 8 - 3 + d, th.FloorLight);
                    Put(px, 8 - d, 8 - 3 + d, th.FloorLight);
                    Put(px, 8 + d, 8 + 3 - d, th.FloorDark);
                    Put(px, 8 - d, 8 + 3 - d, th.FloorDark);
                }
                break;
        }

        // Grid line (kept strong on every variant for readability).
        for (int i = 0; i < S; i++)
        {
            px[i] = th.FloorGrid;
            px[i * S] = th.FloorGrid;
        }

        for (int n = 0; n < 3; n++)
            Put(px, 3 + rng.Next(S - 5), 3 + rng.Next(S - 5), n % 2 == 0 ? th.FloorLight : th.FloorDark);

        switch (variant % 4)
        {
            case 1: Crack(px, 4, 4, 1, 1, 7, rng, th.FloorCrack); break;
            case 2: Blotch(px, 9, 9, rng, th.FloorDark); break;
            case 3: Crack(px, 12, 5, -1, 1, 6, rng, th.FloorCrack); break;
        }

        return px;
    }

    private static Color[] Wall(TileTheme th, int variant, bool lip)
    {
        var px = new Color[S * S];
        var rng = new Random(2000 + variant);
        Array.Fill(px, th.Mortar);

        switch (th.Wall)
        {
            case WallStyle.Blocks:
                // Large square blocks: two courses of 7px, staggered.
                for (int row = 0; row < 2; row++)
                {
                    int y0 = row * 8;
                    int shift = row == 0 ? 0 : 6;
                    for (int x0 = -12 + shift; x0 < S; x0 += 12)
                        Brick(px, th, rng, x0, y0, 12, 7);
                }
                break;

            case WallStyle.Boards:
                // Vertical timber boards with a lit top edge and a dark gap between them.
                for (int x0 = 0; x0 < S; x0 += 5)
                {
                    Color body = th.Brick[rng.Next(th.Brick.Length)];
                    for (int y = 0; y < S - 1; y++)
                        for (int x = x0; x < Math.Min(S, x0 + 4); x++)
                            px[y * S + x] = y == 0 ? th.BrickLit : (x == x0 ? th.BrickLit : body);
                    Put(px, x0 + 2, 3 + rng.Next(9), th.BrickShade);
                    Put(px, x0 + 1, 5 + rng.Next(8), th.BrickShade);
                }
                break;

            case WallStyle.Ornate:
                // Three brick rows with a gold stud band through the middle.
                for (int row = 0; row < 3; row++)
                {
                    int y0 = row * 5 + 1;
                    int shift = row % 2 == 0 ? 0 : 4;
                    for (int x0 = -8 + shift; x0 < S; x0 += 8)
                        Brick(px, th, rng, x0, y0, 8, 4);
                }

                for (int x = 2; x < S; x += 4)
                    Put(px, x, 7, th.Accent);
                break;

            default:
                // Three brick rows of 5px, staggered; row 15 is mortar.
                for (int row = 0; row < 3; row++)
                {
                    int y0 = row * 5 + 1;
                    int shift = row % 2 == 0 ? 0 : 4;
                    for (int x0 = -8 + shift; x0 < S; x0 += 8)
                        Brick(px, th, rng, x0, y0, 8, 4);
                }
                break;
        }

        if (variant is 1 or 3 && th.Wall != WallStyle.Boards)
            Crack(px, 4 + rng.Next(8), 1, rng.Next(2) * 2 - 1, 1, 10, rng, th.Mortar);

        if (lip)
        {
            // Wall base in shadow where it meets the floor below.
            for (int y = S - 3; y < S; y++)
                for (int x = 0; x < S; x++)
                    px[y * S + x] = y == S - 3 ? th.BrickShade : th.WallBase;
        }

        return px;
    }

    /// <summary>One brick or block: a body colour with a lit top row, a shaded bottom row and a speck.</summary>
    private static void Brick(Color[] px, TileTheme th, Random rng, int x0, int y0, int width, int height)
    {
        Color body = th.Brick[rng.Next(th.Brick.Length)];
        for (int y = y0; y < y0 + height; y++)
        {
            for (int x = Math.Max(0, x0 + 1); x < Math.Min(S, x0 + width); x++)
            {
                Color c = body;
                if (y == y0) c = th.BrickLit;
                else if (y == y0 + height - 1) c = th.BrickShade;
                if (y >= 0 && y < S)
                    px[y * S + x] = c;
            }
        }

        Put(px, Math.Clamp(x0 + 2 + rng.Next(Math.Max(1, width - 3)), 0, S - 1), y0 + 1 + rng.Next(Math.Max(1, height - 2)), th.BrickShade);
    }

    private static Color Shade(Color c, float factor) =>
        new((int)(c.R * factor), (int)(c.G * factor), (int)(c.B * factor));

    private static void Put(Color[] px, int x, int y, Color c)
    {
        if (x >= 0 && y >= 0 && x < S && y < S)
            px[y * S + x] = c;
    }

    private static void Crack(Color[] px, int x, int y, int dx, int dy, int length, Random rng, Color color)
    {
        for (int i = 0; i < length; i++)
        {
            Put(px, x, y, color);
            if (rng.Next(3) == 0) x += dx; else if (rng.Next(2) == 0) y += dy; else { x += dx; y += dy; }
        }
    }

    private static void Blotch(Color[] px, int cx, int cy, Random rng, Color color)
    {
        for (int y = -2; y <= 2; y++)
            for (int x = -2; x <= 2; x++)
                if (Math.Abs(x) + Math.Abs(y) <= 2 && rng.Next(4) != 0)
                    Put(px, cx + x, cy + y, color);
    }
}
