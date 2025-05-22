using Clipper2Lib;
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
        Polygon = new PathD
        {
            new PointD(0,5.8),
            new PointD(-5,-2.9),
            new PointD(5,-2.9)
        };
        Radius = 6;
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

    public override void VertexDraw(GraphicsDevice graphicsDevice, BasicEffect effect, Player player, Rectangle viewport)
    {
        WorldlineEvent? evt = Worldline.GetVisibleEvent(player.Ship.Origin);
        if (evt != null)
        {
            var vertices =
                Transformations.ToVertexArray(
                    Transformations.Translate(
                        FrameOfReference.ApplyLengthContractionInFrame(
                            Transformations.Translate(
                                Transformations.Rotate(Polygon, evt.Rotation),
                                evt.Origin.X, evt.Origin.Y),
                            new Vector2((float)evt.Origin.X, (float)evt.Origin.Y), player.Ship.Frame, evt.Velocity),
                        -player.Ship.Origin.X, -player.Ship.Origin.Y),
                    Color.Lime);
            
            player.Shapes.Add(vertices);
        }
    }
}