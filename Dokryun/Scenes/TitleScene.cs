using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Dokryun.Engine;

namespace Dokryun.Scenes;

public class TitleScene : Scene
{
    private Texture2D _pixel;
    private float _timer;
    private float _titlePulse;
    private bool _started;
    private ParticleSystem _ambientParticles;

    public override void Enter()
    {
        _pixel = new Texture2D(GraphicsDevice, 1, 1);
        _pixel.SetData(new[] { Color.White });
        _ambientParticles = new ParticleSystem(500);
        _timer = 0;
    }

    public override void Update(GameTime gameTime)
    {
        float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;
        _timer += dt;
        _titlePulse += dt * 2f;

        if (_timer % 0.1f < dt)
        {
            float x = Random.Shared.Next(Game1.ScreenWidth);
            _ambientParticles.Emit(
                new Vector2(x, -10),
                new Vector2((float)(Random.Shared.NextDouble() * 20 - 10), 30 + (float)Random.Shared.NextDouble() * 20),
                new Color(100, 80, 60) * 0.5f,
                4f + (float)Random.Shared.NextDouble() * 3f,
                1f + (float)Random.Shared.NextDouble() * 2f
            );
        }

        _ambientParticles.Update(dt);

        if (!_started && (InputManager.IsKeyPressed(Keys.Enter) || InputManager.IsKeyPressed(Keys.Space) || InputManager.IsLeftClick()))
        {
            _started = true;
            SceneManager.ChangeScene(new ReincarnationScene());
        }
    }

    public override void Draw(SpriteBatch spriteBatch)
    {
        GraphicsDevice.Clear(new Color(15, 12, 10));

        spriteBatch.Begin(samplerState: SamplerState.PointClamp);

        _ambientParticles.Draw(spriteBatch);

        // Decorative lines
        int lineY = 260;
        DrawHorizontalLine(spriteBatch, lineY, new Color(80, 60, 40));
        DrawHorizontalLine(spriteBatch, lineY + 180, new Color(80, 60, 40));

        // Title: 독련
        float pulse = 0.9f + 0.1f * MathF.Sin(_titlePulse);
        var titleColor = new Color(220, 190, 140);
        var shadowColor = new Color(30, 20, 10);
        string title = "독 련";
        var titleSize = Fonts.Title.MeasureString(title);
        var titlePos = new Vector2(Game1.ScreenWidth / 2f - titleSize.X * pulse / 2f, 280);

        // Shadow
        spriteBatch.DrawString(Fonts.Title, title, titlePos + new Vector2(2, 2), shadowColor, 0, Vector2.Zero, pulse, SpriteEffects.None, 0);
        // Main
        spriteBatch.DrawString(Fonts.Title, title, titlePos, titleColor, 0, Vector2.Zero, pulse, SpriteEffects.None, 0);

        // Subtitle: DOKRYUN
        float alpha = MathF.Max(0, MathF.Min(1, (_timer - 0.5f) * 2f));
        string subtitle = "D O K R Y U N";
        var subSize = Fonts.Game.MeasureString(subtitle);
        var subPos = new Vector2(Game1.ScreenWidth / 2f - subSize.X / 2f, 330);
        spriteBatch.DrawString(Fonts.Game, subtitle, subPos, new Color(150, 120, 80) * alpha);

        // Description
        if (_timer > 1f)
        {
            float descAlpha = MathF.Min(1f, (_timer - 1f) * 2f);
            string desc = "홀로 싸우며, 윤회하는 자";
            var descSize = Fonts.Game.MeasureString(desc);
            spriteBatch.DrawString(Fonts.Game, desc,
                new Vector2(Game1.ScreenWidth / 2f - descSize.X / 2f, 360),
                new Color(120, 100, 70) * descAlpha);
        }

        // "Enter 키를 눌러 시작" blinking
        if (_timer > 1.5f)
        {
            float blink = MathF.Sin(_timer * 3f) * 0.5f + 0.5f;
            string prompt = "[ Enter 키를 눌러 시작 ]";
            var promptSize = Fonts.Game.MeasureString(prompt);
            spriteBatch.DrawString(Fonts.Game, prompt,
                new Vector2(Game1.ScreenWidth / 2f - promptSize.X / 2f, 500),
                new Color(180, 160, 130) * blink);
        }

        // Version
        spriteBatch.DrawString(Fonts.Game, "v0.1",
            new Vector2(Game1.ScreenWidth - 60, Game1.ScreenHeight - 30),
            new Color(60, 50, 40));

        spriteBatch.End();
    }

    private void DrawHorizontalLine(SpriteBatch spriteBatch, int y, Color color)
    {
        int margin = 200;
        for (int x = margin; x < Game1.ScreenWidth - margin; x += 8)
            spriteBatch.Draw(_pixel, new Rectangle(x, y, 4, 1), color);

        int ornSize = 5;
        spriteBatch.Draw(_pixel, new Rectangle(margin - ornSize - 4, y - ornSize / 2, ornSize, ornSize), color * 0.8f);
        spriteBatch.Draw(_pixel, new Rectangle(Game1.ScreenWidth - margin + 4, y - ornSize / 2, ornSize, ornSize), color * 0.8f);
    }
}
