using Clipper2Lib;
using Microsoft.Xna.Framework;
using ProjectMinkowski.Relativity;
using ProjectMinkowski.Rendering;

namespace ProjectMinkowski.Gameplay;

public static class CollisionManager
{
    public static void Update(float dt)
    {
        for (int i = 0; i < WorldlineEntity.Instances.Count; i++)
        {
            for (int j = i + 1; j < WorldlineEntity.Instances.Count; j++)
            {
                if (Collide(WorldlineEntity.Instances[i], WorldlineEntity.Instances[j]))
                {
                    Console.WriteLine("Collided");
                }
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
}