using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace Dokryun.Scenes;

public class MapScene : Scene
{
    private SpriteFont _font;
    private Texture2D _pixel;
    private MouseState _prevMouse;

    private const int PX = 3; // chunky pixel scale

    // Province data
    private struct Province
    {
        public string Name;
        public Rectangle Bounds;
        public bool Unlocked;
        public Vector2[] LandBlocks; // pixel blocks forming the region
    }

    private Province[] _provinces;

    // Map offset (center the peninsula)
    private int _mapX, _mapY;

    // Sea animation
    private float _waveTimer;

    protected override void LoadContent()
    {
        _font = Content.Load<SpriteFont>("Fonts/GameFont");
        _pixel = new Texture2D(GraphicsDevice, 1, 1);
        _pixel.SetData(new[] { Color.White });

        int sw = GraphicsDevice.Viewport.Width;
        int sh = GraphicsDevice.Viewport.Height;
        _mapX = sw / 2 - 80;
        _mapY = sh / 2 - 200;

        BuildProvinces();
        _prevMouse = Mouse.GetState();
    }

    private void BuildProvinces()
    {
        // Korean peninsula shape built from province regions
        // Coordinates relative to _mapX, _mapY in PX units
        // The peninsula is roughly 55 wide x 140 tall in PX units

        _provinces = new Province[8];

        // 함경도 (northeast - top right)
        _provinces[0] = MakeProvince("함경도",
            new Rectangle(_mapX + 28 * PX, _mapY + 0 * PX, 30 * PX, 30 * PX),
            false, GenerateRegionBlocks(28, 0, 30, 30, 0));

        // 평안도 (northwest - top left)
        _provinces[1] = MakeProvince("평안도",
            new Rectangle(_mapX + 5 * PX, _mapY + 5 * PX, 28 * PX, 30 * PX),
            false, GenerateRegionBlocks(5, 5, 28, 30, 1));

        // 황해도 (west-center)
        _provinces[2] = MakeProvince("황해도",
            new Rectangle(_mapX + 3 * PX, _mapY + 35 * PX, 22 * PX, 20 * PX),
            false, GenerateRegionBlocks(3, 35, 22, 20, 2));

        // 강원도 (east-center)
        _provinces[3] = MakeProvince("강원도",
            new Rectangle(_mapX + 25 * PX, _mapY + 30 * PX, 25 * PX, 25 * PX),
            false, GenerateRegionBlocks(25, 30, 25, 25, 3));

        // 경기도 (center)
        _provinces[4] = MakeProvince("경기도",
            new Rectangle(_mapX + 8 * PX, _mapY + 52 * PX, 22 * PX, 16 * PX),
            false, GenerateRegionBlocks(8, 52, 22, 16, 4));

        // 충청도 (center-south) - UNLOCKED
        _provinces[5] = MakeProvince("충청도",
            new Rectangle(_mapX + 5 * PX, _mapY + 68 * PX, 28 * PX, 20 * PX),
            true, GenerateRegionBlocks(5, 68, 28, 20, 5));

        // 전라도 (southwest)
        _provinces[6] = MakeProvince("전라도",
            new Rectangle(_mapX + 2 * PX, _mapY + 88 * PX, 24 * PX, 28 * PX),
            false, GenerateRegionBlocks(2, 88, 24, 28, 6));

        // 경상도 (southeast)
        _provinces[7] = MakeProvince("경상도",
            new Rectangle(_mapX + 26 * PX, _mapY + 55 * PX, 28 * PX, 58 * PX),
            false, GenerateRegionBlocks(26, 55, 28, 58, 7));
    }

    private Province MakeProvince(string name, Rectangle bounds, bool unlocked, Vector2[] blocks)
    {
        return new Province { Name = name, Bounds = bounds, Unlocked = unlocked, LandBlocks = blocks };
    }

    // Generate pixel blocks that fill a region with organic edges
    private Vector2[] GenerateRegionBlocks(int rx, int ry, int rw, int rh, int seed)
    {
        var rng = new Random(seed * 777 + 42);
        var blocks = new List<Vector2>();

        for (int y = 0; y < rh; y++)
        {
            for (int x = 0; x < rw; x++)
            {
                // Create organic shape by trimming corners and adding noise
                float cx = (x - rw / 2f) / (rw / 2f);
                float cy = (y - rh / 2f) / (rh / 2f);
                float dist = cx * cx + cy * cy;

                // Soft ellipse with noise
                float noise = (float)(rng.NextDouble() * 0.3);
                if (dist < 0.85f + noise)
                {
                    blocks.Add(new Vector2(rx + x, ry + y));
                }
            }
        }
        return blocks.ToArray();
    }

    public override void Update(GameTime gameTime)
    {
        float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;
        _waveTimer += dt;

        var mouse = Mouse.GetState();
        var mousePos = new Point(mouse.X, mouse.Y);
        bool clicked = mouse.LeftButton == ButtonState.Released && _prevMouse.LeftButton == ButtonState.Pressed;

        if (clicked)
        {
            for (int i = 0; i < _provinces.Length; i++)
            {
                if (_provinces[i].Unlocked && _provinces[i].Bounds.Contains(mousePos))
                {
                    SceneManager.ChangeScene(new StageIntroScene("1-1"), fadeDuration: 0.6f);
                    break;
                }
            }
        }

        _prevMouse = mouse;
    }

