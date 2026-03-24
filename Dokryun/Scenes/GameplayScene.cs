using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Dokryun.Scenes;

public class GameplayScene : Scene
{
    private Texture2D _pixel;

    // Background colors
    private Color _skyColor = new Color(45, 30, 60);
    private Color _groundColor = new Color(35, 55, 35);
    private Color _groundDark = new Color(25, 40, 25);

    // Character position
    private Vector2 _charPos;
    private const int CharW = 32;
    private const int CharH = 48;

    private int _groundY;

    protected override void LoadContent()
    {
        _pixel = new Texture2D(GraphicsDevice, 1, 1);
        _pixel.SetData(new[] { Color.White });

        int w = GraphicsDevice.Viewport.Width;
        int h = GraphicsDevice.Viewport.Height;

        _groundY = (int)(h * 0.65f);
        _charPos = new Vector2(w / 2f - CharW / 2f, _groundY - CharH);
    }

    public override void Update(GameTime gameTime)
    {
    }

    public override void Draw(SpriteBatch spriteBatch)
    {
        int w = GraphicsDevice.Viewport.Width;
        int h = GraphicsDevice.Viewport.Height;

        GraphicsDevice.Clear(_skyColor);

        spriteBatch.Begin(samplerState: SamplerState.PointClamp);

        // Moon
        DrawFilledCircle(spriteBatch, new Vector2(w * 0.8f, h * 0.15f), 40, new Color(220, 210, 180, 180));

        // Distant mountains
        DrawMountain(spriteBatch, 0, _groundY, w, new Color(30, 25, 45));
        DrawMountain(spriteBatch, w / 4, _groundY, w / 2, new Color(35, 28, 50));

        // Ground
        spriteBatch.Draw(_pixel, new Rectangle(0, _groundY, w, h - _groundY), _groundColor);
        spriteBatch.Draw(_pixel, new Rectangle(0, _groundY + 40, w, h - _groundY - 40), _groundDark);

        // Ground line detail
        for (int x = 0; x < w; x += 60)
        {
            spriteBatch.Draw(_pixel, new Rectangle(x, _groundY, 30, 2), new Color(50, 70, 50));
        }

        // Character
        DrawCharacter(spriteBatch);

        spriteBatch.End();
    }

    private void DrawCharacter(SpriteBatch sb)
    {
        int x = (int)_charPos.X;
        int y = (int)_charPos.Y;

        Color robeColor = new Color(60, 45, 90);
        Color robeLight = new Color(80, 60, 120);
        Color skinColor = new Color(220, 190, 150);
        Color hairColor = new Color(30, 20, 15);

        // Head
        sb.Draw(_pixel, new Rectangle(x + 10, y, 12, 12), skinColor);

        // Hair + 상투
        sb.Draw(_pixel, new Rectangle(x + 9, y - 2, 14, 5), hairColor);
        sb.Draw(_pixel, new Rectangle(x + 13, y - 6, 6, 6), hairColor);

        // 도포 (robe)
        sb.Draw(_pixel, new Rectangle(x + 6, y + 12, 20, 28), robeColor);
        sb.Draw(_pixel, new Rectangle(x + 12, y + 12, 8, 28), robeLight);

        // Belt
        sb.Draw(_pixel, new Rectangle(x + 6, y + 24, 20, 3), new Color(150, 120, 60));

        // Legs
        sb.Draw(_pixel, new Rectangle(x + 8, y + 40, 6, 8), robeColor);
        sb.Draw(_pixel, new Rectangle(x + 18, y + 40, 6, 8), robeColor);

        // Feet
        sb.Draw(_pixel, new Rectangle(x + 7, y + 46, 8, 3), new Color(80, 60, 40));
        sb.Draw(_pixel, new Rectangle(x + 17, y + 46, 8, 3), new Color(80, 60, 40));
    }

    private void DrawMountain(SpriteBatch sb, int offsetX, int baseY, int width, Color color)
    {
        int peakY = baseY - width / 3;
        int centerX = offsetX + width / 2;

        for (int row = peakY; row < baseY; row++)
        {
            float t = (float)(row - peakY) / (baseY - peakY);
            int halfW = (int)(t * width / 2);
            sb.Draw(_pixel, new Rectangle(centerX - halfW, row, halfW * 2, 1), color);
        }
    }

    private void DrawFilledCircle(SpriteBatch sb, Vector2 center, int radius, Color color)
    {
        for (int y = -radius; y <= radius; y++)
        {
            int halfW = (int)Math.Sqrt(radius * radius - y * y);
            sb.Draw(_pixel, new Rectangle((int)center.X - halfW, (int)center.Y + y, halfW * 2, 1), color);
        }
    }
}
