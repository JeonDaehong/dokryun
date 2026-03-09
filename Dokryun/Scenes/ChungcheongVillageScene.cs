using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Dokryun.Engine;
using Dokryun.Entities;

namespace Dokryun.Scenes;

public class ChungcheongVillageScene : Scene
{
    private Texture2D _pixel;
    private float _timer;
    private ParticleSystem _particles;
    private Camera _camera;
    private Player _player;

    // Dungeon entrance
    private Vector2 _dungeonPos = new(400, 200);
    private const float InteractRange = 50f;

    // Village bounds
    private const float BoundsLeft = 80;
    private const float BoundsRight = 720;
    private const float BoundsTop = 100;
    private const float BoundsBottom = 550;

    // Buildings
    private static readonly Rectangle[] Buildings = {
        new(120, 300, 80, 55),
        new(580, 320, 90, 60),
    };

    // Rocks around dungeon entrance (impassable)
    private static readonly Rectangle DungeonRock = new(360, 160, 80, 50);

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
        _player.Position = new Vector2(400, 480);
        _player.Speed = 150f;
    }

    public override void Update(GameTime gameTime)
    {
        float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;
        _timer += dt;

        // Ambient particles
        if (_timer % 0.15f < dt)
        {
            float wx = _player.Position.X + Random.Shared.Next(-300, 300);
            float wy = _player.Position.Y - 200;
            _particles.Emit(
                new Vector2(wx, wy),
                new Vector2(10f + (float)Random.Shared.NextDouble() * 8f, 20f + (float)Random.Shared.NextDouble() * 10f),
                new Color(100, 120, 50) * 0.3f,
                2f + (float)Random.Shared.NextDouble() * 2f,
                2f + (float)Random.Shared.NextDouble() * 2f
            );
        }
        _particles.Update(dt);

        // Aim
        var mouseScreen = InputManager.MousePosition;
        var inverseTransform = Matrix.Invert(_camera.GetTransform());
        var mouseWorld = Vector2.Transform(mouseScreen, inverseTransform);
        var aimDir = mouseWorld - _player.Position;
        if (aimDir.LengthSquared() > 0) aimDir.Normalize();
        _player.AimDirection = aimDir;

        // Player movement
        _player.Update(gameTime);

        // Clamp to bounds
        var pos = _player.Position;
        pos.X = Math.Clamp(pos.X, BoundsLeft, BoundsRight);
        pos.Y = Math.Clamp(pos.Y, BoundsTop, BoundsBottom);

        // Building collision
        foreach (var b in Buildings)
        {
            var inflated = new Rectangle(b.X - 12, b.Y - 12, b.Width + 24, b.Height + 24);
            if (inflated.Contains((int)pos.X, (int)pos.Y))
            {
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

        // Dungeon rock collision (but allow approach from front/south)
        {
            var rockInflated = new Rectangle(DungeonRock.X - 8, DungeonRock.Y - 8, DungeonRock.Width + 16, DungeonRock.Height + 16);
            if (rockInflated.Contains((int)pos.X, (int)pos.Y))
            {
                float cx = DungeonRock.X + DungeonRock.Width / 2f;
                float cy = DungeonRock.Y + DungeonRock.Height / 2f;
                float dx = pos.X - cx;
                float dy = pos.Y - cy;
                if (MathF.Abs(dx) / DungeonRock.Width > MathF.Abs(dy) / DungeonRock.Height)
                    pos.X = dx > 0 ? rockInflated.Right : rockInflated.Left;
                else
                    pos.Y = dy > 0 ? rockInflated.Bottom : rockInflated.Top;
            }
        }

        _player.Position = pos;

        // Interact with dungeon entrance
        if (Vector2.Distance(_player.Position, _dungeonPos + new Vector2(0, 30)) < InteractRange
            && InputManager.IsKeyPressed(Keys.E))
        {
            SceneManager.ChangeScene(new GameplayScene());
        }

        // Back to main village
        if (InputManager.IsKeyPressed(Keys.Escape))
            SceneManager.ChangeScene(new VillageScene());

        _camera.Follow(_player.Position, dt);
        _camera.Update(dt);
    }

    public override void Draw(SpriteBatch spriteBatch)
    {
        GraphicsDevice.Clear(new Color(30, 45, 22));

        // === World space ===
        spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp, null, null, null, _camera.GetTransform());

        // Ground
        spriteBatch.Draw(_pixel, new Rectangle(0, 0, 800, 700), new Color(50, 65, 30));

        // Mountain backdrop (top area)
        for (int i = 0; i < 3; i++)
        {
            int mx = 100 + i * 250;
            int mw = 200 + (i % 2) * 80;
            int mh = 100 + (i % 3) * 30;
            var mColor = new Color(35, 48, 25);
            for (int row = 0; row < mh; row++)
            {
                float t = row / (float)mh;
                int w = (int)(mw * t);
                spriteBatch.Draw(_pixel, new Rectangle(mx - w / 2, 50 - mh + row, w, 1), mColor);
            }
        }

        // Dirt path to dungeon
        spriteBatch.Draw(_pixel, new Rectangle(370, 180, 60, 400), new Color(75, 60, 35));

        // Grass patches
        for (int gx = 0; gx < 800; gx += 35)
        for (int gy = 0; gy < 700; gy += 35)
        {
            if (((gx * 3 + gy) * 11) % 17 < 5)
                spriteBatch.Draw(_pixel, new Rectangle(gx + 3, gy + 3, 6, 3), new Color(55, 75, 28) * 0.4f);
        }

        _particles.Draw(spriteBatch);

        // Y-sorted drawing
        var drawList = new List<(float y, Action draw)>();

        // Dungeon entrance
        drawList.Add((_dungeonPos.Y + 30, () => DrawDungeonEntrance(spriteBatch)));

        // Buildings
        foreach (var b in Buildings)
        {
            var bCopy = b;
            drawList.Add((b.Y + b.Height, () => DrawBuilding(spriteBatch, bCopy)));
        }

        // Player
        drawList.Add((_player.Position.Y, () =>
        {
            _player.Draw(spriteBatch);
        }));

        drawList.Sort((a, b) => a.y.CompareTo(b.y));
        foreach (var item in drawList)
            item.draw();

        // Interact prompt near dungeon
        if (Vector2.Distance(_player.Position, _dungeonPos + new Vector2(0, 30)) < InteractRange)
        {
            float bob = MathF.Sin(_timer * 3f) * 2f;
            var promptPos = _dungeonPos + new Vector2(0, -20 + bob);
            spriteBatch.Draw(_pixel, new Rectangle((int)promptPos.X - 7, (int)promptPos.Y - 5, 14, 12),
                new Color(40, 35, 25));
            DrawRectOutline(spriteBatch, new Rectangle((int)promptPos.X - 7, (int)promptPos.Y - 5, 14, 12),
                new Color(200, 100, 50), 1);
        }

        spriteBatch.End();

        // === Screen UI ===
        spriteBatch.Begin(samplerState: SamplerState.PointClamp);

        float fadeIn = MathF.Min(1f, _timer * 2f);
        string title = SanitizeForFont(Fonts.Title, "충청도 마을");
        var titleSize = Fonts.Title.MeasureString(title);
        spriteBatch.DrawString(Fonts.Title, title,
            new Vector2(Game1.ScreenWidth / 2f - titleSize.X * 0.8f / 2f, 16),
            new Color(180, 200, 130) * fadeIn, 0, Vector2.Zero, 0.8f, SpriteEffects.None, 0);

        string subtitle = SanitizeForFont(Fonts.Game, "월악산 인근");
        var subSize = Fonts.Game.MeasureString(subtitle);
        spriteBatch.DrawString(Fonts.Game, subtitle,
            new Vector2(Game1.ScreenWidth / 2f - subSize.X * 0.55f / 2f, 55),
            new Color(140, 160, 100) * fadeIn, 0, Vector2.Zero, 0.55f, SpriteEffects.None, 0);

        // Hints
        DrawCenteredText(spriteBatch, "[ ESC : 마을로 돌아가기 ]", Game1.ScreenHeight - 30,
            new Color(120, 100, 80) * 0.6f, 0.5f);

        spriteBatch.End();
    }

    private void DrawDungeonEntrance(SpriteBatch spriteBatch)
    {
        int cx = (int)_dungeonPos.X;
        int cy = (int)_dungeonPos.Y;

        // Rock formation
        var rockColor = new Color(60, 55, 45);
        var darkRock = new Color(40, 35, 28);
        spriteBatch.Draw(_pixel, new Rectangle(cx - 40, cy - 20, 80, 50), rockColor);
        spriteBatch.Draw(_pixel, new Rectangle(cx - 45, cy - 12, 90, 8), rockColor);
        spriteBatch.Draw(_pixel, new Rectangle(cx - 35, cy - 28, 70, 10), darkRock);

        // Cave opening
        spriteBatch.Draw(_pixel, new Rectangle(cx - 18, cy - 8, 36, 38), new Color(10, 8, 5));
        spriteBatch.Draw(_pixel, new Rectangle(cx - 14, cy - 12, 28, 5), new Color(15, 12, 8));

        // Eerie glow
        float pulse = MathF.Sin(_timer * 2f) * 0.15f + 0.3f;
        spriteBatch.Draw(_pixel, new Rectangle(cx - 16, cy - 6, 32, 34), new Color(80, 40, 30) * pulse * 0.3f);

        // Torches
        DrawTorch(spriteBatch, cx - 28, cy - 5);
        DrawTorch(spriteBatch, cx + 24, cy - 5);

        // Label
        string label = SanitizeForFont(Fonts.Game, "던전 입구");
        var labelSize = Fonts.Game.MeasureString(label);
        spriteBatch.DrawString(Fonts.Game, label,
            new Vector2(cx - labelSize.X * 0.5f / 2f, cy - 42),
            new Color(220, 180, 100), 0, Vector2.Zero, 0.5f, SpriteEffects.None, 0);
    }

    private void DrawTorch(SpriteBatch spriteBatch, int x, int y)
    {
        spriteBatch.Draw(_pixel, new Rectangle(x, y, 4, 16), new Color(80, 60, 30));
        float flicker = MathF.Sin(_timer * 10f + x * 0.1f) * 2f;
        spriteBatch.Draw(_pixel, new Rectangle(x - 2, (int)(y - 6 + flicker), 8, 8), new Color(255, 150, 30) * 0.8f);
        spriteBatch.Draw(_pixel, new Rectangle(x - 1, (int)(y - 8 + flicker), 6, 5), new Color(255, 200, 60) * 0.6f);
        spriteBatch.Draw(_pixel, new Rectangle(x - 6, y - 10, 16, 20), new Color(255, 120, 30) * 0.08f);
    }

    private void DrawBuilding(SpriteBatch spriteBatch, Rectangle b)
    {
        var wallColor = new Color(65, 52, 32);
        spriteBatch.Draw(_pixel, b, wallColor);
        int ro = 10;
        spriteBatch.Draw(_pixel, new Rectangle(b.X - ro, b.Y - 5, b.Width + ro * 2, 8), new Color(50, 40, 30));
        spriteBatch.Draw(_pixel, new Rectangle(b.X - ro + 3, b.Y - 10, b.Width + ro * 2 - 6, 6), new Color(58, 46, 33));
        spriteBatch.Draw(_pixel, new Rectangle(b.X + b.Width / 2 - 5, b.Y + b.Height - 16, 10, 16), new Color(35, 28, 18));
    }

    private void DrawShadow(SpriteBatch spriteBatch, Vector2 pos, int radius)
    {
        spriteBatch.Draw(_pixel,
            new Rectangle((int)(pos.X - radius), (int)(pos.Y + 8), radius * 2, radius / 2),
            new Color(0, 0, 0) * 0.3f);
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