    public override void Draw(SpriteBatch spriteBatch)
    {
        int sw = GraphicsDevice.Viewport.Width;
        int sh = GraphicsDevice.Viewport.Height;

        // Dark sea background
        GraphicsDevice.Clear(new Color(15, 25, 45));

        spriteBatch.Begin(samplerState: SamplerState.PointClamp);

        // Animated wave dots on sea
        DrawSeaWaves(spriteBatch, sw, sh);

        // Draw provinces
        var mouse = Mouse.GetState();
        var mousePos = new Point(mouse.X, mouse.Y);

        for (int i = 0; i < _provinces.Length; i++)
        {
            var prov = _provinces[i];
            bool hovered = prov.Bounds.Contains(mousePos);

            Color landColor;
            if (!prov.Unlocked)
            {
                landColor = new Color(30, 28, 32); // dark locked
            }
            else if (hovered)
            {
                landColor = new Color(120, 90, 55); // highlighted
            }
            else
            {
                landColor = new Color(85, 65, 40); // unlocked normal
            }

            // Draw land blocks
            foreach (var block in prov.LandBlocks)
            {
                int bx = _mapX + (int)block.X * PX;
                int by = _mapY + (int)block.Y * PX;
                spriteBatch.Draw(_pixel, new Rectangle(bx, by, PX, PX), landColor);
            }

            // Province border (slightly brighter edge blocks)
            Color borderColor = prov.Unlocked ? new Color(110, 85, 55) : new Color(45, 40, 50);
            DrawRegionBorder(spriteBatch, prov, borderColor);

            // Province name label
            Color textColor = prov.Unlocked ? Color.White : new Color(60, 55, 65);
            var center = new Vector2(
                prov.Bounds.X + prov.Bounds.Width / 2f,
                prov.Bounds.Y + prov.Bounds.Height / 2f);
            var nameSize = _font.MeasureString(prov.Name);

            // Text shadow
            spriteBatch.DrawString(_font, prov.Name,
                center - nameSize / 2f + new Vector2(1, 1), new Color(0, 0, 0, 150));
            spriteBatch.DrawString(_font, prov.Name,
                center - nameSize / 2f, textColor);

            // Lock icon for locked provinces
            if (!prov.Unlocked)
            {
                string lockStr = "X";
                var lockSize = _font.MeasureString(lockStr);
                spriteBatch.DrawString(_font, lockStr,
                    center - lockSize / 2f + new Vector2(0, nameSize.Y * 0.7f),
                    new Color(80, 40, 40));
            }
        }

        // Title at top
        string title = "조선팔도";
        var titleSize = _font.MeasureString(title);
        spriteBatch.DrawString(_font, title,
            new Vector2(sw / 2 - titleSize.X / 2 + 2, 28), new Color(0, 0, 0, 120));
        spriteBatch.DrawString(_font, title,
            new Vector2(sw / 2 - titleSize.X / 2, 26), new Color(200, 170, 110));

        // Subtitle
        string sub = "지역을 선택하세요";
        var subSize = _font.MeasureString(sub);
        spriteBatch.DrawString(_font, sub,
            new Vector2(sw / 2 - subSize.X / 2, 54), new Color(140, 120, 90));

        spriteBatch.End();
    }

    private void DrawSeaWaves(SpriteBatch spriteBatch, int sw, int sh)
    {
        // Sparse animated dots for sea feel
        var rng = new Random(12345);
        for (int i = 0; i < 80; i++)
        {
            int x = rng.Next(0, sw / PX) * PX;
            int y = rng.Next(0, sh / PX) * PX;
            float phase = (float)(rng.NextDouble() * Math.PI * 2);
            float alpha = 0.15f + 0.1f * (float)Math.Sin(_waveTimer * 1.5f + phase);
            spriteBatch.Draw(_pixel, new Rectangle(x, y, PX, PX),
                new Color(40, 70, 110) * alpha);
        }
    }

    private void DrawRegionBorder(SpriteBatch spriteBatch, Province prov, Color color)
    {
        // Simple: draw slightly offset blocks to create border effect
        var blockSet = new HashSet<(int, int)>();
        foreach (var b in prov.LandBlocks)
            blockSet.Add(((int)b.X, (int)b.Y));

        foreach (var block in prov.LandBlocks)
        {
            int bx = (int)block.X;
            int by = (int)block.Y;
            // Check if edge block
            if (!blockSet.Contains((bx - 1, by)) || !blockSet.Contains((bx + 1, by)) ||
                !blockSet.Contains((bx, by - 1)) || !blockSet.Contains((bx, by + 1)))
            {
                int px = _mapX + bx * PX;
                int py = _mapY + by * PX;
                spriteBatch.Draw(_pixel, new Rectangle(px, py, PX, PX), color);
            }
        }
    }
}
