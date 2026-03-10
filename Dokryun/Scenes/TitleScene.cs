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
        GraphicsDevice.Clear(new Color(12, 10, 8));

        spriteBatch.Begin(samplerState: SamplerState.PointClamp);

        // Subtle vertical gradient overlay (darker edges)
        for (int i = 0; i < 6; i++)
        {
            float a = (1f - i / 6f) * 0.15f;
            spriteBatch.Draw(_pixel, new Rectangle(0, i * 20, Game1.ScreenWidth, 20), new Color(0, 0, 0) * a);
            spriteBatch.Draw(_pixel, new Rectangle(0, Game1.ScreenHeight - (i + 1) * 20, Game1.ScreenWidth, 20), new Color(0, 0, 0) * a);
        }

        _ambientParticles.Draw(spriteBatch);

        // Decorative lines
        int lineY = 265;
        DrawHorizontalLine(spriteBatch, lineY, new Color(100, 75, 45) * 0.6f);
        DrawHorizontalLine(spriteBatch, lineY + 175, new Color(100, 75, 45) * 0.6f);

        // Title glow
        float glowPulse = MathF.Sin(_titlePulse * 0.8f) * 0.04f + 0.08f;
        var titleCenter = new Vector2(Game1.ScreenWidth / 2f, 300);
        spriteBatch.Draw(_pixel, new Rectangle((int)titleCenter.X - 80, (int)titleCenter.Y - 15, 160, 40), new Color(220, 180, 100) * glowPulse);

        // Title: 독련
        float pulse = 0.95f + 0.05f * MathF.Sin(_titlePulse);
        var titleColor = new Color(230, 200, 150);
        var shadowColor = new Color(20, 15, 8);
        string title = "독 련";
        var titleSize = Fonts.Title.MeasureString(title);
        var titlePos = new Vector2(Game1.ScreenWidth / 2f - titleSize.X * pulse / 2f, 280);

        // Shadow (deeper)
        spriteBatch.DrawString(Fonts.Title, title, titlePos + new Vector2(3, 3), shadowColor * 0.6f, 0, Vector2.Zero, pulse, SpriteEffects.None, 0);
        spriteBatch.DrawString(Fonts.Title, title, titlePos + new Vector2(1, 1), shadowColor, 0, Vector2.Zero, pulse, SpriteEffects.None, 0);
        spriteBatch.DrawString(Fonts.Title, title, titlePos, titleColor, 0, Vector2.Zero, pulse, SpriteEffects.None, 0);

        // Subtitle: DOKRYUN (letter-spaced, fade in)
        float alpha = MathF.Max(0, MathF.Min(1, (_timer - 0.5f) * 2f));
        string subtitle = "D O K R Y U N";
        var subSize = Fonts.Game.MeasureString(subtitle);
        var subPos = new Vector2(Game1.ScreenWidth / 2f - subSize.X * 0.8f / 2f, 332);
        spriteBatch.DrawString(Fonts.Game, subtitle, subPos, new Color(160, 130, 85) * alpha, 0, Vector2.Zero, 0.8f, SpriteEffects.None, 0);

        // Description
        if (_timer > 1f)
        {
            float descAlpha = MathF.Min(1f, (_timer - 1f) * 2f);
            string desc = "홀로 싸우며, 윤회하는 자";
            var descSize = Fonts.Game.MeasureString(desc);
            spriteBatch.DrawString(Fonts.Game, desc,
                new Vector2(Game1.ScreenWidth / 2f - descSize.X * 0.8f / 2f, 360),
                new Color(110, 95, 65) * descAlpha, 0, Vector2.Zero, 0.8f, SpriteEffects.None, 0);
        }

        // "Enter 키를 눌러 시작" - smooth fade pulse instead of harsh blink
        if (_timer > 1.5f)
        {
            float fadeIn = MathF.Min(1f, (_timer - 1.5f) * 2f);
            float blink = MathF.Sin(_timer * 2.5f) * 0.3f + 0.7f;
            string prompt = "Enter 키를 눌러 시작";
            var promptSize = Fonts.Game.MeasureString(prompt);
            float px = Game1.ScreenWidth / 2f - promptSize.X * 0.7f / 2f;
            spriteBatch.DrawString(Fonts.Game, prompt,
                new Vector2(px, 500),
                new Color(180, 160, 130) * blink * fadeIn, 0, Vector2.Zero, 0.7f, SpriteEffects.None, 0);
        }

        // Version
        spriteBatch.DrawString(Fonts.Game, "v0.1",
            new Vector2(Game1.ScreenWidth - 55, Game1.ScreenHeight - 28),
            new Color(50, 42, 32), 0, Vector2.Zero, 0.7f, SpriteEffects.None, 0);

        spriteBatch.End();
    }

    private void DrawHorizontalLine(SpriteBatch spriteBatch, int y, Color color)
    {
        int margin = 220;
        // Continuous thin line with fading ends
        int lineW = Game1.ScreenWidth - margin * 2;
        spriteBatch.Draw(_pixel, new Rectangle(margin, y, lineW, 1), color);

        // End ornaments (small diamonds)
        int ornSize = 3;
        spriteBatch.Draw(_pixel, new Rectangle(margin - ornSize - 3, y - 1, ornSize, ornSize), color * 0.7f);
        spriteBatch.Draw(_pixel, new Rectangle(Game1.ScreenWidth - margin + 3, y - 1, ornSize, ornSize), color * 0.7f);
    }
}
