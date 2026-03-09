using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Dokryun.Engine;
using Dokryun.Entities;

namespace Dokryun.Scenes;

public class VillageScene : Scene
{
    private Texture2D _pixel;
    private float _timer;
    private ParticleSystem _particles;
    private Camera _camera;
    private Player _player;

    // NPC
    private Vector2 _chiefPos = new(400, 350);
    private const float InteractRange = 50f;

    // Dialog state
    private bool _talkingToChief;
    private bool _showProvinces;
    private int _selectedProvince;
    private float _dialogTimer;

    // Village bounds
    private const float BoundsLeft = 80;
    private const float BoundsRight = 720;
    private const float BoundsTop = 150;
    private const float BoundsBottom = 550;

    private static readonly string[] Provinces = {
        "충청도", "경상도", "전라도", "강원도",
        "경기도", "황해도", "평안도", "함경도"
    };

    // Buildings (world positions: x, y, width, height)
    private static readonly Rectangle[] Buildings = {
        new(100, 200, 100, 70),
        new(550, 220, 90, 60),
        new(250, 160, 80, 50),
        new(600, 380, 110, 70),
    };

    public override void Enter()
    {
        _pixel = new Texture2D(GraphicsDevice, 1, 1);
        _pixel.SetData(new[] { Color.White });
        _particles = new ParticleSystem(500);
        _camera = new Camera(GraphicsDevice.Viewport) { Zoom = 1.5f };

        _player = new Player();
        if (Game1.SelectedClass == CharacterClass.Swordsman)
        {
            _player.IsSwordsman = true;
            _player.LoadAnimations(Content.Load<Texture2D>("Sprites/Move"), Content.Load<Texture2D>("Sprites/Idle"), Content.Load<Texture2D>("Sprites/attack"));
        }
        _player.Position = new Vector2(400, 450);
        _player.Speed = 150f;
    }

    public override void Update(GameTime gameTime)
    {
        float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;
        _timer += dt;

        // Ambient leaf particles (world space)
        if (_timer % 0.2f < dt)
        {
            float wx = _player.Position.X + Random.Shared.Next(-300, 300);
            float wy = _player.Position.Y - 200;
            _particles.Emit(
                new Vector2(wx, wy),
                new Vector2(15f + (float)Random.Shared.NextDouble() * 10f, 25f + (float)Random.Shared.NextDouble() * 15f),
                new Color(130, 100, 40) * 0.4f,
                2f + (float)Random.Shared.NextDouble() * 2f,
                2f + (float)Random.Shared.NextDouble() * 2f
            );
        }
        _particles.Update(dt);

        // Aim direction from mouse
        var mouseScreen = InputManager.MousePosition;
        var inverseTransform = Matrix.Invert(_camera.GetTransform());
        var mouseWorld = Vector2.Transform(mouseScreen, inverseTransform);
        var aimDir = mouseWorld - _player.Position;
        if (aimDir.LengthSquared() > 0) aimDir.Normalize();
        _player.AimDirection = aimDir;

        if (_showProvinces)
        {
            UpdateProvinceSelection();
        }
        else if (_talkingToChief)
        {
            _dialogTimer += dt;
            if (_dialogTimer > 0.3f && (InputManager.IsKeyPressed(Keys.E) || InputManager.IsKeyPressed(Keys.Enter) || InputManager.IsKeyPressed(Keys.Space)))
                _showProvinces = true;
            if (InputManager.IsKeyPressed(Keys.Escape))
                _talkingToChief = false;
        }
        else
        {
            // Player movement
            _player.Update(gameTime);

            // Clamp to bounds + building collision
            var pos = _player.Position;
            pos.X = Math.Clamp(pos.X, BoundsLeft, BoundsRight);
            pos.Y = Math.Clamp(pos.Y, BoundsTop, BoundsBottom);

            // Simple building collision
            foreach (var b in Buildings)
            {
                var inflated = new Rectangle(b.X - 12, b.Y - 12, b.Width + 24, b.Height + 24);
                if (inflated.Contains((int)pos.X, (int)pos.Y))
                {
                    // Push out of building
                    float cx = b.X + b.Width / 2f;
                    float cy = b.Y + b.Height / 2f;
                    float dx = pos.X - cx;
                    float dy = pos.Y - cy;
                    if (MathF.Abs(dx) / b.Width > MathF.Abs(dy) / b.Height)
                        pos.X = dx > 0 ? inflated.Right : inflated.Left;
                    else
                        pos.Y = dy > 0 ? inflated.Bottom : inflated.Top;
                }
            }
            _player.Position = pos;

            // Interact with chief
            if (Vector2.Distance(_player.Position, _chiefPos) < InteractRange && InputManager.IsKeyPressed(Keys.E))
            {
                _talkingToChief = true;
                _dialogTimer = 0;
            }
        }

        _camera.Follow(_player.Position, dt);
        _camera.Update(dt);
    }

