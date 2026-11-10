using EchoesOfThePast.Core;
using EchoesOfThePast.UI;
using EchoesOfThePast.World;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace EchoesOfThePast.Screens;

/// <summary>Pick one of the chapter's levels; Back or Esc returns to the Main Menu.</summary>
public sealed class LevelSelectScreen : Screen
{
    private readonly LevelMenu _menu;

    public LevelSelectScreen(GameContext context) : base(context)
    {
        _menu = new LevelMenu(context.WorldSize.X, context.WorldSize.Y, Levels.EpisodeNames, Levels.PerEpisode);
    }

    public override void OnShown() => _menu.Open(0);

    public override void Update(GameTime gameTime, InputState input)
    {
        int pick = _menu.Update(input.Cursor, input.Clicked);

        if (input.Pressed(Keys.Escape) || pick == Pick.Back)
            Context.Screens.Show<MainMenuScreen>();
        else if (pick >= 0)
            Context.Screens.Show<GameplayScreen>().StartLevel(pick);
    }

    public override void Draw(SpriteBatch spriteBatch, Texture2D pixel) => _menu.Draw(spriteBatch, pixel);
}
