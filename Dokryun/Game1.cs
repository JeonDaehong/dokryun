using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Dokryun.Engine;
using Dokryun.Scenes;
using Dokryun.Systems;

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
    public static MeteoriteId? InitialMeteoriteId { get; set; }

    // Meta progression (persistent across runs)
    public static MetaProgression Meta { get; set; } = MetaProgression.Load();

    // Run results (passed from gameplay to death screen)
    public static int LastFloor { get; set; }
    public static int LastKills { get; set; }
    public static bool LastBossDefeated { get; set; }
    public static int LastGold { get; set; }

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
        try
        {
            InputManager.Update();
            AudioManager.Update((float)gameTime.ElapsedGameTime.TotalSeconds);
            _sceneManager.Update(gameTime);
        }
        catch (Exception ex)
        {
            System.IO.File.WriteAllText("crash_log.txt",
                $"[UPDATE] {DateTime.Now}\nScene: {_sceneManager.CurrentScene?.GetType().Name}\n{ex}");
            throw;
        }
        base.Update(gameTime);
    }

    protected override void Draw(GameTime gameTime)
    {
        try
        {
            _sceneManager.Draw(_spriteBatch);
        }
        catch (Exception ex)
        {
            System.IO.File.WriteAllText("crash_log.txt",
                $"[DRAW] {DateTime.Now}\nScene: {_sceneManager.CurrentScene?.GetType().Name}\n{ex}");
            throw;
        }
        base.Draw(gameTime);
    }
}
