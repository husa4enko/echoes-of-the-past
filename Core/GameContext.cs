using System;
using EchoesOfThePast.Entities;
using Microsoft.Xna.Framework;

namespace EchoesOfThePast.Core;

/// <summary>What every screen shares: the world size, the screen switcher, the quit action and the settings that outlive a level.</summary>
public sealed class GameContext
{
    private readonly Action _exit;

    public GameContext(Point worldSize, Action exit)
    {
        WorldSize = worldSize;
        _exit = exit;
    }

    public Point WorldSize { get; }
    public ScreenManager Screens { get; } = new();

    /// <summary>The echo's delay is a setting, so it survives level changes and restarts.</summary>
    public int PhantomDelay { get; set; } = Phantom.DefaultDelay;

    public void Exit() => _exit();
}
