using System.Reflection;
using Clipper2Lib;
using Microsoft.Xna.Framework;
using ProjectMinkowski.Entities;
using ProjectMinkowski.Relativity;
using ProjectMinkowski.Rendering;

namespace ProjectMinkowski.Gameplay;

public delegate void CollisionHandler(RenderableEntity a, RenderableEntity b);
public static class CollisionManager
{
    private static Dictionary<(Type, Type), CollisionHandler> handlers = new();
    public static void Register<TA, TB>(CollisionHandler handler)
        where TA : RenderableEntity
        where TB : RenderableEntity
    {
        handlers[(typeof(TA), typeof(TB))] = handler;
    }
    public static bool TryGetHandler(Type a, Type b, out CollisionHandler handler)
    {
        return handlers.TryGetValue((a, b), out handler);
    }
    
    public static void Update(float dt)
    {
        var entities = EntityManager.Entities;
        int count = entities.Count;

        for (int i = 0; i < count; i++)
        {
            var a = entities[i];
            for (int j = i + 1; j < count; j++)
            {
                var b = entities[j];
                // Try both (A,B) and (B,A) handler orderings for symmetry
                if (CollisionManager.TryGetHandler(a.GetType(), b.GetType(), out var handler))
                    handler(a, b);
                else if (CollisionManager.TryGetHandler(b.GetType(), a.GetType(), out handler))
                    handler(b, a);
            }
        }
    }
    
    static bool InvokeCollisionHandler(RenderableEntity a, RenderableEntity b)
    {
        var method = typeof(CollisionManager).GetMethod(
            "Collide",
            BindingFlags.Public | BindingFlags.Static,
            null,
            new Type[] { a.GetType(), b.GetType() },
            null);

        if (method != null)
        {
            method.Invoke(null, new object[] { a, b });
            return true;
        }
        return false;
    }

    public static void Collide(Ship ship, Bullet bullet)
    {
        if (bullet.Ship != ship)
        {
            // if (bullet.Line.Intersects(ship.Origin))
            // {
            Vector2? point = bullet.Line.PositionAtZ((float)ship.Origin.T);
            if (point != null) {
                Vector2 p = (Vector2)point;
                if (Vector2.DistanceSquared(p, ship.Origin.ToVector2()) < Math.Pow(ship.Radius, 2))
                {
                    bullet.Line.SetEndTime((float)ship.Origin.T);
                    bullet.Tracers[ship] = new BulletTracer(ship, bullet.Ship.Color, p, bullet.Line.Phi + MathF.PI);
                    ship.Health -= 10;
                }
            }
        }
    }
    public static void Collide(Ship a, Ship b)
    {
    }
    
    public static void Collide(Ship ship, Mine mine)
    {
        // if (mine.Ship != ship)
        // {
        //     Vector2? point = mine.Polygon.Intersects(ship.Origin.ToVector2());
        //     if (point != null)
        //     {
        //         Vector2 p = (Vector2)point;
        //         if (Vector2.DistanceSquared(p, ship.Origin.ToVector2()) < 600)
        //         {
        //             mine.Flags = 1; //detonate
        //             ship.Health -= 20;
        //         }
        //     }
        // }
    }
    
    public static void Collide(Mine mine, Bullet bullet)
    {
        if (mine.Flags == 0)
        {
            //find the point of intersection between the bullet line and the mine polygon
            //create a new worldline event for the at that point and set the mine's flags to detonate
            //MinkowskiVector? point = bullet.Line.IntersectsAt(mine.Origin);
            Vector2? point = bullet.Line.PositionAtZ((float)mine.Origin.T);
            if (point != null)
            {
                Vector2 p = (Vector2)point;
                if (Vector2.DistanceSquared(p, mine.Origin.ToVector2()) < Math.Pow(mine.Radius, 2))
                {
                    mine.Flags = 1; //detonate
                    new Shockwave(mine.Origin.Clone());
                }
            }
        }
    }

    public static void Collide(Ship ship, Shockwave shockwave)
    {
        if (shockwave.Worldcone.IsOnShell(ship.Origin, 1))
        {
            if (!ship.AttackHash.Contains(shockwave.GetHashCode()))
            {
                ship.AttackHash.Add(shockwave.GetHashCode()); //add to attacked list to prevent double hurting
                ship.Health -= 50;
            }
        }
    }

    public static void Collide(Ship ship, Asteroid asteroid)
    {
        if (Vector2.DistanceSquared(ship.Origin.ToVector2(), asteroid.Origin.ToVector2()) < Math.Pow(asteroid.Radius + ship.Radius, 2))
        {
            PathsD solution = Clipper.Intersect(new PathsD {ship.Polygon}, new PathsD {asteroid.Polygon}, FillRule.NonZero, 0);
            bool collided = solution.Any(path => Clipper.Area(path) > 0.0001);
            if (collided)
            {
                asteroid.Despawn();
                ship.Health -= 10;
            }
        }
    }

    public static void Collide(Bullet bullet, Asteroid asteroid) //broken
    {
            Vector2? point = bullet.Line.PositionAtZ((float)asteroid.Origin.T);
            if (point != null) {
                Vector2 p = (Vector2)point;
                if (Vector2.DistanceSquared(p, asteroid.Origin.ToVector2()) < Math.Pow(asteroid.Radius, 2))
                {
                    bullet.Line.SetEndTime((float)asteroid.Origin.T);
                    asteroid.Despawn();
                }
            }
    }
}