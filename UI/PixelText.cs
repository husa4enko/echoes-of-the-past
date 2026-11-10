using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace EchoesOfThePast.UI;

/// <summary>
/// Tiny 5x7 bitmap font drawn with the shared 1x1 pixel texture, so on-screen text needs no
/// content pipeline. Only the glyphs the game uses are defined (letters are case-folded); anything else is drawn as a gap.
/// </summary>
public static class PixelText
{
    private const int GlyphWidth = 5;
    private const int GlyphHeight = 7;
    private const int Spacing = 1;

    private static readonly Dictionary<char, string[]> Glyphs = new()
    {
        ['L'] = new[] { "X....", "X....", "X....", "X....", "X....", "X....", "XXXXX" },
        ['E'] = new[] { "XXXXX", "X....", "X....", "XXXX.", "X....", "X....", "XXXXX" },
        ['V'] = new[] { "X...X", "X...X", "X...X", "X...X", "X...X", ".X.X.", "..X.." },
        ['C'] = new[] { ".XXXX", "X....", "X....", "X....", "X....", "X....", ".XXXX" },
        ['A'] = new[] { ".XXX.", "X...X", "X...X", "XXXXX", "X...X", "X...X", "X...X" },
        ['R'] = new[] { "XXXX.", "X...X", "X...X", "XXXX.", "X.X..", "X..X.", "X...X" },
        ['D'] = new[] { "XXXX.", "X...X", "X...X", "X...X", "X...X", "X...X", "XXXX." },
        ['F'] = new[] { "XXXXX", "X....", "X....", "XXXX.", "X....", "X....", "X...." },
        ['G'] = new[] { ".XXXX", "X....", "X....", "X..XX", "X...X", "X...X", ".XXXX" },
        ['H'] = new[] { "X...X", "X...X", "X...X", "XXXXX", "X...X", "X...X", "X...X" },
        ['I'] = new[] { "XXXXX", "..X..", "..X..", "..X..", "..X..", "..X..", "XXXXX" },
        ['M'] = new[] { "X...X", "XX.XX", "X.X.X", "X.X.X", "X...X", "X...X", "X...X" },
        ['O'] = new[] { ".XXX.", "X...X", "X...X", "X...X", "X...X", "X...X", ".XXX." },
        ['P'] = new[] { "XXXX.", "X...X", "X...X", "XXXX.", "X....", "X....", "X...." },
        ['S'] = new[] { ".XXXX", "X....", "X....", ".XXX.", "....X", "....X", "XXXX." },
        ['T'] = new[] { "XXXXX", "..X..", "..X..", "..X..", "..X..", "..X..", "..X.." },
        ['X'] = new[] { "X...X", "X...X", ".X.X.", "..X..", ".X.X.", "X...X", "X...X" },
        ['B'] = new[] { "XXXX.", "X...X", "X...X", "XXXX.", "X...X", "X...X", "XXXX." },
        ['K'] = new[] { "X...X", "X..X.", "X.X..", "XX...", "X.X..", "X..X.", "X...X" },
        ['N'] = new[] { "X...X", "XX..X", "X.X.X", "X..XX", "X...X", "X...X", "X...X" },
        ['U'] = new[] { "X...X", "X...X", "X...X", "X...X", "X...X", "X...X", ".XXX." },
        ['W'] = new[] { "X...X", "X...X", "X...X", "X.X.X", "X.X.X", "XX.XX", "X...X" },
        ['Y'] = new[] { "X...X", "X...X", ".X.X.", "..X..", "..X..", "..X..", "..X.." },
        [':'] = new[] { ".....", "..X..", "..X..", ".....", "..X..", "..X..", "....." },
        ['0'] = new[] { ".XXX.", "X...X", "X..XX", "X.X.X", "XX..X", "X...X", ".XXX." },
        ['1'] = new[] { "..X..", ".XX..", "..X..", "..X..", "..X..", "..X..", ".XXX." },
        ['2'] = new[] { ".XXX.", "X...X", "....X", "...X.", "..X..", ".X...", "XXXXX" },
        ['3'] = new[] { "XXXX.", "....X", "....X", ".XXX.", "....X", "....X", "XXXX." },
        ['4'] = new[] { "...X.", "..XX.", ".X.X.", "X..X.", "XXXXX", "...X.", "...X." },
        ['5'] = new[] { "XXXXX", "X....", "XXXX.", "....X", "....X", "X...X", ".XXX." },
        ['6'] = new[] { ".XXX.", "X....", "X....", "XXXX.", "X...X", "X...X", ".XXX." },
        ['7'] = new[] { "XXXXX", "....X", "...X.", "..X..", ".X...", ".X...", ".X..." },
        ['8'] = new[] { ".XXX.", "X...X", "X...X", ".XXX.", "X...X", "X...X", ".XXX." },
        ['9'] = new[] { ".XXX.", "X...X", "X...X", ".XXXX", "....X", "....X", ".XXX." },
        ['-'] = new[] { ".....", ".....", ".....", "XXXXX", ".....", ".....", "....." },
        ['Z'] = new[] { "XXXXX", "....X", "...X.", "..X..", ".X...", "X....", "XXXXX" },
        ['.'] = new[] { ".....", ".....", ".....", ".....", ".....", ".....", "..X.." },
        ['('] = new[] { "...X.", "..X..", ".X...", ".X...", ".X...", "..X..", "...X." },
        [')'] = new[] { ".X...", "..X..", "...X.", "...X.", "...X.", "..X..", ".X..." },
        ['&'] = new[] { ".XX..", "X..X.", "X..X.", ".XX..", "X.X.X", "X..XX", ".XX.X" },
        ['['] = new[] { ".XXX.", ".X...", ".X...", ".X...", ".X...", ".X...", ".XXX." },
        [']'] = new[] { ".XXX.", "...X.", "...X.", "...X.", "...X.", "...X.", ".XXX." },
        ['/'] = new[] { "....X", "....X", "...X.", "..X..", ".X...", "X....", "X...." },
        ['>'] = new[] { "X....", ".X...", "..X..", "...X.", "..X..", ".X...", "X...." },
        ['<'] = new[] { "....X", "...X.", "..X..", ".X...", "..X..", "...X.", "....X" },
        ['\''] = new[] { "..X..", "..X..", ".X...", ".....", ".....", ".....", "....." },
        ['!'] =new[] { "..X..", "..X..", "..X..", "..X..", "..X..", ".....", "..X.." },
    };

    public static Point Measure(string text, int scale) =>
        new(text.Length * (GlyphWidth + Spacing) * scale - Spacing * scale, GlyphHeight * scale);

    /// <summary>Draws <paramref name="text"/> with its top-left corner at <paramref name="position"/>.</summary>
    public static void Draw(SpriteBatch spriteBatch, Texture2D pixel, string text, Point position, int scale, Color color)
    {
        int x = position.X;
        foreach (char c in text)
        {
            if (Glyphs.TryGetValue(char.ToUpperInvariant(c), out string[] rows))
            {
                for (int row = 0; row < GlyphHeight; row++)
                {
                    for (int col = 0; col < GlyphWidth; col++)
                    {
                        if (rows[row][col] == 'X')
                            spriteBatch.Draw(pixel, new Rectangle(x + col * scale, position.Y + row * scale, scale, scale), color);
                    }
                }
            }

            x += (GlyphWidth + Spacing) * scale;
        }
    }
}
