using Microsoft.Xna.Framework;

namespace ProjectMinkowski;

//How does any of this work? Truthfully, I have no idea. Black magic and dark science.

public class MinkowskiVector {
    public double T; // time component (in light-seconds)
    public double X;
    public double Y;

    public MinkowskiVector(double t, double x, double y) {
        T = t;
        X = x;
        Y = y;
    }

    public MinkowskiVector(MinkowskiVector vector)
    {
        T = vector.T;
        X = vector.X;
        Y = vector.Y;
    }

    /// <summary>
    // Minkowski interval squared: s² = t² - x² - y²
    /// </summary>
    public double IntervalSquared() {
        double ct = T * Config.C;
        return ct * ct - X * X - Y * Y;
    }

    public bool IsTimelike() => IntervalSquared() > 0;
    public bool IsSpacelike() => IntervalSquared() < 0;
    public bool IsLightlike(double tolerance = 1e-10) => Math.Abs(IntervalSquared()) < tolerance;

    /// <summary>
    // Minkowski "dot product" using (+, –, –) metric
    /// </summary>
    public double Dot(MinkowskiVector other) =>
        T * other.T - X * other.X - Y * other.Y;

    /// <summary>
    // Apply Lorentz transform into a frame moving at velocity v (relative to current frame)
    /// </summary>
    public MinkowskiVector LorentzTo(Vector2 frameVelocity) {
        double vMag = Math.Sqrt(frameVelocity.X * frameVelocity.X + frameVelocity.Y * frameVelocity.Y);
        if (vMag == 0) return this;

        double gamma = Gamma(vMag);
        double vx = frameVelocity.X;
        double vy = frameVelocity.Y;

        double vDotR = vx * X + vy * Y;

        double tPrime = gamma * (T - vDotR);
        double xPrime = X + ((gamma - 1) * vDotR / (vMag * vMag) - gamma * T) * vx;
        double yPrime = Y + ((gamma - 1) * vDotR / (vMag * vMag) - gamma * T) * vy;

        return new MinkowskiVector(tPrime, xPrime, yPrime);
    }

    /// <summary>
    // Apply inverse Lorentz transform (from moving frame back to global)
    /// </summary>
    public MinkowskiVector LorentzFrom(Vector2 frameVelocity) {
        double vMag = Math.Sqrt(frameVelocity.X * frameVelocity.X + frameVelocity.Y * frameVelocity.Y);
        if (vMag == 0) return this;

        double gamma = Gamma(vMag);
        double vx = frameVelocity.X;
        double vy = frameVelocity.Y;

        double vDotR = vx * X + vy * Y;

        double tGlobal = gamma * (T + vDotR);
        double xGlobal = X + ((gamma - 1) * vDotR / (vMag * vMag) + gamma * T) * vx;
        double yGlobal = Y + ((gamma - 1) * vDotR / (vMag * vMag) + gamma * T) * vy;

        return new MinkowskiVector(tGlobal, xGlobal, yGlobal);
    }

    public static double Gamma(double vMag) => //this doesnt belong here
        1.0 / Math.Sqrt(1 - vMag * vMag);

    public static MinkowskiVector operator +(MinkowskiVector a, MinkowskiVector b) =>
        new(a.T + b.T, a.X + b.X, a.Y + b.Y);

    public static MinkowskiVector operator -(MinkowskiVector a, MinkowskiVector b) =>
        new(a.T - b.T, a.X - b.X, a.Y - b.Y);

    public static MinkowskiVector operator *(MinkowskiVector a, double scalar) =>
        new(a.T * scalar, a.X * scalar, a.Y * scalar);

    public override string ToString() => $"(T={T:F3}, X={X:F3}, Y={Y:F3})";
}

public struct WorldlineEvent {
    public float T { get; }
    public Vector2 Position { get; }
    public float Rotation { get; }
    public Vector2 Velocity { get; }

    public WorldlineEvent(float t, Vector2 position, float rotation, Vector2 velocity) {
        T = t;
        Position = position;
        Rotation = rotation;
        Velocity = velocity;
    }

