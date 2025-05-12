using Microsoft.Xna.Framework;
using System;

namespace ProjectMinkowski.Relativity {
    public class Worldline {
        public List<WorldlineEvent> Events { get; } = new();

        public void AddEvent(WorldlineEvent evt) {
            Events.Add(evt);
        }

        public void Prune(float currentTime, float maxAge = 30f) {
            Events.RemoveAll(e => currentTime - e.Origin.T > maxAge);
        }
    }
    
    public class Worldcone {
        public MinkowskiVector Apex;         // Apex of the cone in spacetime
        public float Angle;                  // Expansion rate (0 < Angle ≤ 1), as fraction of speed of light
        public int TemporalDirection;        // +1 (forward), -1 (backward), 0 (undefined)

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
        public MinkowskiVector Center; //center of the arc
        public float Radius;
        public float AngleStart; //radians
        public float AngleEnd;

        public Arc(MinkowskiVector center, float radius, float angleStart, float angleEnd) {
            Center = center;
            Radius = radius;
            AngleStart = angleStart;
            AngleEnd = angleEnd;
        }
        public Vector2[] ToVertices(int segmentCount = 64) {
            var verts = new Vector2[segmentCount + 1];
            float angleSpan = AngleEnd - AngleStart;

            for (int i = 0; i <= segmentCount; i++) {
                float t = i / (float)segmentCount;
                float angle = AngleStart + t * angleSpan;
                verts[i] = new Vector2((float)(Center.X + MathF.Cos(angle) * Radius), (float)(Center.Y + MathF.Sin(angle) * Radius));
            }
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

        // Worldcone - Worldcone
        public static Arc? Intersects(Worldcone a, Worldcone b, float globalTime) {
            Console.WriteLine($"[DEBUG] Intersecting at t = {globalTime}");
            Console.WriteLine($"  Cone A apex = {a.Apex}, dir = {a.TemporalDirection}");
            Console.WriteLine($"  Cone B apex = {b.Apex}, dir = {b.TemporalDirection}");

            // Step 1: Compute spatial radius of cone A at this time
            float dtA = globalTime - (float)a.Apex.T;
            if ((a.TemporalDirection > 0 && dtA < 0) ||
                (a.TemporalDirection < 0 && dtA > 0) ||
                a.TemporalDirection == 0) return null;

            dtA = Math.Abs(dtA);

            float radiusA = (float)(a.Angle * Config.C * dtA);
            Vector2 centerA = new Vector2((float)a.Apex.X, (float)a.Apex.Y);

            // Step 2: Repeat for cone B
            float dtB = globalTime - (float)b.Apex.T;
            if ((b.TemporalDirection > 0 && dtB < 0) ||
                (b.TemporalDirection < 0 && dtB > 0) ||
                b.TemporalDirection == 0) return null;

            dtB = Math.Abs(dtB);

            float radiusB = (float)(b.Angle * Config.C * dtB);
            Vector2 centerB = new Vector2((float)b.Apex.X, (float)b.Apex.Y);

            Console.WriteLine($"  ΔtA = {dtA}, ΔtB = {dtB}");
            Console.WriteLine($"  RadiusA = {radiusA}, RadiusB = {radiusB}");
            Console.WriteLine($"  CenterA = {centerA}, CenterB = {centerB}");
            
            // Step 3: Compute distance between centers
            Vector2 offset = centerB - centerA;
            float d = offset.Length();

            if (d > radiusA + radiusB || d < Math.Abs(radiusA - radiusB)) {
                // No intersection
                return null;
            }

            if (d < 1e-6f && Math.Abs(radiusA - radiusB) < 1e-6f) {
                // Full overlap
                return new Arc(new MinkowskiVector(globalTime, centerA.X, centerA.Y), radiusA, 0, MathHelper.TwoPi);
            }

            Console.WriteLine($"  Distance between centers = {Vector2.Distance(centerA, centerB)}");
            Console.WriteLine($"  Sum of radii = {radiusA + radiusB}"); 
            
            // Step 4: Compute angular span
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