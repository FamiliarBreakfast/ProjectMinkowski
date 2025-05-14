using Microsoft.Xna.Framework;
using ProjectMinkowski.Relativity;
using System;
using ProjectMinkowski;

public static class WorldconeAnalyticalIntersectionTests {
    public static void RunAll() {
        Console.WriteLine("Running analytical Worldcone intersection tests...");

        float c = (float)Config.C;

        // Case 1: Forward cone intersects backward cone (ping + observer)
        {
            var a = new Worldcone(new MinkowskiVector(0, 0, 0), 1f, 1);      // radar ping at t=0
            var b = new Worldcone(new MinkowskiVector(10, 0, 0), 1f, -1);  // observer at t=10
            var arc = World.Intersects(a, b);
            PrintTest("Radar vs Lightcone (Intersecting)", arc != null);
            //Console.WriteLine(arc.ToString());
            
        }
        // Case 1: Forward cone intersects backward cone (ping + observer)
        {
            var a = new Worldcone(new MinkowskiVector(0, 0, 0), 1f, 1);      // radar ping at t=0
            var b = new Worldcone(new MinkowskiVector(10, 1, 0), 1f, -1);  // observer at t=10
            var arc = World.Intersects(a, b);
            PrintTest("Radar vs Lightcone (offset arc)", arc != null);
            //Console.WriteLine(arc.ToString());
            
        }

        // Case 2: Forward cone and backward cone, no intersection (observer too early)
        {
            var a = new Worldcone(new MinkowskiVector(0, 0, 0), 1f, 1);
            var b = new Worldcone(new MinkowskiVector(0, 10, 10), 1f, -1);
            var arc = World.Intersects(a, b);
            PrintTest("Radar vs Lightcone (No causal overlap)", arc == null);
        }

        // Case 3: Full overlap at same apex
        {
            var a = new Worldcone(new MinkowskiVector(0, 0, 0), 1f, 1);
            var b = new Worldcone(new MinkowskiVector(0, 0, 0), 1f, -1);
            var arc = World.Intersects(a, b);
            bool isCircle = arc != null && Math.Abs(arc.AngleEnd - arc.AngleStart - MathHelper.TwoPi) < 0.01f;
            PrintTest("Identical cones (full circle)", isCircle);
        }

        // Case 4: Too far apart in space
        {
            var a = new Worldcone(new MinkowskiVector(0, 0, 0), 1f, 1);
            var b = new Worldcone(new MinkowskiVector(20, 10000, 10000), 1f, -1);
            var arc = World.Intersects(a, b);
            PrintTest("Cones too far apart", arc == null);
            //Console.WriteLine(arc.ToString());
        }

        // Case 5: Just tangent
        {
            float separation = c * 5; // Ping radius = 5c, observer reaches back 5c
            var a = new Worldcone(new MinkowskiVector(0, 0, 0), 1f, 1);
            var b = new Worldcone(new MinkowskiVector(separation, 0, 10), 1f, -1);
            var arc = World.Intersects(a, b);
            PrintTest("Cones just tangent", arc != null);
            //Console.WriteLine(arc.ToString());
        }

        Console.WriteLine("All analytical intersection tests complete.");
    }

    private static void PrintTest(string label, bool passed) {
        Console.WriteLine($"[{(passed ? "PASS" : "FAIL")}] {label}");
    }
}
