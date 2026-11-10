using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace EchoesOfThePast.UI;

/// <summary>Result codes shared by the selection screens; any value of zero or more is a picked item.</summary>
public static class Pick
{
    public const int None = -1;
    public const int Back = -2;
}

/// <summary>Layout and colours shared by the episode and level screens.</summary>
internal static class SelectionStyle
{
    public const int BackWidth = 150;
    public const int BackHeight = 26;

    public static readonly Color TitleColor = new(235, 228, 205);
    public static readonly Color AccentColor = new(240, 220, 140);
    public static readonly Color MutedColor = new(150, 140, 120);

    public static Rectangle BackButton(int width, int height) =>
        new((width - BackWidth) / 2, height - BackHeight - 18, BackWidth, BackHeight);

    public static void DrawTitle(SpriteBatch spriteBatch, Texture2D pixel, string title, int width, int y)
    {
        const int scale = 2;
        Point size = PixelText.Measure(title, scale);
        var at = new Point((width - size.X) / 2, y);
        PixelText.Draw(spriteBatch, pixel, title, at + new Point(3, 3), scale, Color.Black * 0.4f);
        PixelText.Draw(spriteBatch, pixel, title, at, scale, TitleColor);
    }

    public static void DrawCentered(SpriteBatch spriteBatch, Texture2D pixel, string text, Rectangle bounds, int y, int scale, Color color)
    {
        Point size = PixelText.Measure(text, scale);
        PixelText.Draw(spriteBatch, pixel, text, new Point(bounds.X + (bounds.Width - size.X) / 2, y), scale, color);
    }
}
/// <summary>The numbered level buttons of one episode, plus a Back button to the main menu.</summary>
public class LevelMenu
{
    private const int ButtonSize = 40;
    private const int ButtonGap = 12;
    private const int NumberScale = 3;

    private readonly Rectangle _screen;
    private readonly Rectangle[] _buttons;
    private readonly Rectangle _back;
    private readonly string[] _episodeNames;
    private readonly int _levelsPerEpisode;

    private Point _cursor = new(-1, -1);
    private int _episode;

    public LevelMenu(int width, int height, string[] episodeNames, int levelsPerEpisode)
    {
        _screen = new Rectangle(0, 0, width, height);
        _episodeNames = episodeNames;
        _levelsPerEpisode = levelsPerEpisode;

        int rowWidth = levelsPerEpisode * ButtonSize + (levelsPerEpisode - 1) * ButtonGap;
        _buttons = new Rectangle[levelsPerEpisode];
        for (int i = 0; i < _buttons.Length; i++)
            _buttons[i] = new Rectangle((width - rowWidth) / 2 + i * (ButtonSize + ButtonGap), 100, ButtonSize, ButtonSize);

        _back = SelectionStyle.BackButton(width, height);
    }

    /// <param name="episode">Zero-based episode whose levels to show.</param>
    public void Open(int episode) => _episode = episode;

    /// <summary>Returns the slot (0-based, within the episode) that was clicked, <see cref="Pick.Back"/>, or <see cref="Pick.None"/>.</summary>
    public int Update(Point cursor, bool clicked)
    {
        _cursor = cursor;
        if (!clicked)
            return Pick.None;

        if (_back.Contains(cursor))
            return Pick.Back;
        for (int i = 0; i < _buttons.Length; i++)
        {
            if (_buttons[i].Contains(cursor))
                return i;
        }

        return Pick.None;
    }

    public void Draw(SpriteBatch spriteBatch, Texture2D pixel)
    {
        MainMenu.DrawBackground(spriteBatch, pixel, _screen);
        SelectionStyle.DrawTitle(spriteBatch, pixel, _episodeNames[_episode], _screen.Width, _screen.Height / 6);
        SelectionStyle.DrawCentered(
            spriteBatch, pixel, $"CHAPTER {_episode + 1}", _screen, _screen.Height / 6 + 22, 1, SelectionStyle.MutedColor);

        for (int i = 0; i < _buttons.Length; i++)
        {
            Rectangle button = _buttons[i];
            MainMenu.DrawButton(spriteBatch, pixel, button, string.Empty, button.Contains(_cursor));

            string number = (_episode * _levelsPerEpisode + i + 1).ToString();
            int y = button.Y + (button.Height - PixelText.Measure(number, NumberScale).Y) / 2;
            SelectionStyle.DrawCentered(spriteBatch, pixel, number, button, y, NumberScale, SelectionStyle.TitleColor);
        }

        MainMenu.DrawButton(spriteBatch, pixel, _back, "BACK", _back.Contains(_cursor));
    }
}
