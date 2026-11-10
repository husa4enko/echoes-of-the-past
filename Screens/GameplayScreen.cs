using System;
using EchoesOfThePast.Core;
using EchoesOfThePast.Gameplay;
using EchoesOfThePast.UI;
using EchoesOfThePast.World;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace EchoesOfThePast.Screens;

/// <summary>
/// Playing a level, plus the overlays that sit on top of it (pause, level complete, caught). The rules live in
/// <see cref="LevelSession"/>; this screen reads its input, reacts to its status and draws the overlays.
/// </summary>
public sealed class GameplayScreen : Screen
{
    private enum Mode
    {
        Playing,
        Paused,
        LevelComplete,
        Caught,
    }

    private readonly OverlayMenu _pauseMenu;
    private readonly OverlayMenu _levelCompleteMenu;
    private readonly OverlayMenu _chapterClearedMenu;
    private readonly CaughtOverlay _caughtOverlay;

    private LevelSession _session;
    private LevelHints _hints;
    private Mode _mode;

    public GameplayScreen(GameContext context) : base(context)
    {
        int width = context.WorldSize.X, height = context.WorldSize.Y;
        _pauseMenu = new OverlayMenu(width, height, "PAUSED", new Color(235, 228, 205), true, 132,
            "RESUME [ESC]", "RESTART LEVEL", "LEVEL SELECT", "MAIN MENU");
        _levelCompleteMenu = new OverlayMenu(width, height, "LEVEL COMPLETED", new Color(150, 230, 160), true, 132,
            "NEXT LEVEL [ENTER]", "LEVEL SELECT [ESC]");
        _chapterClearedMenu = new OverlayMenu(width, height, "CHAPTER 1 CLEARED!", new Color(240, 210, 120), "THANKS FOR PLAYING THE DEMO!", 132,
            "LEVEL SELECT [ENTER]", "MAIN MENU [ESC]");
        _caughtOverlay = new CaughtOverlay(context.WorldSize);
    }

    /// <summary>Locked inside the window only while Ben is being steered; pause, caught and the win banner free the cursor.</summary>
    public override bool CapturesMouse => _mode == Mode.Playing;

    private bool IsLastLevel => _session.Index + 1 >= Levels.Count;

    /// <summary>Builds a fresh copy of a level. Also used to restart the current one.</summary>
    public void StartLevel(int index)
    {
        _session = new LevelSession(index, Context.PhantomDelay);
        _hints = new LevelHints(Levels.Hints(index));
        _mode = Mode.Playing;
    }

    public override void Update(GameTime gameTime, InputState input)
    {
        float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;

        switch (_mode)
        {
            case Mode.Paused:
                UpdatePaused(input);
                break;
            case Mode.LevelComplete:
                UpdateLevelComplete(input, dt);
                break;
            case Mode.Caught:
                UpdateCaught(input, dt);
                break;
            default:
                UpdatePlaying(gameTime, input, dt);
                break;
        }
    }

    private void UpdatePlaying(GameTime gameTime, InputState input, float dt)
    {
        if (input.Pressed(Keys.Escape))
        {
            _pauseMenu.Select(0);
            _mode = Mode.Paused;
            return;
        }

        // R: restart the current level.
        if (input.Pressed(Keys.R))
            StartLevel(_session.Index);

        // Z / X: shorten / lengthen the echo's delay (one step per press).
        if (input.Pressed(Keys.Z))
            _session.PhantomDelay--;
        if (input.Pressed(Keys.X))
            _session.PhantomDelay++;
        Context.PhantomDelay = _session.PhantomDelay;

        _session.Update(gameTime, input.KeyboardState);
        _hints.Update(dt, _session.StepsTaken);

        if (_session.Status == SessionStatus.Completed)
        {
            _mode = Mode.LevelComplete;
        }
        else if (_session.Status == SessionStatus.Caught)
        {
            _caughtOverlay.Begin(_session.CaughtAt.Value);
            _mode = Mode.Caught;
        }
    }

    /// <summary>
    /// The world is frozen and the cursor is free. Esc or RESUME carries on; W/S or the arrows move the highlight and Enter
    /// picks it (or click an option): RESTART LEVEL starts the level over, LEVEL SELECT and MAIN MENU leave it.
    /// </summary>
    private void UpdatePaused(InputState input)
    {
        int button = _pauseMenu.Update(input.Cursor, input.Clicked);

        if (input.Pressed(Keys.W) || input.Pressed(Keys.Up))
            _pauseMenu.Move(-1);
        else if (input.Pressed(Keys.S) || input.Pressed(Keys.Down))
            _pauseMenu.Move(1);
        else if ((input.Pressed(Keys.Enter) || input.Pressed(Keys.Space)) && _pauseMenu.Selected >= 0)
            button = _pauseMenu.Selected;

        if (input.Pressed(Keys.Escape))
            button = 0;

        switch (button)
        {
            case 0:
                _mode = Mode.Playing;
                break;
            case 1:
                StartLevel(_session.Index);
                break;
            case 2:
                Context.Screens.Show<LevelSelectScreen>();
                break;
            case 3:
                Context.Screens.Show<MainMenuScreen>();
                break;
        }
    }

    /// <summary>
    /// The world stays frozen behind the banner. Enter / NEXT LEVEL moves on, Esc / LEVEL SELECT goes back to the list.
    /// After the last level the banner reads CHAPTER 1 CLEARED and Enter / Esc lead to the level list / Main Menu.
    /// </summary>
    private void UpdateLevelComplete(InputState input, float dt)
    {
        _session.UpdateIdle(dt);
        _hints.FadeOut(dt);

        bool lastLevel = IsLastLevel;
        int button = (lastLevel ? _chapterClearedMenu : _levelCompleteMenu).Update(input.Cursor, input.Clicked);
        if (input.Pressed(Keys.Enter))
            button = 0;
        else if (input.Pressed(Keys.Escape))
            button = 1;

        if (button == 0 && !lastLevel)
            StartLevel(_session.Index + 1);
        else if (button == 0 || (button == 1 && !lastLevel))
            Context.Screens.Show<LevelSelectScreen>();
        else if (button == 1)
            Context.Screens.Show<MainMenuScreen>();
    }

    /// <summary>
    /// The world freezes under a red flash, then the Game Over menu appears. R / RESTART LEVEL retries at once
    /// (even during the flash); Esc / LEVEL SELECT returns to the level list.
    /// </summary>
    private void UpdateCaught(InputState input, float dt)
    {
        int button = _caughtOverlay.Update(dt, input.Cursor, input.Clicked);
        if (input.Pressed(Keys.R))
            button = 0;
        else if (input.Pressed(Keys.Escape))
            button = 1;

        if (button == 0)
            StartLevel(_session.Index);
        else if (button == 1)
            Context.Screens.Show<LevelSelectScreen>();
    }

    public override void Draw(SpriteBatch spriteBatch, Texture2D pixel)
    {
        _session.Draw(spriteBatch, pixel);
        _hints.Draw(spriteBatch, pixel, Context.WorldSize);

        switch (_mode)
        {
            case Mode.Paused:
                _pauseMenu.Draw(spriteBatch, pixel);
                break;
            case Mode.LevelComplete:
                (IsLastLevel ? _chapterClearedMenu : _levelCompleteMenu).Draw(spriteBatch, pixel);
                break;
            case Mode.Caught:
                _caughtOverlay.Draw(spriteBatch, pixel);
                break;
        }
    }
}
