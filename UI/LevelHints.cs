using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace EchoesOfThePast.UI;

/// <summary>
/// A level's tutorial hints: a fixed-size banner along the bottom wall (as wide as the longest hint) that shows one hint
/// at a time, so nothing is drawn over the puzzle floor. A level with no hints draws nothing.
/// </summary>
public sealed class LevelHints
{
    private readonly (int FromStep, string Text)[] _hints;

    /// <summary>The hint currently shown, and how visible its text is (0 to 1). When the step changes the old text fades out and the new one fades in.</summary>
    private int _shown;
    private float _textFade = 1f;

    /// <summary>Opacity of the whole banner: full while playing, fading out once the level is cleared.</summary>
    private float _alpha = 1f;

    public LevelHints((int FromStep, string Text)[] hints)
    {
        _hints = hints;
    }

    public void Update(float dt, int stepsTaken)
    {
        if (_hints.Length == 0)
            return;

        int target = StepFor(stepsTaken);
        if (_shown != target)
        {
            _textFade -= dt * 6f;
            if (_textFade <= 0f)
            {
                _shown = target;
                _textFade = 0f;
            }
        }
        else
        {
            _textFade = Math.Min(1f, _textFade + dt * 6f);
        }
    }

    public void FadeOut(float dt) => _alpha = Math.Max(0f, _alpha - dt * 2.5f);

    /// <summary>The hint Ben's progress calls for: the last one whose step count he has reached.</summary>
    private int StepFor(int stepsTaken)
    {
        int step = 0;
        for (int i = 0; i < _hints.Length; i++)
        {
            if (stepsTaken >= _hints[i].FromStep)
                step = i;
        }

        return step;
    }

    public void Draw(SpriteBatch spriteBatch, Texture2D pixel, Point worldSize)
    {
        if (_hints.Length == 0 || _alpha <= 0f)
            return;

        int textWidth = 0;
        foreach ((int _, string hint) in _hints)
            textWidth = Math.Max(textWidth, PixelText.Measure(hint, 1).X);

        const int padding = 8, height = 17;
        var panel = new Rectangle((worldSize.X - (textWidth + padding * 2)) / 2, worldSize.Y - height, textWidth + padding * 2, height);
        float a = _alpha;

        // Dark translucent plate with a thin two-tone pixel border, like the menu panels.
        spriteBatch.Draw(pixel, panel, new Color(14, 12, 22) * (0.88f * a));
        Color edge = new Color(104, 92, 140) * a, inner = new Color(56, 48, 82) * a;
        spriteBatch.Draw(pixel, new Rectangle(panel.X, panel.Y, panel.Width, 1), edge);
        spriteBatch.Draw(pixel, new Rectangle(panel.X, panel.Y, 1, panel.Height), edge);
        spriteBatch.Draw(pixel, new Rectangle(panel.Right - 1, panel.Y, 1, panel.Height), edge);
        spriteBatch.Draw(pixel, new Rectangle(panel.X + 1, panel.Y + 1, panel.Width - 2, 1), inner);
        spriteBatch.Draw(pixel, new Rectangle(panel.X + 1, panel.Y + 1, 1, panel.Height - 1), inner);
        spriteBatch.Draw(pixel, new Rectangle(panel.Right - 2, panel.Y + 1, 1, panel.Height - 1), inner);

        // The text sits centred in the fixed banner.
        string text = _hints[_shown].Text;
        Point textSize = PixelText.Measure(text, 1);
        var textAt = new Point(panel.X + (panel.Width - textSize.X) / 2, panel.Y + (height - textSize.Y) / 2 + 1);
        float t = a * _textFade;
        PixelText.Draw(spriteBatch, pixel, text, textAt + new Point(1, 1), 1, Color.Black * t);
        PixelText.Draw(spriteBatch, pixel, text, textAt, 1, new Color(255, 244, 205) * t);
    }
}
