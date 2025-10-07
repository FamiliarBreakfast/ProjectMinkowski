using Clipper2Lib;
using Microsoft.Xna.Framework;

namespace Minkowski.Gameplay.Relativity;

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

    public MinkowskiVector Clone()
    {
        return new MinkowskiVector(this.T, this.X, this.Y);
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

        // dot product v · r
        double vDotR = frameVelocity.X * X + frameVelocity.Y * Y;
        double c2 = Config.C * Config.C;

        // t' = γ (t - (v · r)/c²)
        double tPrime = gamma * (T - vDotR / c2);

        // Spatial transform
        double coeff = (gamma - 1) / (vMag * vMag);
        double xPrime = X + coeff * vDotR * frameVelocity.X - gamma * T * frameVelocity.X;
        double yPrime = Y + coeff * vDotR * frameVelocity.Y - gamma * T * frameVelocity.Y;

        // Scale by 1/c for the gamma * T * v subtraction (units: time * velocity = distance)
        xPrime += ( - gamma * T * frameVelocity.X );
        yPrime += ( - gamma * T * frameVelocity.Y );

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

    public static double Gamma(double vMag)
    {
        double beta = vMag / Config.C;
        return 1.0 / Math.Sqrt(1.0 - beta * beta);
    }

    public static MinkowskiVector operator +(MinkowskiVector a, MinkowskiVector b) =>
        new(a.T + b.T, a.X + b.X, a.Y + b.Y);

    public static MinkowskiVector operator -(MinkowskiVector a, MinkowskiVector b) =>
        new(a.T - b.T, a.X - b.X, a.Y - b.Y);

    public static MinkowskiVector operator *(MinkowskiVector a, double scalar) =>
        new(a.T * scalar, a.X * scalar, a.Y * scalar);

    public override string ToString() => $"(T={T:F3}, X={X:F3}, Y={Y:F3})";

    public Vector2 ToVector2() => new Vector2((float)X, (float)Y);
    public Vector3 ToVector3() => new Vector3((float)T, (float)X, (float)Y);
}

[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
public class WorldlineAttribute : Attribute { }

public class WorldlineEvent
{
    public required MinkowskiVector Origin;
    public Dictionary<string, object> Data = new();

    public T Get<T>(string name) => Data.TryGetValue(name, out var val) ? (T)val : default!;
}

public class FrameOfReference
{
    public Worldcone Lightcone;
    public Vector2 Velocity { get; set; } // Velocity in the frame

    public FrameOfReference(MinkowskiVector origin, Vector2 velocity)
    {
        Lightcone = new Worldcone(origin, 1, -1); //new backwards facing lightcone
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
        var delta = globalEvent - Lightcone.Apex;
        return delta.LorentzTo(Velocity);
    }

    /// <summary>
    /// Converts a local event in this frame into global spacetime.
    /// </summary>
    public MinkowskiVector ToGlobal(MinkowskiVector localEvent) {
        var transformed = localEvent.LorentzFrom(Velocity);
        return transformed + Lightcone.Apex;
    }
    
    public Vector2 LorentzTransformVelocity(Vector2 globalVelocity)
    {
        float c = (float)Config.C;
        Vector2 u = this.Velocity; // frame's velocity (what we're boosting against)

        float uSq = u.LengthSquared();
        if (uSq == 0f)
            return globalVelocity;

        float gamma = 1f / MathF.Sqrt(1f - uSq / (c * c));
        float dot = Vector2.Dot(globalVelocity, u);
        float uLen = MathF.Sqrt(uSq);

        // Project v into components
        Vector2 uHat = u / uLen;
        Vector2 vParallel = Vector2.Dot(globalVelocity, uHat) * uHat;
        Vector2 vPerp = globalVelocity - vParallel;

        // Apply relativistic formulas
        float denom = 1f - dot / (c * c);
        if (MathF.Abs(denom) < 1e-6f)
            denom = 1e-6f * MathF.Sign(denom); // clamp to avoid divide-by-zero

        Vector2 vPrimeParallel = (vParallel - u) / denom;
        Vector2 vPrimePerp = vPerp / (gamma * denom);

        return vPrimeParallel + vPrimePerp;
    }
    
    public static PathD ApplyTerrelPenroseEffect(
        PathD path,
        Vector2 origin,          // object rest origin (in observer frame)
        Vector2 velocity,        // velocity in observer frame
        Vector2 observerPos      // position of observer in same frame
    )
    {
        double c = Config.C;
        double vx = velocity.X, vy = velocity.Y;
        double v2 = vx * vx + vy * vy;

        if (v2 < 1e-12 || v2 >= c * c)
            return new PathD(path);

        PathD apparent = new(path.Count);

        foreach (var p in path)
        {
            double px = p.x;
            double py = p.y;

            // position at t = 0 (object's current position)
            double dx = px - observerPos.X;
            double dy = py - observerPos.Y;

            // Solve for emission time t_emit < 0
            // |(p + v * t_emit) - observer| = -c * t_emit
            // Square both sides:
            // (dx + vx*t)^2 + (dy + vy*t)^2 = c^2 * t^2
            // Expand and rearrange to quadratic in t:
            double A = v2 - c * c;
            double B = 2 * (dx * vx + dy * vy);
            double C = dx * dx + dy * dy;

            // Solve A t^2 + B t + C = 0
            double discriminant = B * B - 4 * A * C;
            if (discriminant < 0)
                discriminant = 0;

            double t_emit = (-B - Math.Sqrt(discriminant)) / (2 * A); // past light cone

            // apparent position = where the vertex was when light left
            double xApp = px + vx * t_emit;
            double yApp = py + vy * t_emit;

            apparent.Add(new PointD(xApp, yApp));
        }

        return apparent;
    }

    
    public override string ToString() =>
        $"Frame[Origin={Lightcone.Apex}, Velocity=({Velocity.X:F2}, {Velocity.Y:F2})]";
}