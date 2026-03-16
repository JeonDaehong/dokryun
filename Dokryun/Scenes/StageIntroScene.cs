using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Dokryun.Scenes;

public class StageIntroScene : Scene
{
    private SpriteFont _font;
    private Texture2D _pixel;
    private string _stageText;
    private float _timer;
    private bool _transitioning;

    private const float DisplayDuration = 1.8f;
    private const float FadeInTime = 0.4f;
    private const float FadeOutTime = 0.4f;

    public StageIntroScene(string stageId)
    {
        _stageText = $"Stage {stageId}";
    }

    protected override void LoadContent()
    {
        _font = Content.Load<SpriteFont>("Fonts/GameFont");
        _pixel = new Texture2D(GraphicsDevice, 1, 1);
        _pixel.SetData(new[] { Color.White });
    }

    public override void Update(GameTime gameTime)
    {
        if (_transitioning) return;

        _timer += (float)gameTime.ElapsedGameTime.TotalSeconds;

        if (_timer >= DisplayDuration)
        {
            _transitioning = true;
            SceneManager.ChangeScene(new GameplayScene(), fadeDuration: 0.8f);
        }
    }

    public override void Draw(SpriteBatch spriteBatch)
    {
        GraphicsDevice.Clear(new Color(5, 3, 8));

        spriteBatch.Begin(samplerState: SamplerState.PointClamp);

        int sw = GraphicsDevice.Viewport.Width;
        int sh = GraphicsDevice.Viewport.Height;

        // Calculate alpha for fade in/out
        float alpha;
        if (_timer < FadeInTime)
            alpha = _timer / FadeInTime;
        else if (_timer > DisplayDuration - FadeOutTime)
            alpha = (DisplayDuration - _timer) / FadeOutTime;
        else
            alpha = 1f;
        alpha = MathHelper.Clamp(alpha, 0f, 1f);

        // Decorative line (top)
        int lineW = 300;
        int lineY = sh / 2 - 40;
        spriteBatch.Draw(_pixel,
            new Rectangle(sw / 2 - lineW / 2, lineY, lineW, 2),
            new Color(120, 90, 50) * alpha);

        // Stage text
        var textSize = _font.MeasureString(_stageText);
        var pos = new Vector2(sw / 2 - textSize.X / 2, sh / 2 - textSize.Y / 2);

        // Shadow
        spriteBatch.DrawString(_font, _stageText, pos + new Vector2(2, 2),
            new Color(0, 0, 0, (int)(180 * alpha)));
        // Main text
        spriteBatch.DrawString(_font, _stageText, pos,
            new Color(220, 190, 130) * alpha);

        // Decorative line (bottom)
        spriteBatch.Draw(_pixel,
            new Rectangle(sw / 2 - lineW / 2, (int)(pos.Y + textSize.Y + 12), lineW, 2),
            new Color(120, 90, 50) * alpha);

        spriteBatch.End();
    }
}
