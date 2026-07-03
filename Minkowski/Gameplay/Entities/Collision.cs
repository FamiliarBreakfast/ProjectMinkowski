using System.Reflection;
using Clipper2Lib;
using Microsoft.Xna.Framework;
using Minowski.Gameplay.Entities;

namespace Minkowski.Gameplay.Entities;

public static class CollisionManager
{
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
                CollideAll(a, b);
            }
        }
    }
    
        private static readonly Dictionary<(Type, Type), Action<object, object>> _handlers = new();

        // Auto-register all methods named "Collide" from a type
        public static void AutoRegister(Type handlerType)
        {
            var methods = handlerType.GetMethods(BindingFlags.Public | BindingFlags.Static)
                .Where(m => m.Name == "Collide" && m.GetParameters().Length == 2);

            foreach (var method in methods)
            {
                var parameters = method.GetParameters();
                var typeA = parameters[0].ParameterType;
                var typeB = parameters[1].ParameterType;

                _handlers[(typeA, typeB)] = (a, b) => method.Invoke(null, new[] { a, b });
            }
        }

        // Call all relevant handlers based on type hierarchy
        public static void CollideAll(object a, object b)
        {
            var typeA = a.GetType();
            var typeB = b.GetType();

            // Get all base types for both objects
            var typesA = GetTypeHierarchy(typeA);
            var typesB = GetTypeHierarchy(typeB);

            // Try all combinations from most specific to most general
            foreach (var tA in typesA)
            {
                foreach (var tB in typesB)
                {
                    if (_handlers.TryGetValue((tA, tB), out var handler))
                    {
                        handler(a, b);
                    }
                }
            }
        }

        private static List<Type> GetTypeHierarchy(Type type)
        {
            var types = new List<Type>();
            while (type != null)
            {
                types.Add(type);
                type = type.BaseType;
            }
            return types;
        }

    
    public static void Collide(Ship ship, Planet planet)
    {
        
    }
    
    public static void Collide(Ship ship, Laser laser)
    {
        if (laser.Ship != ship)
        {
            // if (bullet.Line.Intersects(ship.Origin))
            // {
            Vector2? point = laser.Line.PositionAtZ((float)ship.Origin.T);
            if (point != null) {
                Vector2 p = (Vector2)point;
                if (Vector2.DistanceSquared(p, ship.Origin.ToVector2()) < Math.Pow(ship.Radius, 2))
                {
                    laser.Line.SetEndTime((float)ship.Origin.T);
                    laser.Tracers[ship] = new LaserTracer(ship, laser.Ship.Color, p, laser.Line.Phi + MathF.PI);
                    ship.Health -= 10;
                }
            }
        }
    }
    
    public static void Collide(Asteroid asteroid, Laser laser) //broken
    {
        Vector2? point = laser.Line.PositionAtZ((float)asteroid.Origin.T);
        if (point != null) {
            Vector2 p = (Vector2)point;
            if (Vector2.DistanceSquared(p, asteroid.Origin.ToVector2()) < Math.Pow(asteroid.Radius, 2))
            {
                laser.Line.SetEndTime((float)asteroid.Origin.T);
                asteroid.Despawn();
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
    
    public static void Collide(Mine mine, Laser laser)
    {
        if (mine.Flags == 0)
        {
            //find the point of intersection between the bullet line and the mine polygon
            //create a new worldline event for the at that point and set the mine's flags to detonate
            //MinkowskiVector? point = bullet.Line.IntersectsAt(mine.Origin);
            Vector2? point = laser.Line.PositionAtZ((float)mine.Origin.T);
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

    public static void Collide(MotileEntity a, MotileEntity b)
    {
        a.Acceleration = Vector2.Zero;
        b.Acceleration = Vector2.Zero;
        //newtons gravitation
        /*
        if (a.Mass != 0 || b.Mass != 0)
        {
            double distance = Vector2.Distance(a.Origin.ToVector2(), b.Origin.ToVector2());
            //a=f/m
            double force;
                    
            force = Config.G * ((a.Mass * b.Mass) / (Math.Pow(distance, Config.F)));

            if (distance < a.Radius + b.Radius)
            {
                PathsD solution = Clipper.Intersect(new PathsD {Clipper.TranslatePath(a.Polygon, a.Origin.X, a.Origin.Y)}, new PathsD {Clipper.TranslatePath(b.Polygon, b.Origin.X, b.Origin.Y)}, FillRule.NonZero, 0);
                bool collided = solution.Any(path => Clipper.Area(path) > 0.0001);
                if (collided)
                {
                    //Console.WriteLine("collided");
                    Vector2 normal = Vector2.Normalize(ComputeNormal(solution));
                    //Console.WriteLine(normal);
                    Vector2 relativeVelocity = a.Velocity - b.Velocity;
                    
                    float relativeNormal = Vector2.Dot(relativeVelocity, normal);
                    //Console.WriteLine(relativeNormal);
                    
                    a.Velocity = a.Velocity - ((2 * b.Mass)/(a.Mass + b.Mass) * relativeNormal * normal);
                    //Console.WriteLine(a.Velocity);
                    b.Velocity = b.Velocity + ((2 * a.Mass)/(a.Mass + b.Mass) * relativeNormal * normal);
                }
            }
            
            double aAccel = Math.Min(-force / a.Mass, Math.Pow(Config.C, 0.5));
            double bAccel = Math.Min(-force / b.Mass, Math.Pow(Config.C, 0.5));
            
            //for a
            Vector2 aVec = a.Origin.ToVector2() - b.Origin.ToVector2();
            aVec.Normalize();
            a.Acceleration += aVec * (float)aAccel;
            
            //for b
            Vector2 bVec = b.Origin.ToVector2() - a.Origin.ToVector2();
            bVec.Normalize();
            b.Acceleration += bVec * (float)bAccel;
        }*/
    }
    
    public static Vector2 ComputeNormal(PathsD intersectionPaths)
{
    if (intersectionPaths == null || intersectionPaths.Count == 0)
        throw new ArgumentException("Intersection paths must not be null or empty.");

    // collect boundary points and edge normals + lengths
    var points = new List<Vector2>();
    var edgeNormals = new List<Vector2>();
    var edgeLengths = new List<float>();

    foreach (var path in intersectionPaths)
    {
        if (path == null || path.Count < 2) continue;
        for (int i = 0; i < path.Count; i++)
        {
            PointD p1 = path[i];
            PointD p2 = path[(i + 1) % path.Count];

            var v1 = new Vector2((float)p1.x, (float)p1.y);
            var v2 = new Vector2((float)p2.x, (float)p2.y);

            points.Add(v1);

            var edge = v2 - v1;
            float len = edge.Length();
            if (len <= 1e-8f) continue; // degenerate edge

            // edge normal (perpendicular). Note: sign depends on winding.
            var n = new Vector2(-edge.Y, edge.X) / len;
            edgeNormals.Add(n);
            edgeLengths.Add(len);
        }
    }

    if (points.Count == 0)
        return new Vector2(1f, 0f); // arbitrary fallback

    // centroid of intersection points
    Vector2 centroid = Vector2.Zero;
    foreach (var p in points) centroid += p;
    centroid /= points.Count;

    // covariance elements (use double for stability)
    double sxx = 0, syy = 0, sxy = 0;
    foreach (var p in points)
    {
        double dx = p.X - centroid.X;
        double dy = p.Y - centroid.Y;
        sxx += dx * dx;
        syy += dy * dy;
        sxy += dx * dy;
    }
    int npts = points.Count;
    sxx /= npts;
    syy /= npts;
    sxy /= npts;

    // principal axis angle (analytic for 2x2)
    double theta = 0.5 * Math.Atan2(2.0 * sxy, sxx - syy);
    var axis = new Vector2((float)Math.Cos(theta), (float)Math.Sin(theta)); // major axis direction

    // normal is perpendicular to major axis
    var normal = new Vector2(-axis.Y, axis.X);
    if (normal.LengthSquared() < 1e-12f)
    {
        // Degenerate case (near-circular or numerical). Fallback to weighted average of edge normals.
        Vector2 avg = Vector2.Zero;
        float totalLen = 0f;
        for (int i = 0; i < edgeNormals.Count; i++)
        {
            avg += edgeNormals[i] * edgeLengths[i];
            totalLen += edgeLengths[i];
        }
        if (totalLen > 0f)
        {
            avg /= totalLen;
            if (avg.LengthSquared() > 1e-10f) return Vector2.Normalize(avg);
        }
        return new Vector2(1f, 0f);
    }

    normal = Vector2.Normalize(normal);

    // pick sign: make normal align with the majority of edge normals (weighted by edge length)
    double dotSum = 0;
    for (int i = 0; i < edgeNormals.Count; i++)
    {
        dotSum += Vector2.Dot(edgeNormals[i], normal) * edgeLengths[i];
    }
    if (dotSum < 0) normal = -normal;

    return normal;
}
}