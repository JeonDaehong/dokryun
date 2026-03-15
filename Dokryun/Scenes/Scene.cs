using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

namespace Dokryun.Scenes;

public abstract class Scene
{
    protected ContentManager Content;
    protected GraphicsDevice GraphicsDevice;
    protected SceneManager SceneManager;

    public void Init(ContentManager content, GraphicsDevice graphicsDevice, SceneManager sceneManager)
    {
        Content = content;
        GraphicsDevice = graphicsDevice;
        SceneManager = sceneManager;
        LoadContent();
    }

    protected virtual void LoadContent() { }
    public abstract void Update(GameTime gameTime);
    public abstract void Draw(SpriteBatch spriteBatch);
}
