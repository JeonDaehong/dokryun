using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

namespace Dokryun.Engine;

public static class Fonts
{
    public static SpriteFont Game { get; private set; }
    public static SpriteFont Title { get; private set; }

    public static void Load(ContentManager content)
    {
        Game = content.Load<SpriteFont>("Fonts/GameFont");
        Title = content.Load<SpriteFont>("Fonts/TitleFont");
    }
}
