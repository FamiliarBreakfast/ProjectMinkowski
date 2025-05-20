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
            
            Vector2 entityVelocity = player.VisibleVelocities[this];         // in global frame
            Vector2 observerVelocity = player.Ship.Frame.Velocity * (float)Config.C;              // also in global frame

            Vector2 relativeVelocity = entityVelocity - observerVelocity;
            
            Vector2[] points = new[]
            {
                new Vector2(center.X, center.Y+5.8f),
                new Vector2(center.X-5f, center.Y-2.9f),
                new Vector2(center.X+5f, center.Y-2.9f)
            };
            
            Vector2[] transformed = FrameOfReference.ApplyLengthContractionInFrame(points, center, player.Ship.Frame, relativeVelocity);
            
            var vertices = new VertexPositionColor[] {
                new(new Vector3(transformed[0], 0), Color.Lime),
                new(new Vector3(transformed[1], 0), Color.Lime),
                new(new Vector3(transformed[2], 0), Color.Lime),
                new(new Vector3(transformed[0], 0), Color.Lime)
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