using System;
using System.Collections.Generic;

namespace EchoesOfThePast.Core;

/// <summary>
/// Owns every screen (one instance each) and which one is current. Screens switch with
/// <c>Show&lt;T&gt;()</c>, which returns the screen so the caller can hand it what it needs to start.
/// </summary>
public sealed class ScreenManager
{
    private readonly Dictionary<Type, Screen> _screens = new();

    public Screen Current { get; private set; }

    public void Register(Screen screen) => _screens[screen.GetType()] = screen;

    public T Show<T>() where T : Screen
    {
        var screen = (T)_screens[typeof(T)];
        Current = screen;
        screen.OnShown();
        return screen;
    }
}
