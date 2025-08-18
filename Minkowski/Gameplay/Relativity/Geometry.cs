using Microsoft.Xna.Framework;
using Clipper2Lib;
using Microsoft.Xna.Framework.Graphics;
using ProjectMinkowski.Rendering;

namespace ProjectMinkowski.Relativity {

    public class Line
    {
        public MinkowskiVector Origin;
        public float Theta; //speed
        public float Phi; //rotation
        public float Length;

        public Vector3 Direction;

        public Line(MinkowskiVector origin, float theta, float phi, float length = 10000)
        {
            Origin = origin;
            Theta = (float)Math.Atan(theta);
            Phi = phi;
            Length = length;
            Direction = new Vector3(
                (float)Math.Cos(Theta),
                (float)(Math.Sin(Theta) * Math.Cos(Phi)),
                (float)(Math.Sin(Theta) * Math.Sin(Phi))
            );
        }

        public void SetEndTime(float time)
        {
            Length = (float)(time - Origin.T);
        }

        public bool Intersects(MinkowskiVector point, int radius = 20)
        {
            
            Vector3 v = (point - Origin).ToVector3();
            float dot = Vector3.Dot(v, Direction);
            float s = Math.Max(0, Math.Min(Length, dot));
            Vector3 C = Origin.ToVector3() + s * Direction;
            double check = Math.Pow(C.Z - point.T, 2) + Math.Pow(C.X - point.X, 2) + Math.Pow(C.Y - point.Y, 2);
            if (check < Math.Pow(radius, 2))
            {
                return true;
            }
            return false;
        }
        
        public MinkowskiVector? IntersectsAt(MinkowskiVector point, int radius = 20)
        {
            // Convert segment to 3D vectors: (T, X, Y)
            Vector3 v = (point - Origin).ToVector3();
            float dot = Vector3.Dot(v, Direction);
            float s = Math.Max(0, Math.Min(Length, dot));
            Vector3 closest = Origin.ToVector3() + s * Direction;

            // Check Euclidean distance in (T, X, Y)
            double distSquared = Math.Pow(closest.Z - point.T, 2) +
                                 Math.Pow(closest.X - point.X, 2) +
                                 Math.Pow(closest.Y - point.Y, 2);

            if (distSquared < Math.Pow(radius, 2))
            {
                // Return the closest point as MinkowskiVector (T, X, Y) = (closest.Z, closest.X, closest.Y)
                return new MinkowskiVector(closest.Z, closest.X, closest.Y);
            }
            return null;
        }

        
        public Vector2? PositionAtZ(float z)
        {
            if (Math.Abs(Math.Cos(Theta)) < 1e-6)
            {
                return null;
            }
            
            float t = (float)((z - Origin.T) / Math.Cos(Theta));
            if (t < 0 || t > Length)
            {
                return null;
            }
            
            float x = (float)(Origin.X + (z - Origin.T) * Math.Tan(Theta) * Math.Cos(Phi));
            float y = (float)(Origin.Y + (z - Origin.T) * Math.Tan(Theta) * Math.Sin(Phi));
            return new Vector2(x, y);
        }
        
        public float? RotationAtZ(double time)
        {
            if (time > Origin.T && time < (Origin.T + Length))
            {
                return Phi;
            }
            return null;
        }
    }
    
    public class Worldline {
        public List<WorldlineEvent> Events { get; } = new();

        public void AddEvent(WorldlineEntity entity) {
            var newData = entity.GetWorldlineData();
            var lastEvent = Events.Count > 0 ? Events[Events.Count - 1] : null;
            
            if (lastEvent == null || !Equals(lastEvent.Data, newData))
                Events.Add(new WorldlineEvent { Origin = entity.Origin.Clone(), Data = newData });
        }

        public bool HasVisibleEvent(MinkowskiVector origin)
            => GetVisibleEventIndex(origin) >= 0;

