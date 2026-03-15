using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

namespace Dokryun.Scenes;

public class SceneManager
{
    private readonly ContentManager _content;
    private readonly GraphicsDevice _graphicsDevice;

    private Scene _currentScene;
    private Scene _nextScene;

    private float _fadeAlpha;
    private float _fadeDuration;
    private bool _isFading;
    private bool _fadeIn; // true = fading in (black -> clear), false = fading out (clear -> black)
    private Texture2D _pixel;

    public SceneManager(ContentManager content, GraphicsDevice graphicsDevice)
    {
        _content = content;
        _graphicsDevice = graphicsDevice;

        _pixel = new Texture2D(graphicsDevice, 1, 1);
        _pixel.SetData(new[] { Color.White });
    }

    public void ChangeScene(Scene scene, float fadeDuration = 0f)
    {
        if (fadeDuration > 0f && _currentScene != null)
        {
            _nextScene = scene;
            _fadeDuration = fadeDuration;
            _fadeAlpha = 0f;
            _isFading = true;
            _fadeIn = false; // fade out first
        }
        else
        {
            scene.Init(_content, _graphicsDevice, this);
            _currentScene = scene;

            if (fadeDuration > 0f)
            {
                // No previous scene, just fade in
                _fadeAlpha = 1f;
                _fadeDuration = fadeDuration;
                _isFading = true;
                _fadeIn = true;
            }
        }
    }

    public void Update(GameTime gameTime)
    {
        float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;

        if (_isFading)
        {
            float speed = 1f / _fadeDuration;

            if (!_fadeIn)
            {
                // Fading out (to black)
                _fadeAlpha += speed * dt;
                if (_fadeAlpha >= 1f)
                {
                    _fadeAlpha = 1f;
                    // Switch scene at full black
                    _nextScene.Init(_content, _graphicsDevice, this);
                    _currentScene = _nextScene;
                    _nextScene = null;
                    _fadeIn = true; // now fade in
                }
            }
            else
            {
                // Fading in (from black)
                _fadeAlpha -= speed * dt;
                if (_fadeAlpha <= 0f)
                {
                    _fadeAlpha = 0f;
                    _isFading = false;
                }
            }
        }

        _currentScene?.Update(gameTime);
    }

    public void Draw(SpriteBatch spriteBatch)
    {
        _currentScene?.Draw(spriteBatch);

        if (_isFading && _fadeAlpha > 0f)
        {
            spriteBatch.Begin();
            spriteBatch.Draw(_pixel,
                new Rectangle(0, 0, _graphicsDevice.Viewport.Width, _graphicsDevice.Viewport.Height),
                Color.Black * _fadeAlpha);
            spriteBatch.End();
        }
    }
}
