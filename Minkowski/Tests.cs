using Microsoft.Xna.Framework;
using System;
using ProjectMinkowski;
using ProjectMinkowski.Relativity;

public static class WorldconeRefinedIntersectionTests {
    public static void RunAll() {
        Console.WriteLine("Running refined Worldcone intersection tests...");

        float c = (float)Config.C;

        // Case 1: Forward cones, intersecting at t = 5
        {
            var a = new Worldcone(new MinkowskiVector(0, 0, 0), 1f, 1); // forward
            var b = new Worldcone(new MinkowskiVector(0, 5, 0), 1f, 1); // offset
            var arc = World.Intersects(a, b, 5f);
            PrintTest("Forward cones, intersecting", arc != null);
        }
        
        // Case 2: Forward cones, no intersection at t = 1
        {
            var a = new Worldcone(new MinkowskiVector(0, 0, 0), 1f, 1);
            var b = new Worldcone(new MinkowskiVector(50, 0, 0), 1f, 1);
            var arc = World.Intersects(a, b, 1f); // too far apart
            PrintTest("Forward cones, too far at t=1", arc == null);
        }

        // Case 3: Radar vs lightcone intersecting at t = 5
        {
            var a = new Worldcone(new MinkowskiVector(0, 0, 0), 1f, 1);   // radar ping
            var b = new Worldcone(new MinkowskiVector(10, 10, 10), 1f, -1); // observer vision
            var arc = World.Intersects(a, b, 5f);
            PrintTest("Radar vs Lightcone, intersecting", arc != null);
        }

        // Case 4: Radar vs lightcone, not yet visible at t = 3
        {
            var a = new Worldcone(new MinkowskiVector(0, 0, 0), 1f, 1);    // ping
            var b = new Worldcone(new MinkowskiVector(0, 10, 10), 1f, -1); // observer vision
            var arc = World.Intersects(a, b, 3f);
            PrintTest("Radar vs Lightcone, not yet intersecting", arc == null);
        }
        
        // Case 5: Identical cones, full overlap at t = 5
        {
            var a = new Worldcone(new MinkowskiVector(0, 0, 0), 1f, 1);
            var b = new Worldcone(new MinkowskiVector(0, 0, 0), 1f, 1);
            var arc = World.Intersects(a, b, 5f);
            bool isCircle = arc != null && Math.Abs(arc.AngleEnd - arc.AngleStart - MathHelper.TwoPi) < 0.01f;
            PrintTest("Identical cones (full circle)", isCircle);
        }

        Console.WriteLine("Refined intersection tests complete.");
    }

    private static void PrintTest(string label, bool passed) {
        Console.WriteLine($"[{(passed ? "PASS" : "FAIL")}] {label}");
    }
}