        public int GetVisibleEventIndex(MinkowskiVector origin) //only works at subluminal speeds
        {
            if (Events.Count == 0)
                return -1;
        
            Vector2 observerPos = new((float)origin.X, (float)origin.Y);
            float observerTime = (float)origin.T; //todo: adjust for curved spacetime
        
            int low = 0, high = Events.Count - 1, resultIndex = -1;
        
            while (low <= high)
            {
                int mid = (low + high) / 2;
                var evt = Events[mid];
                double arrivalTime = evt.Origin.T + Vector2.Distance(observerPos, evt.Origin.ToVector2()) / Config.C - 0.001f;
        
                if (arrivalTime <= observerTime)
                {
                    resultIndex = mid;
                    low = mid + 1;
                }
                else
                {
                    high = mid - 1;
                }
            }
        
            return resultIndex;
        }

        public T? GetVisibleVariable<T>(MinkowskiVector origin, string variable, bool interpolate = false)
        {
            int idx = GetVisibleEventIndex(origin);
            if (idx < 0)
                return default;

            // If not interpolating or at the end, just return the value
            if (!interpolate || idx == Events.Count - 1)
                return Events[idx].Get<T>(variable);

            // Next event, for interpolation
            var evt0 = Events[idx];
            var evt1 = Events[idx + 1];

            Vector2 observerPos = new((float)origin.X, (float)origin.Y);
            float observerTime = (float)origin.T;

            double arrivalTime0 = evt0.Origin.T + Vector2.Distance(observerPos, evt0.Origin.ToVector2()) / Config.C;
            double arrivalTime1 = evt1.Origin.T + Vector2.Distance(observerPos, evt1.Origin.ToVector2()) / Config.C;

            if (observerTime < arrivalTime1)
            {
                // Interpolate if both events have the variable
                bool has0 = evt0.Data.TryGetValue(variable, out var v0);
                bool has1 = evt1.Data.TryGetValue(variable, out var v1);

                if (has0 && has1 && v0?.GetType() == v1?.GetType())
                {
                    double alpha = (arrivalTime1 == arrivalTime0)
                        ? 0
                        : (observerTime - arrivalTime0) / (arrivalTime1 - arrivalTime0);

                    // Basic type interpolation
                    if (v0 is float f0 && v1 is float f1)
                        return (T)(object)((float)(f0 + (f1 - f0) * alpha));
                    if (v0 is int i0 && v1 is int i1)
                        return (T)(object)((int)Math.Round(i0 + (i1 - i0) * alpha));
                    if (v0 is Vector2 vec0 && v1 is Vector2 vec1)
                        return (T)(object)Vector2.Lerp(vec0, vec1, (float)alpha);
                    // Add more types as needed

                    // Not interpolatable? Just return the earlier
                    return (T)v0!;
                }
                else if (has0)
                    return (T)v0!;
                else if (has1)
                    return (T)v1!;
                else
                    return default;
            }
            else
            {
                // observerTime >= arrivalTime1, so just take evt1
                return evt1.Get<T>(variable);
            }
        }
    }
    
    public class Worldcone {
        public readonly MinkowskiVector Apex;         // Apex of the cone in spacetime
        public readonly float Angle;                  // Expansion rate (0 < Angle ≤ 1), as fraction of speed of light
        public readonly int TemporalDirection;        // +1 (forward), -1 (backward)
        public readonly int Height;                   // Height of the cone, -1 for infinite

        public Worldcone(MinkowskiVector apex, float angle, int temporalDirection, int height = -1) {
            Apex = apex;
            Height = height;
            Angle = MathHelper.Clamp(angle, 0f, 1f);
            TemporalDirection = Math.Clamp(temporalDirection, -1, 1);
        }

        /// <summary>
        /// Checks if a point lies on the cone shell (within epsilon margin).
        /// </summary>
        public bool IsOnShell(MinkowskiVector point, float epsilon = 0.01f) {
            if (!IsInTemporalRange(point)) return false;

            float dt = (float)(point.T - Apex.T) * TemporalDirection;
            if (dt < 0) return false;

            Vector2 dx = new((float)(point.X - Apex.X), (float)(point.Y - Apex.Y));
            float radius = dx.Length();
            float expectedRadius = (float)(Config.C * Angle * dt);

            return Math.Abs(radius - expectedRadius) <= epsilon;
        }

