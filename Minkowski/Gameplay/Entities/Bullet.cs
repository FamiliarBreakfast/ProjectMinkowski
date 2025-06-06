using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ProjectMinkowski.Relativity;
using ProjectMinkowski.Rendering;

namespace ProjectMinkowski.Entities;

public class Bullet : TracerEntity
{
    public Dictionary<Ship, BulletTracer> Tracers = new();
    public Ship Ship;
    
    public Bullet(MinkowskiVector origin, Ship ship)
    {
        Ship = ship;
        Line = new Line(origin, Config.C, Ship.Rotation);
    }
    
    public override void Update(float deltaTime)
    { }
    
    public override void RelativityUpdate(float deltaTime, Ship ship)
    { }

    public override void Draw(SpriteBatch spriteBatch, Ship ship)
    { }

    public override void VertexDraw(GraphicsDevice graphicsDevice, BasicEffect effect, Ship ship)
    { }
}

public class BulletTracer : WorldlineEntity //todo: this does not need to be a WorldlineEntity
{
    public Ship? Ship;
    public Vector2 Origin;
    public float Rotation;
    public Color Color;
    
    private static int _fadeTimerMax = 100;
    private int _fadeTimer;
    private static float _tracerLength = Config.C * _fadeTimerMax * 2; //ensure tracer end is never visible
    public BulletTracer(Ship? ship, Color color, Vector2 origin, float rotation)
    {
        Ship = ship;
        Origin = origin;
        Rotation = rotation;
        Color = color;
        _fadeTimer = _fadeTimerMax;
    }
    
    public override void Update(float deltaTime)
    {
        if (_fadeTimer == 0)
        {
            Despawn();
        }
        _fadeTimer--;
    }
    
    public override void RelativityUpdate(float deltaTime, Ship ship)
    { }

    public override void Draw(SpriteBatch spriteBatch, Ship ship)
    { }

    public override void VertexDraw(GraphicsDevice graphicsDevice, BasicEffect effect, Ship ship)
    {
        if (ship == Ship || Ship == null)
        {
            Vector2 direction = new Vector2(MathF.Cos(Rotation), MathF.Sin(Rotation));
            
            Vector2 worldStart = Origin;
            Vector2 worldEnd = Origin + direction * _tracerLength;

            float t = (float)_fadeTimer / _fadeTimerMax;
            
            Color fade = new Color(Color.R / 255f * t, Color.G / 255f * t, Color.B / 255f * t, t);

            VertexPositionColor[] vertices = new VertexPositionColor[2];
            vertices[0] = new VertexPositionColor(new Vector3(worldStart, 0), fade);
            vertices[1] = new VertexPositionColor(new Vector3(worldEnd, 0), fade);

            ship.Shapes.Add(vertices);
        }
    }
}
