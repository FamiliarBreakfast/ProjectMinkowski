using Clipper2Lib;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Minkowski.Gameplay.Relativity;
using Minkowski.Rendering;

namespace Minkowski.Gameplay.Entities;

public class Asteroid : WorldlineEntity
{
    public Asteroid(MinkowskiVector absolutePosition)
    {
        Origin = absolutePosition;
        Worldline = new Worldline();
        Polygon = new PathD
        {
            new PointD(0,5.8),
            new PointD(-5,-2.9),
            new PointD(5,-2.9)
        };
        Radius = 20;
    }
    
    public override void Update(float deltaTime)
    {
        int i = 0;
        foreach (Ship ship in PlayerManager.Ships)
        {
            if (Vector2.DistanceSquared(ship.Origin.ToVector2(), Origin.ToVector2()) < Math.Pow(Config.AsteroidLoadRadius*Config.AsteroidSpacing, 2))
            {
                i++;
            }
        }
        if (i == 0)
        {
            AsteroidManager.Asteroids.Remove(Origin.ToVector2());
            Despawn();
        }

        Origin.T += deltaTime;
        Worldline.AddEvent(this);
    }
    
    public override void RelativityUpdate(float deltaTime, Ship ship)
    { }

    public override void Draw(SpriteBatch spriteBatch, Ship ship)
    { }

    public override void VertexDraw(GraphicsDevice graphicsDevice, BasicEffect effect, Ship ship)
    {
        if (Worldline.HasVisibleEvent(ship.Origin))
        {
            Vector2 position = Worldline.GetVisibleVariable<Vector2>(ship.Origin, "Position", interpolate: true);
            Vector2 velocity = Worldline.GetVisibleVariable<Vector2>(ship.Origin, "Velocity", interpolate: true);
            float rotation = Worldline.GetVisibleVariable<float>(ship.Origin, "Rotation", interpolate: true);

            Vector2 relativeVelocity = ship.Frame.LorentzTransformVelocity(velocity);

            Color color = Config.DopplerEffect switch
            {
                true => ColorHelper.DopplerShift(Color.Lime, relativeVelocity),
                _ => Color.Lime
            };
            
            var vertices =
                Transformations.ToVertexArray(
                    FrameOfReference.ApplyLengthContractionInFrame(
                        Transformations.Translate(
                            Transformations.Rotate(Polygon, rotation),
                            position.X, position.Y),
                        position, relativeVelocity),
                    color);

            ship.Shapes.Add(vertices);
        }
    }
}

public static class AsteroidManager {
    public static HashSet<Vector2> Asteroids { get; } = new HashSet<Vector2>();
    public static void UpdatePlayer(Ship ship, int gridRadius, int gridSpacing)
    {
        int snappedX = (int)Math.Round(ship.Origin.X / gridSpacing) * gridSpacing;
        int snappedY = (int)Math.Round(ship.Origin.Y / gridSpacing) * gridSpacing;
        Vector2 origin = new Vector2(snappedX, snappedY);

        for (int dx = -gridRadius; dx <= gridRadius; dx++)
        {
            for (int dy = -gridRadius; dy <= gridRadius; dy++)
            {
                Vector2 spawnPos = origin + new Vector2(dx * gridSpacing, dy * gridSpacing);
                Asteroid? asteroid = TrySpawnAsteroid(Random(spawnPos));
                if (asteroid != null)
                {
                    EntityManager.Spawn(asteroid);
                }
            }
        }
    }

    static Asteroid? TrySpawnAsteroid(Vector2 position)
    {
        if (Asteroids.Contains(position))
            return null;
        
        Asteroids.Add(position);
        return new Asteroid(new MinkowskiVector(0, position.X, position.Y));
    }
    public static Vector2 Random(Vector2 v)
    {
        uint FloatToUint(float f) => BitConverter.ToUInt32(BitConverter.GetBytes(f), 0);

        uint seedA = FloatToUint(v.X);
        uint seedB = FloatToUint(v.Y);

        uint x = seedA;
        x ^= 0xA3C59AC3u;
        x ^= seedB * 0xACAB1312u;
        x ^= 0xBADC0FFEu;
        x ^= x >> 17;
        x *= 0x85ebca6bu;
        x ^= x >> 13;
        x *= 0xc2b2ae35u;
        x ^= x >> 16;

        uint y = seedB;
        y ^= 0x1F123BB5u;
        y ^= seedA * 0xDEADBEEFu;
        y ^= unchecked(0x157u * 0xBADC0FFEu);
        y ^= y >> 17;
        y *= 0x85ebca6bu;
        y ^= y >> 13;
        y *= 0xc2b2ae35u;
        y ^= y >> 16;

        float xf = (x & 0xFFFFFF) / (float)0x1000000;
        float yf = (y & 0xFFFFFF) / (float)0x1000000;

        return new Vector2(Config.AsteroidRandomMagnitude * xf + v.X, Config.AsteroidRandomMagnitude * yf + v.Y);
    }
}
