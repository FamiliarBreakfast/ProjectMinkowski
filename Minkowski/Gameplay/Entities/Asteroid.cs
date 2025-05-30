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
        
        // Worldline.AddEvent(new WorldlineEvent(
        //     new MinkowskiVector(Origin.T, Origin.X, Origin.Y),
        //     0,
        //     new Vector2(0,0)
        // ));
        Worldline.AddEvent(this);
    }
    
    public override void RelativityUpdate(float deltaTime, Ship ship)
    { }

    public override void Draw(SpriteBatch spriteBatch, Ship ship)
    { }

    public override void VertexDraw(GraphicsDevice graphicsDevice, BasicEffect effect, Ship ship)
    {
        // WorldlineEvent? evt = Worldline.GetVisibleEvent(ship.Origin);
        // if (evt != null)
        // {
        //     Vector2 relativeVelocity = ship.Frame.LorentzTransformVelocity(evt.Velocity);
        //     var vertices =
        //         Transformations.ToVertexArray(
        //                 FrameOfReference.ApplyLengthContractionInFrame(
        //                     Transformations.Translate(
        //                         Transformations.Rotate(Polygon, evt.Rotation),
        //                         evt.Origin.X, evt.Origin.Y),
        //                     new Vector2((float)evt.Origin.X, (float)evt.Origin.Y), relativeVelocity),
        //             Color.Lime);
        //     
        //     ship.Shapes.Add(vertices);
        // }
        
        // Get position and velocity in a single step (with interpolation)
        // Vector2 position = Worldline.GetVisibleVariable<Vector2>(ship.Origin, "Position", interpolate: true) ?? default;
        // Vector2 velocity = Worldline.GetVisibleVariable<Vector2>(ship.Origin, "Velocity", interpolate: true) ?? default;
        // float rotation = Worldline.GetVisibleVariable<float>(ship.Origin, "Rotation", interpolate: true) ?? 0f;
        if (Worldline.HasVisibleEvent(ship.Origin))
        {
            Vector2 position = Worldline.GetVisibleVariable<Vector2>(ship.Origin, "Position", interpolate: true);
            Vector2 velocity = Worldline.GetVisibleVariable<Vector2>(ship.Origin, "Velocity", interpolate: true);
            float rotation = Worldline.GetVisibleVariable<float>(ship.Origin, "Rotation", interpolate: true);

            Vector2 relativeVelocity = ship.Frame.LorentzTransformVelocity(velocity);

            Color color = Config.DopplerEffect switch
            {
                true => ColorHelper.DopplerShift(Color.Lime, relativeVelocity),
                _ => Color.Lime
            };
            
            var vertices =
                Transformations.ToVertexArray(
                    FrameOfReference.ApplyLengthContractionInFrame(
                        Transformations.Translate(
                            Transformations.Rotate(Polygon, rotation),
                            position.X, position.Y),
                        position, relativeVelocity),
                    color);

            ship.Shapes.Add(vertices);
        }
    }
}