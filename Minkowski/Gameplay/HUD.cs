using Clipper2Lib;
using FontStashSharp;
using MathNet.Numerics.Distributions;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Minkowski.Gameplay.Entities;
using Minkowski.Gameplay.Relativity;

namespace Minkowski.Gameplay;

public static class HUD
{
    private const int Range = 200;
    private const int OrbitRange = 50;
    
    public static PathD Polygon = new PathD
    {
        new PointD(6.66,0),
        new PointD(-2.22,3.33),
        new PointD(-2.22,-3.33)
    };

    private static int AlphaAsymptote(float distance)
    {
        return 0;
    }
    
    public static void Draw(SpriteBatch spriteBatch, Ship ship)
    {
        var speed = ship.Velocity.Length();

        var font = GameResources.DefaultFont;
        spriteBatch.DrawString(font, $"UNK-{ship.Id}    Velocity: {speed / Config.C:F3}c    Gamma: {MotileEntity.Gamma(ship.Velocity):F2}", new Vector2(10, 12), Color.White);
        spriteBatch.DrawString(font, $"", new Vector2(10, 22), Color.White);

        int i = 1;
        
        foreach (Ship target in PlayerManager.Ships)
        {
            if (target == ship)  continue;
            if (target.Worldline.HasVisibleEvent(ship.Origin))
            {
                Vector2 position =
                    target.Worldline.GetVisibleVariable<Vector2>(ship.Origin, "Position", interpolate: true);
                
                float distance = Vector2.Distance(ship.Position, position);
                
                spriteBatch.DrawString(font, $"UNK-{target.Id}    Distance: {distance / Config.C:F2}ls", new Vector2(10, 18*i+12), target.Color);
                i++;
            }
        }
    }

    public static void VertexDraw(SpriteBatch spriteBatch, Ship ship)
    {
        foreach (Ship target in PlayerManager.Ships)
        {
            if (target.Worldline.HasVisibleEvent(ship.Origin))
            {
                Vector2 position =
                    target.Worldline.GetVisibleVariable<Vector2>(ship.Origin, "Position", interpolate: true);
                float distance = Vector2.DistanceSquared(ship.Position, position);
                if (distance > Math.Pow(Range, 2))
                {
                    Vector2 orbit = Vector2.Normalize(position - ship.Position);
                    float angle = MathF.Atan2(orbit.Y, orbit.X);

                    float x = ship.Position.X + float.Cos(angle) * OrbitRange;
                    float y = ship.Position.Y + float.Sin(angle) * OrbitRange;

                    var vertices = Transformations.ToVertexArray(
                        Transformations.Translate(
                            Transformations.Rotate(Polygon, angle),
                            x, y),
                        target.Color);

                    ship.Shapes.Add(vertices);
                }
            }
        }
    }
}

