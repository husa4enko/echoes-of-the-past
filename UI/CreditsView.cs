using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace EchoesOfThePast.UI;

/// <summary>
/// The credits: game title, developer, version and design reference, plus a Back button to the main
/// menu. World-pixel space, same look as the other menus.
/// </summary>
public class CreditsView
{
    private const string Title = "CREDITS";

    private const int LabelScale = 1;
    private const int LabelGap = 4;
    private const int LineGap = 2;
    private const int EntryGap = 8;

    private static readonly (string Label, int ValueScale, string[] Lines)[] Entries =
    {
        ("GAME TITLE", 2, new[] { "ECHOES OF THE PAST" }),
        ("DEVELOPER", 2, new[] { "ANDRII HUSAK" }),
        ("VERSION", 1, new[] { "1.0 (NON-OFFICIAL VERSION)" }),
        ("DESIGN REFERENCE", 1, new[] { "GAME DESIGN DOCUMENT (GDD)", "& CORE PUZZLE MECHANICS" }),
    };

    private readonly Rectangle _screen;
    private readonly Rectangle _back;

    private Point _cursor = new(-1, -1);

    public CreditsView(int width, int height)
    {
        _screen = new Rectangle(0, 0, width, height);
        _back = SelectionStyle.BackButton(width, height);
    }

    /// <summary>Returns <see cref="Pick.Back"/> when Back is clicked, otherwise <see cref="Pick.None"/>.</summary>
    public int Update(Point cursor, bool clicked)
    {
        _cursor = cursor;
        return clicked && _back.Contains(cursor) ? Pick.Back : Pick.None;
    }

    public void Draw(SpriteBatch spriteBatch, Texture2D pixel)
    {
        MainMenu.DrawBackground(spriteBatch, pixel, _screen);
        SelectionStyle.DrawTitle(spriteBatch, pixel, Title, _screen.Width, 22);

        int y = 54;
        foreach ((string label, int valueScale, string[] lines) in Entries)
        {
            SelectionStyle.DrawCentered(spriteBatch, pixel, label, _screen, y, LabelScale, SelectionStyle.AccentColor);
            y += PixelText.Measure(label, LabelScale).Y + LabelGap;

            foreach (string line in lines)
            {
                SelectionStyle.DrawCentered(spriteBatch, pixel, line, _screen, y, valueScale, SelectionStyle.TitleColor);
                y += PixelText.Measure(line, valueScale).Y + LineGap;
            }

            y += EntryGap - LineGap;
        }

        MainMenu.DrawButton(spriteBatch, pixel, _back, "BACK", _back.Contains(_cursor));
    }
}
