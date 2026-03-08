using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

namespace Dokryun.Engine;

public abstract class Scene
{
    protected SceneManager SceneManager { get; private set; }
    protected ContentManager Content { get; private set; }
    protected GraphicsDevice GraphicsDevice { get; private set; }

    public void Init(SceneManager manager, ContentManager content, GraphicsDevice device)
    {
        SceneManager = manager;
        Content = content;
        GraphicsDevice = device;
    }

    public virtual void Enter() { }
    public virtual void Exit() { }
    public abstract void Update(GameTime gameTime);
    public abstract void Draw(SpriteBatch spriteBatch);
}

public class SceneManager
{
    private Scene _currentScene;
    private Scene _nextScene;
    private readonly ContentManager _content;
    private readonly GraphicsDevice _graphicsDevice;

    public Scene CurrentScene => _currentScene;

    public SceneManager(ContentManager content, GraphicsDevice graphicsDevice)
    {
        _content = content;
        _graphicsDevice = graphicsDevice;
    }

    public void ChangeScene(Scene scene)
    {
        _nextScene = scene;
    }

    public void Update(GameTime gameTime)
    {
        if (_nextScene != null)
        {
            _currentScene?.Exit();
            _currentScene = _nextScene;
            _currentScene.Init(this, _content, _graphicsDevice);
            _currentScene.Enter();
            _nextScene = null;
        }

        _currentScene?.Update(gameTime);
    }

    public void Draw(SpriteBatch spriteBatch)
    {
        _currentScene?.Draw(spriteBatch);
    }
}
