using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ProjectMinkowski.Relativity;
using ProjectMinkowski.Rendering;

namespace ProjectMinkowski.Entities;

public class Bullet : TracerEntity
{
    public List<BulletTracer> Tracers = new();
    public Ship Ship;
    
    public Bullet(MinkowskiVector origin, Ship ship)
    {
        Ship = ship;
        Line = new Line(origin, Config.C, Ship.Rotation);
    }
    
    public override void Update(float deltaTime)
    { }

    public override void Draw(SpriteBatch spriteBatch, Player player)
    { }

    public override void VertexDraw(GraphicsDevice graphicsDevice, BasicEffect effect, Player player, Rectangle viewport)
    { }
}

public class BulletTracer : WorldlineEntity
{
    public Player Player;
    public Vector2 Origin;
    public float Rotation;
    public Color Color;

    private int _fadeTimer;
    public BulletTracer(Player player, Vector2 origin, float rotation)
    {
        Player = player;
        Origin = origin;
        Rotation = rotation;
        Color = player.Ship.Color;
        _fadeTimer = 100;
    }
    
    public override void Update(float deltaTime)
    {
        _fadeTimer--;
    }

    public override void Draw(SpriteBatch spriteBatch, Player player)
    { }

    public override void VertexDraw(GraphicsDevice graphicsDevice, BasicEffect effect, Player player, Rectangle viewport)
    {
            //compute endpoint
            //center in viewport????
            //profit?????????????
            
            float tracerLength = 150f; // Or whatever length looks good for your game

            // Compute the vector for the tracer's direction
            Vector2 direction = new Vector2(MathF.Cos(Rotation), MathF.Sin(Rotation));

            // Start and end points in world coordinates
            Vector2 worldStart = Origin;
            Vector2 worldEnd = Origin + direction * tracerLength;

            // Convert to screen coordinates (player centered)
            Vector2 screenStart = worldStart - new Vector2((float)player.Ship.Origin.X, (float)player.Ship.Origin.Y);
            Vector2 screenEnd   = worldEnd   - new Vector2((float)player.Ship.Origin.X, (float)player.Ship.Origin.Y);

            // Optionally, add offset to place the player in the screen center
            // var viewportCenter = new Vector2(viewport.Width / 2, viewport.Height / 2);
            // screenStart += viewportCenter;
            // screenEnd   += viewportCenter;

            // Build vertex array for the line
            VertexPositionColor[] vertices = new VertexPositionColor[2];
            vertices[0] = new VertexPositionColor(new Vector3(screenStart, 0), Color);
            vertices[1] = new VertexPositionColor(new Vector3(screenEnd, 0), Color);
            
            player.Shapes.Add(vertices);
        
    }
}