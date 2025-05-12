using Microsoft.Xna.Framework;
using System;

namespace ProjectMinkowski.Relativity {
    public class Worldline {
        public List<WorldlineEvent> Events { get; } = new();

        public void AddEvent(WorldlineEvent evt) {
            Events.Add(evt);
        }

        public void Prune(float currentTime, float maxAge = 30f) {
            Events.RemoveAll(e => currentTime - e.T > maxAge);
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

                var am = a.ToMinkowski();
                var bm = b.ToMinkowski();

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
                        MathHelper.Lerp(a.T, b.T, blend),
                        Vector2.Lerp(a.Position, b.Position, blend),
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
        public static bool Intersects(Worldcone a, Worldcone b) {
            // TODO: implement volumetric lightcone collision test
            return false;
        }
    }
}