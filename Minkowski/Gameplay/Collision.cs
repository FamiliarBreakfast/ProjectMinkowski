using System.Reflection;
using Microsoft.Xna.Framework;
using ProjectMinkowski.Entities;
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
                if (Vector2.DistanceSquared(p, ship.Origin.ToVector2()) < 600)
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
}