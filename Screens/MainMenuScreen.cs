using EchoesOfThePast.Core;
using EchoesOfThePast.UI;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace EchoesOfThePast.Screens;

public sealed class MainMenuScreen : Screen
{
    private readonly MainMenu _menu;

    public MainMenuScreen(GameContext context) : base(context)
    {
        _menu = new MainMenu(context.WorldSize.X, context.WorldSize.Y);
    }

    public override void Update(GameTime gameTime, InputState input)
    {
        if (input.Pressed(Keys.Escape))
            Context.Exit();

        switch (_menu.Update(input.Cursor, input.Clicked))
        {
            case MenuAction.Start:
                Context.Screens.Show<LevelSelectScreen>();
                break;
            case MenuAction.Credits:
                Context.Screens.Show<CreditsScreen>();
                break;
            case MenuAction.Exit:
                Context.Exit();
                break;
        }
    }

    public override void Draw(SpriteBatch spriteBatch, Texture2D pixel) => _menu.Draw(spriteBatch, pixel);
}
