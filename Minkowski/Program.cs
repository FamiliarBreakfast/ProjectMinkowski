using FontStashSharp;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Graphics;
using Minkowski.Gameplay;
using Minkowski.Gameplay.Entities;
using Minkowski.Gameplay.Relativity;
using Minkowski.Multiplayer.Local;
using Minkowski.Rendering;
using Minowski.Gameplay.Entities;

namespace Minkowski;

public static class Program
{
    [STAThread]
    static void Main()
    {
        using var game = new ProjectMinkowskiGame();
        game.Run();
    }
}

public static class Config
{
    /// <summary>
    /// Number of players. Currently only 2 or 4 supported.
    /// </summary>
    public const int Players = 4;
    /// <summary>
    /// The speed of light, in distance per time unit (e.g. 10 means light moves 10 units/sec).
    /// </summary>
    public const int C = 100;
    /// <summary>
    /// Gravitational constant. Sets the overall strength of gravity.
    /// Appears in the acceleration formula: a = G * (m1 * m2) / r^F
    /// where m is the other body's mass, r is distance, and F is the exponent.
    /// Larger G means stronger acceleration at all distances.
    /// </summary>
    public const double G = 0;
    /// <summary>
    /// Gravitational exponent, unitless.
    /// Controls how acceleration falls off with distance in the formula: a = G * (m1 * m2) / r^F.
    /// F = 2 gives the real-world inverse-square law.
    /// Smaller values make gravity decay more slowly with distance,
    /// larger values make it decay more quickly.
    /// </summary>
    public const double F = 0.9; // 0.9 is kinda fun
    public const bool DopplerEffect = false;
    public const bool RotateWorld = true;
    
    //asteroid stuff
    public const int AsteroidSpacing = 250;
    public const int AsteroidLoadRadius = 3;
    public const int AsteroidRandomMagnitude = 500;
    
    public const bool Sound = false; //doesnt really work in splitscreen does it?
    public const int sampleRate = 44100;
    public const int bufferSize = 2048;

    public static ProjectMinkowskiGame Game;
}

public static class GameResources {
    public static SpriteFontBase? DefaultFont { get; set; }
    public static BasicEffect? BasicEffect { get; set; }
}

public class ProjectMinkowskiGame : Game
{
    //GRAPHICS
    private FontSystem fontSystem;
    private SpriteBatch spriteBatch;
    private GraphicsDeviceManager graphics;
    private DynamicSoundEffectInstance synthInstance;

    public ProjectMinkowskiGame() {
        graphics = new GraphicsDeviceManager(this);
        Content.RootDirectory = "Assets";
        IsMouseVisible = true;
        graphics.PreferredBackBufferWidth = 1600;
        graphics.PreferredBackBufferHeight = 900;
        Config.Game = this;
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
        GameResources.DefaultFont = fontSystem.GetFont(24);  // 16 pt size
        
        GameResources.BasicEffect = new BasicEffect(GraphicsDevice) {
            VertexColorEnabled = true,
            Projection = Matrix.CreateOrthographicOffCenter(0, GraphicsDevice.Viewport.Width,
                GraphicsDevice.Viewport.Height, 0, 0, 1),
            View = Matrix.Identity,
            World = Matrix.Identity
        };

        //WorldconeAnalyticalIntersectionTests.RunAll();
        
        PlayerManager.InitializeLocalPlayers(Config.Players); // or 4
        
        spriteBatch = new SpriteBatch(GraphicsDevice);

        new Planet(1000, 500, new MinkowskiVector(0, 1100, 0));
        
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
        foreach (Ship ship in PlayerManager.Ships)
        {
            ship.View.Render(spriteBatch);
        }
        MapView.Render(spriteBatch);
        spriteBatch.End();
    }
}
