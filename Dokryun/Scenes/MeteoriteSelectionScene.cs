using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Dokryun.Engine;
using Dokryun.Systems;

namespace Dokryun.Scenes;

public class MeteoriteSelectionScene : Scene
{
    private Texture2D _pixel;
    private float _timer;
    private ParticleSystem _particles;
    private int _selectedIndex = 1;
    private bool _selected;
    private float _selectTimer;

    // 3 choices: randomized each run
    private MeteoriteId[] _choices;

    public override void Enter()
    {
        _pixel = new Texture2D(GraphicsDevice, 1, 1);
        _pixel.SetData(new[] { Color.White });
        _particles = new ParticleSystem(500);

        // Pick 3 unique meteorites (1 stackable fragment, 2 unique or mix)
        _choices = PickThreeChoices();
    }

    private MeteoriteId[] PickThreeChoices()
    {
        // Pool: unique meteorites only (no fragments, no Gold rarity)
        var pool = MeteoriteDatabase.All.Values
            .Where(m => !m.Stackable && m.Rarity != MeteoriteRarity.Gold)
            .Select(m => m.Id)
            .ToList();

        var picked = new List<MeteoriteId>();
        while (picked.Count < 3 && pool.Count > 0)
        {
            int idx = Random.Shared.Next(pool.Count);
            picked.Add(pool[idx]);
            pool.RemoveAt(idx);
        }
        return picked.ToArray();
    }

    public override void Update(GameTime gameTime)
    {
        float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;
        _timer += dt;
        _particles.Update(dt);

        // Ambient particles
        if (_timer % 0.1f < dt)
        {
            float x = Random.Shared.Next(Game1.ScreenWidth);
            _particles.Emit(
                new Vector2(x, Game1.ScreenHeight + 5),
                new Vector2((float)(Random.Shared.NextDouble() * 16 - 8), -25f - (float)Random.Shared.NextDouble() * 15f),
                new Color(180, 150, 90) * 0.25f,
                2f + (float)Random.Shared.NextDouble() * 2f,
                2f + (float)Random.Shared.NextDouble() * 3f
            );
        }

        if (_selected)
        {
            _selectTimer -= dt;
            if (_selectTimer <= 0)
            {
                Game1.InitialMeteoriteId = _choices[_selectedIndex];
                SceneManager.ChangeScene(new VillageScene());
            }
            return;
        }

        if (_timer > 1.5f)
        {
            if (InputManager.IsKeyPressed(Keys.Left) || InputManager.IsKeyPressed(Keys.A))
                _selectedIndex = Math.Max(0, _selectedIndex - 1);
            if (InputManager.IsKeyPressed(Keys.Right) || InputManager.IsKeyPressed(Keys.D))
                _selectedIndex = Math.Min(_choices.Length - 1, _selectedIndex + 1);

            if (InputManager.IsKeyPressed(Keys.Enter) || InputManager.IsKeyPressed(Keys.Space) || InputManager.IsLeftClick())
            {
                _selected = true;
                _selectTimer = 0.6f;

                var info = MeteoriteDatabase.Get(_choices[_selectedIndex]);
                float cardCx = GetCardCenterX(_selectedIndex);
                _particles.EmitBurst(new Vector2(cardCx, 420), 25, info.MainColor, 180f, 0.5f, 2.5f);
            }
        }
    }

    private float GetCardCenterX(int index)
    {
        int cardW = 200;
        int gap = 24;
        int totalW = cardW * 3 + gap * 2;
        int startX = (Game1.ScreenWidth - totalW) / 2;
        return startX + index * (cardW + gap) + cardW / 2f;
    }

