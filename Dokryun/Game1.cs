using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Dokryun.Engine;
using Dokryun.Scenes;

namespace Dokryun;

public enum CharacterClass
{
    Swordsman,  // 검사
    Archer      // 궁수
}

public class Game1 : Game
{
    private GraphicsDeviceManager _graphics;
    private SpriteBatch _spriteBatch;
    private SceneManager _sceneManager;

    public const int ScreenWidth = 1280;
    public const int ScreenHeight = 720;

    public static CharacterClass SelectedClass { get; set; } = CharacterClass.Archer;

    public Game1()
    {
        _graphics = new GraphicsDeviceManager(this);
        Content.RootDirectory = "Content";
        IsMouseVisible = true;

        _graphics.PreferredBackBufferWidth = ScreenWidth;
        _graphics.PreferredBackBufferHeight = ScreenHeight;
        IsFixedTimeStep = true;
        TargetElapsedTime = TimeSpan.FromSeconds(1.0 / 60.0); // 60fps
    }

    protected override void Initialize()
    {
        Window.Title = "독련 (Dokryun)";
        base.Initialize();
    }

    protected override void LoadContent()
    {
        _spriteBatch = new SpriteBatch(GraphicsDevice);
        Fonts.Load(Content);
        AudioManager.Initialize();
        _sceneManager = new SceneManager(Content, GraphicsDevice);
        _sceneManager.ChangeScene(new TitleScene());
    }

    protected override void Update(GameTime gameTime)
    {
        InputManager.Update();
        AudioManager.Update((float)gameTime.ElapsedGameTime.TotalSeconds);
        _sceneManager.Update(gameTime);
        base.Update(gameTime);
    }

    protected override void Draw(GameTime gameTime)
    {
        _sceneManager.Draw(_spriteBatch);
        base.Draw(gameTime);
    }
}
