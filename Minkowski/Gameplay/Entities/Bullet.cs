using Clipper2Lib;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ProjectMinkowski.Relativity;
using ProjectMinkowski.Rendering;

namespace ProjectMinkowski.Entities;

public class Bullet : WorldlineEntity
{
    public const int bulletSpeed = 25;
    
    public float Rotation;
    public Vector2 Velocity;

    public Ship Ship;
    
    public Color Color;
    public Bullet(MinkowskiVector absolutePosition, Ship ship)
    {
        Ship = ship;
        
        Origin = absolutePosition;
        Worldline = new Worldline();
        Color = ship.Color;
        
        Rotation = ship.Rotation;
        Velocity = ship.Velocity;
        
        Vector2 vPrime = new Vector2(MathF.Cos(Rotation) * bulletSpeed, MathF.Sin(Rotation) * bulletSpeed);
        float c = Config.C;

        if (Velocity.LengthSquared() < 1e-6f)
        {
            Velocity = vPrime;
        }
        else
        {
            // Unit vector of ship's velocity
            Vector2 uHat = Velocity / Velocity.Length();
            float uMag = Velocity.Length();

            // Parallel component
            float vPrimeParallelMag = Vector2.Dot(vPrime, uHat);
            Vector2 vPrimeParallel = vPrimeParallelMag * uHat;

            // Perpendicular component
            Vector2 vPrimePerp = vPrime - vPrimeParallel;

            // Relativistic addition for parallel component:
            float vParallel = (vPrimeParallelMag + uMag) / (1 + (uMag * vPrimeParallelMag) / (c * c));

            // Relativistic addition for perpendicular component:
            float gamma = 1f / MathF.Sqrt(1f - (uMag * uMag) / (c * c));
            Vector2 vPerp = vPrimePerp / (gamma * (1 + (uMag * vPrimeParallelMag) / (c * c)));

            // Add together for world velocity:
            Velocity = vParallel * uHat + vPerp;
        }

        Polygon = new PathD
        {
            new PointD(15,0),
            new PointD(-15,1),
            new PointD(-15,-1)
        };
    }

    public override void Update(float deltaTime)
    {
        Origin.X += Velocity.X * deltaTime;
        Origin.Y += Velocity.Y * deltaTime;
        Origin.T += deltaTime;
        
        Worldline.AddEvent(new WorldlineEvent(
            new MinkowskiVector(Origin.T, Origin.X, Origin.Y),
            Rotation,
            Velocity
        ));
    }

    public override void Draw(SpriteBatch spriteBatch, Player player)
    { }

    public override void VertexDraw(GraphicsDevice graphicsDevice, BasicEffect effect, Player player, Rectangle viewport)
    {
        WorldlineEvent? evt = Worldline.GetVisibleEvent(player.Ship.Origin);
        if (evt != null)
        {
            var vertices =
                Transformations.ToVertexArray(
                    Transformations.Translate(
                        FrameOfReference.ApplyLengthContractionInFrame(
                            Transformations.Translate(
                                Transformations.Rotate(Polygon, evt.Rotation),
                                evt.Origin.X, evt.Origin.Y),
                            new Vector2((float)evt.Origin.X, (float)evt.Origin.Y), player.Ship.Frame, evt.Velocity),
                        -player.Ship.Origin.X, -player.Ship.Origin.Y),
                    Color);
            
            player.Shapes.Add(vertices);
        }
    }
}