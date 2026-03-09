using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Dokryun.Engine;

namespace Dokryun.Scenes;

public class ReincarnationScene : Scene
{
    private Texture2D _pixel;
    private float _timer;
    private ParticleSystem _particles;
    private bool _selected;
    private float _selectTimer;
    private int _selectedIndex = 1; // 0=검사, 1=궁수

    public override void Enter()
    {
        _pixel = new Texture2D(GraphicsDevice, 1, 1);
        _pixel.SetData(new[] { Color.White });
        _particles = new ParticleSystem(500);
    }

    public override void Update(GameTime gameTime)
    {
        float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;
        _timer += dt;
        _particles.Update(dt);

        // Ambient soul particles
        if (_timer % 0.08f < dt)
        {
            float x = Random.Shared.Next(Game1.ScreenWidth);
            _particles.Emit(
                new Vector2(x, Game1.ScreenHeight + 5),
                new Vector2((float)(Random.Shared.NextDouble() * 20 - 10), -30f - (float)Random.Shared.NextDouble() * 20f),
                new Color(180, 140, 80) * 0.3f,
                2f + (float)Random.Shared.NextDouble() * 2f,
                2f + (float)Random.Shared.NextDouble() * 3f
            );
        }

        if (_selected)
        {
            _selectTimer -= dt;
            if (_selectTimer <= 0)
                SceneManager.ChangeScene(new VillageScene());
            return;
        }

        // Class selection (after UI appears)
        if (_timer > 2.5f)
        {
            if (InputManager.IsKeyPressed(Keys.Left) || InputManager.IsKeyPressed(Keys.A))
                _selectedIndex = Math.Max(0, _selectedIndex - 1);
            if (InputManager.IsKeyPressed(Keys.Right) || InputManager.IsKeyPressed(Keys.D))
                _selectedIndex = Math.Min(1, _selectedIndex + 1);

            if (InputManager.IsKeyPressed(Keys.Enter) || InputManager.IsKeyPressed(Keys.Space) || InputManager.IsLeftClick())
            {
                Game1.SelectedClass = _selectedIndex == 0 ? CharacterClass.Swordsman : CharacterClass.Archer;
                _selected = true;
                _selectTimer = 0.5f;

                float cardX = _selectedIndex == 0
                    ? Game1.ScreenWidth / 2f - 160f
                    : Game1.ScreenWidth / 2f;
                _particles.EmitBurst(new Vector2(cardX, 500), 30, new Color(200, 170, 100), 200f, 0.5f, 2f);
            }
        }
    }

    public override void Draw(SpriteBatch spriteBatch)
    {
        GraphicsDevice.Clear(new Color(8, 5, 3));

        spriteBatch.Begin(samplerState: SamplerState.PointClamp);

        _particles.Draw(spriteBatch);

        float fadeIn = MathF.Min(1f, _timer * 1.5f);

        // Reincarnation symbol
        DrawReincarnationSymbol(spriteBatch, new Vector2(Game1.ScreenWidth / 2f, 200), fadeIn);

        // Title
        if (_timer > 0.5f)
        {
            float alpha = MathF.Min(1f, (_timer - 0.5f) * 2f);
            string title = "윤회의 장소";
            var titleSize = Fonts.Title.MeasureString(title);
            spriteBatch.DrawString(Fonts.Title, title,
                new Vector2(Game1.ScreenWidth / 2f - titleSize.X / 2f, 260),
                new Color(200, 160, 100) * alpha);
        }

        // Flavor text
        if (_timer > 1.2f)
        {
            float alpha = MathF.Min(1f, (_timer - 1.2f) * 2f);
            DrawCenteredText(spriteBatch, "정신을 차렸다...", 320, new Color(150, 130, 100) * alpha);
        }

        // Character selection
        if (_timer > 2f)
        {
            float alpha = MathF.Min(1f, (_timer - 2f) * 2f);
            DrawCenteredText(spriteBatch, "직업을 선택하시오", 380, new Color(180, 150, 110) * alpha);

            int cardW = 140;
            int cardH = 180;
            int cy = 410;
            int gap = 20;

            // Card positions: Swordsman(left), Archer(center), Locked(right)
            int[] cardX = {
                Game1.ScreenWidth / 2 - cardW - gap / 2 - cardW / 2,  // 검사
                Game1.ScreenWidth / 2 - cardW / 2,                     // 궁수
                Game1.ScreenWidth / 2 + cardW / 2 + gap                // 잠금
            };

            // --- Swordsman card (index 0) ---
            DrawClassCard(spriteBatch, cardX[0], cy, cardW, cardH, alpha, _selectedIndex == 0,
                "검사", "검을 다루는 자", DrawSwordIcon);

            // --- Archer card (index 1) ---
            DrawClassCard(spriteBatch, cardX[1], cy, cardW, cardH, alpha, _selectedIndex == 1,
                "궁수", "활을 다루는 자", DrawBowIcon);

            // --- Locked slot (right) ---
            spriteBatch.Draw(_pixel, new Rectangle(cardX[2], cy, cardW, cardH), new Color(15, 12, 10) * alpha);
            DrawRectOutline(spriteBatch, new Rectangle(cardX[2], cy, cardW, cardH), new Color(60, 50, 40) * alpha, 1);
            DrawCenteredTextAt(spriteBatch, "?", cardX[2] + cardW / 2, cy + cardH / 2 - 10, new Color(60, 50, 40) * alpha);

            // Prompt
            if (_timer > 3f)
            {
                float blink = MathF.Sin(_timer * 3f) * 0.4f + 0.6f;
                DrawCenteredText(spriteBatch, "[ Enter 키로 선택 ]", cy + cardH + 20, new Color(180, 160, 130) * blink);
            }
        }

        spriteBatch.End();
    }