        /// <summary>
        /// Checks if a point is within the interior of the cone (not just on the shell).
        /// </summary>
        public bool IsInside(MinkowskiVector point) {
            if (!IsInTemporalRange(point)) return false;

            float dt = (float)(point.T - Apex.T) * TemporalDirection;
            if (dt < 0) return false;

            Vector2 dx = new((float)(point.X - Apex.X), (float)(point.Y - Apex.Y));
            float radius = dx.Length();

            return radius <= Config.C * Angle * dt;
        }

        /// <summary>
        /// Enforces temporal orientation: no visibility "upstream"
        /// </summary>
        private bool IsInTemporalRange(MinkowskiVector point) {
            switch (TemporalDirection) {
                case 1: // Forward in time
                    if (Height < 0)
                        return point.T >= Apex.T;
                    else
                        return point.T >= Apex.T && point.T <= Apex.T + Height;
                case -1: // Backward in time
                    if (Height < 0)
                        return point.T <= Apex.T;
                    else
                        return point.T <= Apex.T && point.T >= Apex.T - Height;
                case 0: // No temporal direction; degenerate case
                    return true;
                default:
                    return false;
            }
        }
    }
    
    public class Arc {
        public MinkowskiVector Center;  // Center of the ellipse
        public float RadiusX;           // Semi-major axis (X radius)
        public float RadiusY;           // Semi-minor axis (Y radius)
        public float AngleStart;        // Start angle in radians
        public float AngleEnd;          // End angle in radians
        public Vector2 Rotation;

        public Arc(MinkowskiVector center, float radiusX, float radiusY, float angleStart, float angleEnd, Vector2 rotation) {
            Center = center;
            RadiusX = radiusX;
            RadiusY = radiusY;
            AngleStart = angleStart;
            AngleEnd = angleEnd;
            Rotation = rotation;
        }
        
        public Arc(MinkowskiVector center, float radius, float angleStart, float angleEnd)
            : this(center, radius, radius, angleStart, angleEnd, Vector2.Zero) {
        }

        public override string ToString() =>
            $"Arc[Center={Center}, Radius=({RadiusX}, {RadiusY}), Angle=({AngleStart}, {AngleEnd})]";

        public VertexPositionColor[] ToVertices(int segmentCount = 64)
        {
            var verts = new VertexPositionColor[segmentCount + 1];
            float angleSpan = AngleEnd - AngleStart;

            Vector2 majorAxis = Rotation;
            if (majorAxis.LengthSquared() < 1e-6f)
                majorAxis = Vector2.UnitX; // fallback if uninitialized

            majorAxis = Vector2.Normalize(majorAxis);
            Vector2 minorAxis = new Vector2(-majorAxis.Y, majorAxis.X); // 90° rotation

            for (int i = 0; i <= segmentCount; i++) {
                float t = i / (float)segmentCount;
                float angle = AngleStart + t * angleSpan;

                float cos = MathF.Cos(angle);
                float sin = MathF.Sin(angle);

                float x = (float)Center.X + cos * RadiusX * majorAxis.X + sin * RadiusY * minorAxis.X;
                float y = (float)Center.Y + cos * RadiusX * majorAxis.Y + sin * RadiusY * minorAxis.Y;

                verts[i] = new VertexPositionColor {
                    Position = new Vector3(x, y, 0f),
                    Color = Color.White
                };
            }

            return verts;
        }
        
        public Arc ToLorentzTransformed(FrameOfReference frame) {
            // Step 1: Lorentz transform the center
            MinkowskiVector transformedCenter = frame.ToLocal(Center);

            Vector2 velocity = frame.Velocity;
            float speed = velocity.Length();

            if (speed < 1e-5f)
                return new Arc(transformedCenter, RadiusX, RadiusY, AngleStart, AngleEnd, Vector2.Zero);

            Vector2 direction = Vector2.Normalize(velocity);
            Vector2 perpendicular = new Vector2(-direction.Y, direction.X);

            float gamma = FrameOfReference.Gamma(velocity); // 1 / sqrt(1 - v^2)

            // Step 2: Construct ellipse axes analytically
            float radiusParallel = RadiusX / gamma;
            float radiusPerpendicular = RadiusY; // unchanged

            // Optional: if RadiusX != RadiusY, treat it as a full ellipse and transform both axes

            // Step 3: Reconstruct ellipse aligned to motion direction
            // We'll use direction & perpendicular as new basis vectors
            // and treat the ellipse as axis-aligned in this local frame

            // Arc represents an ellipse oriented with the motion vector
            return new Arc(
                transformedCenter,
                radiusParallel,
                radiusPerpendicular,
                AngleStart,
                AngleEnd,
                direction // add this if Arc supports orientation
            );
        }

    }

