using System.Reflection;
using Clipper2Lib;
using Microsoft.Xna.Framework;
using ProjectMinkowski.Entities;
using ProjectMinkowski.Relativity;
using ProjectMinkowski.Rendering;

namespace ProjectMinkowski.Gameplay;

public delegate void CollisionHandler(object a, object b);

public static class CollisionManager
{
    private static Dictionary<(Type, Type), CollisionHandler> handlers = new();

    static CollisionManager()
    {
        AutoRegisterHandlers();
    }

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
                if (TryGetHandler(a.GetType(), b.GetType(), out var handler))
                    handler(a, b);
                else if (TryGetHandler(b.GetType(), a.GetType(), out handler))
                    handler(b, a); // symmetric fallback
            }
        }
    }

    private static void AutoRegisterHandlers()
    {
        var asm = Assembly.GetExecutingAssembly();
        var allTypes = asm.GetTypes()
            .Where(t => typeof(RenderableEntity).IsAssignableFrom(t))
            .ToArray();

        foreach (var type in asm.GetTypes())
        {
            foreach (var method in type.GetMethods(
                         BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static))
            {
                if (method.Name != "Collide") continue;

                var p = method.GetParameters();
                if (p.Length != 2) continue;

                var pA = p[0].ParameterType;
                var pB = p[1].ParameterType;

                // Wrap reflection method into delegate
                CollisionHandler del = (a, b) => method.Invoke(null, new object[] { a, b });

                // Register for (pA, pB) and all subclass combinations
                foreach (var subA in allTypes.Where(t => pA.IsAssignableFrom(t)))
                foreach (var subB in allTypes.Where(t => pB.IsAssignableFrom(t)))
                    handlers[(subA, subB)] = del;
            }
        }
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
    
    public static void Collide(Asteroid asteroid, Bullet bullet) //broken
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
    
    // public static void Collide(Ship a, Ship b)
    // {
    // }
    
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

    public static void Collide(Ship ship, WorldlineEntity entity)
    {
        //append to list nearby masses for event sightlines
    }

    public static void Collide(MotileEntity a, MotileEntity b)
    {
        //newtons gravitation
        if (a.Mass != 0 || b.Mass != 0)
        {
            double distance = Vector2.Distance(a.Origin.ToVector2(), b.Origin.ToVector2());
            //a=f/m
            double force;
            if (distance > 3)
            {
                force = Config.G * ((a.Mass * b.Mass) / (Math.Pow(distance, Config.F)));
            }
            else
            {
                force = a.Mass * b.Mass * -10;
            }

            double aAccel = Math.Min(-force / a.Mass, Math.Pow(Config.C, 0.5));
            double bAccel = Math.Min(-force / b.Mass, Math.Pow(Config.C, 0.5));
            //for a
            Vector2 aVec = a.Origin.ToVector2() - b.Origin.ToVector2();
            aVec.Normalize();
            a.Acceleration = aVec * (float)aAccel;
            //for b
            Vector2 bVec = b.Origin.ToVector2() - a.Origin.ToVector2();
            bVec.Normalize();
            b.Acceleration = bVec * (float)bAccel;
        }
    }
}