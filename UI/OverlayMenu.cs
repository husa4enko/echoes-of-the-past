using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace EchoesOfThePast.UI;

/// <summary>
/// A dimmed overlay with a title and a column of menu-style buttons, drawn over the frozen game.
/// Used for the pause menu, the caught (game over) screen and the level-completed screen; the last two sit in a framed panel. Works in world pixels like <see cref="MainMenu"/>.
/// </summary>
public class OverlayMenu
{
    private const int TitleScale = 2;
    private const int LabelScale = 1;
    private const int TitleGap = 11;

    private const int DefaultButtonWidth = 132;
    private const int PanelPadding = 10;
    private const int ButtonHeight = 16;
    private const int ButtonGap = 3;

    private readonly Rectangle _screen;
    private readonly string _title;
    private readonly Color _titleColor;
    private readonly string[] _labels;
    private readonly Rectangle[] _buttons;
    private readonly Point _titleAt;
    private readonly Rectangle _panel;
    private readonly bool _framed;
    private readonly string _subtitle;
    private readonly Point _subtitleAt;
    private const int SubtitleGap = 10;
    private const int SubtitleScale = 1;

    private Point _cursor = new(-1, -1);

    /// <summary>The highlighted option (moved by the mouse or the keyboard), or -1 for none yet.</summary>
    public int Selected { get; private set; } = -1;

    public int Count => _buttons.Length;

    public OverlayMenu(int width, int height, string title, Color titleColor, params string[] labels)
        : this(width, height, title, titleColor, false, DefaultButtonWidth, labels)
    {
    }

    /// <param name="framed">Draw a dark panel with a pixel-art border behind the title and buttons.</param>
    public OverlayMenu(int width, int height, string title, Color titleColor, bool framed, int buttonWidth, params string[] labels)
        : this(width, height, title, titleColor, framed, null, buttonWidth, labels)
    {
    }

    /// <summary>A framed menu with a line of small text under the title.</summary>
    public OverlayMenu(int width, int height, string title, Color titleColor, string subtitle, int buttonWidth, params string[] labels)
        : this(width, height, title, titleColor, true, subtitle, buttonWidth, labels)
    {
    }

    private OverlayMenu(int width, int height, string title, Color titleColor, bool framed, string subtitle, int buttonWidth, string[] labels)
    {
        _framed = framed;
        _subtitle = subtitle;
        _screen = new Rectangle(0, 0, width, height);
        _title = title;
        _titleColor = titleColor;
        _labels = labels;

        int titleHeight = PixelText.Measure(title, TitleScale).Y;
        int subtitleHeight = subtitle == null ? 0 : SubtitleGap + PixelText.Measure(subtitle, SubtitleScale).Y;
        int buttonsHeight = labels.Length * ButtonHeight + (labels.Length - 1) * ButtonGap;
        int top = (height - (titleHeight + subtitleHeight + TitleGap + buttonsHeight)) / 2;

        _titleAt = new Point((width - PixelText.Measure(title, TitleScale).X) / 2, top);
        if (subtitle != null)
            _subtitleAt = new Point((width - PixelText.Measure(subtitle, SubtitleScale).X) / 2, top + titleHeight + SubtitleGap);

        _buttons = new Rectangle[labels.Length];
        int y = top + titleHeight + subtitleHeight + TitleGap;
        for (int i = 0; i < labels.Length; i++)
        {
            _buttons[i] = new Rectangle((width - buttonWidth) / 2, y, buttonWidth, ButtonHeight);
            y += ButtonHeight + ButtonGap;
        }

        int widest = System.Math.Max(buttonWidth, System.Math.Max(PixelText.Measure(title, TitleScale).X, subtitle == null ? 0 : PixelText.Measure(subtitle, SubtitleScale).X));
        int panelWidth = widest + PanelPadding * 2;
        // The last button's box is taller than its text, so the top gets the same extra room to keep the text block centred.
        int slack = (ButtonHeight - PixelText.Measure(labels.Length > 0 ? labels[0] : "A", LabelScale).Y) / 2;
        _panel = new Rectangle((width - panelWidth) / 2, top - PanelPadding - slack, panelWidth, titleHeight + subtitleHeight + TitleGap + buttonsHeight + PanelPadding * 2 + slack * 2);
    }

    /// <summary>
    /// Call once per frame with the cursor in world pixels and whether the button was just clicked.
    /// Returns the index of the clicked button, or -1.
    /// </summary>
    public int Update(Point cursor, bool clicked)
    {
        // Moving the mouse onto an option selects it (a resting cursor doesn't override the keyboard).
        if (cursor != _cursor)
        {
            for (int i = 0; i < _buttons.Length; i++)
            {
                if (_buttons[i].Contains(cursor))
                    Selected = i;
            }
        }

        _cursor = cursor;
        if (!clicked)
            return -1;

        for (int i = 0; i < _buttons.Length; i++)
        {
            if (_buttons[i].Contains(cursor))
                return i;
        }

        return -1;
    }

