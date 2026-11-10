using System;
using System.Runtime.InteropServices;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;

namespace EchoesOfThePast.UI;

/// <summary>
/// Keeps the hardware cursor inside the game window while it is locked (the game window has focus and a level is being played): SDL's input grab where it is
/// available, and otherwise (or as well) the cursor is pulled back onto the window's edge whenever it strays.
/// </summary>
public static class CursorLock
{
    [DllImport("SDL2", EntryPoint = "SDL_SetWindowGrab", CallingConvention = CallingConvention.Cdecl)]
    private static extern void SdlSetWindowGrab(IntPtr window, int grabbed);

    private static bool _grabUnavailable;
    private static bool _grabbed;

    /// <summary>Call once per frame with the window, whether the cursor should be locked in it, and the current mouse state. When it shouldn't, the cursor is released.</summary>
    public static void Update(GameWindow window, bool locked, MouseState mouse)
    {
        SetGrab(window, locked);
        if (!locked)
            return;

        Rectangle bounds = window.ClientBounds;
        int x = Math.Clamp(mouse.X, 0, Math.Max(0, bounds.Width - 1));
        int y = Math.Clamp(mouse.Y, 0, Math.Max(0, bounds.Height - 1));
        if (x != mouse.X || y != mouse.Y)
            Mouse.SetPosition(x, y);
    }

    private static void SetGrab(GameWindow window, bool grab)
    {
        if (_grabUnavailable || _grabbed == grab)
            return;

        try
        {
            SdlSetWindowGrab(window.Handle, grab ? 1 : 0);
            _grabbed = grab;
        }
        catch (Exception e) when (e is DllNotFoundException or EntryPointNotFoundException)
        {
            _grabUnavailable = true;
        }
    }
}
