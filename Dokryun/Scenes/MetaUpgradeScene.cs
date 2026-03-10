using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Dokryun.Engine;
using Dokryun.Systems;

namespace Dokryun.Scenes;

public class MetaUpgradeScene : Scene
{
    private Texture2D _pixel;
    private float _timer;
    private int _selectedIndex;
    private string _notification;
    private float _notifTimer;

    public override void Enter()
    {
        _pixel = new Texture2D(GraphicsDevice, 1, 1);
        _pixel.SetData(new[] { Color.White });
        _timer = 0;
        _selectedIndex = 0;
    }

    public override void Update(GameTime gameTime)
    {
        float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;
        _timer += dt;
        if (_notifTimer > 0) _notifTimer -= dt;

        if (InputManager.IsKeyPressed(Keys.W) || InputManager.IsKeyPressed(Keys.Up))
            _selectedIndex = Math.Max(0, _selectedIndex - 1);
        if (InputManager.IsKeyPressed(Keys.S) || InputManager.IsKeyPressed(Keys.Down))
            _selectedIndex = Math.Min(MetaProgression.Upgrades.Length - 1, _selectedIndex + 1);

        if (InputManager.IsKeyPressed(Keys.Enter) || InputManager.IsKeyPressed(Keys.Space))
        {
            if (Game1.Meta.TryUpgrade(_selectedIndex))
            {
                Game1.Meta.Save();
                _notification = "강화 성공!";
                _notifTimer = 1.5f;
                AudioManager.Play("pickup", 0.8f, -0.2f);
            }
            else
            {
                _notification = "업(業)이 부족하거나 최대 레벨입니다";
                _notifTimer = 1.5f;
            }
        }

        if (InputManager.IsKeyPressed(Keys.Escape))
            SceneManager.ChangeScene(new ReincarnationScene());
    }