    public override void Draw(SpriteBatch spriteBatch)
    {
        GraphicsDevice.Clear(new Color(8, 6, 4));

        spriteBatch.Begin(samplerState: SamplerState.PointClamp);

        _particles.Draw(spriteBatch);

        // Title
        if (_timer > 0.3f)
        {
            float alpha = MathF.Min(1f, (_timer - 0.3f) * 2f);
            string title = "초기 운석 선택";
            var titleSize = Fonts.Title.MeasureString(title);
            spriteBatch.DrawString(Fonts.Title, title,
                new Vector2(Game1.ScreenWidth / 2f - titleSize.X / 2f + 1, 81),
                new Color(0, 0, 0) * alpha * 0.5f);
            spriteBatch.DrawString(Fonts.Title, title,
                new Vector2(Game1.ScreenWidth / 2f - titleSize.X / 2f, 80),
                new Color(220, 190, 130) * alpha);
        }

        // Flavor text
        if (_timer > 0.8f)
        {
            float alpha = MathF.Min(1f, (_timer - 0.8f) * 2f);
            DrawCenteredText(spriteBatch, "하나의 운석이 그대를 부른다...", 135, new Color(150, 130, 95) * alpha, 0.7f);
        }

        // Cards
        if (_timer > 1.2f)
        {
            float cardAlpha = MathF.Min(1f, (_timer - 1.2f) * 2.5f);
            int cardW = 200;
            int cardH = 300;
            int gap = 24;
            int totalW = cardW * 3 + gap * 2;
            int startX = (Game1.ScreenWidth - totalW) / 2;
            int cardY = 180;

            for (int i = 0; i < _choices.Length; i++)
            {
                int cx = startX + i * (cardW + gap);
                bool isSelected = i == _selectedIndex;
                DrawMeteoriteCard(spriteBatch, cx, cardY, cardW, cardH, _choices[i], isSelected, cardAlpha);
            }

            // Prompt
            if (_timer > 2.5f && !_selected)
            {
                float blink = MathF.Sin(_timer * 2.5f) * 0.3f + 0.7f;
                DrawCenteredText(spriteBatch, "Enter 키로 선택", cardY + cardH + 30,
                    new Color(180, 160, 130) * blink, 0.7f);
            }
        }

        // Selection flash
        if (_selected)
        {
            float flashAlpha = MathF.Max(0, _selectTimer / 0.6f);
            spriteBatch.Draw(_pixel, new Rectangle(0, 0, Game1.ScreenWidth, Game1.ScreenHeight),
                new Color(255, 240, 200) * (1f - flashAlpha) * 0.2f);
        }

        spriteBatch.End();
    }

