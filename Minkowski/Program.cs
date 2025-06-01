using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

using FontStashSharp;
using Microsoft.Xna.Framework.Audio;
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
    public const int C = 50;
    public const bool DopplerEffect = false;
    public const bool RotateWorld = true;
    
    //asteroid stuff
    public const int AsteroidSpacing = 250;
    public const int AsteroidLoadRadius = 3;
    public const int AsteroidRandomMagnitude = 500;
    
    public const bool Sound = false;
    public const int sampleRate = 44100;
    public const int bufferSize = 2048;
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
    private DynamicSoundEffectInstance synthInstance;

    public ProjectMinowskiGame() {
        graphics = new GraphicsDeviceManager(this);
        Content.RootDirectory = "Assets";
        IsMouseVisible = true;
        graphics.PreferredBackBufferWidth = 800;
        graphics.PreferredBackBufferHeight = 400;
    }
    
    protected override void LoadContent()
    {
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
        
        //todo: urgent: automate
        CollisionManager.Register<Ship, Ship>((a, b) => CollisionManager.Collide((Ship)a, (Ship)b));
        CollisionManager.Register<Ship, Bullet>((a, b) => CollisionManager.Collide((Ship)a, (Bullet)b));
        CollisionManager.Register<Mine, Bullet>((a, b) => CollisionManager.Collide((Mine)a, (Bullet)b));
        CollisionManager.Register<Ship, Mine>((a, b) => CollisionManager.Collide((Ship)a, (Mine)b));
        CollisionManager.Register<Ship, Shockwave>((a, b) => CollisionManager.Collide((Ship)a, (Shockwave)b));
        CollisionManager.Register<Ship, Asteroid>((a, b) => CollisionManager.Collide((Ship)a, (Asteroid)b));
        CollisionManager.Register<Bullet, Asteroid>((a, b) => CollisionManager.Collide((Bullet)a, (Asteroid)b));
        
        PlayerManager.InitializeLocalPlayers(2); // or 4
        
        spriteBatch = new SpriteBatch(GraphicsDevice);
        renderer = new SplitScreenRenderer(GraphicsDevice);

        if (Config.Sound)
        {
            synthInstance = new DynamicSoundEffectInstance(Config.sampleRate, AudioChannels.Mono);
            synthInstance.Play();
        }
    }

    protected override void Update(GameTime gameTime)
    {
        float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;
        foreach (var player in PlayerManager.Ships)
        {
            InputSystem.Update(dt, player);
            AsteroidManager.UpdatePlayer(player, Config.AsteroidLoadRadius, Config.AsteroidSpacing);
            foreach (var entity in EntityManager.Entities)
            {
                entity.RelativityUpdate(dt, player);
            }
        }

        // Update all entities
        foreach (var entity in EntityManager.Entities)
        {
            entity.Update(dt);
        }
        
        EntityManager.ProcessQueues();
        CollisionManager.Update(dt);
        if (Config.Sound)
        {
            Sound.Update(dt, synthInstance);
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