    /// <summary>Highlights an option (-1 for none).</summary>
    public void Select(int index) => Selected = index;

    /// <summary>Moves the highlight up or down the list, wrapping round; the first press from "none" lands on the first/last option.</summary>
    public void Move(int delta)
    {
        if (_buttons.Length == 0)
            return;

        Selected = Selected < 0
            ? (delta > 0 ? 0 : _buttons.Length - 1)
            : ((Selected + delta) % _buttons.Length + _buttons.Length) % _buttons.Length;
    }

    public void Draw(SpriteBatch spriteBatch, Texture2D pixel)
    {
        spriteBatch.Draw(pixel, _screen, Color.Black * (_framed ? 0.38f : 0.6f));
        if (_framed)
            DrawPanel(spriteBatch, pixel);

        PixelText.Draw(spriteBatch, pixel, _title, _titleAt + new Point(TitleScale, TitleScale), TitleScale, Color.Black);
        PixelText.Draw(spriteBatch, pixel, _title, _titleAt, TitleScale, _titleColor);

        if (_subtitle != null)
        {
            PixelText.Draw(spriteBatch, pixel, _subtitle, _subtitleAt + new Point(1, 1), SubtitleScale, Color.Black);
            PixelText.Draw(spriteBatch, pixel, _subtitle, _subtitleAt, SubtitleScale, new Color(190, 184, 210));
        }

        for (int i = 0; i < _buttons.Length; i++)
        {
            if (_framed)
                DrawTextItem(spriteBatch, pixel, _buttons[i], _labels[i], i == Selected);
            else
                MainMenu.DrawButton(spriteBatch, pixel, _buttons[i], _labels[i], _buttons[i].Contains(_cursor));
        }
    }

    /// <summary>A plain text option: muted until hovered, then bright gold between two pixel arrows.</summary>
    private static void DrawTextItem(SpriteBatch spriteBatch, Texture2D pixel, Rectangle bounds, string label, bool hover)
    {
        const int scale = LabelScale;
        Point size = PixelText.Measure(label, scale);
        var at = new Point(bounds.X + (bounds.Width - size.X) / 2, bounds.Y + (bounds.Height - size.Y) / 2);
        Color color = hover ? new Color(255, 214, 90) : new Color(150, 142, 176);

        PixelText.Draw(spriteBatch, pixel, label, at + new Point(scale, scale), scale, Color.Black * 0.8f);
        PixelText.Draw(spriteBatch, pixel, label, at, scale, color);

        if (hover)
        {
            Point arrow = PixelText.Measure(">", scale);
            Point left = new(at.X - arrow.X - 6, at.Y);
            Point right = new(at.X + size.X + 6, at.Y);
            PixelText.Draw(spriteBatch, pixel, ">", left + new Point(scale, scale), scale, Color.Black * 0.8f);
            PixelText.Draw(spriteBatch, pixel, "<", right + new Point(scale, scale), scale, Color.Black * 0.8f);
            PixelText.Draw(spriteBatch, pixel, ">", left, scale, color);
            PixelText.Draw(spriteBatch, pixel, "<", right, scale, color);
        }
    }

    /// <summary>A dark banner with a stepped pixel-art border: outer rim, inner line and studded corners.</summary>
    private void DrawPanel(SpriteBatch spriteBatch, Texture2D pixel)
    {
        Color rim = new(24, 20, 36), edge = new(104, 92, 140), inner = new(56, 48, 82), fill = new(14, 12, 22);
        Rectangle p = _panel;

        spriteBatch.Draw(pixel, p, rim);
        spriteBatch.Draw(pixel, new Rectangle(p.X + 2, p.Y + 2, p.Width - 4, p.Height - 4), edge);
        spriteBatch.Draw(pixel, new Rectangle(p.X + 4, p.Y + 4, p.Width - 8, p.Height - 8), inner);
        spriteBatch.Draw(pixel, new Rectangle(p.X + 5, p.Y + 5, p.Width - 10, p.Height - 10), fill);

        foreach (var corner in new[] { new Point(p.X, p.Y), new Point(p.Right - 6, p.Y), new Point(p.X, p.Bottom - 6), new Point(p.Right - 6, p.Bottom - 6) })
        {
            spriteBatch.Draw(pixel, new Rectangle(corner.X, corner.Y, 6, 6), rim);
            spriteBatch.Draw(pixel, new Rectangle(corner.X + 1, corner.Y + 1, 4, 4), edge);
        }
    }
}