    private void DrawMeteoriteCard(SpriteBatch spriteBatch, int x, int y, int w, int h,
        MeteoriteId id, bool isSelected, float alpha)
    {
        var info = MeteoriteDatabase.Get(id);
        var rarityColor = MeteoriteDatabase.RarityColor(info.Rarity);

        // Card shadow
        if (isSelected)
            spriteBatch.Draw(_pixel, new Rectangle(x + 3, y + 4, w, h), new Color(0, 0, 0) * alpha * 0.5f);

        // Card background
        var bgColor = isSelected ? new Color(28, 22, 15) : new Color(18, 14, 10);
        spriteBatch.Draw(_pixel, new Rectangle(x, y, w, h), bgColor * alpha);

        // Selection highlight
        if (isSelected)
        {
            // Top accent
            spriteBatch.Draw(_pixel, new Rectangle(x, y, w, 2), rarityColor * alpha * 0.7f);
            // Subtle glow
            float pulse = MathF.Sin(_timer * 3f) * 0.05f + 0.08f;
            spriteBatch.Draw(_pixel, new Rectangle(x, y, w, h), rarityColor * pulse * alpha);
        }

        // Border
        DrawRectOutline(spriteBatch, new Rectangle(x, y, w, h),
            isSelected ? rarityColor * alpha * 0.6f : new Color(70, 60, 40) * alpha, 1);

        // Large icon area
        int iconSize = 48;
        int iconX = x + w / 2 - iconSize / 2;
        int iconY = y + 30;
        spriteBatch.Draw(_pixel, new Rectangle(iconX, iconY, iconSize, iconSize), new Color(12, 10, 6) * alpha);
        DrawRectOutline(spriteBatch, new Rectangle(iconX, iconY, iconSize, iconSize), rarityColor * alpha * 0.4f, 1);

        // Inner icon
        spriteBatch.Draw(_pixel, new Rectangle(iconX + 8, iconY + 8, iconSize - 16, iconSize - 16), info.MainColor * alpha * 0.7f);
        spriteBatch.Draw(_pixel, new Rectangle(iconX + 14, iconY + 14, iconSize - 28, iconSize - 28), Color.White * alpha * 0.25f);

        // Sparkle for unique items
        if (!info.Stackable && isSelected)
        {
            float sparkle = MathF.Sin(_timer * 5f);
            if (sparkle > 0.3f)
            {
                int scx = iconX + iconSize / 2;
                int scy = iconY + iconSize / 2;
                spriteBatch.Draw(_pixel, new Rectangle(scx - 1, scy - 10, 2, 4), info.MainColor * alpha * 0.8f);
                spriteBatch.Draw(_pixel, new Rectangle(scx + 8, scy - 1, 4, 2), info.MainColor * alpha * 0.8f);
                spriteBatch.Draw(_pixel, new Rectangle(scx - 11, scy - 1, 4, 2), info.MainColor * alpha * 0.8f);
                spriteBatch.Draw(_pixel, new Rectangle(scx - 1, scy + 8, 2, 4), info.MainColor * alpha * 0.8f);
            }
        }

        // Rarity label
        string rarityName = SanitizeForFont(Fonts.Game, $"[{MeteoriteDatabase.RarityName(info.Rarity)}]");
        var raritySize = Fonts.Game.MeasureString(rarityName);
        spriteBatch.DrawString(Fonts.Game, rarityName,
            new Vector2(x + w / 2f - raritySize.X * 0.55f / 2f, iconY + iconSize + 12),
            rarityColor * alpha, 0, Vector2.Zero, 0.55f, SpriteEffects.None, 0);

        // Name
        string name = SanitizeForFont(Fonts.Game, info.Name);
        var nameSize = Fonts.Game.MeasureString(name);
        float nameScale = 0.8f;
        // Shrink if too wide
        if (nameSize.X * nameScale > w - 20)
            nameScale = (w - 20) / nameSize.X;
        var nameColor = isSelected ? new Color(255, 245, 210) * alpha : new Color(200, 190, 160) * alpha;
        spriteBatch.DrawString(Fonts.Game, name,
            new Vector2(x + w / 2f - nameSize.X * nameScale / 2f, iconY + iconSize + 30),
            nameColor, 0, Vector2.Zero, nameScale, SpriteEffects.None, 0);

        // Divider
        int divY = iconY + iconSize + 55;
        spriteBatch.Draw(_pixel, new Rectangle(x + 16, divY, w - 32, 1), new Color(100, 80, 50) * alpha * 0.4f);

        // Description (wrapped)
        string desc = SanitizeForFont(Fonts.Game, info.Description);
        DrawWrappedText(spriteBatch, desc, x + 16, divY + 10, w - 32, new Color(210, 200, 175) * alpha, 0.6f);

        // Stackable info
        if (info.Stackable)
        {
            string stackInfo = SanitizeForFont(Fonts.Game, "중첩 가능");
            var stackSize = Fonts.Game.MeasureString(stackInfo);
            spriteBatch.DrawString(Fonts.Game, stackInfo,
                new Vector2(x + w / 2f - stackSize.X * 0.5f / 2f, y + h - 30),
                new Color(140, 130, 100) * alpha, 0, Vector2.Zero, 0.5f, SpriteEffects.None, 0);
        }
        else
        {
            string uniqueInfo = SanitizeForFont(Fonts.Game, "고유 운석");
            var uniqueSize = Fonts.Game.MeasureString(uniqueInfo);
            spriteBatch.DrawString(Fonts.Game, uniqueInfo,
                new Vector2(x + w / 2f - uniqueSize.X * 0.5f / 2f, y + h - 30),
                rarityColor * alpha * 0.7f, 0, Vector2.Zero, 0.5f, SpriteEffects.None, 0);
        }

        // Bottom accent for selected
        if (isSelected)
            spriteBatch.Draw(_pixel, new Rectangle(x + 20, y + h - 3, w - 40, 2), rarityColor * alpha * 0.4f);
    }

    private void DrawWrappedText(SpriteBatch spriteBatch, string text, int x, int y, int maxWidth, Color color, float scale)
    {
        float lineHeight = Fonts.Game.LineSpacing * scale;
        float currentX = 0;
        float currentY = 0;

        foreach (char c in text)
        {
            string ch = c.ToString();
            var charSize = Fonts.Game.MeasureString(ch) * scale;
            if (currentX + charSize.X > maxWidth && c != ' ')
            {
                currentX = 0;
                currentY += lineHeight;
            }
            spriteBatch.DrawString(Fonts.Game, ch, new Vector2(x + currentX, y + currentY), color, 0, Vector2.Zero, scale, SpriteEffects.None, 0);
            currentX += charSize.X;
        }
    }

    private void DrawCenteredText(SpriteBatch spriteBatch, string text, int y, Color color, float scale = 0.7f)
    {
        text = SanitizeForFont(Fonts.Game, text);
        var size = Fonts.Game.MeasureString(text);
        spriteBatch.DrawString(Fonts.Game, text,
            new Vector2(Game1.ScreenWidth / 2f - size.X * scale / 2f, y),
            color, 0, Vector2.Zero, scale, SpriteEffects.None, 0);
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
