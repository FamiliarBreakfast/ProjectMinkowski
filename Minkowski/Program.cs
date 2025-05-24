using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

using FontStashSharp;
using ProjectMinkowski.Entities;
using ProjectMinkowski.Gameplay;
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
    public const int C = 25;
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

        
        var config = new FontSystemSettings
        {
            FontResolutionFactor = 2, // Default is 1; higher means sharper
            TextureWidth = 1024,
            TextureHeight = 1024
        };
        fontSystem = new FontSystem(config);
        fontSystem.AddFont(File.ReadAllBytes("Assets/SpaceMono.ttf"));
        GameResources.DefaultFont = fontSystem.GetFont(18);  // 16 pt size
        
        GameResources.BasicEffect = new BasicEffect(GraphicsDevice) {
            VertexColorEnabled = true,
            Projection = Matrix.CreateOrthographicOffCenter(0, GraphicsDevice.Viewport.Width,
                GraphicsDevice.Viewport.Height, 0, 0, 1),
            View = Matrix.Identity,
            World = Matrix.Identity
        };

        //WorldconeAnalyticalIntersectionTests.RunAll();
        
        CollisionManager.Register<Ship, Ship>((a, b) => CollisionManager.Collide((Ship)a, (Ship)b));
        CollisionManager.Register<Ship, Bullet>((a, b) => CollisionManager.Collide((Ship)a, (Bullet)b));
        
        PlayerManager.InitializeLocalPlayers(2); // or 4
    }

    protected override void Update(GameTime gameTime)
    {
        float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;
        InputSystem.Update(gameTime);
        foreach (var player in PlayerManager.Ships) {
            player.Update(dt);
        }

        foreach (var entity in RenderableEntity.Instances)
        {
            entity.Update(dt);
        }

        foreach (var entity in RenderableEntity.Purge)
        {
            RenderableEntity.Instances.Remove(entity);
        }
        foreach (var entity in WorldlineEntity.Purge)
        {
            WorldlineEntity.Instances.Remove(entity);
        }
        RenderableEntity.Purge.Clear();
        WorldlineEntity.Purge.Clear();
        
        CollisionManager.Update(dt);
        
        //Console.WriteLine("0");
        
    }

    protected override void Draw(GameTime gameTime)
    {
        GraphicsDevice.Clear(Color.Black);
        spriteBatch.Begin();
        renderer.RenderAllPlayers(spriteBatch);
        spriteBatch.End();
    }
}
