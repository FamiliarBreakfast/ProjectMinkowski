using Clipper2Lib;
using Microsoft.Xna.Framework;
using ProjectMinkowski.Rendering;

namespace ProjectMinkowski.Relativity;

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
    
    public static PathD ApplyLengthContractionInFrame(
        PathD path,
        Vector2 centerV,
        Vector2 relativeVelocity
    )
    {
        // Step 1: Transform object's global velocity to observer frame
        //Vector2 relativeVelocity = objectVelocityGlobal - observerFrame.Velocity;
        

        float speedSq = relativeVelocity.LengthSquared();
        float c = (float)Config.C;

        if (speedSq < 1e-10 || speedSq >= c * c)
            return new PathD(path); // Just copy the original path

        float gamma = 1f / MathF.Sqrt(1f - speedSq / (c * c));
        float contraction = 1f / gamma;

        Vector2 direction = Vector2.Normalize(relativeVelocity);

        PathD contracted = new PathD(path.Count);

        for (int i = 0; i < path.Count; i++)
        {
            // Convert point to Vector2 for math
            Vector2 v = new Vector2((float)path[i].x, (float)path[i].y);

            Vector2 relative = v - centerV;

            // Project onto direction of motion
            float parallelMag = Vector2.Dot(relative, direction);
            Vector2 parallel = direction * parallelMag;
            Vector2 orthogonal = relative - parallel;

            // Contract the parallel component
            Vector2 contractedRelative = orthogonal + parallel * contraction;
            Vector2 contractedV = centerV + contractedRelative;

            contracted.Add(new PointD(contractedV.X, contractedV.Y));
        }

        return contracted;
    }


    
    public override string ToString() =>
        $"Frame[Origin={Lightcone.Apex}, Velocity=({Velocity.X:F2}, {Velocity.Y:F2})]";
}