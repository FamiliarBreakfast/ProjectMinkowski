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

public static class Diagnostics
{
    public static bool Enabled = true;
    private static float _updateTimer = 0;
    private static float _updateInterval = 0.5f; // Update stats every 0.5 seconds

    // Cached stats
    public static int TotalEntities;
    public static int TotalWorldlineEvents;
    public static int MaxWorldlineEvents;
    public static string MaxWorldlineEntityType = "";
    public static Dictionary<string, int> EntityCounts = new();
    public static Dictionary<string, int> WorldlineEventCounts = new();
    public static float FrameTime;
    public static float UpdateTime;
    public static float DrawTime;

    private static System.Diagnostics.Stopwatch _updateStopwatch = new();
    private static System.Diagnostics.Stopwatch _drawStopwatch = new();

    public static void StartUpdateTimer() => _updateStopwatch.Restart();
    public static void StopUpdateTimer() { _updateStopwatch.Stop(); UpdateTime = (float)_updateStopwatch.Elapsed.TotalMilliseconds; }
    public static void StartDrawTimer() => _drawStopwatch.Restart();
    public static void StopDrawTimer() { _drawStopwatch.Stop(); DrawTime = (float)_drawStopwatch.Elapsed.TotalMilliseconds; }

    public static void Update(float deltaTime)
    {
        if (!Enabled) return;

        FrameTime = deltaTime * 1000; // Convert to ms
        _updateTimer += deltaTime;

        if (_updateTimer >= _updateInterval)
        {
            _updateTimer = 0;
            RefreshStats();
        }
    }

    private static void RefreshStats()
    {
        EntityCounts.Clear();
        WorldlineEventCounts.Clear();
        TotalEntities = Gameplay.Entities.EntityManager.Entities.Count;
        TotalWorldlineEvents = 0;
        MaxWorldlineEvents = 0;
        MaxWorldlineEntityType = "";

        foreach (var entity in Gameplay.Entities.EntityManager.Entities)
        {
            string typeName = entity.GetType().Name;
            EntityCounts.TryGetValue(typeName, out int count);
            EntityCounts[typeName] = count + 1;

            if (entity is Gameplay.Entities.WorldlineEntity wle && wle.Worldline != null)
            {
                int eventCount = wle.Worldline.Events.Count;
                TotalWorldlineEvents += eventCount;

                WorldlineEventCounts.TryGetValue(typeName, out int wlCount);
                WorldlineEventCounts[typeName] = wlCount + eventCount;

                if (eventCount > MaxWorldlineEvents)
                {
                    MaxWorldlineEvents = eventCount;
                    MaxWorldlineEntityType = typeName;
                }
            }
        }
    }

    public static string GetReport()
    {
        var sb = new System.Text.StringBuilder();
        sb.AppendLine($"");
        sb.AppendLine($"=== DIAGNOSTICS ===");
        sb.AppendLine($"Frame: {FrameTime:F1}ms | Update: {UpdateTime:F1}ms | Draw: {DrawTime:F1}ms");
        sb.AppendLine($"Entities: {TotalEntities} | WL Events: {TotalWorldlineEvents}");
        sb.AppendLine($"Max WL: {MaxWorldlineEvents} ({MaxWorldlineEntityType})");
        sb.AppendLine($"--- By Type ---");

        foreach (var kvp in EntityCounts.OrderByDescending(x => x.Value))
        {
            WorldlineEventCounts.TryGetValue(kvp.Key, out int wlEvents);
            sb.AppendLine($"  {kvp.Key}: {kvp.Value} (WL: {wlEvents})");
        }

        return sb.ToString();
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
    public const int AsteroidLoadRadius = 0;
    public const int AsteroidRandomMagnitude = 500;

    //grid stuff
    public const bool ShowGrid = false;
    public const int GridSpacing = 100;
    public const int GridLoadRadius = 3;
    
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
        
        PlayerManager.Initialize();
        
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
        Diagnostics.StartUpdateTimer();
        float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;

        // Input and chunk loading happen once per frame
        InputSystem.Update(dt, Ship.Instance);
        AsteroidManager.UpdatePlayer(Ship.Instance, Config.AsteroidLoadRadius, Config.AsteroidSpacing);
        if (Config.ShowGrid)
            GridManager.UpdatePlayer(Ship.Instance, Config.GridLoadRadius, Config.GridSpacing);

        // Update all entities
        foreach (var entity in EntityManager.Entities)
        {
            entity.Update(dt);
        }
        EntityManager.ProcessQueues();

        // Prune worldlines every frame (lightweight when nothing to prune)
        foreach (var entity in EntityManager.Entities)
        {
            if (entity is WorldlineEntity wle && wle.Worldline != null)
                wle.Worldline.PruneObservedEvents([0]);
        }

        if (Config.Sound)
        {
            Sound.Update(dt, synthInstance);
        }

        Diagnostics.StopUpdateTimer();
        Diagnostics.Update(dt);
    }

    protected override void Draw(GameTime gameTime)
    {
        Diagnostics.StartDrawTimer();
        GraphicsDevice.Clear(Color.Black);
        spriteBatch.Begin();
        Ship.Instance.View.Render(spriteBatch);
        MapView.Render(spriteBatch);

        // Render diagnostics overlay
        if (Diagnostics.Enabled && GameResources.DefaultFont != null)
        {
            string report = Diagnostics.GetReport();
            GameResources.DefaultFont.DrawText(spriteBatch, report, new Vector2(10, 10), Color.Yellow);
        }

        spriteBatch.End();
        Diagnostics.StopDrawTimer();
    }
}