    public MinkowskiVector ToMinkowski() => new(T, Position.X, Position.Y);
}
public class Worldline {
    public List<WorldlineEvent> Events { get; } = new();

    public void AddEvent(WorldlineEvent evt) {
        Events.Add(evt);
    }

    public void Prune(float currentTime, float maxAge = 30f) {
        Events.RemoveAll(e => currentTime - e.T > maxAge);
    }

    /// <summary>
    // Interpolate visible event as seen from observer spacetime position
    /// </summary>
    public WorldlineEvent? GetVisibleFrom(MinkowskiVector observer) {
        for (int i = Events.Count - 2; i >= 0; i--) {
            var a = Events[i];
            var b = Events[i + 1];

            double sa = (observer - a.ToMinkowski()).IntervalSquared();
            double sb = (observer - b.ToMinkowski()).IntervalSquared();

            bool aVisible = sa >= 0 && a.T <= observer.T;
            bool bVisible = sb >= 0 && b.T <= observer.T;

            if (!aVisible && bVisible) {
                double t = sa / (sa - sb);

                return new WorldlineEvent(
                    MathHelper.Lerp(a.T, b.T, (float)t),
                    Vector2.Lerp(a.Position, b.Position, (float)t),
                    MathHelper.Lerp(a.Rotation, b.Rotation, (float)t),
                    Vector2.Lerp(a.Velocity, b.Velocity, (float)t)
                );
            } else if (aVisible && bVisible) {
                return b;
            }
        }

        return null;
    }
}

public class FrameOfReference {
    public MinkowskiVector Origin { get; set; } // Position in global spacetime/3D minowski space/absolute position
    public Vector2 Velocity { get; set; } // Velocity in the frame

    public FrameOfReference(MinkowskiVector origin, Vector2 velocity) {
        Origin = origin;
        Velocity = velocity;
    }
    
    public static float Gamma(Vector2 v) {
        float c = (float)Config.C;
        float speed2 = v.LengthSquared();
        return 1f / MathF.Sqrt(1f - speed2 / (c * c));
    }

    /// <summary>
    /// Converts a global spacetime event into this local frame.
    /// </summary>
    public MinkowskiVector ToLocal(MinkowskiVector globalEvent) {
        var delta = globalEvent - Origin;
        return delta.LorentzTo(Velocity);
    }

    /// <summary>
    /// Converts a local event in this frame into global spacetime.
    /// </summary>
    public MinkowskiVector ToGlobal(MinkowskiVector localEvent) {
        var transformed = localEvent.LorentzFrom(Velocity);
        return transformed + Origin;
    }

    public static Vector2[] ApplyLengthContractionInFrame(
        Vector2[] vertices,
        Vector2 center,
        FrameOfReference observerFrame,
        Vector2 objectVelocityGlobal
    ) {
        // Step 1: Transform object's global velocity to observer frame
        Vector2 relativeVelocity = objectVelocityGlobal - observerFrame.Velocity;

        float speedSq = relativeVelocity.LengthSquared();
        float c = (float)Config.C;

        if (speedSq == 0 || speedSq >= c * c)
            return (Vector2[])vertices.Clone();

        float gamma = 1f / MathF.Sqrt(1f - speedSq / (c * c));
        float contraction = 1f / gamma;

        Vector2 direction = Vector2.Normalize(relativeVelocity);
        var contracted = new Vector2[vertices.Length];

        for (int i = 0; i < vertices.Length; i++) {
            Vector2 relative = vertices[i] - center;

            // Project onto direction of motion
            float parallelMag = Vector2.Dot(relative, direction);
            Vector2 parallel = direction * parallelMag;
            Vector2 orthogonal = relative - parallel;

            // Contract the parallel component
            Vector2 contractedRelative = orthogonal + parallel * contraction;
            contracted[i] = center + contractedRelative;
        }

        return contracted;
    }

    
    public override string ToString() =>
        $"Frame[Origin={Origin}, Velocity=({Velocity.X:F2}, {Velocity.Y:F2})]";
}