    private void UpdateProvinceSelection()
    {
        if (InputManager.IsKeyPressed(Keys.W) || InputManager.IsKeyPressed(Keys.Up))
            _selectedProvince = Math.Max(0, _selectedProvince - 1);
        if (InputManager.IsKeyPressed(Keys.S) || InputManager.IsKeyPressed(Keys.Down))
            _selectedProvince = Math.Min(Provinces.Length - 1, _selectedProvince + 1);

        if (InputManager.IsKeyPressed(Keys.Enter) || InputManager.IsKeyPressed(Keys.Space) || InputManager.IsKeyPressed(Keys.E))
        {
            if (_selectedProvince == 0)
                SceneManager.ChangeScene(new ChungcheongVillageScene());
        }

        if (InputManager.IsKeyPressed(Keys.Escape))
        {
            _showProvinces = false;
            _talkingToChief = false;
        }
    }

    public override void Draw(SpriteBatch spriteBatch)
    {
        GraphicsDevice.Clear(new Color(35, 50, 25));

        // === World space ===
        spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp, null, null, null, _camera.GetTransform());

        // Ground
        spriteBatch.Draw(_pixel, new Rectangle(0, 0, 800, 700), new Color(55, 70, 35));

        // Dirt path
        spriteBatch.Draw(_pixel, new Rectangle(370, 100, 60, 500), new Color(80, 65, 38));
        spriteBatch.Draw(_pixel, new Rectangle(150, 340, 500, 40), new Color(80, 65, 38));

        // Grass patches
        for (int gx = 0; gx < 800; gx += 40)
        for (int gy = 0; gy < 700; gy += 40)
        {
            if (((gx + gy) * 7) % 13 < 4)
                spriteBatch.Draw(_pixel, new Rectangle(gx + 5, gy + 5, 8, 4), new Color(60, 80, 30) * 0.4f);
        }

        _particles.Draw(spriteBatch);

        // Y-sort: buildings, NPC, player
        var drawList = new List<(float y, Action draw)>();

        // Buildings
        foreach (var b in Buildings)
        {
            var bCopy = b;
            drawList.Add((b.Y + b.Height, () => DrawBuilding(spriteBatch, bCopy)));
        }

        // Chief NPC
        drawList.Add((_chiefPos.Y, () => DrawChief(spriteBatch)));

        // Player
        drawList.Add((_player.Position.Y, () =>
        {
            _player.Draw(spriteBatch);
        }));

        drawList.Sort((a, b) => a.y.CompareTo(b.y));
        foreach (var item in drawList)
            item.draw();

        // Interact prompt (world space)
        if (!_talkingToChief && !_showProvinces && Vector2.Distance(_player.Position, _chiefPos) < InteractRange)
        {
            float bob = MathF.Sin(_timer * 3f) * 2f;
            var promptPos = _chiefPos + new Vector2(0, -50 + bob);
            spriteBatch.Draw(_pixel, new Rectangle((int)promptPos.X - 7, (int)promptPos.Y - 5, 14, 12),
                new Color(40, 35, 25));
            DrawRectOutline(spriteBatch, new Rectangle((int)promptPos.X - 7, (int)promptPos.Y - 5, 14, 12),
                new Color(200, 170, 80), 1);
        }

