using Clipper2Lib;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ProjectMinkowski.Entities;
using ProjectMinkowski.Relativity;
using ProjectMinkowski.Rendering;

namespace ProjectMinkowski.Gameplay;

public static class CollisionManager
{
    public static void Update(float dt)
    {
        foreach (WorldlineEntity entity in WorldlineEntity.Instances) 
        {
            foreach (TracerEntity tracer in TracerEntity.Instances)
            {
                // if (Collide(entity, tracer))
                // {
                //     //entity.Collide(tracer);
                //     Console.WriteLine("Shot!");
                //     Ship? ship = (Ship)entity;
                //     if (entity != null)
                //     {
                //         new BulletTracer(ship.Owner, entity.Origin.ToVector2(), tracer.Line.Phi);
                //     }
                //     Console.WriteLine("Shot!");
                //     //tracer.Line.SetEndTime((float)entity.Origin.T);
                // }
            }
        }
    }

    public static bool Collide(WorldlineEntity a, WorldlineEntity b)
    {
        if (Vector2.DistanceSquared(a.Origin.ToVector2(), b.Origin.ToVector2()) < Math.Pow(a.Radius + b.Radius, 2))
        {
            PathsD solution = Clipper.Intersect(new PathsD() {Transformations.Translate(a.Polygon, a.Origin.X, a.Origin.Y)}, new PathsD() {Transformations.Translate(b.Polygon, b.Origin.X, b.Origin.Y)}, FillRule.NonZero);
            return solution.Count > 0;
        }
        return false;
    }
    public static bool Collide(WorldlineEntity a, TracerEntity b)
    {
        if (b is Bullet bullet)
        {
            if (a is Ship ship)
            {
                if (bullet.Ship == ship)
                {
                    return false;
                }
            }
        }
        if (b.Line.Intersects(a.Origin))
        {
            return true;
        }
        return false;
    }
}