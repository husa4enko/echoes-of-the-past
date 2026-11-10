using System;
using EchoesOfThePast.Gameplay;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;

namespace EchoesOfThePast.Tests;

/// <summary>
/// Plays a <see cref="LevelSession"/> with scripted key presses, no window or graphics device. A move script is a
/// string of U, D, L, R: each letter is one deliberate step of Ben (a letter that bumps a wall just turns him).
/// The game may also nudge Ben out of a door that shuts on him; the driver lets that finish first and does not count it as a letter.
/// </summary>
public sealed class SessionDriver
{
    /// <summary>One real frame at 60 fps.</summary>
    public const float RealFrame = 1f / 60f;

    /// <summary>One long frame: a whole step starts or lands within a single update.</summary>
    public const float CoarseFrame = 0.16f;

    private const int MaxSettleFrames = 200;

    private readonly float _frameSeconds;
    private TimeSpan _total;

    public SessionDriver(int level, int phantomDelay, bool realFrameRate)
    {
        Session = new LevelSession(level, phantomDelay);
        _frameSeconds = realFrameRate ? RealFrame : CoarseFrame;
    }

    public LevelSession Session { get; }

    /// <summary>Plays the moves in order and stops early once the level is completed or Ben is caught.</summary>
    public SessionStatus Play(string moves)
    {
        foreach (char move in moves)
        {
            Move(move);
            if (Session.Status != SessionStatus.Playing)
                break;
        }

        return Session.Status;
    }

    public void Move(char move)
    {
        (Keys key, Point offset) = move switch
        {
            'U' => (Keys.Up, new Point(0, -1)),
            'D' => (Keys.Down, new Point(0, 1)),
            'L' => (Keys.Left, new Point(-1, 0)),
            'R' => (Keys.Right, new Point(1, 0)),
            _ => throw new ArgumentException($"Unknown move '{move}'; use U, D, L or R.", nameof(move)),
        };

        for (int attempt = 0; attempt < 3 && Session.Status == SessionStatus.Playing; attempt++)
        {
            // An idle frame first, so a door that shut on Ben nudges him out before the key is pressed.
            Tick(new KeyboardState());
            Settle();
            if (Session.Status != SessionStatus.Playing)
                return;

            Point before = Session.BenTile;
            Tick(new KeyboardState(key));
            if (!Session.IsBenMoving || Session.BenTile == before + offset)
                break;
        }

        Settle();
    }

    /// <summary>Idle frames until Ben's step has landed.</summary>
    private void Settle()
    {
        for (int i = 0; i < MaxSettleFrames && Session.Status == SessionStatus.Playing && Session.IsBenMoving; i++)
            Tick(new KeyboardState());
    }

    private void Tick(KeyboardState keyboard)
    {
        var elapsed = TimeSpan.FromSeconds(_frameSeconds);
        _total += elapsed;
        Session.Update(new GameTime(_total, elapsed), keyboard);
    }
}