    private void DrawCenteredText(SpriteBatch spriteBatch, string text, int y, Color color, float scale = 0.7f)
    {
        text = SanitizeForFont(Fonts.Game, text);
        var size = Fonts.Game.MeasureString(text);
        spriteBatch.DrawString(Fonts.Game, text,
            new Vector2(Game1.ScreenWidth / 2f - size.X * scale / 2f, y),
            color, 0, Vector2.Zero, scale, SpriteEffects.None, 0);
    }

    private void DrawCenteredTextAt(SpriteBatch spriteBatch, string text, int cx, int cy, Color color)
    {
        text = SanitizeForFont(Fonts.Title, text);
        var size = Fonts.Title.MeasureString(text);
        spriteBatch.DrawString(Fonts.Title, text,
            new Vector2(cx - size.X / 2f, cy - size.Y / 2f), color);
    }

    private void DrawClassCard(SpriteBatch spriteBatch, int cx, int cy, int cardW, int cardH,
        float alpha, bool isSelected, string name, string desc, Action<SpriteBatch, int, int, Color> drawIcon)
    {
        var borderColor = isSelected ? new Color(200, 170, 100) * alpha : new Color(120, 100, 70) * alpha;
        var bgColor = new Color(25, 20, 15) * alpha;

        spriteBatch.Draw(_pixel, new Rectangle(cx, cy, cardW, cardH), bgColor);
        DrawRectOutline(spriteBatch, new Rectangle(cx, cy, cardW, cardH), borderColor, isSelected ? 2 : 1);

        if (isSelected)
        {
            float pulse = MathF.Sin(_timer * 3f) * 0.1f + 0.15f;
            spriteBatch.Draw(_pixel, new Rectangle(cx, cy, cardW, cardH), borderColor * pulse);
        }

        // Icon
        drawIcon(spriteBatch, cx + cardW / 2, cy + 50, borderColor);

        // Name
        var nameSize = Fonts.Game.MeasureString(name);
        spriteBatch.DrawString(Fonts.Game, name,
            new Vector2(cx + cardW / 2f - nameSize.X * 0.9f / 2f, cy + 100),
            borderColor, 0, Vector2.Zero, 0.9f, SpriteEffects.None, 0);

        // Description
        var descSize = Fonts.Game.MeasureString(desc);
        spriteBatch.DrawString(Fonts.Game, desc,
            new Vector2(cx + cardW / 2f - descSize.X * 0.6f / 2f, cy + 130),
            new Color(140, 120, 90) * alpha, 0, Vector2.Zero, 0.6f, SpriteEffects.None, 0);
    }

    private void DrawSwordIcon(SpriteBatch spriteBatch, int cx, int cy, Color color)
    {
        // Blade
        spriteBatch.Draw(_pixel, new Rectangle(cx - 1, cy - 20, 3, 28), color);
        // Guard
        spriteBatch.Draw(_pixel, new Rectangle(cx - 7, cy + 6, 15, 3), color);
        // Grip
        spriteBatch.Draw(_pixel, new Rectangle(cx - 1, cy + 9, 3, 10), new Color(140, 100, 50));
        // Tip
        spriteBatch.Draw(_pixel, new Rectangle(cx, cy - 22, 1, 3), new Color(255, 240, 200));
    }

    private void DrawBowIcon(SpriteBatch spriteBatch, int cx, int cy, Color color)
    {
        // Bow
        spriteBatch.Draw(_pixel, new Rectangle(cx - 1, cy - 18, 2, 36), color);
        spriteBatch.Draw(_pixel, new Rectangle(cx - 8, cy - 14, 8, 2), color);
        spriteBatch.Draw(_pixel, new Rectangle(cx - 8, cy + 12, 8, 2), color);
        // Arrow
        var arrowColor = new Color(255, 230, 150);
        spriteBatch.Draw(_pixel, new Rectangle(cx - 15, cy - 1, 20, 2), arrowColor);
        spriteBatch.Draw(_pixel, new Rectangle(cx + 4, cy - 3, 2, 2), arrowColor);
        spriteBatch.Draw(_pixel, new Rectangle(cx + 4, cy + 1, 2, 2), arrowColor);
    }

    private void DrawReincarnationSymbol(SpriteBatch spriteBatch, Vector2 center, float alpha)
    {
        var color = new Color(180, 140, 70) * alpha;
        float radius = 35f;
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

    private void DrawRectOutline(SpriteBatch spriteBatch, Rectangle rect, Color color, int thickness)
    {
        spriteBatch.Draw(_pixel, new Rectangle(rect.X, rect.Y, rect.Width, thickness), color);
        spriteBatch.Draw(_pixel, new Rectangle(rect.X, rect.Bottom - thickness, rect.Width, thickness), color);
        spriteBatch.Draw(_pixel, new Rectangle(rect.X, rect.Y, thickness, rect.Height), color);
        spriteBatch.Draw(_pixel, new Rectangle(rect.Right - thickness, rect.Y, thickness, rect.Height), color);
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
