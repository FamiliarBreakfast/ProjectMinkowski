using Microsoft.Xna.Framework;
using System;
using Clipper2Lib;
using Microsoft.Xna.Framework.Graphics;

namespace ProjectMinkowski.Relativity {
    public class Worldline {
        public List<WorldlineEvent> Events { get; } = new();

        public void AddEvent(WorldlineEvent evt) {
            Events.Add(evt);
        }

        public void Prune(float currentTime, float maxAge = 30f) {
            Events.RemoveAll(e => currentTime - e.Origin.T > maxAge);
        }
        
        public WorldlineEvent? GetVisibleEvent(MinkowskiVector origin)
        {
            Vector2 observerPos = new Vector2((float)origin.X, (float)origin.Y);
            float observerTime = (float)origin.T;
            if (Events.Count == 0)
                return null;

            int low = 0;
            int high = Events.Count - 1;
            int resultIndex = -1;

            while (low <= high)
            {
                int mid = (low + high) / 2;
                var evt = Events[mid];
                double arrivalTime = evt.Time + Vector2.Distance(observerPos, evt.Position) / Config.C;

                if (arrivalTime <= observerTime)
                {
                    resultIndex = mid;
                    low = mid + 1; // Try to find a later visible event
                }
                else
                {
                    high = mid - 1; // Too early
                }
            }

            return resultIndex >= 0 ? Events[resultIndex] : null;
        }
    }
    
    public class Worldcone {
        public readonly MinkowskiVector Apex;         // Apex of the cone in spacetime
        public readonly float Angle;                  // Expansion rate (0 < Angle ≤ 1), as fraction of speed of light
        public readonly int TemporalDirection;        // +1 (forward), -1 (backward), 0 (undefined)

        public Worldcone(MinkowskiVector apex, float angle, int temporalDirection) {
            Apex = apex;
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
            return TemporalDirection switch {
                1 => point.T >= Apex.T,
                -1 => point.T <= Apex.T,
                0 => true,
                _ => false
            };
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

                    return new WorldlineEvent(
                        new MinkowskiVector(
                            MathHelper.Lerp((float)a.Origin.T, (float)b.Origin.T, blend),
                            MathHelper.Lerp((float)a.Origin.X, (float)b.Origin.X, blend),
                            MathHelper.Lerp((float)a.Origin.Y, (float)b.Origin.Y, blend)
                        ),
                        MathHelper.Lerp(a.Rotation, b.Rotation, blend),
                        Vector2.Lerp(a.Velocity, b.Velocity, blend)
                    );
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