        spriteBatch.End();

        // === Screen space UI ===
        spriteBatch.Begin(samplerState: SamplerState.PointClamp);

        // Village name
        float fadeIn = MathF.Min(1f, _timer * 2f);
        string villageName = SanitizeForFont(Fonts.Title, "마을");
        var nameSize = Fonts.Title.MeasureString(villageName);
        spriteBatch.DrawString(Fonts.Title, villageName,
            new Vector2(Game1.ScreenWidth / 2f - nameSize.X * 0.8f / 2f, 16),
            new Color(200, 180, 130) * fadeIn, 0, Vector2.Zero, 0.8f, SpriteEffects.None, 0);

        if (_talkingToChief && !_showProvinces)
            DrawDialog(spriteBatch, "어디로 가시겠소?");

        if (_showProvinces)
            DrawProvinceSelection(spriteBatch);

        spriteBatch.End();
    }

    private void DrawChief(SpriteBatch spriteBatch)
    {
        int x = (int)_chiefPos.X;
        int y = (int)_chiefPos.Y;
        var bodyColor = new Color(120, 100, 70);
        var headColor = new Color(200, 170, 120);
        var hatColor = new Color(50, 40, 30);

        DrawShadow(spriteBatch, _chiefPos, 12);
        spriteBatch.Draw(_pixel, new Rectangle(x - 10, y - 5, 20, 25), bodyColor);
        spriteBatch.Draw(_pixel, new Rectangle(x - 1, y - 5, 2, 25), new Color(100, 80, 50));
        spriteBatch.Draw(_pixel, new Rectangle(x - 7, y - 18, 14, 14), headColor);
        spriteBatch.Draw(_pixel, new Rectangle(x - 14, y - 22, 28, 4), hatColor);
        spriteBatch.Draw(_pixel, new Rectangle(x - 5, y - 30, 10, 10), hatColor);
        spriteBatch.Draw(_pixel, new Rectangle(x - 3, y - 12, 2, 2), new Color(30, 20, 15));
        spriteBatch.Draw(_pixel, new Rectangle(x + 1, y - 12, 2, 2), new Color(30, 20, 15));

        string label = SanitizeForFont(Fonts.Game, "촌장");
        var labelSize = Fonts.Game.MeasureString(label);
        spriteBatch.DrawString(Fonts.Game, label,
            new Vector2(x - labelSize.X * 0.5f / 2f, y - 40),
            new Color(220, 200, 150), 0, Vector2.Zero, 0.5f, SpriteEffects.None, 0);
    }

    private void DrawBuilding(SpriteBatch spriteBatch, Rectangle b)
    {
        var wallColor = new Color(70, 55, 35);
        spriteBatch.Draw(_pixel, b, wallColor);
        int roofOverhang = 12;
        spriteBatch.Draw(_pixel, new Rectangle(b.X - roofOverhang, b.Y - 6, b.Width + roofOverhang * 2, 10), new Color(50, 40, 30));
        spriteBatch.Draw(_pixel, new Rectangle(b.X - roofOverhang + 3, b.Y - 11, b.Width + roofOverhang * 2 - 6, 7), new Color(60, 48, 35));
        spriteBatch.Draw(_pixel, new Rectangle(b.X + b.Width / 2 - 6, b.Y + b.Height - 18, 12, 18), new Color(35, 28, 18));
        spriteBatch.Draw(_pixel, new Rectangle(b.X + 8, b.Y + 12, 10, 8), new Color(80, 75, 50));
    }

    private void DrawShadow(SpriteBatch spriteBatch, Vector2 pos, int radius)
    {
        spriteBatch.Draw(_pixel,
            new Rectangle((int)(pos.X - radius), (int)(pos.Y + 8), radius * 2, radius / 2),
            new Color(0, 0, 0) * 0.3f);
    }

    private void DrawDialog(SpriteBatch spriteBatch, string text)
    {
        int boxW = 500;
        int boxH = 80;
        int bx = (Game1.ScreenWidth - boxW) / 2;
        int by = Game1.ScreenHeight - boxH - 40;

        spriteBatch.Draw(_pixel, new Rectangle(bx, by, boxW, boxH), new Color(20, 16, 12) * 0.9f);
        DrawRectOutline(spriteBatch, new Rectangle(bx, by, boxW, boxH), new Color(140, 120, 80), 2);

        string speaker = SanitizeForFont(Fonts.Game, "촌장");
        spriteBatch.DrawString(Fonts.Game, speaker, new Vector2(bx + 12, by + 8), new Color(220, 190, 120),
            0, Vector2.Zero, 0.7f, SpriteEffects.None, 0);

        text = SanitizeForFont(Fonts.Game, text);
        spriteBatch.DrawString(Fonts.Game, text, new Vector2(bx + 12, by + 35), new Color(200, 190, 170),
            0, Vector2.Zero, 0.7f, SpriteEffects.None, 0);

        float blink = MathF.Sin(_timer * 4f) * 0.4f + 0.6f;
        spriteBatch.Draw(_pixel, new Rectangle(bx + boxW - 20, by + boxH - 15, 6, 6), new Color(180, 160, 120) * blink);
    }

    private void DrawProvinceSelection(SpriteBatch spriteBatch)
    {
        spriteBatch.Draw(_pixel, new Rectangle(0, 0, Game1.ScreenWidth, Game1.ScreenHeight), new Color(0, 0, 0) * 0.5f);

        string title = SanitizeForFont(Fonts.Title, "조선 팔도");
        var titleSize = Fonts.Title.MeasureString(title);
        spriteBatch.DrawString(Fonts.Title, title,
            new Vector2(Game1.ScreenWidth / 2f - titleSize.X / 2f, 100), new Color(220, 190, 120));

        DrawCenteredText(spriteBatch, "탐험할 지역을 선택하시오", 160, new Color(160, 140, 100));

        int startY = 220;
        int gap = 36;

        for (int i = 0; i < Provinces.Length; i++)
        {
            bool isSelected = i == _selectedProvince;
            bool isAvailable = i == 0;

            int itemY = startY + i * gap;
            string text = SanitizeForFont(Fonts.Game, Provinces[i]);

            Color textColor = isSelected && isAvailable ? new Color(255, 220, 120)
                : isAvailable ? new Color(200, 180, 130)
                : new Color(60, 50, 40);

            if (isSelected)
            {
                int boxW = 200;
                int bxx = Game1.ScreenWidth / 2 - boxW / 2;
                spriteBatch.Draw(_pixel, new Rectangle(bxx, itemY - 2, boxW, 28), new Color(40, 35, 25));
                DrawRectOutline(spriteBatch, new Rectangle(bxx, itemY - 2, boxW, 28),
                    isAvailable ? new Color(200, 170, 80) : new Color(60, 50, 40), 1);
                if (isAvailable)
                    spriteBatch.Draw(_pixel, new Rectangle(bxx - 12, itemY + 6, 8, 2), new Color(200, 170, 80));
            }

            var textSize = Fonts.Game.MeasureString(text);
            spriteBatch.DrawString(Fonts.Game, text,
                new Vector2(Game1.ScreenWidth / 2f - textSize.X * 0.8f / 2f, itemY),
                textColor, 0, Vector2.Zero, 0.8f, SpriteEffects.None, 0);

            if (!isAvailable)
            {
                int lockX = Game1.ScreenWidth / 2 + (int)(textSize.X * 0.8f / 2f) + 8;
                spriteBatch.Draw(_pixel, new Rectangle(lockX, itemY + 2, 8, 10), new Color(60, 50, 40));
                DrawRectOutline(spriteBatch, new Rectangle(lockX + 1, itemY - 2, 6, 6), new Color(60, 50, 40), 1);
            }
        }

        DrawCenteredText(spriteBatch, "[ ESC : 뒤로 ]", Game1.ScreenHeight - 60, new Color(120, 100, 80));
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
