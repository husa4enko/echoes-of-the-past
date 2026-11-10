using EchoesOfThePast.Core;
using EchoesOfThePast.UI;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace EchoesOfThePast.Screens;

/// <summary>Back or Esc returns to the Main Menu.</summary>
public sealed class CreditsScreen : Screen
{
    private readonly CreditsView _view;

    public CreditsScreen(GameContext context) : base(context)
    {
        _view = new CreditsView(context.WorldSize.X, context.WorldSize.Y);
    }

    public override void Update(GameTime gameTime, InputState input)
    {
        if (_view.Update(input.Cursor, input.Clicked) == Pick.Back || input.Pressed(Keys.Escape))
            Context.Screens.Show<MainMenuScreen>();
    }

    public override void Draw(SpriteBatch spriteBatch, Texture2D pixel) => _view.Draw(spriteBatch, pixel);
}