    public static class Transformations
    {
        public static PathD Translate(PathD path, double dx, double dy)
        {
            return new PathD(path.Select(p => new PointD(p.x + dx, p.y + dy)));
        }
        
        public static PathD Rotate(PathD path, double angleRadians)
        {
            double cos = Math.Cos(angleRadians);
            double sin = Math.Sin(angleRadians);

            return new PathD(path.Select(p =>
                new PointD(
                    p.x * cos - p.y * sin,
                    p.x * sin + p.y * cos
                )
            ));
        }
        
        public static PathD Scale(PathD path, double scale)
        {
            return new PathD(path.Select(p => new PointD(p.x * scale, p.y * scale)));
        }
        
        public static PathD Scale(PathD path, double scaleX, double scaleY)
        {
            return new PathD(path.Select(p => new PointD(p.x * scaleX, p.y * scaleY)));
        }
        
        public static VertexPositionColor[] ToVertexArray(
            PathD path, Color color, float z = 0f)
        {
            var verts = new VertexPositionColor[path.Count + 1];
            for (int i = 0; i < path.Count; i++)
            {
                var p = path[i];
                verts[i] = new VertexPositionColor(
                    new Vector3((float)p.x, (float)p.y, z),
                    color
                );
            }
            verts[path.Count] = verts[0];
            return verts;
        }
    }

    public static class World { //todo: gpu compute
        // Worldline - Worldline
        public static bool Intersects(Worldline a, Worldline b) {
            // TODO: implement parametric Minkowski line intersection
            return false;
        }
        
        /// <summary>
        /// Returns the first intersection (if any) of a Worldline with a Worldcone shell.
        /// Equivalent to when the line "enters" the radar ping or light cone shell.
        /// </summary>
        public static WorldlineEvent? Intersects(Worldline line, Worldcone cone) { //todo: cleanup
            var events = line.Events;
            float slope = (float)(cone.Angle * Config.C); // spatial growth rate

            for (int i = events.Count - 2; i >= 0; i--) {
                var a = events[i];
                var b = events[i + 1];

                var am = a.Origin;
                var bm = b.Origin;

                MinkowskiVector da = am - cone.Apex;
                MinkowskiVector db = bm - cone.Apex;

                float ta = (float)(da.T * cone.TemporalDirection); // oriented time
                float tb = (float)(db.T * cone.TemporalDirection);

                if (ta < 0 && tb < 0) continue; // both before/after cone in time

                float ra = new Vector2((float)da.X, (float)da.Y).Length();
                float rb = new Vector2((float)db.X, (float)db.Y).Length();

                float expectedRa = ta * slope;
                float expectedRb = tb * slope;

                bool aInside = ra <= expectedRa;
                bool bInside = rb <= expectedRb;

                // Transition from outside → inside in temporal direction
                if (!aInside && bInside && ta >= 0 && tb >= 0) {
                    float blend = (expectedRa - ra) / ((expectedRb - rb) - (expectedRa - ra));

                    // return new WorldlineEvent(
                    // new MinkowskiVector(
                    //     MathHelper.Lerp((float)a.Origin.T, (float)b.Origin.T, blend),
                    //     MathHelper.Lerp((float)a.Origin.X, (float)b.Origin.X, blend),
                    //     MathHelper.Lerp((float)a.Origin.Y, (float)b.Origin.Y, blend)
                    // ),
                    //     MathHelper.Lerp(a.Rotation, b.Rotation, blend),
                    //     Vector2.Lerp(a.Velocity, b.Velocity, blend)
                    // );

                    Dictionary<string, object> newData = new Dictionary<string, object>()
                    {
                        { "Rotation", MathHelper.Lerp(a.Get<float>("Rotation"), b.Get<float>("Rotation"), blend) },
                        { "Velocity", Vector2.Lerp(a.Get<Vector2>("Velocity"), b.Get<Vector2>("Velocity"), blend) }
                    };

                    return new WorldlineEvent
                    {
                        Origin = new MinkowskiVector(
                            MathHelper.Lerp((float)a.Origin.T, (float)b.Origin.T, blend),
                            MathHelper.Lerp((float)a.Origin.X, (float)b.Origin.X, blend),
                            MathHelper.Lerp((float)a.Origin.Y, (float)b.Origin.Y, blend)
                        ),
                        Data = newData
                    };
                }

                // Already inside cone in temporal direction
                if (aInside && bInside && ta >= 0 && tb >= 0) {
                    return b;
                }
            }

            return null;
        }

