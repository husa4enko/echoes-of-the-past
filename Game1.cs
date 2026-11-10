using EchoesOfThePast.Art;
using EchoesOfThePast.Core;
using EchoesOfThePast.Screens;
using EchoesOfThePast.UI;
using EchoesOfThePast.World;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace EchoesOfThePast;

/// <summary>
/// The MonoGame host: window, input polling, the view transform and the frame loop. Everything the player sees
/// is a <see cref="Screen"/> run by the <see cref="ScreenManager"/>; the rules of a level live in <c>LevelSession</c>.
/// </summary>
public class Game1 : Game
{
    /// <summary>Fixed windowed size: exactly 4x the 320x240 world, so pixels stay sharp.</summary>
    private static readonly Point WindowSize = new(1280, 960);

    private readonly GraphicsDeviceManager _graphics;
    private readonly InputState _input = new();
    private SpriteBatch _spriteBatch;
    private Texture2D _pixel;
    private GameContext _context;
    private ViewTransform _view;

    public Game1()
    {
        _graphics = new GraphicsDeviceManager(this);
        Content.RootDirectory = "Content";
        IsMouseVisible = true;
        _graphics.IsFullScreen = false;
        _graphics.PreferredBackBufferWidth = WindowSize.X;
        _graphics.PreferredBackBufferHeight = WindowSize.Y;
        Window.Title = "Echoes of the Past";
    }

    protected override void Initialize()
    {
        Point world = Levels.WorldPixelSize;
        _view = ViewTransform.Fit(WindowSize, world);
        _context = new GameContext(world, Exit);

        ScreenManager screens = _context.Screens;
        screens.Register(new MainMenuScreen(_context));
        screens.Register(new CreditsScreen(_context));
        screens.Register(new LevelSelectScreen(_context));
        screens.Register(new GameplayScreen(_context));
        screens.Show<MainMenuScreen>();

        base.Initialize();
    }

    protected override void LoadContent()
    {
        _spriteBatch = new SpriteBatch(GraphicsDevice);
        SpriteBank.Load(GraphicsDevice);

        _pixel = new Texture2D(GraphicsDevice, 1, 1);
        _pixel.SetData(new[] { Color.White });
    }

    protected override void UnloadContent()
    {
        _pixel?.Dispose();
        _spriteBatch?.Dispose();
    }

    protected override void Update(GameTime gameTime)
    {
        _input.Update(IsActive, _view);

        if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed)
            Exit();

        CursorLock.Update(Window, IsActive && _context.Screens.Current.CapturesMouse, _input.MouseState);
        _context.Screens.Current.Update(gameTime, _input);

        // The cursor is only out of the way while Ben is being steered.
        IsMouseVisible = !_context.Screens.Current.CapturesMouse;

        base.Update(gameTime);
    }

    protected override void Draw(GameTime gameTime)
    {
        GraphicsDevice.Clear(Color.Black);

        _spriteBatch.Begin(samplerState: SamplerState.PointClamp, transformMatrix: _view.Matrix);
        _context.Screens.Current.Draw(_spriteBatch, _pixel);
        _spriteBatch.End();

        base.Draw(gameTime);
    }
}