using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Dokryun.Engine;

namespace Dokryun.Scenes;

public class DeathScene : Scene
{
    private Texture2D _pixel;
    private float _timer;
    private int _floor;
    private int _totalKills;
    private ParticleSystem _particles;
    private bool _canContinue;

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
            SceneManager.ChangeScene(new ReincarnationScene());
    }

    public override void Draw(SpriteBatch spriteBatch)
    {
        GraphicsDevice.Clear(new Color(6, 4, 2));

        spriteBatch.Begin(samplerState: SamplerState.PointClamp);

        _particles.Draw(spriteBatch);

        float fadeIn = MathF.Min(1f, _timer * 1.5f);

        DrawReincarnationSymbol(spriteBatch, new Vector2(Game1.ScreenWidth / 2f, Game1.ScreenHeight / 2f - 85), fadeIn);

        if (_timer > 0.8f)
        {
            float textAlpha = MathF.Min(1f, (_timer - 0.8f) * 2f);
            var color = new Color(210, 170, 110) * textAlpha;

            string reborn = "윤 회";
            var rebornSize = Fonts.Title.MeasureString(reborn);
            // Shadow
            spriteBatch.DrawString(Fonts.Title, reborn,
                new Vector2(Game1.ScreenWidth / 2f - rebornSize.X / 2f + 2, Game1.ScreenHeight / 2f - 18),
                new Color(0, 0, 0) * textAlpha * 0.5f);
            spriteBatch.DrawString(Fonts.Title, reborn,
                new Vector2(Game1.ScreenWidth / 2f - rebornSize.X / 2f, Game1.ScreenHeight / 2f - 20),
                color);
        }

        if (_timer > 1.5f)
        {
            float statAlpha = MathF.Min(1f, (_timer - 1.5f) * 2f);
            int cy = Game1.ScreenHeight / 2 + 35;

            // Stat cards with subtle background
            int cardW = 220;
            int cardH = 60;
            int cardX = Game1.ScreenWidth / 2 - cardW / 2;
            spriteBatch.Draw(_pixel, new Rectangle(cardX, cy - 6, cardW, cardH), new Color(0, 0, 0) * statAlpha * 0.3f);
            spriteBatch.Draw(_pixel, new Rectangle(cardX, cy - 6, 2, cardH), new Color(200, 160, 100) * statAlpha * 0.3f);

            DrawCenteredText(spriteBatch, Fonts.Game, $"도달 깊이 : 지하 {_floor}층", cy, new Color(200, 175, 120) * statAlpha);
            DrawCenteredText(spriteBatch, Fonts.Game, $"처치 수 : {_totalKills}", cy + 28, new Color(160, 140, 105) * statAlpha);
        }

        if (_canContinue)
        {
            float blink = MathF.Sin(_timer * 2.5f) * 0.3f + 0.7f;
            DrawCenteredText(spriteBatch, Fonts.Game, "Enter 키로 다시 윤회",
                Game1.ScreenHeight - 80, new Color(150, 130, 100) * blink);
        }

        spriteBatch.End();
    }

    private void DrawCenteredText(SpriteBatch spriteBatch, SpriteFont font, string text, int y, Color color)
    {
        var size = font.MeasureString(text);
        spriteBatch.DrawString(font, text, new Vector2(Game1.ScreenWidth / 2f - size.X / 2f, y), color);
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
