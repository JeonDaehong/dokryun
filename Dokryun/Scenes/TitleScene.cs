using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace Dokryun.Scenes;

public class TitleScene : Scene
{
    private SpriteFont _font;
    private MouseState _prevMouse;

    private Rectangle _startButton;
    private Rectangle _exitButton;

    private const int ButtonWidth = 200;
    private const int ButtonHeight = 50;

    private Texture2D _pixel;

    protected override void LoadContent()
    {
        _font = Content.Load<SpriteFont>("Fonts/GameFont");

        _pixel = new Texture2D(GraphicsDevice, 1, 1);
        _pixel.SetData(new[] { Color.White });

        int centerX = GraphicsDevice.Viewport.Width / 2 - ButtonWidth / 2;
        int centerY = GraphicsDevice.Viewport.Height / 2;

        _startButton = new Rectangle(centerX, centerY - 10, ButtonWidth, ButtonHeight);
        _exitButton = new Rectangle(centerX, centerY + 60, ButtonWidth, ButtonHeight);

        _prevMouse = Mouse.GetState();
    }

    public override void Update(GameTime gameTime)
    {
        var mouse = Mouse.GetState();
        var mousePos = new Point(mouse.X, mouse.Y);

        bool clicked = mouse.LeftButton == ButtonState.Released &&
                       _prevMouse.LeftButton == ButtonState.Pressed;

        if (clicked)
        {
            if (_startButton.Contains(mousePos))
            {
                SceneManager.ChangeScene(new MapScene(), fadeDuration: 0.8f);
            }
            else if (_exitButton.Contains(mousePos))
            {
                // Exit game
                System.Environment.Exit(0);
            }
        }

        _prevMouse = mouse;
    }

    public override void Draw(SpriteBatch spriteBatch)
    {
        GraphicsDevice.Clear(new Color(20, 12, 28));

        spriteBatch.Begin();

        // Title
        string title = "독련";
        var titleSize = _font.MeasureString(title);
        spriteBatch.DrawString(_font, title,
            new Vector2(GraphicsDevice.Viewport.Width / 2 - titleSize.X / 2,
                        GraphicsDevice.Viewport.Height / 2 - 120),
            Color.White);

        // Buttons
        var mouse = Mouse.GetState();
        var mousePos = new Point(mouse.X, mouse.Y);

        DrawButton(spriteBatch, _startButton, "게임 시작", _startButton.Contains(mousePos));
        DrawButton(spriteBatch, _exitButton, "게임 종료", _exitButton.Contains(mousePos));

        spriteBatch.End();
    }

    private void DrawButton(SpriteBatch spriteBatch, Rectangle rect, string text, bool hovered)
    {
        Color bgColor = hovered ? new Color(80, 60, 100) : new Color(50, 35, 65);
        Color borderColor = hovered ? new Color(180, 150, 220) : new Color(120, 100, 150);

        // Background
        spriteBatch.Draw(_pixel, rect, bgColor);

        // Border (top, bottom, left, right)
        int b = 2;
        spriteBatch.Draw(_pixel, new Rectangle(rect.X, rect.Y, rect.Width, b), borderColor);
        spriteBatch.Draw(_pixel, new Rectangle(rect.X, rect.Bottom - b, rect.Width, b), borderColor);
        spriteBatch.Draw(_pixel, new Rectangle(rect.X, rect.Y, b, rect.Height), borderColor);
        spriteBatch.Draw(_pixel, new Rectangle(rect.Right - b, rect.Y, b, rect.Height), borderColor);

        // Text centered
        var textSize = _font.MeasureString(text);
        var textPos = new Vector2(
            rect.X + (rect.Width - textSize.X) / 2,
            rect.Y + (rect.Height - textSize.Y) / 2);
        spriteBatch.DrawString(_font, text, textPos, Color.White);
    }
}
