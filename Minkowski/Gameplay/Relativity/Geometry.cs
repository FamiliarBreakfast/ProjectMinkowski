using Clipper2Lib;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Minkowski.Gameplay.Entities;

namespace Minkowski.Gameplay.Relativity {

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
        public const int MaxEvents = 1000; // Hard cap on worldline size
        public List<WorldlineEvent> Events { get; } = new();

        // Track last observed event index per observer (keyed by Ship.Id)
        private Dictionary<int, int> _lastObservedIndex = new();

        // Record that an observer has seen an event at this index
        public void RecordObservation(int observerId, int eventIndex)
        {
            if (eventIndex < 0) return;

            if (!_lastObservedIndex.TryGetValue(observerId, out int current) || eventIndex > current)
                _lastObservedIndex[observerId] = eventIndex;
        }

        // Prune events observed by all active observers
        public int PruneObservedEvents(IEnumerable<int> activeObserverIds)
        {
            if (Events.Count <= 1) return 0;

            var activeSet = new HashSet<int>(activeObserverIds);

            // Remove stale observer entries
            foreach (var id in _lastObservedIndex.Keys.Where(id => !activeSet.Contains(id)).ToList())
                _lastObservedIndex.Remove(id);

            // Find minimum observed index (all observers must have seen events up to this point)
            int minIndex = int.MaxValue;
            foreach (var observerId in activeSet)
            {
                if (!_lastObservedIndex.TryGetValue(observerId, out int idx))
                    return 0; // Observer hasn't observed yet - can't prune
                minIndex = Math.Min(minIndex, idx);
            }

            if (minIndex <= 0 || minIndex == int.MaxValue) return 0;

            // Prune events before minIndex
            Events.RemoveRange(0, minIndex);

            // Adjust tracked indices
            foreach (var key in _lastObservedIndex.Keys.ToList())
                _lastObservedIndex[key] -= minIndex;

            return minIndex;
        }

        public void AddEvent(WorldlineEntity entity) {
            var newData = entity.GetWorldlineData();
            var lastEvent = Events.Count > 0 ? Events[Events.Count - 1] : null;

            if (lastEvent == null || !Equals(lastEvent.Data, newData))
                Events.Add(new WorldlineEvent { Origin = entity.Origin.Clone(), Data = newData });

            // Enforce hard cap - remove oldest events
            while (Events.Count > MaxEvents)
                Events.RemoveAt(0);
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

    /// <summary>
    /// Analytical worldline for constant-velocity objects.
    /// Computes position at any time without event sampling.
    /// </summary>
    public class LinearWorldline {
        public MinkowskiVector SpawnOrigin { get; }
        public Vector2 Velocity { get; }

        // Additional tracked state (not position-dependent)
        public float Rotation { get; set; }
        public float RotationSpeed { get; set; }

        public LinearWorldline(MinkowskiVector origin, Vector2 velocity) {
            SpawnOrigin = origin.Clone();
            Velocity = velocity;
        }

        /// <summary>
        /// Analytical position at global time t.
        /// </summary>
        public MinkowskiVector GetPositionAt(float t) {
            float dt = t - (float)SpawnOrigin.T;
            return new MinkowskiVector(
                t,
                SpawnOrigin.X + Velocity.X * dt,
                SpawnOrigin.Y + Velocity.Y * dt
            );
        }

        /// <summary>
        /// Light-cone visibility: find what time the observer sees.
        /// Solves: observerTime = eventTime + distance(observer, position(eventTime)) / c
        /// </summary>
        public float? GetVisibleTime(MinkowskiVector observer) {
            float ox = (float)observer.X, oy = (float)observer.Y, ot = (float)observer.T;
            float sx = (float)SpawnOrigin.X, sy = (float)SpawnOrigin.Y, st = (float)SpawnOrigin.T;
            float vx = Velocity.X, vy = Velocity.Y;
            float c = Config.C;

            // position(t) = (sx + vx*(t-st), sy + vy*(t-st))
            // distance² = (ox - sx - vx*(t-st))² + (oy - sy - vy*(t-st))²
            // (ot - t)² * c² = distance²

            // Let τ = t - st (time since spawn)
            // (ot - st - τ)² * c² = (ox - sx - vx*τ)² + (oy - sy - vy*τ)²

            float dx0 = ox - sx, dy0 = oy - sy, dt0 = ot - st;

            // Expand: (dt0 - τ)²c² = (dx0 - vx*τ)² + (dy0 - vy*τ)²
            // c²dt0² - 2c²dt0*τ + c²τ² = dx0² - 2dx0*vx*τ + vx²τ² + dy0² - 2dy0*vy*τ + vy²τ²
            // (c² - vx² - vy²)τ² + (-2c²dt0 + 2dx0*vx + 2dy0*vy)τ + (c²dt0² - dx0² - dy0²) = 0

            float v2 = vx*vx + vy*vy;
            float a = c*c - v2;
            float b = -2*c*c*dt0 + 2*dx0*vx + 2*dy0*vy;
            float cCoef = c*c*dt0*dt0 - dx0*dx0 - dy0*dy0;

            // Handle edge case where a ≈ 0 (velocity ≈ c)
            if (MathF.Abs(a) < 1e-6f) {
                // Linear equation: b*τ + cCoef = 0
                if (MathF.Abs(b) < 1e-6f) return null;
                float tau = -cCoef / b;
                if (tau >= 0 && tau <= dt0) return st + tau;
                return null;
            }

            float discriminant = b*b - 4*a*cCoef;
            if (discriminant < 0) return null;

            float sqrtD = MathF.Sqrt(discriminant);
            float tau1 = (-b - sqrtD) / (2*a);
            float tau2 = (-b + sqrtD) / (2*a);

            // We want the largest τ ≤ dt0 and τ ≥ 0
            float? result = null;
            if (tau1 >= 0 && tau1 <= dt0) result = tau1;
            if (tau2 >= 0 && tau2 <= dt0 && (result == null || tau2 > result)) result = tau2;

            return result.HasValue ? st + result.Value : null;
        }

        /// <summary>
        /// Get the visible position from an observer's perspective.
        /// </summary>
        public Vector2? GetVisiblePosition(MinkowskiVector observer) {
            var t = GetVisibleTime(observer);
            if (t == null) return null;
            var pos = GetPositionAt(t.Value);
            return pos.ToVector2();
        }

        /// <summary>
        /// Get the rotation at the visible time.
        /// </summary>
        public float GetVisibleRotation(MinkowskiVector observer) {
            var t = GetVisibleTime(observer);
            if (t == null) return Rotation;
            float dt = t.Value - (float)SpawnOrigin.T;
            return Rotation + RotationSpeed * dt;
        }

        /// <summary>
        /// Get the decay time at the visible time, clamped to maxDecay.
        /// </summary>
        public float GetVisibleDecay(MinkowskiVector observer, float maxDecay) {
            var t = GetVisibleTime(observer);
            if (t == null) return 0;
            float dt = t.Value - (float)SpawnOrigin.T;
            return MathF.Min(dt, maxDecay);
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