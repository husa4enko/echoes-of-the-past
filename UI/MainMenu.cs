using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace EchoesOfThePast.UI;

public enum MenuAction
{
    None,
    Start,
    Credits,
    Exit,
}

/// <summary>
/// The title screen: the game's name and three buttons (chapters, credits, exit). Works in world pixels (the same space the
/// game draws in), so the caller converts the mouse position down by the render scale.
/// </summary>
public class MainMenu
{
    private const string Title = "ECHOES OF THE PAST";
    private const int TitleScale = 2;
    private const int LabelScale = 2;

    private const int ButtonWidth = 150;
    private const int ButtonHeight = 26;
    private const int ButtonGap = 10;

    private static readonly Color BackgroundA = new(34, 28, 44);
    private static readonly Color BackgroundB = new(40, 33, 52);
    private static readonly Color TitleColor = new(235, 228, 205);
    private static readonly Color EchoColor = new Color(150, 230, 255) * 0.4f;
    private static readonly Color ButtonFill = new(60, 52, 70);
    private static readonly Color ButtonFillHover = new(92, 80, 112);
    private static readonly Color ButtonFillSelected = new(70, 110, 90);
    private static readonly Color ButtonBorder = new(150, 140, 120);
    private static readonly Color ButtonBorderHover = new(240, 220, 140);
    private static readonly Color LabelColor = new(235, 228, 205);

    private readonly Rectangle _screen;
    private readonly Rectangle _startButton;
    private readonly Rectangle _creditsButton;
    private readonly Rectangle _exitButton;

    private bool _hoverStart;
    private bool _hoverCredits;
    private bool _hoverExit;

    public MainMenu(int width, int height)
    {
        _screen = new Rectangle(0, 0, width, height);

        int x = (width - ButtonWidth) / 2;
        int startY = height / 2 - 10;
        _startButton = new Rectangle(x, startY, ButtonWidth, ButtonHeight);
        _creditsButton = new Rectangle(x, startY + ButtonHeight + ButtonGap, ButtonWidth, ButtonHeight);
        _exitButton = new Rectangle(x, startY + 2 * (ButtonHeight + ButtonGap), ButtonWidth, ButtonHeight);
    }

    /// <summary>Call once per frame with the cursor in world pixels and whether the button was just clicked.</summary>
    public MenuAction Update(Point cursor, bool clicked)
    {
        _hoverStart = _startButton.Contains(cursor);
        _hoverCredits = _creditsButton.Contains(cursor);
        _hoverExit = _exitButton.Contains(cursor);

        if (!clicked)
            return MenuAction.None;
        if (_hoverStart)
            return MenuAction.Start;
        if (_hoverCredits)
            return MenuAction.Credits;
        if (_hoverExit)
            return MenuAction.Exit;
        return MenuAction.None;
    }

    public void Draw(SpriteBatch spriteBatch, Texture2D pixel)
    {
        DrawBackground(spriteBatch, pixel, _screen);

        // The title with a faint offset copy behind it, like an echo.
        Point titleSize = PixelText.Measure(Title, TitleScale);
        var titleAt = new Point((_screen.Width - titleSize.X) / 2, _screen.Height / 6);
        PixelText.Draw(spriteBatch, pixel, Title, titleAt + new Point(3, 3), TitleScale, EchoColor);
        PixelText.Draw(spriteBatch, pixel, Title, titleAt, TitleScale, TitleColor);

        DrawButton(spriteBatch, pixel, _startButton, "CHAPTERS", _hoverStart);
        DrawButton(spriteBatch, pixel, _creditsButton, "CREDITS", _hoverCredits);
        DrawButton(spriteBatch, pixel, _exitButton, "EXIT GAME", _hoverExit);
    }

    internal static void DrawBackground(SpriteBatch spriteBatch, Texture2D pixel, Rectangle screen)
    {
        const int tile = 16;
        for (int y = 0; y * tile < screen.Height; y++)
        {
            for (int x = 0; x * tile < screen.Width; x++)
            {
                spriteBatch.Draw(
                    pixel,
                    new Rectangle(x * tile, y * tile, tile, tile),
                    (x + y) % 2 == 0 ? BackgroundA : BackgroundB);
            }
        }
    }

    internal static void DrawButton(
        SpriteBatch spriteBatch, Texture2D pixel, Rectangle bounds, string label, bool hover,
        int labelScale = LabelScale, bool selected = false, bool dimmed = false)
    {
        spriteBatch.Draw(pixel, bounds, hover || selected ? ButtonBorderHover : ButtonBorder);
        spriteBatch.Draw(
            pixel,
            new Rectangle(bounds.X + 2, bounds.Y + 2, bounds.Width - 4, bounds.Height - 4),
            selected ? ButtonFillSelected : hover ? ButtonFillHover : ButtonFill);

        if (dimmed)
            spriteBatch.Draw(pixel, bounds, Color.Black * 0.5f);

        Point size = PixelText.Measure(label, labelScale);
        var at = new Point(bounds.X + (bounds.Width - size.X) / 2, bounds.Y + (bounds.Height - size.Y) / 2);
        PixelText.Draw(spriteBatch, pixel, label, at, labelScale, LabelColor);
    }
}
