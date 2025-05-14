using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

using FontStashSharp;
using ProjectMinkowski.Multiplayer.Local;
using ProjectMinkowski.Rendering;
using ProjectMinkowski.Rendering.SplitScreen;

namespace ProjectMinkowski;

public static class Program
{
    [STAThread]
    static void Main()
    {
        using var game = new ProjectMinowskiGame();
        game.Run();
    }
}

public static class Config
{
    /// <summary>
    /// The speed of light, in space units per time unit (e.g. 10 means light moves 10 units/sec).
    /// </summary>
    public static double C = 25;
}

public static class GameResources {
    public static SpriteFontBase? DefaultFont { get; set; }
    public static BasicEffect? BasicEffect { get; set; }
}

public class ProjectMinowskiGame : Game
{
    //GRAPHICS
    private FontSystem fontSystem;
    private SpriteBatch spriteBatch;
    private GraphicsDeviceManager graphics;
    private SplitScreenRenderer renderer;

    public ProjectMinowskiGame() {
        graphics = new GraphicsDeviceManager(this);
        Content.RootDirectory = "Assets";
        IsMouseVisible = true;
        graphics.PreferredBackBufferWidth = 800;
        graphics.PreferredBackBufferHeight = 400;
    }
    
    protected override void LoadContent()
    {
        spriteBatch = new SpriteBatch(GraphicsDevice);
        renderer = new SplitScreenRenderer(GraphicsDevice);

        fontSystem = new FontSystem();
        fontSystem.AddFont(File.ReadAllBytes("Assets/SpaceMono.ttf")); //todo: i do not have the rights to this font
        GameResources.DefaultFont = fontSystem.GetFont(16);  // 16 pt size
        
        GameResources.BasicEffect = new BasicEffect(GraphicsDevice) {
            VertexColorEnabled = true,
            Projection = Matrix.CreateOrthographicOffCenter(0, GraphicsDevice.Viewport.Width,
                GraphicsDevice.Viewport.Height, 0, 0, 1),
            View = Matrix.Identity,
            World = Matrix.Identity
        };

        WorldconeAnalyticalIntersectionTests.RunAll();
        
        LocalMultiplayerManager.InitializeLocalPlayers(2); // or 4
    }

    protected override void Update(GameTime gameTime)
    {
        float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;
        InputSystem.Update(gameTime);
        foreach (var player in PlayerManager.Players) {
            player.Update(dt);
        }

        foreach (var entity in RenderableEntity.Instances)
        {
            entity.Update(dt);
        }
        
    }

    protected override void Draw(GameTime gameTime)
    {
        GraphicsDevice.Clear(Color.Black);
        spriteBatch.Begin();
        renderer.RenderAllPlayers(spriteBatch);
        spriteBatch.End();
    }
}