        public static Arc? Intersects(Worldcone forward, Worldcone backwards)
        {
            //get the point where the cones "touch", ie when the front enters the lightcone
            //get a slice from each cone
            //do simple circle intersection
            //return arc

            Vector2 vForwards = new Vector2((float)forward.Apex.X, (float)forward.Apex.Y);
            Vector2 vBackwards = new Vector2((float)backwards.Apex.X, (float)backwards.Apex.Y);
            float tDetect = //point in time of intersection
                (float)(((vBackwards - vForwards).Length() + forward.Angle * forward.Apex.T +
                         backwards.Angle * backwards.Apex.T) / (forward.Angle + backwards.Angle));
            return Intersects(forward, backwards, tDetect);
        }
        
        public static Arc? Intersects(Worldcone a, Worldcone b, float globalTime) {
            // Console.WriteLine($"[DEBUG] Intersecting at t = {globalTime}");
            // Console.WriteLine($"  Cone A apex = {a.Apex}, dir = {a.TemporalDirection}");
            // Console.WriteLine($"  Cone B apex = {b.Apex}, dir = {b.TemporalDirection}");

            float dtA = globalTime - (float)a.Apex.T;
            float dtB = (float)b.Apex.T - globalTime;
            
            if (dtA < 0 || dtB < 0)
                return null; // One of the cones hasn't reached this time slice yet

            float radiusA = (float)(a.Angle * Config.C * dtA);
            float radiusB = (float)(b.Angle * Config.C * dtB);

            Vector2 centerA = new Vector2((float)a.Apex.X, (float)a.Apex.Y);
            Vector2 centerB = new Vector2((float)b.Apex.X, (float)b.Apex.Y);

            // Console.WriteLine($"  ΔtA = {dtA}, ΔtB = {dtB}");
            // Console.WriteLine($"  RadiusA = {radiusA}, RadiusB = {radiusB}");
            // Console.WriteLine($"  CenterA = {centerA}, CenterB = {centerB}");

            Vector2 offset = centerB - centerA;
            float d = offset.Length();

            if (d > radiusA + radiusB ) {
                // No intersection
                return null;
            }
            
            if (d < Math.Abs(radiusA - radiusB))
            {
                // Return a full circle from the perspective of the outer circle
                return new Arc(
                    new MinkowskiVector(globalTime, centerA.X, centerA.Y),
                    radiusA,
                    0,
                    MathHelper.TwoPi
                );
            }

            if (d < 1e-6f && Math.Abs(radiusA - radiusB) < 1e-6f) {
                // Full overlap
                return new Arc(new MinkowskiVector(globalTime, centerA.X, centerA.Y), radiusA, 0, MathHelper.TwoPi);
            }

            // Console.WriteLine($"  Distance between centers = {d}");
            // Console.WriteLine($"  Sum of radii = {radiusA + radiusB}");

            // Compute angular span
            float a0 = MathF.Atan2(offset.Y, offset.X);
            float cosAlpha = (radiusA * radiusA + d * d - radiusB * radiusB) / (2 * radiusA * d);
            float alpha = MathF.Acos(Math.Clamp(cosAlpha, -1f, 1f));

            float angleStart = a0 - alpha;
            float angleEnd = a0 + alpha;

            return new Arc(
                new MinkowskiVector(globalTime, centerA.X, centerA.Y),
                radiusA,
                angleStart,
                angleEnd
            );
        }
    }
}