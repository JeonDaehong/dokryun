using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Dokryun.Engine;
using Dokryun.Systems;

namespace Dokryun.Scenes;

public class DeathScene : Scene
{
    private Texture2D _pixel;
    private float _timer;
    private int _floor;
    private int _totalKills;
    private ParticleSystem _particles;
    private bool _canContinue;
    private int _karmaEarned;

    public DeathScene(int floor, int totalKills)
    {
        _floor = floor;
        _totalKills = totalKills;
    }

    public override void Enter()
    {
        _pixel = new Texture2D(GraphicsDevice, 1, 1);
        _pixel.SetData(new[] { Color.White });
        _particles = new ParticleSystem(300);
        _timer = 0;

        AudioManager.StopBgm();

        // Calculate and grant karma
        var meta = Game1.Meta;
        _karmaEarned = meta.CalculateKarma(_floor, _totalKills, Game1.LastBossDefeated);
        meta.Karma += _karmaEarned;
        meta.TotalRuns++;
        meta.TotalKills += _totalKills;
        if (_floor > meta.BestFloor) meta.BestFloor = _floor;
        meta.Save();

        for (int i = 0; i < 60; i++)
        {
            float angle = (float)(Random.Shared.NextDouble() * MathHelper.TwoPi);
            float speed = 30f + (float)Random.Shared.NextDouble() * 80f;
            var vel = new Vector2(MathF.Cos(angle), MathF.Sin(angle)) * speed;
            _particles.Emit(
                new Vector2(Game1.ScreenWidth / 2f, Game1.ScreenHeight / 2f - 60),
                vel,
                new Color(180, 140, 80) * 0.7f,
                2f + (float)Random.Shared.NextDouble() * 2f,
                2f + (float)Random.Shared.NextDouble() * 3f
            );
        }
    }

    public override void Update(GameTime gameTime)
    {
        float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;
        _timer += dt;
        _particles.Update(dt);

        if (_timer % 0.15f < dt)
        {
            float x = Random.Shared.Next(Game1.ScreenWidth);
            _particles.Emit(
                new Vector2(x, Game1.ScreenHeight + 5),
                new Vector2(0, -20f - (float)Random.Shared.NextDouble() * 15f),
                new Color(120, 80, 40) * 0.3f,
                3f, 1.5f
            );
        }

        if (_timer > 2f)
            _canContinue = true;

        if (_canContinue && (InputManager.IsKeyPressed(Keys.Enter) || InputManager.IsKeyPressed(Keys.Space) || InputManager.IsLeftClick()))
            SceneManager.ChangeScene(new MetaUpgradeScene());
    }

    public override void Draw(SpriteBatch spriteBatch)
    {
        GraphicsDevice.Clear(new Color(6, 4, 2));

        spriteBatch.Begin(samplerState: SamplerState.PointClamp);

        _particles.Draw(spriteBatch);

        float fadeIn = MathF.Min(1f, _timer * 1.5f);

        DrawReincarnationSymbol(spriteBatch, new Vector2(Game1.ScreenWidth / 2f, Game1.ScreenHeight / 2f - 100), fadeIn);

        if (_timer > 0.8f)
        {
            float textAlpha = MathF.Min(1f, (_timer - 0.8f) * 2f);
            var color = new Color(210, 170, 110) * textAlpha;

            string reborn = SanitizeForFont(Fonts.Title, "윤 회");
            var rebornSize = Fonts.Title.MeasureString(reborn);
            spriteBatch.DrawString(Fonts.Title, reborn,
                new Vector2(Game1.ScreenWidth / 2f - rebornSize.X / 2f + 2, Game1.ScreenHeight / 2f - 38),
                new Color(0, 0, 0) * textAlpha * 0.5f);
            spriteBatch.DrawString(Fonts.Title, reborn,
                new Vector2(Game1.ScreenWidth / 2f - rebornSize.X / 2f, Game1.ScreenHeight / 2f - 40),
                color);
        }

        if (_timer > 1.5f)
        {
            float statAlpha = MathF.Min(1f, (_timer - 1.5f) * 2f);
            int cy = Game1.ScreenHeight / 2 + 10;

            int cardW = 260;
            int cardH = 110;
            int cardX = Game1.ScreenWidth / 2 - cardW / 2;
            spriteBatch.Draw(_pixel, new Rectangle(cardX, cy - 6, cardW, cardH), new Color(0, 0, 0) * statAlpha * 0.3f);
            spriteBatch.Draw(_pixel, new Rectangle(cardX, cy - 6, 2, cardH), new Color(200, 160, 100) * statAlpha * 0.3f);

            DrawCenteredText(spriteBatch, Fonts.Game, $"도달 깊이 : 지하 {_floor}층", cy, new Color(200, 175, 120) * statAlpha);
            DrawCenteredText(spriteBatch, Fonts.Game, $"처치 수 : {_totalKills}", cy + 26, new Color(160, 140, 105) * statAlpha);

            // Karma earned
            DrawCenteredText(spriteBatch, Fonts.Game, $"획득 업(業) : +{_karmaEarned}", cy + 56,
                new Color(255, 200, 80) * statAlpha);

            // Total karma
            DrawCenteredText(spriteBatch, Fonts.Game, $"총 업(業) : {Game1.Meta.Karma}", cy + 78,
                new Color(200, 170, 100) * statAlpha * 0.7f);
        }

        if (_canContinue)
        {
            float blink = MathF.Sin(_timer * 2.5f) * 0.3f + 0.7f;
            DrawCenteredText(spriteBatch, Fonts.Game, "Enter 키로 영구 강화",
                Game1.ScreenHeight - 80, new Color(150, 130, 100) * blink);
        }

        spriteBatch.End();
    }

    private void DrawCenteredText(SpriteBatch spriteBatch, SpriteFont font, string text, int y, Color color)
    {
        text = SanitizeForFont(font, text);
        var size = font.MeasureString(text);
        spriteBatch.DrawString(font, text, new Vector2(Game1.ScreenWidth / 2f - size.X / 2f, y), color);
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

    private void DrawReincarnationSymbol(SpriteBatch spriteBatch, Vector2 center, float alpha)
    {
        var color = new Color(180, 140, 70) * alpha;
        float radius = 40f;
        float rotation = _timer * 0.5f;

        int segments = 24;
        for (int i = 0; i < segments; i++)
        {
            if (i % 3 == 0) continue;
            float angle = rotation + (i / (float)segments) * MathHelper.TwoPi;
            float x = center.X + MathF.Cos(angle) * radius;
            float y = center.Y + MathF.Sin(angle) * radius;
            spriteBatch.Draw(_pixel, new Rectangle((int)x - 1, (int)y - 1, 3, 3), color);
        }
        spriteBatch.Draw(_pixel, new Rectangle((int)center.X - 2, (int)center.Y - 2, 5, 5), color);
    }
}
