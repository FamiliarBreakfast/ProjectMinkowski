using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ProjectMinkowski.Relativity;
using ProjectMinkowski.Rendering;

namespace ProjectMinkowski.Entities;

public class Asteroid : WorldlineEntity
{
    public Asteroid(MinkowskiVector absolutePosition)
    {
        Origin = absolutePosition;
        Worldline = new Worldline();
    }
    
    public override void Update(float deltaTime)
    {
        Origin.T += deltaTime;
        
        Worldline.AddEvent(new WorldlineEvent(
            new MinkowskiVector(Origin.T, Origin.X, Origin.Y),
            0,
            new Vector2(0,0)
        ));
    }

    public override void Draw(SpriteBatch spriteBatch, Player player)
    { }

    public override void VertexDraw(GraphicsDevice graphicsDevice, BasicEffect effect, Player player)
    {
        //grab rendered position
        //make triangle 
        if (player.RenderedPositions.ContainsKey(this)) {
            Vector2 center = player.RenderedPositions[this];
            
            var vertices = new VertexPositionColor[] {
                new(new Vector3(center.X, center.Y+5.8f, 0), Color.Lime),
                new(new Vector3(center.X-5f, center.Y-2.9f, 0), Color.Lime),
                new(new Vector3(center.X+5f, center.Y-2.9f, 0), Color.Lime),
                new(new Vector3(center.X, center.Y+5.8f, 0), Color.Lime)
            };
            
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