    public override void Draw(SpriteBatch spriteBatch)
    {
        GraphicsDevice.Clear(new Color(8, 6, 4));

        spriteBatch.Begin(samplerState: SamplerState.PointClamp);

        // Title
        string title = SanitizeForFont(Fonts.Title, "영구 강화");
        var titleSize = Fonts.Title.MeasureString(title);
        spriteBatch.DrawString(Fonts.Title, title,
            new Vector2(Game1.ScreenWidth / 2f - titleSize.X / 2f, 40),
            new Color(210, 170, 110));

        // Karma display
        string karmaText = SanitizeForFont(Fonts.Game, $"보유 업(業): {Game1.Meta.Karma}");
        var karmaSize = Fonts.Game.MeasureString(karmaText);
        spriteBatch.DrawString(Fonts.Game, karmaText,
            new Vector2(Game1.ScreenWidth / 2f - karmaSize.X * 0.6f / 2f, 90),
            new Color(255, 200, 80), 0, Vector2.Zero, 0.6f, SpriteEffects.None, 0);

        // Stats summary
        string statsText = SanitizeForFont(Fonts.Game, $"총 윤회: {Game1.Meta.TotalRuns}  최고 깊이: {Game1.Meta.BestFloor}층  총 처치: {Game1.Meta.TotalKills}");
        var statsSize = Fonts.Game.MeasureString(statsText);
        spriteBatch.DrawString(Fonts.Game, statsText,
            new Vector2(Game1.ScreenWidth / 2f - statsSize.X * 0.4f / 2f, 116),
            new Color(150, 130, 100) * 0.7f, 0, Vector2.Zero, 0.4f, SpriteEffects.None, 0);

        // Upgrade list
        int startY = 155;
        for (int i = 0; i < MetaProgression.Upgrades.Length; i++)
        {
            var info = MetaProgression.Upgrades[i];
            int level = Game1.Meta.GetLevel(i);
            bool maxed = level >= info.MaxLevel;
            bool canBuy = Game1.Meta.CanUpgrade(i);
            bool selected = i == _selectedIndex;

            int iy = startY + i * 80;
            int cardW = 500;
            int cardX = (Game1.ScreenWidth - cardW) / 2;

            // Selection highlight
            if (selected)
            {
                float pulse = MathF.Sin(_timer * 4f) * 0.1f + 0.3f;
                spriteBatch.Draw(_pixel, new Rectangle(cardX - 4, iy - 4, cardW + 8, 72), new Color(200, 160, 80) * pulse);
            }

            // Card background
            spriteBatch.Draw(_pixel, new Rectangle(cardX, iy, cardW, 64), new Color(20, 16, 12));
            spriteBatch.Draw(_pixel, new Rectangle(cardX, iy, 3, 64), maxed ? new Color(100, 200, 100) * 0.5f : new Color(80, 70, 50));

            // Name
            string upgName = SanitizeForFont(Fonts.Game, info.Name);
            spriteBatch.DrawString(Fonts.Game, upgName,
                new Vector2(cardX + 16, iy + 6), maxed ? new Color(100, 200, 100) : new Color(220, 190, 140),
                0, Vector2.Zero, 0.55f, SpriteEffects.None, 0);

            // Description
            string upgDesc = SanitizeForFont(Fonts.Game, info.Description);
            spriteBatch.DrawString(Fonts.Game, upgDesc,
                new Vector2(cardX + 16, iy + 28), Color.White * 0.6f,
                0, Vector2.Zero, 0.38f, SpriteEffects.None, 0);

            // Level pips
            for (int l = 0; l < info.MaxLevel; l++)
            {
                int pipX = cardX + 16 + l * 18;
                int pipY = iy + 46;
                var pipColor = l < level ? new Color(255, 200, 80) : new Color(40, 35, 28);
                spriteBatch.Draw(_pixel, new Rectangle(pipX, pipY, 12, 6), pipColor);
                spriteBatch.Draw(_pixel, new Rectangle(pipX, pipY, 12, 1), new Color(255, 240, 160) * (l < level ? 0.4f : 0.1f));
            }

            // Cost
            if (!maxed)
            {
                int cost = info.Costs[level];
                string costText = SanitizeForFont(Fonts.Game, $"{cost} 업");
                var costColor = canBuy ? new Color(255, 210, 60) : new Color(255, 80, 80);
                spriteBatch.DrawString(Fonts.Game, costText,
                    new Vector2(cardX + cardW - 90, iy + 20), costColor,
                    0, Vector2.Zero, 0.45f, SpriteEffects.None, 0);
            }
            else
            {
                spriteBatch.DrawString(Fonts.Game, "MAX",
                    new Vector2(cardX + cardW - 60, iy + 20), new Color(100, 200, 100),
                    0, Vector2.Zero, 0.45f, SpriteEffects.None, 0);
            }
        }

        // Notification
        if (_notifTimer > 0 && _notification != null)
        {
            string notifText = SanitizeForFont(Fonts.Game, _notification);
            var notifSize = Fonts.Game.MeasureString(notifText);
            float alpha = Math.Min(1f, _notifTimer);
            spriteBatch.DrawString(Fonts.Game, notifText,
                new Vector2(Game1.ScreenWidth / 2f - notifSize.X * 0.5f / 2f, Game1.ScreenHeight - 120),
                new Color(255, 220, 100) * alpha, 0, Vector2.Zero, 0.5f, SpriteEffects.None, 0);
        }

        // Instructions
        string instrText = SanitizeForFont(Fonts.Game, "W/S: 선택  Enter: 강화  ESC: 다시 윤회");
        var instrSize = Fonts.Game.MeasureString(instrText);
        spriteBatch.DrawString(Fonts.Game, instrText,
            new Vector2(Game1.ScreenWidth / 2f - instrSize.X * 0.4f / 2f, Game1.ScreenHeight - 50),
            Color.White * 0.5f, 0, Vector2.Zero, 0.4f, SpriteEffects.None, 0);

        spriteBatch.End();
    }

    private static string SanitizeForFont(SpriteFont font, string text)
    {
        var sb = new System.Text.StringBuilder(text.Length);
        foreach (char c in text)
        {
            if (font.Characters.Contains(c) || c == '\n' || c == '\r')
                sb.Append(c);
        }
        return sb.ToString();
    }
}
