using Clipper2Lib;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ProjectMinkowski.Relativity;
using ProjectMinkowski.Rendering;

namespace ProjectMinkowski.Entities;

public class Ship : WorldlineEntity
{
    public float Rotation; //todo: cleanup variables
    public Vector2 Velocity;

    public int Health = 100;
    
    public float ThrustPower = 10f;
    public float StrafePower = 3f;
    public float RotationSpeed = 2f;
    
    public Color Color;
    public FrameOfReference Frame;

    public Ship(MinkowskiVector absolutePosition, Player player) {
        Origin = absolutePosition;
        Worldline = new Worldline();
        Frame = new FrameOfReference(absolutePosition, Velocity);
        player.Ship = this;
        
        Color = PlayerColorGenerator.GetColorFromID(player.Id);

        Polygon = new PathD
        {
            new PointD(20,0),
            new PointD(-6.66,10),
            new PointD(-6.66,-10)
        };
    }

    public void ApplyMovement(float dt, float forwardInput, float strafeInput, float rotateInput) {
        Rotation += rotateInput * RotationSpeed * dt;

        Vector2 forward = new((float)Math.Cos(Rotation), (float)Math.Sin(Rotation));
        Vector2 right = new(-forward.Y, forward.X); // perpendicular

        // Proper acceleration in ship's rest frame
        Vector2 properAccel = 
            forward * forwardInput * ThrustPower +
            right * strafeInput * StrafePower;

        // Apply relativistic correction
        float gamma = FrameOfReference.Gamma(Velocity);
        Vector2 coordAccel = properAccel / (gamma * gamma * gamma);

        Velocity += coordAccel * dt;
    }
    
    public override void Update(float deltaTime)
    {
        Origin.X += Velocity.X * deltaTime;
        Origin.Y += Velocity.Y * deltaTime;
        Origin.T += deltaTime; 
        
        Frame.Lightcone.Apex.X = Origin.X;
        Frame.Lightcone.Apex.Y = Origin.Y;
        Frame.Velocity = Velocity;
        
        Worldline.AddEvent(new WorldlineEvent(
            new MinkowskiVector(Origin.T, Origin.X, Origin.Y),
            Rotation,
            Velocity
        ));
        //Console.WriteLine($"[Ship {GetHashCode()}] added event: Vel = {Velocity.Length():0.000} | T = {AbsolutePosition.T:0.000}");
    }

    public override void Draw(SpriteBatch spriteBatch, Player player) { }
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
                    Color);
            
            player.Shapes.Add(vertices);
        }
    }
}