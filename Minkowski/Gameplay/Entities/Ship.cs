using Clipper2Lib;
using FontStashSharp;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ProjectMinkowski.Relativity;
using ProjectMinkowski.Rendering;

namespace ProjectMinkowski.Entities;

public class Ship : WorldlineEntity
{
    public List<VertexPositionColor[]> Shapes = new();
    
    public int Id;
    
    public float Rotation; //todo: cleanup variables
    public Vector2 Velocity;

    public int Health = 100;
    
    public float ThrustPower = 10f;
    public float StrafePower = 3f;
    public float RotationSpeed = 2f;
    
    public Color Color;
    public FrameOfReference Frame;

    public byte Flags = 0;

    public Ship(MinkowskiVector absolutePosition, int id) {
        Origin = absolutePosition;
        Worldline = new Worldline();
        Frame = new FrameOfReference(absolutePosition, Velocity);
        Id = id;
        Color = PlayerColorGenerator.GetColorFromID(Id);

        Polygon = new PathD
        {
            new PointD(20,0),
            new PointD(-6.66,10),
            new PointD(-6.66,-10)
        };
    }
    //batch.DrawString(GameResources.DefaultFont, "Player " + ship.Id, new Vector2(10, 10), Color.White);
    public void DrawHud(SpriteBatch batch)
    {
        var speed = Velocity.Length();
        var fraction = speed / Config.C;

        string text = $"Player {Id}    Speed: {fraction:0.00}c    Health: {Health}";

        var position = new Vector2(10, 10); // top-left of player's viewport
        var font = GameResources.DefaultFont;

        batch.DrawString(font, text, position, Color.White);
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
        Frame.Lightcone.Apex.T += deltaTime; //ensure we proceed forward through time. generally considered a good thing
        Frame.Velocity = Velocity;
        
        Worldline.AddEvent(new WorldlineEvent(
            new MinkowskiVector(Origin.T, Origin.X, Origin.Y),
            Rotation,
            Velocity,
            Flags
        ));
        Flags = 0b0;
        //Console.WriteLine($"[Ship {GetHashCode()}] added event: Vel = {Velocity.Length():0.000} | T = {AbsolutePosition.T:0.000}");
    }

    public override void Draw(SpriteBatch spriteBatch, Ship ship) { }
    public override void VertexDraw(GraphicsDevice graphicsDevice, BasicEffect effect, Ship ship)
    {
        WorldlineEvent? evt = Worldline.GetVisibleEvent(ship.Origin);
        //todo bullet radar tracer
        if (evt != null)
        {
            Vector2 relativeVelocity = ship.Frame.LorentzTransformVelocity(evt.Velocity);
            var vertices =
                Transformations.ToVertexArray(
                        FrameOfReference.ApplyLengthContractionInFrame(
                            Transformations.Translate(
                                Transformations.Rotate(Polygon, evt.Rotation),
                                evt.Origin.X, evt.Origin.Y),
                            new Vector2((float)evt.Origin.X, (float)evt.Origin.Y), relativeVelocity),
                    Color);

            ship.Shapes.Add(vertices);
        }
    }
}
