using System.Diagnostics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ProjectMinkowski.Relativity;
using ProjectMinkowski.Rendering;

namespace ProjectMinkowski.Entities;

public class Radar : WorldconeEntity
{
    public Radar(MinkowskiVector origin)
    {
        Worldcone = new Worldcone(origin, 0.1f, 1);
    }

    public override void Update(float deltaTime)
    {
        Console.WriteLine(Worldcone.Apex);
    }

    public override void Draw(SpriteBatch spriteBatch, Player player) { }
    
    public static VertexPositionColor[] ConvertPointsToTriangles(VertexPositionColor[] points, float size)
    {
        List<VertexPositionColor> triangleVertices = new();

        foreach (var point in points)
        {
            var pos = point.Position;
            var color = point.Color;

            // Form an upright triangle centered on the point
            triangleVertices.Add(new VertexPositionColor(pos + new Vector3(-size, size, 0), color));  // left top
            triangleVertices.Add(new VertexPositionColor(pos + new Vector3(size, size, 0), color));   // right top
            triangleVertices.Add(new VertexPositionColor(pos + new Vector3(0, -size, 0), color));     // bottom center
        }

        return triangleVertices.ToArray();
    }

    public override void VertexDraw(GraphicsDevice graphicsDevice, BasicEffect effect, Player player)
    {
        if (player.TransformedArcs.ContainsKey(this)) {
            Arc arc = player.TransformedArcs[this];
            var vertices = arc.ToVertices();
            
            //     Console.WriteLine(p);
            foreach (EffectPass pass in effect.CurrentTechnique.Passes)
            {
                pass.Apply();
                graphicsDevice.DrawUserPrimitives(
                    PrimitiveType.LineStrip, // or LineList
                    vertices,
                    0,
                    vertices.Length - 1      // LineStrip: count is N-1 segments
                );
            }
        }
    }
}